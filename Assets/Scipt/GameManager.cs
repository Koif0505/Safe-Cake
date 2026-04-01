using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player & Camera")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public Camera mainVRCamera;
    public Camera winCamera;
    private CharacterController playerController;
    private CharacterControlHybrid playerScript;

    [Header("Game State")]
    public bool isGameStarted = false;
    public int score = 0;
    public int collectedCakes = 0;
    public int totalCakes = 6;
    public int nextCakeIndex = 1;
    public bool IsGameEnded { get; private set; } = false;
    public bool cake6HintUnlocked = false; // Từ script cũ: Để kiểm tra đã qua Riddle chưa

    [Header("Lose & Smart Logic")]
    public float roofY = 164f;
    public float loseThresholdY = 147f;
    public float groundY = 0f;
    public CanvasGroup losePanelGroup;
    public float groundLevelY_Old = 1.5f; // Dự phòng từ script cũ

    [Header("Checkpoint & Continue")]
    private Vector3 lastCheckpoint;
    public int continueCost = 10; // Phí hồi sinh từ script cũ
    public GameObject continueButton; // Nút hồi sinh từ script cũ

    [Header("Win Celebration")]
    public GameObject fireworksObject;
    public string jumpAnimationParam = "WinJump";
    public CanvasGroup winPanelGroup; // Để làm hiệu ứng Fade từ script cũ

    [Header("Targets & Environment")]
    public Transform[] cakeTargets;
    public DayNight dayNight;
    public GameObject elevatorDoors; // Quản lý cửa từ script cũ

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI cakeText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI winScoreText;
    public float totalGameTimer = 0f;

    [Header("Panels")]
    public GameObject startPanel;
    public GameObject howToPlayPanel;
    public GameObject riddlePanel;
    public TextMeshProUGUI riddleTimerText;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject pausePanel;
    public TextMeshProUGUI loseReasonText;
    public Button leftAnswerButton;
    public Button rightAnswerButton;

    [Header("Effects & Audio")]
    public GameObject floatingScorePrefab;
    public Transform uiOverlayParent;
    public AudioClip collectSound;
    public TextMeshProUGUI questNotificationText;
    public float comboThreshold = 8f; // Ngưỡng combo từ script cũ

    // Biến nội bộ quản lý Coroutine và Input
    private bool inputHandled = false;
    private bool isAnsweringRiddle = false;
    private float lastCollectTime = -99f;
    private Coroutine riddleTimerCoroutine;
    private Coroutine hintTimerCoroutine;

    void Awake() { Instance = this; }

    void Start()
    {
        playerController = playerTransform.GetComponent<CharacterController>();
        playerScript = playerTransform.GetComponent<CharacterControlHybrid>();
        lastCheckpoint = playerTransform.position;
        lastCollectTime = Time.time - 10f;
        Time.timeScale = 1f;

        // Reset UI và Panels (Kỹ càng từ cả 2 script)
        if (startPanel) startPanel.SetActive(true);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) { losePanel.SetActive(false); if (losePanelGroup) losePanelGroup.alpha = 0; }
        if (riddlePanel) riddlePanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (fireworksObject) fireworksObject.SetActive(false);
        if (elevatorDoors) elevatorDoors.SetActive(true); // Giữ cửa đóng lúc đầu
        if (questNotificationText) questNotificationText.gameObject.SetActive(false);

        if (hintText) hintText.text = "Hint: Tìm chiếc bánh đầu tiên trên sân thượng.";

        UpdateUI();

        // Trạng thái chuột ban đầu (VR Friendly)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (isAnsweringRiddle) { HandleRiddleInput(); return; }
        if (!isGameStarted || IsGameEnded) { HandleMenuInput(); return; }

        if (Time.timeScale > 0)
        {
            totalGameTimer += Time.deltaTime;
            CheckSmartLoseCondition();
            UpdateDistanceUI();
            UpdateTimerUI();
        }

        // Pause Game
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7)) TogglePause();
    }

    // --- HÀM XỬ LÝ MENU GỘP CẢ PHÍM TẮT VÀ JOYSTICK (CỰC KỸ) ---
    void HandleMenuInput()
    {
        float hAxis = Input.GetAxisRaw("Horizontal");
        float vAxis = Input.GetAxisRaw("Vertical");

        bool leftK = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || hAxis < -0.5f;
        bool rightK = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || hAxis > 0.5f;
        bool downK = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || vAxis < -0.5f;

        if (!inputHandled && (leftK || rightK || downK))
        {
            // 1. Nếu đang ở Lose Panel
            if (losePanel && losePanel.activeSelf)
            {
                if (leftK) RestartScene();
                else if (rightK) OnClickContinue();
            }
            // 2. Nếu chưa Start Game
            else if (!isGameStarted)
            {
                if (howToPlayPanel && howToPlayPanel.activeSelf)
                {
                    if (rightK) OnClickStart();
                    else if (downK) OnClickCloseHowToPlay(); // Phím tắt thoát hướng dẫn từ script cũ
                }
                else if (startPanel && startPanel.activeSelf)
                {
                    if (leftK) OnClickHowToPlay();
                    else if (rightK) OnClickStart();
                }
            }
            // 3. Nếu đang ở Win Panel
            else if (IsGameEnded && winPanel.activeSelf)
            {
                if (leftK) RestartScene();
            }

            inputHandled = true;
        }

        if (Mathf.Abs(hAxis) < 0.1f && Mathf.Abs(vAxis) < 0.1f && !Input.anyKey) inputHandled = false;
    }

    // --- LOGIC ĂN BÁNH (KẾT HỢP COMBO VÀ EVENT) ---
    public void CollectCake(int index, int scoreValue)
    {
        if (!CanCollectCake(index)) return;

        // Tính Combo (Dùng Threshold từ script cũ)
        bool isCombo = (collectedCakes > 0 && (Time.time - lastCollectTime <= comboThreshold));
        int finalScore = isCombo ? scoreValue * 2 : scoreValue;

        score += finalScore;
        collectedCakes++;
        lastCollectTime = Time.time;
        lastCheckpoint = playerTransform.position;

        SpawnFloatingScore(finalScore, isCombo);

        if (collectSound) AudioSource.PlayClipAtPoint(collectSound, playerTransform.position);

        nextCakeIndex++;

        // Kích hoạt hiệu ứng đặc biệt cho bánh tiếp theo
        if (nextCakeIndex <= totalCakes)
        {
            Transform next = cakeTargets[nextCakeIndex - 1];
            var effect = next.GetComponent<CakeEffect>();
            if (effect)
            {
                float dur = 10f; float hMul = 2f; bool glow = true;
                if (nextCakeIndex == 2) { dur = 5f; hMul = 1f; glow = false; }
                else if (nextCakeIndex == 3) { dur = 12f; }
                else if (nextCakeIndex == 6) { glow = false; }
                effect.TriggerEffect(dur, hMul, glow);
            }
        }

        UpdateUI();
        UpdateQuestAndHints();

        if (collectedCakes >= totalCakes) StartCoroutine(WinSequence());
    }

    void UpdateQuestAndHints()
    {
        string m = "";
        string h = "";

        switch (collectedCakes)
        {
            case 1:
                m = "NHẢY QUA TẤM VÁN ĂN CAKE 2";
                h = "Hint: Nhảy ra tấm ván để ăn Bánh 2";
                break;
            case 2:
                m = "XUỐNG CẦU TÌM CAKE 3!";
                h = "Hint: Nhảy xuống mái dưới và tìm Cake";
                break;
            case 3:
                m = "TÌM BÁNH 4 PHÍA TRƯỚC!";
                h = "Hint: Bánh 4 ở trên những toà nhà phía trước.";
                break;
            case 4:
                m = "XUỐNG QUỐC LỘ TÌM BÁNH 5!";
                h = "Hint: Nhảy xuống quốc lộ phía bên trái để tìm Bánh";
                if (dayNight) dayNight.SetNightMode();
                break;
            case 5:
                isAnsweringRiddle = true;
                Time.timeScale = 0;
                if (riddlePanel) riddlePanel.SetActive(true);
                Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
                if (riddleTimerCoroutine != null) StopCoroutine(riddleTimerCoroutine);
                riddleTimerCoroutine = StartCoroutine(RiddleTimerRoutine(15f)); // Tăng lên 15s như script cũ
                break;
        }

        if (questNotificationText && m != "") StartCoroutine(ShowQuest(m));

        if (hintText && h != "")
        {
            hintText.text = h;
            if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = StartCoroutine(HideHintAfterDelay(8f));
        }
    }

    // --- HỆ THỐNG HỒI SINH (TỪ SCRIPT CŨ) ---
    public void OnClickContinue()
    {
        if (score >= continueCost)
        {
            SubtractScore(continueCost); // Dùng hàm helper mới
            isAnsweringRiddle = false;
            Time.timeScale = 1f;
            if (losePanel) losePanel.SetActive(false);

            // Hồi sinh tại Checkpoint
            if (playerController) playerController.enabled = false;
            playerTransform.position = lastCheckpoint + Vector3.up * 2f;
            if (playerController) playerController.enabled = true;

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            UpdateUI();
        }
        else
        {
            if (hintText) hintText.text = "Không đủ điểm để tiếp tục!";
        }
    }

    // --- HIỆU ỨNG THẮNG/THUA (RUNG CAMERA & FADE UI) ---
    public void LoseGame(string reason = "BẠN ĐÃ THẤT BẠI!")
    {
        if (IsGameEnded) return;

        StartCoroutine(LoseRoutine(reason));
    }

    IEnumerator LoseRoutine(string reason)
    {
        IsGameEnded = true;

        // Rung camera ngay
        StartCoroutine(CameraShake(0.4f, 0.3f));

        // Set text ngay
        if (loseReasonText)
            loseReasonText.text = "LÍ DO: " + reason + "\nScore: " + score;

        // Hiện nút continue nếu đủ điểm
        if (continueButton)
            continueButton.SetActive(score >= continueCost);

        // Delay 1s cho hiệu ứng rơi
        yield return new WaitForSeconds(1.0f);

        // Hiện panel + fade
        if (losePanel) losePanel.SetActive(true);

        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            if (losePanelGroup) losePanelGroup.alpha = t;
            yield return null;
        }

        // Pause game ở cuối
        Time.timeScale = 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator WinSequence()
    {
        IsGameEnded = true;
        if (dayNight) dayNight.SetDayMode();
        if (playerScript) playerScript.enabled = false;
        if (mainVRCamera) mainVRCamera.gameObject.SetActive(false);
        if (winCamera) { winCamera.gameObject.SetActive(true); winCamera.transform.LookAt(playerTransform.position + Vector3.up); }
        if (fireworksObject) fireworksObject.SetActive(true);
        if (winScoreText) winScoreText.text = "Final Score: " + score;

        // Nhảy ăn mừng
        Animator anim = playerTransform.GetComponent<Animator>();
        float timer = 3f;
        while (timer > 0)
        {
            if (anim != null) anim.SetTrigger(jumpAnimationParam);
            playerTransform.Rotate(0, 100 * Time.deltaTime, 0);
            timer -= Time.deltaTime;
            yield return null;
        }

        if (winPanel) winPanel.SetActive(true);
        StartCoroutine(FadeInWinUI()); // Fade mượt từ script cũ
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    // --- CÁC HÀM HỖ TRỢ (HELPER METHODS) ---
    public void SubtractScore(int amount)
    {
        score = Mathf.Max(0, score - amount);
        UpdateUI();
    }

    IEnumerator HideHintAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hintText) hintText.text = "";
        hintTimerCoroutine = null;
    }

    IEnumerator CameraShake(float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cameraTransform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cameraTransform.localPosition = originalPos;
    }

    IEnumerator FadeInWinUI()
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.5f;
            if (winPanelGroup) winPanelGroup.alpha = t;
            yield return null;
        }
    }

    IEnumerator RiddleTimerRoutine(float timeLimit)
    {
        float timer = timeLimit;
        while (timer > 0 && isAnsweringRiddle)
        {
            timer -= Time.unscaledDeltaTime;
            if (riddleTimerText) riddleTimerText.text = "GIẢI ĐỐ: " + Mathf.Ceil(timer) + "s";
            yield return null;
        }
        if (isAnsweringRiddle && !cake6HintUnlocked) LoseGame("HẾT THỜI GIAN GIẢI ĐỐ!");
    }

    IEnumerator RiddleResultRoutine(bool isCorrect)
    {
        if (isCorrect)
        {
            cake6HintUnlocked = true;
            if (leftAnswerButton) leftAnswerButton.image.color = Color.green;
            score += 10; SpawnFloatingScore(10);
        }
        else
        {
            SubtractScore(10);
            if (rightAnswerButton) rightAnswerButton.image.color = Color.red;
            SpawnFloatingScore(-10);
        }

        UpdateUI();
        yield return new WaitForSecondsRealtime(1.0f);

        riddlePanel.SetActive(false);
        isAnsweringRiddle = false;
        Time.timeScale = 1f;
        Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;

        if (hintText)
        {
            hintText.text = "Hint: Bánh ở trên cây, hàng cây có ghế đá";
            if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = StartCoroutine(HideHintAfterDelay(8f));
        }
    }

    // Các hàm xử lý Menu cơ bản
    public void OnClickStart()
    {
        isGameStarted = true;
        if (startPanel) startPanel.SetActive(false);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        StartCoroutine(ShowQuest("GAME BẮT ĐẦU! TÌM CAKE 1"));
    }

    public void OnClickHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(true);
        if (startPanel) startPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void OnClickCloseHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);
    }

    public void RestartScene() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    public void TogglePause()
    {
        if (IsGameEnded) return;
        bool p = Time.timeScale == 0;
        Time.timeScale = p ? 1f : 0f;
        if (pausePanel) pausePanel.SetActive(!p);
        Cursor.visible = !p; Cursor.lockState = p ? CursorLockMode.Locked : CursorLockMode.None;
    }

    void CheckSmartLoseCondition()
    {
        if (IsGameEnded || !playerController.isGrounded) return;
        float pY = playerTransform.position.y;

        // Kết hợp logic Y chi tiết của script mới
        if (collectedCakes < 2 && pY < roofY - 2f) LoseGame("RƠI KHỎI TẦNG THƯỢNG!");
        else if (collectedCakes == 2 && pY < loseThresholdY) LoseGame("RƠI XUỐNG DƯỚI CẦU!");
        else if (collectedCakes == 3 && pY < groundY + 5f) LoseGame("CHẠM XUỐNG MẶT ĐẤT!");
        // Logic rơi khỏi thành phố từ script cũ
        else if (collectedCakes >= 4 && pY < -15f) LoseGame("RƠI KHỎI THÀNH PHỐ!");
    }

    // UI & Logic nội bộ
    void UpdateUI() { if (scoreText) scoreText.text = "Score: " + score; if (cakeText) cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes; }
    void UpdateTimerUI() { if (timerText) timerText.text = "Time: " + totalGameTimer.ToString("F1") + "s"; }
    void UpdateDistanceUI() { Transform t = GetTarget(); if (t && distanceText) distanceText.text = "Next: " + Vector3.Distance(playerTransform.position, t.position).ToString("F1") + "m"; }
    Transform GetTarget() { int i = nextCakeIndex - 1; return (i >= 0 && i < cakeTargets.Length) ? cakeTargets[i] : null; }

    public void SpawnFloatingScore(int value, bool isCombo = false)
    {
        if (floatingScorePrefab && uiOverlayParent)
        {
            GameObject g = Instantiate(floatingScorePrefab, uiOverlayParent);
            g.transform.localPosition = Vector3.zero;
            TextMeshProUGUI txt = g.GetComponent<TextMeshProUGUI>();
            if (txt)
            {
                txt.text = (value >= 0 ? "+" : "") + value + (isCombo ? " COMBO!" : "");
                txt.color = isCombo ? Color.yellow : (value >= 0 ? Color.white : Color.red);
            }
        }
    }

    IEnumerator ShowQuest(string m)
    {
        if (questNotificationText)
        {
            questNotificationText.text = m;
            questNotificationText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            questNotificationText.gameObject.SetActive(false);
        }
    }

    public void HandleRiddleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (riddleTimerCoroutine != null) StopCoroutine(riddleTimerCoroutine);
            StartCoroutine(RiddleResultRoutine(true));
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (riddleTimerCoroutine != null) StopCoroutine(riddleTimerCoroutine);
            StartCoroutine(RiddleResultRoutine(false));
        }
    }
    public bool CanCollectCake(int i) => isGameStarted && !IsGameEnded && i == nextCakeIndex;
    // Thêm hàm này vào cuối GameManager.cs để sửa lỗi CS1061
    public void ShowWrongOrderHint(int index)
    {
        if (hintText)
        {
            hintText.text = "Hãy ăn Cake " + nextCakeIndex + " trước!";

            // Cho hint này cũng biến mất sau 8s cho đồng bộ
            if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = StartCoroutine(HideHintAfterDelay(8f));
        }
    }
}