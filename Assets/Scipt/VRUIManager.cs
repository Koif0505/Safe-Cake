using UnityEngine;
using TMPro;
using System.Collections;

public class VRUIManager : MonoBehaviour
{
    public static VRUIManager Instance;

    [Header("Panels (Require CanvasGroup)")]
    public CanvasGroup startPanel;
    public CanvasGroup riddlePanel;
    public CanvasGroup resultPanel;
    public CanvasGroup hudPanel;

    [Header("Riddle UI")]
    public TextMeshProUGUI riddleTimerText;

    [Header("Result UI")]
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public GameObject continueButton;

    public float fadeDuration = 0.5f;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Khởi tạo trạng thái ban đầu
        ShowPanel(startPanel);
        HidePanel(riddlePanel);
        HidePanel(resultPanel);
        HidePanel(hudPanel);
    }

    // --- CÁC HÀM GỌI TỪ GAMEMANAGER ---

    public void ShowStartMenu() => ShowPanel(startPanel);
    public void HideStartMenu() { HidePanel(startPanel); ShowPanel(hudPanel); }

    public void ShowRiddle()
    {
        HidePanel(hudPanel);
        ShowPanel(riddlePanel);
    }

    public void HideRiddle()
    {
        HidePanel(riddlePanel);
        ShowPanel(hudPanel);
    }

    public void ShowResult(bool isWin, int score, string reason, bool canContinue)
    {
        HidePanel(hudPanel);
        HidePanel(riddlePanel);

        resultTitleText.text = isWin ? "<color=green>YOU WIN!</color>" : "<color=red>YOU LOSE!</color>\n<size=50%>" + reason + "</size>";
        resultScoreText.text = "Final Score: " + score;
        continueButton.SetActive(canContinue);

        ShowPanel(resultPanel);
    }

    // --- LOGIC FADE IN/OUT (Tạo cảm giác mượt mà cho VR) ---
    private void ShowPanel(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(panel, panel.alpha, 1f));
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }

    private void HidePanel(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        StartCoroutine(FadeCanvasGroup(panel, panel.alpha, 0f, true));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, bool disableAfter = false)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Dùng unscaled để hoạt động cả khi Time.timeScale = 0
            cg.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = end;
        if (disableAfter) cg.gameObject.SetActive(false);
    }
}