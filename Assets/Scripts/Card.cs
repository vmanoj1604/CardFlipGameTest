using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    private RectTransform rt;

    public Sprite hiddenIconSprite;
    public Sprite iconSprite;

    public bool isSelected;
    public bool isMatched;
    public bool IsAnimating { get; private set; }

    public CardController controller;

    [SerializeField] private float flipDuration = 0.25f;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        iconImage.sprite = hiddenIconSprite;
        rt.localScale = Vector3.one;
        isSelected = false;
        isMatched = false;

        if (!controller) controller = FindFirstObjectByType<CardController>();
    }

    public void OnCardClick()
    {
        if (IsAnimating || isMatched || isSelected) return;
        controller.SetSelected(this);
    }

    public void SetIconSprite(Sprite sp) => iconSprite = sp;

    public IEnumerator FlipToReveal()
    {
        if (isSelected || IsAnimating) yield break;
        controller?.PlayFlipSfx();
        yield return StartCoroutine(FlipRoutine(true));
        isSelected = true;
    }

    public IEnumerator FlipToHide()
    {
        if (!isSelected || IsAnimating) yield break;
        controller?.PlayFlipSfx();
        yield return StartCoroutine(FlipRoutine(false));
        isSelected = false;
    }

    IEnumerator FlipRoutine(bool showFront)
    {
        IsAnimating = true;
        float half = flipDuration * 0.5f;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            float s = Mathf.Lerp(1f, 0f, t / half);
            rt.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }
        rt.localScale = new Vector3(0f, 1f, 1f);

        iconImage.sprite = showFront ? iconSprite : hiddenIconSprite;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            float s = Mathf.Lerp(0f, 1f, t / half);
            rt.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        IsAnimating = false;
    }
}
