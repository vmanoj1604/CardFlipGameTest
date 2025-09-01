using System.Collections;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Game")]
    [SerializeField] private CardController cardController;

    void Start()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (gamePanel) gamePanel.SetActive(false);
    }

    public void Start_2x2() => StartCoroutine(StartGameRoutine(2, 2));
    public void Start_2x4() => StartCoroutine(StartGameRoutine(2, 4));
    public void Start_4x4() => StartCoroutine(StartGameRoutine(4, 4));

    IEnumerator StartGameRoutine(int rows, int cols)
    {
        if (!cardController) yield break;
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        yield return null; 
        cardController.BuildBoard(rows, cols);
    }

    public void BackToMenu()
    {
        if (cardController) cardController.ClearBoard();
        if (gamePanel) gamePanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    public void QuitApp()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
