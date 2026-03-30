using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private bool isBlinking = false;
    private bool inputHandled = false; // Ngăn việc bấm giữ phím bị nhảy menu liên tục

    [Header("Player & Camera")]
    public Transform playerTransform;
    public Transform cameraTransform;
    private CharacterController playerController;

    [Header("Game State")]
    public bool isGameStarted = false;
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
    public float groundLevelY = 1.5f;

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

    [Header("Panels & Start Screen")]
    public GameObject startPanel;
    public GameObject howToPlayPanel;
    public GameObject elevatorDoors;
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

        isGameStarted = false;
        if (startPanel != null) startPanel.SetActive(true);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (riddlePanel != null) riddlePanel.SetActive(false);
        if (questNotificationText != null) questNotificationText.gameObject.SetActive(false);
        if (elevatorDoors != null) elevatorDoors.SetActive(true);
        if (dayNight != null) dayNight.SetDayMode();

        UpdateUI();
        // Cho phép hiện chuột lúc đầu để dự phòng
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Nếu chưa bấm Start thì chỉ xử lý Menu
        if (!isGameStarted)
        {
            HandleMenuInput();
            return;
        }

        // Logic game khi đang chơi
        if (IsGameEnded) return;

        totalGameTimer += Time.deltaTime;
        CheckSmartLoseCondition();
        UpdateDistanceUI();
        UpdateTimerUI();
    }

    // --- HÀM XỬ LÝ MENU GỘP CHUNG (FIX LỖI) ---
    void HandleMenuInput()
    {
        // 1. Lấy dữ liệu từ Phím (A/D/S/Mũi tên)
        bool leftK = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool rightK = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        bool downK = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        // 2. Lấy dữ liệu từ Joystick (Controller VR)
        float hAxis = Input.GetAxisRaw("Horizontal");
        float vAxis = Input.GetAxisRaw("Vertical");

        if (!inputHandled)
        {
            // Bấm TRÁI (Phím hoặc Gạt cần trái) -> How To Play
            if (leftK || hAxis < -0.5f)
            {
                OnClickHowToPlay();
                inputHandled = true;
            }
            // Bấm PHẢI (Phím hoặc Gạt cần phải) -> Start Game
            else if (rightK || hAxis > 0.5f)
            {
                OnClickStart();
                inputHandled = true;
            }
            // Bấm XUỐNG -> Thoát hướng dẫn
            else if (downK || (vAxis < -0.5f && howToPlayPanel.activeSelf))
            {
                OnClickCloseHowToPlay();
                inputHandled = true;
            }
        }

        // Reset lại cờ khi người chơi thả phím/thả cần Joystick ra
        if (Mathf.Abs(hAxis) < 0.1f && Mathf.Abs(vAxis) < 0.1f && !Input.anyKey)
        {
            inputHandled = false;
        }
    }

    public void OnClickStart()
    {
        isGameStarted = true;
        if (startPanel != null) startPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);

        // ElevatorDoors sẽ được DoorController xử lý trượt, nên ở đây mình không SetActive(false) nữa
        // nhường quyền cho DoorController trượt cửa sau 1.5s

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(ShowQuestNotification("GAME BẮT ĐẦU! HÃY ĂN CAKE 1"));
    }

    public void OnClickHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
        if (startPanel != null) startPanel.SetActive(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OnClickCloseHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (startPanel != null) startPanel.SetActive(true);
    }

    // ================= PHẦN LOGIC CÒN LẠI =================
    void CheckSmartLoseCondition()
    {
        if (playerTransform == null) return;
        float pY = playerTransform.position.y;
        if (nextCakeIndex <= 4) { if (pY < groundLevelY) LoseGame("Rơi xuống phố!"); }
        else { if (pY < -15f) LoseGame("Rơi khỏi thành phố!"); }
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (!CanCollectCake(index)) return;
        bool isCombo = (collectedCakes > 0 && (Time.time - lastCakeTime <= comboThreshold));
        int finalScore = isCombo ? scoreValue * 2 : scoreValue;
        score += finalScore; collectedCakes++;
        lastCakeTime = Time.time; lastCheckpoint = playerTransform.position;
        SpawnFloatingScore(finalScore, isCombo);
        if (collectSound != null) AudioSource.PlayClipAtPoint(collectSound, playerTransform.position);
        nextCakeIndex++; TriggerQuestEvents(); UpdateUI();
        if (collectedCakes >= totalCakes) WinGame();
    }

    void SpawnFloatingScore(int v, bool c)
    {
        if (floatingScorePrefab == null || uiOverlayParent == null) return;
        GameObject go = Instantiate(floatingScorePrefab, uiOverlayParent);
        go.transform.localPosition = Vector3.zero; go.transform.localScale = Vector3.one;
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        if (txt != null) { txt.text = "+" + v + (c ? " COMBO!" : ""); if (c) txt.color = Color.yellow; }
    }

    public void ShowWrongOrderHint(int index) { if (!IsGameEnded) SetHint("Hãy ăn Cake " + nextCakeIndex + " trước!"); }

    void TriggerQuestEvents()
    {
        string questMsg = "";
        switch (collectedCakes)
        {
            case 1: questMsg = "CHẠY RA TẤM VÁN ĂN CAKE 2"; break;
            case 2: questMsg = "NHẢY XUỐNG MÁI DƯỚI!"; break;
            case 3: questMsg = "TÌM ĐƯỜNG ĂN CAKE 4!"; break;
            case 4: questMsg = "XUỐNG ĐẠI LỘ TÌM CAKE 5!"; if (dayNight != null) dayNight.SetNightMode(); break;
            case 5: StartCoroutine(RiddleEvent()); break;
        }
        if (!string.IsNullOrEmpty(questMsg)) StartCoroutine(ShowQuestNotification(questMsg));
    }

    IEnumerator ShowQuestNotification(string message)
    {
        if (questNotificationText == null) yield break;
        questNotificationText.text = message; questNotificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f); questNotificationText.gameObject.SetActive(false);
    }

    public void OnClickContinue()
    {
        if (score >= continueCost)
        {
            score -= continueCost; IsGameEnded = false; losePanel.SetActive(false);
            if (playerController != null) playerController.enabled = false;
            playerTransform.position = lastCheckpoint + Vector3.up * 5f;
            if (playerController != null) playerController.enabled = true;
            Time.timeScale = 1f; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            UpdateUI();
        }
    }

    public void WinGame() { IsGameEnded = true; if (dayNight != null) dayNight.SetDayMode(); winPanel.SetActive(true); StartCoroutine(FadeInWinUI()); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    public void LoseGame(string reason = "Thất bại!") { if (IsGameEnded) return; IsGameEnded = true; StartCoroutine(CameraShake(0.3f, 0.5f)); if (loseReasonText != null) loseReasonText.text = reason + "\nScore: " + score; losePanel.SetActive(true); if (continueButton != null) continueButton.SetActive(score >= continueCost); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    public bool CanCollectCake(int index) => isGameStarted && !IsGameEnded && index == nextCakeIndex;
    void UpdateUI() { if (scoreText) scoreText.text = "Score: " + score; if (cakeText) cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes; }
    void UpdateTimerUI() { if (timerText) timerText.text = "Time: " + totalGameTimer.ToString("F1") + "s"; }
    void UpdateDistanceUI() { Transform t = GetCurrentCakeTarget(); if (t && distanceText) distanceText.text = "Next: " + Vector3.Distance(playerTransform.position, t.position).ToString("F1") + "m"; }
    Transform GetCurrentCakeTarget() { int i = nextCakeIndex - 1; return (i >= 0 && i < cakeTargets.Length) ? cakeTargets[i] : null; }
    public void SetHint(string m) { if (hintText) hintText.text = m; }
    public void SubtractScore(int a) { score = Mathf.Max(0, score - a); UpdateUI(); }
    public void RestartScene() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public IEnumerator CameraShake(float d, float m) { Vector3 o = cameraTransform.localPosition; float e = 0; while (e < d) { cameraTransform.localPosition = o + (Vector3)Random.insideUnitCircle * m; e += Time.deltaTime; yield return null; } cameraTransform.localPosition = o; }
    IEnumerator FadeInWinUI() { float t = 0; while (t < 1f) { t += Time.deltaTime * 0.5f; if (winPanelGroup) winPanelGroup.alpha = t; yield return null; } }
    IEnumerator RiddleEvent() { Time.timeScale = 0; riddlePanel.SetActive(true); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; float timer = 15f; while (timer > 0) { timer -= Time.unscaledDeltaTime; riddleTimerText.text = "Giải đố: " + Mathf.Ceil(timer) + "s"; yield return null; } if (!cake6HintUnlocked) LoseGame("Hết thời gian!"); }
    public void OnClickCorrectAnswer() { cake6HintUnlocked = true; Time.timeScale = 1f; riddlePanel.SetActive(false); Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
    public void OnClickWrongAnswer() { SubtractScore(10); SetHint("Sai rồi! -10 điểm."); }
}