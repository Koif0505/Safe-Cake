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
    public float comboThreshold = 8f; // Dưới 8s mới x2

    [Header("Smart Lose Logic")]
    public float maxJumpReach = 2.2f;
    public float groundLevelY = 1.5f;

    [Header("Checkpoint & Continue")]
    private Vector3 lastCheckpoint;
    public int continueCost = 10;
    public GameObject continueButton; // Kéo nút Continue vào đây trong Inspector

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

    [Header("Effects & Questions")]
    public GameObject floatingScorePrefab;
    public Transform uiOverlayParent;
    public AudioClip collectSound;
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

        UpdateUI();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (IsGameEnded) return;
        totalGameTimer += Time.deltaTime;
        CheckSmartLoseCondition();
        UpdateDistanceUI();
        UpdateTimerUI();
    }

    // ================= LOGIC THUA THÔNG MINH (FIXED) =================
    void CheckSmartLoseCondition()
    {
        if (playerTransform == null) return;
        float pY = playerTransform.position.y;
        Transform target = GetCurrentCakeTarget();

        // LUẬT NGHIÊM NGẶT: Cake 1, 2, 3 (Phải ở trên cao)
        if (nextCakeIndex <= 3)
        {
            // 1. Chạm đất là Game Over
            if (pY < groundLevelY)
            {
                LoseGame("Rơi xuống phố! Cake " + nextCakeIndex + " yêu cầu bạn phải ở trên cao.");
                return;
            }
            // 2. Kẹt ở bậc trung gian không leo lên được
            if (target != null && playerController.isGrounded)
            {
                if (pY < (target.position.y - maxJumpReach)) LoseGame("Kẹt rồi! Không thể leo lại Cake " + nextCakeIndex);
            }
        }
        // LUẬT THOẢI MÁI: Cake 4, 5, 6 (Được phép nhảy xuống tầng dưới)
        else
        {
            if (pY < -15f) LoseGame("Rơi khỏi thành phố!");
        }
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (!CanCollectCake(index)) return;

        int finalScore = (collectedCakes > 0 && Time.time - lastCakeTime <= comboThreshold) ? scoreValue * 2 : scoreValue;
        score += finalScore;
        collectedCakes++;
        lastCakeTime = Time.time;
        lastCheckpoint = playerTransform.position;

        SpawnFloatingScore(finalScore, finalScore > scoreValue);
        if (collectSound != null) AudioSource.PlayClipAtPoint(collectSound, playerTransform.position);

        nextCakeIndex++;
        TriggerEvents();
        UpdateUI();
        if (collectedCakes >= totalCakes) WinGame();
    }

    // ================= RE-SPAWN & CONTINUE =================
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

    public void Respawn(int penalty)
    {
        if (IsGameEnded) return;
        score = Mathf.Max(0, score - penalty);
        if (playerController != null) playerController.enabled = false;
        playerTransform.position = lastCheckpoint;
        if (playerController != null) playerController.enabled = true;
        StartCoroutine(CameraShake(0.2f, 0.4f));
        UpdateUI();
    }

    // ================= WIN / LOSE =================
    public void WinGame()
    {
        IsGameEnded = true;
        if (dayNight != null) dayNight.SetDayMode();
        winPanel.SetActive(true);
        StartCoroutine(FadeInWinUI());
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void LoseGame(string reason = "Bạn đã thất bại!")
    {
        if (IsGameEnded) return;
        IsGameEnded = true;
        ToggleBlink(false);
        StartCoroutine(CameraShake(0.3f, 0.5f));
        if (loseReasonText != null) loseReasonText.text = reason + "\nScore: " + score;
        losePanel.SetActive(true);

        // Ẩn nút Continue nếu < 10 điểm
        if (continueButton != null) continueButton.SetActive(score >= continueCost);

        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    // ================= HELPER & UI =================
    void TriggerEvents()
    {
        switch (collectedCakes)
        {
            case 3: SetHint("Cake 4: Hãy nhảy xuống tầng dưới, tìm đường hẹp!"); break; // Nhắc nhở nhảy xuống
            case 4: if (dayNight != null) dayNight.SetNightMode(); ToggleBlink(true); break;
            case 5: StartCoroutine(RiddleEvent()); break;
        }
    }

    IEnumerator RiddleEvent()
    {
        Time.timeScale = 0; riddlePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        float timer = 10f;
        while (timer > 0)
        {
            timer -= Time.unscaledDeltaTime;
            riddleTimerText.text = "Thời gian: " + Mathf.Ceil(timer) + "s";
            yield return null;
        }
        if (!cake6HintUnlocked) LoseGame("Hết thời gian giải đố!");
    }

    public void OnClickCorrectAnswer() { cake6HintUnlocked = true; Time.timeScale = 1f; riddlePanel.SetActive(false); Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    public void OnClickWrongAnswer() { score -= 10; SetHint("Sai rồi! Trừ 10 điểm."); UpdateUI(); }
    public bool CanCollectCake(int index) => !IsGameEnded && index == nextCakeIndex;
    public void ShowWrongOrderHint(int index) { if (!IsGameEnded) SetHint("Ăn Cake " + nextCakeIndex + " trước đã!"); }
    void UpdateUI() { if (scoreText) scoreText.text = "Score: " + score; if (cakeText) cakeText.text = "Cakes: " + collectedCakes + "/6"; }
    void UpdateTimerUI() { if (timerText) timerText.text = "Time: " + totalGameTimer.ToString("F1") + "s"; }
    void UpdateDistanceUI()
    {
        Transform t = GetCurrentCakeTarget();
        if (t && distanceText) distanceText.text = "Next: " + Vector3.Distance(playerTransform.position, t.position).ToString("F1") + "m";
    }
    Transform GetCurrentCakeTarget() { int i = nextCakeIndex - 1; return (i >= 0 && i < cakeTargets.Length) ? cakeTargets[i] : null; }
    void SpawnFloatingScore(int v, bool c) { if (!floatingScorePrefab) return; GameObject go = Instantiate(floatingScorePrefab, uiOverlayParent); go.GetComponent<TextMeshProUGUI>().text = "+" + v + (c ? " COMBO!" : ""); if (c) go.GetComponent<TextMeshProUGUI>().color = Color.yellow; }
    public void SetHint(string m) { if (hintText) hintText.text = m; }
    public void SubtractScore(int a) { score = Mathf.Max(0, score - a); UpdateUI(); }
    public void RestartScene() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public IEnumerator CameraShake(float d, float m) { Vector3 o = cameraTransform.localPosition; float e = 0; while (e < d) { cameraTransform.localPosition = o + (Vector3)Random.insideUnitCircle * m; e += Time.deltaTime; yield return null; } cameraTransform.localPosition = o; }
    public void ToggleBlink(bool s) { if (s && !isBlinking) StartCoroutine(BlinkDistanceText()); else isBlinking = false; }
    IEnumerator BlinkDistanceText() { isBlinking = true; while (isBlinking) { distanceText.alpha = 0.2f; yield return new WaitForSeconds(0.5f); distanceText.alpha = 1.0f; yield return new WaitForSeconds(0.5f); } distanceText.alpha = 1.0f; }
    IEnumerator FadeInWinUI() { float t = 0; while (t < 1f) { t += Time.deltaTime * 0.5f; if (winPanelGroup) winPanelGroup.alpha = t; yield return null; } }
}