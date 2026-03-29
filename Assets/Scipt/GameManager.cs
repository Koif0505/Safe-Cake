using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private bool isBlinking = false;

    [Header("Player & Camera")]
    public Transform playerTransform;
    public Transform cameraTransform;
    private CharacterController playerController;

    [Header("Game State")]
    public int score = 0;
    public int collectedCakes = 0;
    public int totalCakes = 6;
    public int nextCakeIndex = 1;
    public bool IsGameEnded { get; private set; } = false;

    [Header("Speedrun & Combo")]
    public float totalGameTimer = 0f;
    private float lastCakeTime;
    public float comboThreshold = 8f;

    [Header("Smart Lose Logic")]
    public float groundLevelY = 1.5f; // Chỉ cần quan tâm cao độ mặt đất

    [Header("Checkpoint & Continue")]
    private Vector3 lastCheckpoint;
    public int continueCost = 10;
    public GameObject continueButton;

    [Header("Targets")]
    public Transform[] cakeTargets;
    public DayNight dayNight;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI cakeText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timerText;

    [Header("Panels")]
    public GameObject riddlePanel;
    public TextMeshProUGUI riddleTimerText;
    public GameObject winPanel;
    public CanvasGroup winPanelGroup;
    public GameObject losePanel;
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI loseReasonText;

    [Header("Effects & Quest UI")]
    public GameObject floatingScorePrefab;
    public Transform uiOverlayParent;
    public AudioClip collectSound;
    public TextMeshProUGUI questNotificationText;
    public bool cake6HintUnlocked = false;

    void Awake() { Instance = this; }

    void Start()
    {
        playerController = playerTransform.GetComponent<CharacterController>();
        lastCheckpoint = playerTransform.position;
        lastCakeTime = Time.time - 10f;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (riddlePanel != null) riddlePanel.SetActive(false);
        if (dayNight != null) dayNight.SetDayMode();
        if (questNotificationText != null) questNotificationText.gameObject.SetActive(false);

        UpdateUI();
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }

    void Update()
    {
        if (IsGameEnded) return;
        totalGameTimer += Time.deltaTime;
        CheckSmartLoseCondition();
        UpdateDistanceUI();
        UpdateTimerUI();
    }

    // ================= LOGIC THUA (ĐÃ CẬP NHẬT) =================
    void CheckSmartLoseCondition()
    {
        if (playerTransform == null) return;
        float pY = playerTransform.position.y;

        // LUẬT: Từ Cake 1 đến Cake 4 (nextCakeIndex từ 1 đến 4)
        if (nextCakeIndex <= 4)
        {
            // Chỉ thua nếu rớt xuống hẳn mặt phố (Ground)
            if (pY < groundLevelY)
            {
                LoseGame("Rơi xuống phố! Hãy cố gắng ở trên các tòa nhà.");
                return;
            }
            // Đã xóa bỏ đoạn code kiểm tra maxJumpReach (Kẹt tòa nhà thấp) 
            // để bạn có thể nhảy xuống các tòa nhà bên dưới tìm đường thoải mái.
        }
        // LUẬT: Từ Cake 5 trở đi
        else
        {
            if (pY < -15f) LoseGame("Rơi khỏi thành phố!");
        }
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (!CanCollectCake(index)) return;

        bool isCombo = (collectedCakes > 0 && (Time.time - lastCakeTime <= comboThreshold));
        int finalScore = isCombo ? scoreValue * 2 : scoreValue;

        score += finalScore;
        collectedCakes++;
        lastCakeTime = Time.time;
        lastCheckpoint = playerTransform.position;

        SpawnFloatingScore(finalScore, isCombo);

        if (collectSound != null) AudioSource.PlayClipAtPoint(collectSound, playerTransform.position);
        nextCakeIndex++;
        TriggerQuestEvents();
        UpdateUI();
        if (collectedCakes >= totalCakes) WinGame();
    }

    void SpawnFloatingScore(int v, bool c)
    {
        if (floatingScorePrefab == null || uiOverlayParent == null) return;
        GameObject go = Instantiate(floatingScorePrefab, uiOverlayParent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = "+" + v + (c ? " COMBO!" : "");
            if (c) txt.color = Color.yellow;
        }
    }

    public void ShowWrongOrderHint(int index) { if (!IsGameEnded) SetHint("Hãy ăn Cake " + nextCakeIndex + " trước!"); }

    void TriggerQuestEvents()
    {
        string questMsg = "";
        switch (collectedCakes)
        {
            case 1: questMsg = "CHẠY RA TẤM VÁN ĐỂ ĂN CAKE 2"; break;
            case 2: questMsg = "NHẢY XUỐNG MÁI DƯỚI, NHÌN BÊN PHẢI!"; break;
            case 3: questMsg = "NHẢY XUỐNG TOÀ NHÀ PHÍA DƯỚI, TÌM ĐƯỜNG SANG ĂN CAKE 4!"; break;
            case 4: questMsg = "HÃY NHẢY XUỐNG ĐẠI LỘ TÌM CAKE 5!"; if (dayNight != null) dayNight.SetNightMode(); break;
            case 5: StartCoroutine(RiddleEvent()); break;
        }
        if (!string.IsNullOrEmpty(questMsg)) StartCoroutine(ShowQuestNotification(questMsg));
    }

    IEnumerator ShowQuestNotification(string message)
    {
        if (questNotificationText == null) yield break;
        questNotificationText.text = message;
        questNotificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.2f); questNotificationText.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.2f); questNotificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f); questNotificationText.gameObject.SetActive(false);
    }

    public void OnClickContinue()
    {
        if (score >= continueCost)
        {
            score -= continueCost;
            IsGameEnded = false;
            losePanel.SetActive(false);
            if (playerController != null) playerController.enabled = false;
            playerTransform.position = lastCheckpoint + Vector3.up * 10f;
            if (playerController != null) playerController.enabled = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            UpdateUI();
        }
    }

    public void WinGame()
    {
        IsGameEnded = true;
        if (dayNight != null) dayNight.SetDayMode();
        winPanel.SetActive(true);
        StartCoroutine(FadeInWinUI());
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void LoseGame(string reason = "Thất bại!")
    {
        if (IsGameEnded) return;
        IsGameEnded = true;
        StartCoroutine(CameraShake(0.3f, 0.5f));
        if (loseReasonText != null) loseReasonText.text = reason + "\nScore: " + score;
        losePanel.SetActive(true);
        if (continueButton != null) continueButton.SetActive(score >= continueCost);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public bool CanCollectCake(int index) => !IsGameEnded && index == nextCakeIndex;
    void UpdateUI() { if (scoreText) scoreText.text = "Score: " + score; if (cakeText) cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes; }
    void UpdateTimerUI() { if (timerText) timerText.text = "Time: " + totalGameTimer.ToString("F1") + "s"; }
    void UpdateDistanceUI() { Transform t = GetCurrentCakeTarget(); if (t && distanceText) distanceText.text = "Next: " + Vector3.Distance(playerTransform.position, t.position).ToString("F1") + "m"; }
    Transform GetCurrentCakeTarget() { int i = nextCakeIndex - 1; return (i >= 0 && i < cakeTargets.Length) ? cakeTargets[i] : null; }
    public void SetHint(string m) { if (hintText) hintText.text = m; }
    public void SubtractScore(int a) { score = Mathf.Max(0, score - a); UpdateUI(); }
    public void RestartScene() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public IEnumerator CameraShake(float d, float m) { Vector3 o = cameraTransform.localPosition; float e = 0; while (e < d) { cameraTransform.localPosition = o + (Vector3)Random.insideUnitCircle * m; e += Time.deltaTime; yield return null; } cameraTransform.localPosition = o; }
    public void ToggleBlink(bool s) { if (s && !isBlinking) StartCoroutine(BlinkDistanceText()); else isBlinking = false; }
    IEnumerator BlinkDistanceText() { isBlinking = true; while (isBlinking) { distanceText.alpha = 0.2f; yield return new WaitForSeconds(0.5f); distanceText.alpha = 1.0f; yield return new WaitForSeconds(0.5f); } distanceText.alpha = 1.0f; }
    IEnumerator FadeInWinUI() { float t = 0; while (t < 1f) { t += Time.deltaTime * 0.5f; if (winPanelGroup) winPanelGroup.alpha = t; yield return null; } }
    IEnumerator RiddleEvent() { Time.timeScale = 0; riddlePanel.SetActive(true); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; float timer = 10f; while (timer > 0) { timer -= Time.unscaledDeltaTime; riddleTimerText.text = "Thời gian: " + Mathf.Ceil(timer) + "s"; yield return null; } if (!cake6HintUnlocked) LoseGame("Hết thời gian giải đố!"); }
    public void OnClickCorrectAnswer() { cake6HintUnlocked = true; Time.timeScale = 1f; riddlePanel.SetActive(false); Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    public void OnClickWrongAnswer() { SubtractScore(10); SetHint("Sai rồi! Trừ 10 điểm."); }
}