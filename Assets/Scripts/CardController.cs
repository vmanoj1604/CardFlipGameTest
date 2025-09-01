using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class CardController : MonoBehaviour
{
    [Header("Prefabs & Layout")]
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Transform gridTransform;
    [SerializeField] private GridLayoutGroup grid;

    [Header("Sprites (Fronts)")]
    [SerializeField] private Sprite[] sprites;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI matchesText;
    [SerializeField] private TextMeshProUGUI turnsText;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private float winShowDelay = 0.25f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip flipSfx;
    [SerializeField] private AudioClip matchSfx;
    [SerializeField] private AudioClip wrongSfx;
    [SerializeField] private AudioClip victorySfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public Vector2 flipPitchRange = new Vector2(0.97f, 1.03f);

    [System.Serializable]
    public struct Padding { public int left, right, top, bottom; }
    [Header("Per-Layout Spacing & Padding")]
    public Vector2 spacing2x2 = new Vector2(24, 24);
    public Padding padding2x2 = new Padding { left = 24, right = 24, top = 24, bottom = 24 };
    public Vector2 spacing2x4 = new Vector2(18, 18);
    public Padding padding2x4 = new Padding { left = 18, right = 18, top = 18, bottom = 18 };
    public Vector2 spacing4x4 = new Vector2(12, 12);
    public Padding padding4x4 = new Padding { left = 12, right = 12, top = 12, bottom = 12 };
    public Vector2 spacingDefault = new Vector2(12, 12);
    public Padding paddingDefault = new Padding { left = 12, right = 12, top = 12, bottom = 12 };

    private readonly List<Card> pending = new List<Card>();
    private bool processingPairs;
    private int matchesCount;
    private int turnsCount;
    private int totalPairsNeeded;
    private bool gameOver;
    private int lastRows, lastCols;

    void Awake()
    {
        if (!grid && gridTransform) grid = gridTransform.GetComponent<GridLayoutGroup>();
        if (!sfxSource)
        {
            sfxSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    public void BuildBoard(int rows, int cols)
    {
        if (!grid || !gridTransform || !cardPrefab || sprites == null) return;

        ClearBoard();

        rows = Mathf.Max(1, rows);
        cols = Mathf.Max(1, cols);
        lastRows = rows; lastCols = cols;

        int totalCards = rows * cols;
        totalPairsNeeded = totalCards / 2;

        ApplyLayoutStyle(rows, cols);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        RecalculateCellSize(rows, cols);

        var chosenFronts = new List<Sprite>(totalPairsNeeded);
        for (int i = 0; i < totalPairsNeeded; i++)
            chosenFronts.Add(sprites[i < sprites.Length ? i : i % sprites.Length]);

        var spritePairs = new List<Sprite>(totalCards);
        foreach (var sp in chosenFronts) { spritePairs.Add(sp); spritePairs.Add(sp); }
        Shuffle(spritePairs);

        for (int i = 0; i < spritePairs.Count; i++)
        {
            var card = Instantiate(cardPrefab, gridTransform);
            card.SetIconSprite(spritePairs[i]);
            card.controller = this;
        }

        matchesCount = 0;
        turnsCount = 0;
        gameOver = false;
        if (winText) winText.gameObject.SetActive(false);
        UpdateUI();
    }

    public void ClearBoard()
    {
        if (gridTransform)
            for (int i = gridTransform.childCount - 1; i >= 0; i--)
                Destroy(gridTransform.GetChild(i).gameObject);

        pending.Clear();
        processingPairs = false;
        matchesCount = 0;
        turnsCount = 0;
        gameOver = false;
        if (winText) winText.gameObject.SetActive(false);
        UpdateUI();
    }

    void ApplyLayoutStyle(int rows, int cols)
    {
        if (rows == 2 && cols == 2)
        {
            grid.spacing = spacing2x2;
            grid.padding = new RectOffset(padding2x2.left, padding2x2.right, padding2x2.top, padding2x2.bottom);
        }
        else if (rows == 2 && cols == 4)
        {
            grid.spacing = spacing2x4;
            grid.padding = new RectOffset(padding2x4.left, padding2x4.right, padding2x4.top, padding2x4.bottom);
        }
        else if (rows == 4 && cols == 4)
        {
            grid.spacing = spacing4x4;
            grid.padding = new RectOffset(padding4x4.left, padding4x4.right, padding4x4.top, padding4x4.bottom);
        }
        else
        {
            grid.spacing = spacingDefault;
            grid.padding = new RectOffset(paddingDefault.left, paddingDefault.right, paddingDefault.top, paddingDefault.bottom);
        }
    }

    void RecalculateCellSize(int rows, int cols)
    {
        var rt = gridTransform as RectTransform;
        if (!rt) return;

        Canvas.ForceUpdateCanvases();

        var rect = rt.rect;
        float innerW = Mathf.Max(0f, rect.width - (grid.padding.left + grid.padding.right));
        float innerH = Mathf.Max(0f, rect.height - (grid.padding.top + grid.padding.bottom));
        float cellW = (innerW - grid.spacing.x * (cols - 1)) / cols;
        float cellH = (innerH - grid.spacing.y * (rows - 1)) / rows;
        float size = Mathf.Floor(Mathf.Min(cellW, cellH));
        grid.cellSize = new Vector2(size, size);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    public void SetSelected(Card card)
    {
        if (gameOver) return;
        if (card.isSelected || card.IsAnimating || card.isMatched) return;
        StartCoroutine(RevealAndEnqueue(card));
    }

    IEnumerator RevealAndEnqueue(Card card)
    {
        yield return card.FlipToReveal();
        pending.Add(card);
        if (!processingPairs && pending.Count >= 2)
            StartCoroutine(ProcessPairs());
    }

    IEnumerator ProcessPairs()
    {
        processingPairs = true;
        while (pending.Count >= 2)
        {
            var a = pending[0];
            var b = pending[1];

            yield return new WaitForSeconds(0.15f);

            turnsCount++;
            UpdateUI();

            if (a.iconSprite == b.iconSprite)
            {
                a.isMatched = true;
                b.isMatched = true;
                PlayMatchSfx();

                matchesCount++;
                UpdateUI();

                StartCoroutine(Pop(a));
                StartCoroutine(Pop(b));

                if (matchesCount >= totalPairsNeeded)
                {
                    gameOver = true;
                    StartCoroutine(ShowWin());
                }
            }
            else
            {
                PlayWrongSfx();
                yield return a.FlipToHide();
                yield return b.FlipToHide();
            }

            pending.RemoveAt(0);
            pending.RemoveAt(0);
        }
        processingPairs = false;
    }

    IEnumerator Pop(Card c)
    {
        var rt = c.GetComponent<RectTransform>();
        Vector3 a = Vector3.one, b = a * 1.08f;
        float d = 0.12f;

        for (float t = 0f; t < d; t += Time.deltaTime)
        {
            rt.localScale = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t / d));
            yield return null;
        }
        for (float t = 0f; t < d; t += Time.deltaTime)
        {
            rt.localScale = Vector3.Lerp(b, a, Mathf.SmoothStep(0, 1, t / d));
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator ShowWin()
    {
        yield return new WaitForSeconds(winShowDelay);


        PlayVictorySfx();


        if (winText)
        {
            winText.text = $"Level Completed\n({lastRows}×{lastCols})";
            winText.gameObject.SetActive(true);
        }
    }

    void UpdateUI()
    {
        if (matchesText) matchesText.text = matchesCount.ToString();
        if (turnsText) turnsText.text = turnsCount.ToString();
    }

    public void PlayFlipSfx()
    {
        if (!sfxSource || !flipSfx) return;
        sfxSource.pitch = Random.Range(flipPitchRange.x, flipPitchRange.y);
        sfxSource.PlayOneShot(flipSfx, sfxVolume);
        sfxSource.pitch = 1f;
    }
    public void PlayMatchSfx()
    {
        if (!sfxSource || !matchSfx) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(matchSfx, sfxVolume);
    }
    public void PlayWrongSfx()
    {
        if (!sfxSource || !wrongSfx) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(wrongSfx, sfxVolume);
    }
    public void PlayVictorySfx()
    {
        if (!sfxSource || !victorySfx) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(victorySfx, sfxVolume);
    }
}
