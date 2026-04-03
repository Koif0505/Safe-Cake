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

    [Header("VR Controller Settings")]
    public bool useGyro = true;
    private Quaternion baseRotation = Quaternion.identity;

    [Header("Game State")]
    public bool isGameStarted = false;
    public int score = 0;
    public int collectedCakes = 0;
    public int totalCakes = 6;
    public int nextCakeIndex = 1;
    public bool IsGameEnded { get; private set; } = false;
    public bool cake6HintUnlocked = false;

    [Header("Lose & Smart Logic")]
    public float roofY = 164f;
    public float loseThresholdY = 147f;
    public float groundY = 0f;
    public CanvasGroup losePanelGroup;
    public float groundLevelY_Old = 1.5f;

    [Header("Checkpoint & Continue")]
    private Vector3 lastCheckpoint;
    public int continueCost = 10;
    public GameObject continueButton;

    [Header("Win Celebration")]
    public GameObject fireworksObject;
    public string jumpAnimationParam = "WinJump";
    public CanvasGroup winPanelGroup;

    [Header("Targets & Environment")]
    public Transform[] cakeTargets;
    public DayNight dayNight;
    public GameObject elevatorDoors;

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
    public float comboThreshold = 8f;

    private bool inputHandled = false;
    private bool isAnsweringRiddle = false;
    private float lastCollectTime = -99f;
    private Coroutine riddleTimerCoroutine;
    private Coroutine hintTimerCoroutine;

    [Header("Assassin Car Settings")]
    public float carSpeed = 50f;
    public CarPathFollower attackingCar;
    public GameObject assassinCar;
    public float initialCarSpeed = 60f;
    public float brakeIntensity = 5f;
    public AudioClip brakeScreechSound;

    void Awake() { Instance = this; }

    void Start()
    {
        playerController = playerTransform.GetComponent<CharacterController>();
        playerScript = playerTransform.GetComponent<CharacterControlHybrid>();

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            lastCheckpoint = playerTransform.position;
        }

        lastCollectTime = Time.time - 10f;
        Time.timeScale = 1f;
        IsGameEnded = false;

        if (startPanel) startPanel.SetActive(true);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) { losePanel.SetActive(false); if (losePanelGroup) losePanelGroup.alpha = 0; }
        if (riddlePanel) riddlePanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (fireworksObject) fireworksObject.SetActive(false);
        if (elevatorDoors) elevatorDoors.SetActive(true);
        if (questNotificationText) questNotificationText.gameObject.SetActive(false);

        if (hintText) hintText.text = "Hint: Tìm chiếc bánh đầu tiên trên sân thượng.";

        UpdateUI();

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

            HandleVRRotation();

            CheckSmartLoseCondition();
            UpdateDistanceUI();
            UpdateTimerUI();
        }

        if (PauseActionDown()) TogglePause();
    }

    // =========================
    // INPUT ABSTRACTION
    // =========================

    // B <-> LeftArrow
    bool LeftActionDown()
    {
        return Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.JoystickButton1);
    }

    // A <-> RightArrow
    bool RightActionDown()
    {
        return Input.GetKeyDown(KeyCode.RightArrow) ||
               Input.GetKeyDown(KeyCode.D) ||
               Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    // D <-> UpArrow
    bool ForwardAction()
    {
        return Input.GetKey(KeyCode.UpArrow) ||
               Input.GetKey(KeyCode.W) ||
               Input.GetKey(KeyCode.JoystickButton3);
    }

    bool ForwardActionDown()
    {
        return Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.JoystickButton3);
    }

    // C <-> Space
    bool JumpActionDown()
    {
        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.JoystickButton2);
    }

    bool PauseActionDown()
    {
        return Input.GetKeyDown(KeyCode.Escape) ||
               Input.GetKeyDown(KeyCode.JoystickButton7);
    }

    void HandleMenuInput()
    {
        float hAxis = Input.GetAxisRaw("Horizontal");
        float vAxis = Input.GetAxisRaw("Vertical");

        bool isLeftInput = LeftActionDown() || hAxis < -0.5f;
        bool isRightInput = RightActionDown() || hAxis > 0.5f;

        bool isDownInput = Input.GetKeyDown(KeyCode.S) ||
                           Input.GetKeyDown(KeyCode.DownArrow) ||
                           vAxis < -0.5f;

        if (!inputHandled && (isLeftInput || isRightInput || isDownInput))
        {
            if (losePanel && losePanel.activeSelf)
            {
                if (isLeftInput) RestartScene();
                else if (isRightInput) OnClickContinue();
            }
            else if (!isGameStarted)
            {
                if (howToPlayPanel && howToPlayPanel.activeSelf)
                {
                    if (isRightInput) OnClickStart();
                    else if (isDownInput) OnClickCloseHowToPlay();
                }
                else if (startPanel && startPanel.activeSelf)
                {
                    if (isLeftInput) OnClickHowToPlay();
                    else if (isRightInput) OnClickStart();
                }
            }
            else if (IsGameEnded && winPanel.activeSelf)
            {
                if (isLeftInput) RestartScene();
            }

            inputHandled = true;
        }

        if (Mathf.Abs(hAxis) < 0.1f && Mathf.Abs(vAxis) < 0.1f && !Input.anyKey)
        {
            inputHandled = false;
        }
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (!CanCollectCake(index)) return;

        bool isCombo = (collectedCakes > 0 && (Time.time - lastCollectTime <= comboThreshold));
        int finalScore = isCombo ? scoreValue * 2 : scoreValue;

        score += finalScore;
        collectedCakes++;
        lastCollectTime = Time.time;
        lastCheckpoint = playerTransform.position;

        SpawnFloatingScore(finalScore, isCombo);

        if (collectSound) AudioSource.PlayClipAtPoint(collectSound, playerTransform.position);

        nextCakeIndex++;

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
        TriggerQuestEvents();

        if (collectedCakes >= totalCakes) StartCoroutine(WinSequence());
    }

    void TriggerQuestEvents()
    {
        string questMsg = "";

        switch (collectedCakes)
        {
            case 1:
                questMsg = "Hint: Nhảy ra tấm ván để ăn Bánh 2";
                break;

            case 2:
                questMsg = "Hint: Nhảy xuống mái dưới và tìm Cake";
                break;

            case 3:
                questMsg = "Hint: Bánh 4 trên những toà nhà phía trước.";
                break;

            case 4:
                questMsg = "Hint: Nhảy xuống quốc lộ phía bên trái để tìm Bánh";
                if (dayNight != null)
                    dayNight.SetNightMode();
                break;

            case 5:
                StartCoroutine(RiddleEvent());
                break;
        }

        if (!string.IsNullOrEmpty(questMsg))
            StartCoroutine(ShowQuestNotification(questMsg));
    }

    IEnumerator ShowQuestNotification(string message)
    {
        if (questNotificationText == null) yield break;

        questNotificationText.text = message;
        questNotificationText.gameObject.SetActive(true);

        yield return new WaitForSeconds(2.5f);

        questNotificationText.gameObject.SetActive(false);
    }

    IEnumerator RiddleEvent()
    {
        isAnsweringRiddle = true;
        Time.timeScale = 0;

        if (riddlePanel) riddlePanel.SetActive(true);

        if (leftAnswerButton) leftAnswerButton.image.color = Color.white;
        if (rightAnswerButton) rightAnswerButton.image.color = Color.white;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        float t = 10f;

        while (t > 0 && isAnsweringRiddle)
        {
            t -= Time.unscaledDeltaTime;

            if (riddleTimerText)
                riddleTimerText.text = "GIẢI ĐỐ: " + Mathf.Ceil(t) + "s";

            yield return null;
        }

        if (isAnsweringRiddle && !cake6HintUnlocked)
            LoseGame("HẾT THỜI GIAN GIẢI ĐỐ!");
    }

    public void OnClickContinue()
    {
        if (score >= continueCost)
        {
            SubtractScore(continueCost);

            IsGameEnded = false;

            isAnsweringRiddle = false;
            Time.timeScale = 1f;
            if (losePanel) losePanel.SetActive(false);

            if (playerController) playerController.enabled = false;
            playerTransform.position = lastCheckpoint + Vector3.up * 2f;
            if (playerController) playerController.enabled = true;

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (attackingCar != null) attackingCar.StopAndDestroy();

            UpdateUI();
        }
        else
        {
            if (hintText) hintText.text = "Không đủ điểm để tiếp tục!";
        }
    }

    public void LoseGame(string reason = "BẠN ĐÃ THẤT BẠI!")
    {
        if (IsGameEnded) return;

        StartCoroutine(LoseRoutine(reason));
    }

    IEnumerator LoseRoutine(string reason)
    {
        IsGameEnded = true;

        StartCoroutine(CameraShake(0.4f, 0.3f));

        if (loseReasonText)
            loseReasonText.text = "LÍ DO: " + reason + "\nScore: " + score;

        if (continueButton)
            continueButton.SetActive(score >= continueCost);

        yield return new WaitForSeconds(1.0f);

        if (losePanel) losePanel.SetActive(true);

        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            if (losePanelGroup) losePanelGroup.alpha = t;
            yield return null;
        }

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
        StartCoroutine(FadeInWinUI());
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

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
        StartCoroutine(SummonAssassinCar());
        Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
        if (attackingCar != null) attackingCar.StartCarAttack();
        if (hintText)
        {
            hintText.text = "Hint: Bánh ở trên cây, hàng cây có ghế đá";
            if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = StartCoroutine(HideHintAfterDelay(8f));
        }
    }

    public void OnClickStart()
    {
        isGameStarted = true;
        IsGameEnded = false;
        Time.timeScale = 1f;

        if (startPanel) startPanel.SetActive(false);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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

        if (collectedCakes < 2 && pY < roofY - 2f) LoseGame("RƠI KHỎI TẦNG THƯỢNG!");
        else if (collectedCakes == 2 && pY < loseThresholdY) LoseGame("RƠI XUỐNG DƯỚI CẦU!");
        else if (collectedCakes == 3 && pY < groundY + 5f) LoseGame("CHẠM XUỐNG MẶT ĐẤT!");
        else if (collectedCakes >= 4 && pY < -15f) LoseGame("RƠI KHỎI THÀNH PHỐ!");
    }

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
        if (LeftActionDown())
        {
            if (riddleTimerCoroutine != null) StopCoroutine(riddleTimerCoroutine);
            StartCoroutine(RiddleResultRoutine(true));
        }
        else if (RightActionDown())
        {
            if (riddleTimerCoroutine != null) StopCoroutine(riddleTimerCoroutine);
            StartCoroutine(RiddleResultRoutine(false));
        }
    }

    public bool CanCollectCake(int i) => isGameStarted && !IsGameEnded && i == nextCakeIndex;

    public void ShowWrongOrderHint(int index)
    {
        if (hintText)
        {
            hintText.text = "Hãy ăn Cake " + nextCakeIndex + " trước!";

            if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = StartCoroutine(HideHintAfterDelay(8f));
        }

        if (LeftActionDown())
        {
            if (riddleTimerCoroutine != null) StopCoroutine(riddleTimerCoroutine);
            StartCoroutine(RiddleResultRoutine(true));
        }
        else if (RightActionDown())
        {
            if (riddleTimerCoroutine != null) StopCoroutine(riddleTimerCoroutine);
            StartCoroutine(RiddleResultRoutine(false));
        }
    }

    IEnumerator SummonAssassinCar()
    {
        yield return new WaitForSeconds(3f);

        if (playerTransform.position.y < 3f)
        {
            assassinCar.SetActive(true);

            Vector3 spawnPos = playerTransform.position - playerTransform.forward * 40f;
            spawnPos.y = 0.5f;
            assassinCar.transform.position = spawnPos;
            assassinCar.transform.LookAt(new Vector3(playerTransform.position.x, 0.5f, playerTransform.position.z));

            float currentSpeed = initialCarSpeed;
            bool startBraking = false;

            while (currentSpeed > 0.1f)
            {
                assassinCar.transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

                float distanceToPlayer = Vector3.Distance(
                    new Vector3(assassinCar.transform.position.x, 0, assassinCar.transform.position.z),
                    new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
                );

                Vector3 dirToPlayer = playerTransform.position - assassinCar.transform.position;
                bool hasPassedPlayer = Vector3.Dot(assassinCar.transform.forward, dirToPlayer) < 0;

                if ((distanceToPlayer < 5f || hasPassedPlayer) && !startBraking)
                {
                    startBraking = true;
                    if (brakeScreechSound) AudioSource.PlayClipAtPoint(brakeScreechSound, assassinCar.transform.position);
                }

                if (startBraking)
                {
                    currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * brakeIntensity);
                }

                yield return null;
            }
            currentSpeed = 0;

            yield return new WaitForSeconds(3f);
            assassinCar.SetActive(false);
        }
    }

    public void ImmediateLoseGame(string reason = "BẠN ĐÃ THẤT BẠI!")
    {
        if (IsGameEnded) return;
        IsGameEnded = true;

        StartCoroutine(CameraShake(0.3f, 0.4f));

        if (loseReasonText) loseReasonText.text = "LÍ DO: " + reason + "\nScore: " + score;

        if (continueButton) continueButton.SetActive(score >= continueCost);

        if (losePanel) losePanel.SetActive(true);

        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            if (losePanelGroup) losePanelGroup.alpha = t;
        }

        Time.timeScale = 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void HandleVRRotation()
    {
        if (!useGyro) return;

#if UNITY_ANDROID && !UNITY_EDITOR
    if (!SystemInfo.supportsGyroscope) return;

    Quaternion gyroAttitude = Input.gyro.attitude;
    Quaternion rot = new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);

    cameraTransform.localRotation = Quaternion.Euler(90f, 0f, 0f) * rot;
#endif
    }

    void HandleVRMovement()
    {
    }
}