using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;
using System.Collections;
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

    [Header("Controller Mapping (3 Buttons)")]
    public KeyCode leftSelectKey = KeyCode.JoystickButton2;   // Đổi thành Vuông
    public KeyCode rightSelectKey = KeyCode.JoystickButton1;  // Đổi thành Tròn
    public KeyCode jumpKey = KeyCode.JoystickButton3;         // Đổi thành Tam giác
    public KeyCode pauseKey = KeyCode.JoystickButton0;        // Đổi thành X

    [Header("Game State")]
    public bool isGameStarted = false;
    public int score = 0;
    public int collectedCakes = 0;
    public int totalCakes = 6;
    public int nextCakeIndex = 1;
    public bool IsGameEnded { get; private set; } = false;
    public bool IsAnsweringRiddle => isAnsweringRiddle;
    public bool cake6HintUnlocked = false;

    [Header("Lose & Smart Logic")]
    public float roofY = 164f;
    public float loseThresholdY = 147f;
    public float groundY = 0f;
    public CanvasGroup losePanelGroup;

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

    [Header("Assassin Car Settings")]
    public float carSpeed = 50f;
    public CarPathFollower attackingCar;
    public GameObject assassinCar;
    public float initialCarSpeed = 60f;
    public float brakeIntensity = 5f;
    public AudioClip brakeScreechSound;

    private bool inputHandled = false;
    private bool isAnsweringRiddle = false;
    private float lastCollectTime = -99f;
    private Coroutine riddleTimerCoroutine;
    private Coroutine hintTimerCoroutine;
    private Coroutine questTimerCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<CharacterController>();
            playerScript = playerTransform.GetComponent<CharacterControlHybrid>();
            lastCheckpoint = playerTransform.position;
        }
        else
        {
            Debug.LogError("GameManager: playerTransform is missing.");
        }

        lastCollectTime = Time.time - 10f;
        Time.timeScale = 1f;
        IsGameEnded = false;

        if (startPanel) startPanel.SetActive(true);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (losePanel)
        {
            losePanel.SetActive(false);
            if (losePanelGroup) losePanelGroup.alpha = 0f;
        }
        if (riddlePanel) riddlePanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (fireworksObject) fireworksObject.SetActive(false);
        if (elevatorDoors) elevatorDoors.SetActive(true);
        if (questNotificationText) questNotificationText.gameObject.SetActive(false);

        if (hintText) hintText.text = "Hint: Tìm chiếc bánh đầu tiên trên sân thượng.";
        if (mainVRCamera) mainVRCamera.gameObject.SetActive(true);
        if (winCamera) winCamera.gameObject.SetActive(false);

        UpdateUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (isAnsweringRiddle)
        {
            HandleRiddleInput();
            return;
        }

        if (!isGameStarted || IsGameEnded)
        {
            HandleMenuInput();
            return;
        }

        if (Time.timeScale > 0f)
        {
            totalGameTimer += Time.deltaTime;
            CheckSmartLoseCondition();
            UpdateDistanceUI();
            UpdateTimerUI();
        }

        if (PauseActionDown())
        {
            TogglePause();
        }
    }

    bool LeftActionDown()
    {
        // Sử dụng phím A/Trái hoặc leftSelectKey
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(leftSelectKey);
    }

    bool RightActionDown()
    {
        // Sử dụng phím D/Phải hoặc rightSelectKey
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(rightSelectKey);
    }

    bool ForwardAction()
    {
        // Sử dụng phím W/Lên (Forward không dùng chung jump)
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
    }

    bool JumpActionDown()
    {
        // Sử dụng phím Space hoặc jumpKey
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(jumpKey);
    }

    bool PauseActionDown()
    {
        // Sử dụng phím Esc hoặc pauseKey
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(pauseKey);
    }
    // --------------------------------------------

    void HandleMenuInput()
    {
        float hAxis = Input.GetAxisRaw("Horizontal");
        float vAxis = Input.GetAxisRaw("Vertical");

        bool isLeftInput = LeftActionDown() || hAxis < -0.5f;
        bool isRightInput = RightActionDown() || hAxis > 0.5f;

        bool isDownInput = Input.GetKeyDown(KeyCode.S)
                        || Input.GetKeyDown(KeyCode.DownArrow)
                        || vAxis < -0.5f
                        || JumpActionDown();

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
            else if (IsGameEnded && winPanel != null && winPanel.activeSelf)
            {
                if (isLeftInput) RestartScene();
            }

            inputHandled = true;
        }

        if (Mathf.Abs(hAxis) < 0.1f && Mathf.Abs(vAxis) < 0.1f)
        {
            inputHandled = false;
        }
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (!CanCollectCake(index)) return;

        if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
        if (hintText) hintText.text = "";

        if (questTimerCoroutine != null) StopCoroutine(questTimerCoroutine);
        if (questNotificationText) questNotificationText.gameObject.SetActive(false);

        bool isCombo = (collectedCakes > 0 && (Time.time - lastCollectTime <= comboThreshold));
        int finalScore = isCombo ? scoreValue * 2 : scoreValue;

        score += finalScore;
        collectedCakes++;
        lastCollectTime = Time.time;

        if (playerTransform != null)
        {
            lastCheckpoint = playerTransform.position;
        }

        SpawnFloatingScore(finalScore, isCombo);

        if (collectSound && playerTransform != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, playerTransform.position);
        }

        nextCakeIndex++;

        if (nextCakeIndex <= totalCakes && cakeTargets != null && nextCakeIndex - 1 < cakeTargets.Length)
        {
            Transform next = cakeTargets[nextCakeIndex - 1];
            if (next != null)
            {
                var effect = next.GetComponent<CakeEffect>();
                if (effect)
                {
                    float dur = 10f;
                    float hMul = 2f;
                    bool glow = true;

                    if (nextCakeIndex == 2) { dur = 5f; hMul = 1f; glow = false; }
                    else if (nextCakeIndex == 3) { dur = 12f; }
                    else if (nextCakeIndex == 6) { glow = false; }

                    effect.TriggerEffect(dur, hMul, glow);
                }
            }
        }

        UpdateUI();
        TriggerQuestEvents();

        if (collectedCakes >= totalCakes)
        {
            StartCoroutine(WinSequence());
        }
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
                questMsg = "Hint: Nhảy xuống quốc lộ phía bên TRÁI để tìm Bánh";
                if (dayNight != null) dayNight.SetNightMode();
                break;
            case 5:
                StartCoroutine(RiddleEvent());
                break;
        }

        if (!string.IsNullOrEmpty(questMsg))
        {
            questTimerCoroutine = StartCoroutine(ShowQuestNotification(questMsg));
        }
    }

    IEnumerator ShowQuestNotification(string message)
    {
        if (questNotificationText == null) yield break;

        questNotificationText.text = message;
        questNotificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(10f);
        questNotificationText.gameObject.SetActive(false);
    }

    IEnumerator RiddleEvent()
    {
        isAnsweringRiddle = true;
        Time.timeScale = 0f;

        if (riddlePanel) riddlePanel.SetActive(true);
        if (leftAnswerButton) leftAnswerButton.image.color = Color.white;
        if (rightAnswerButton) rightAnswerButton.image.color = Color.white;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        float t = 10f;
        while (t > 0f && isAnsweringRiddle)
        {
            t -= Time.unscaledDeltaTime;
            if (riddleTimerText) riddleTimerText.text = "GIẢI ĐỐ: " + Mathf.Ceil(t) + "s";
            yield return null;
        }

        if (isAnsweringRiddle && !cake6HintUnlocked)
        {
            LoseGame("HẾT THỜI GIAN GIẢI ĐỐ!");
        }
    }

    public void OnClickContinue()
    {
        if (score < continueCost)
        {
            if (hintText) hintText.text = "Không đủ điểm để tiếp tục!";
            return;
        }

        SubtractScore(continueCost);
        IsGameEnded = false;
        isAnsweringRiddle = false;
        Time.timeScale = 1f;

        if (losePanel) losePanel.SetActive(false);

        if (playerController != null && playerTransform != null)
        {
            playerController.enabled = false;
            playerTransform.position = lastCheckpoint + Vector3.up * 2f;
            playerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (attackingCar != null) attackingCar.StopAndDestroy();
        if (assassinCar != null) assassinCar.SetActive(false);

        UpdateUI();
    }

    public void LoseGame(string reason = "BẠN ĐÃ THẤT BẠI!")
    {
        if (IsGameEnded) return;
        StartCoroutine(LoseRoutine(reason));
    }

    IEnumerator LoseRoutine(string reason)
    {
        IsGameEnded = true;

        if (cameraTransform != null)
        {
            StartCoroutine(CameraShake(0.4f, 0.3f));
        }

        if (loseReasonText) loseReasonText.text = "LÍ DO: " + reason + "\\nScore: " + score;
        if (continueButton) continueButton.SetActive(score >= continueCost);

        yield return new WaitForSeconds(1.0f);

        if (losePanel) losePanel.SetActive(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            if (losePanelGroup) losePanelGroup.alpha = t;
            yield return null;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator WinSequence()
    {
        IsGameEnded = true;

        if (dayNight) dayNight.SetDayMode();
        if (playerScript) playerScript.enabled = false;
        if (mainVRCamera) mainVRCamera.gameObject.SetActive(false);

        if (winCamera && playerTransform != null)
        {
            winCamera.gameObject.SetActive(true);
            winCamera.transform.LookAt(playerTransform.position + Vector3.up);
        }

        if (fireworksObject) fireworksObject.SetActive(true);
        if (winScoreText) winScoreText.text = "Final Score: " + score;

        Animator anim = (playerTransform != null) ? playerTransform.GetComponent<Animator>() : null;
        float timer = 3f;

        while (timer > 0f)
        {
            if (anim != null) anim.SetTrigger(jumpAnimationParam);
            if (playerTransform != null) playerTransform.Rotate(0f, 100f * Time.deltaTime, 0f);
            timer -= Time.deltaTime;
            yield return null;
        }

        if (winPanel) winPanel.SetActive(true);
        StartCoroutine(FadeInWinUI());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        if (cameraTransform == null) yield break;

        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;

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
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.5f;
            if (winPanelGroup) winPanelGroup.alpha = t;
            yield return null;
        }
    }

    IEnumerator RiddleResultRoutine(bool isCorrect)
    {
        if (isCorrect)
        {
            cake6HintUnlocked = true;
            if (leftAnswerButton) leftAnswerButton.image.color = Color.green;
            score += 10;
            SpawnFloatingScore(10);
        }
        else
        {
            SubtractScore(10);
            if (rightAnswerButton) rightAnswerButton.image.color = Color.red;
            SpawnFloatingScore(-10);
        }

        UpdateUI();
        yield return new WaitForSecondsRealtime(1.0f);

        if (riddlePanel) riddlePanel.SetActive(false);
        isAnsweringRiddle = false;
        Time.timeScale = 1f;

        StartCoroutine(SummonAssassinCar());

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (attackingCar != null) attackingCar.StartCarAttack();

        if (hintText)
        {
            hintText.text = "Hint: Bánh ở trên cây, hàng cây có ghế đá";
            if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = StartCoroutine(HideHintAfterDelay(10f));
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

        if (questTimerCoroutine != null) StopCoroutine(questTimerCoroutine);
        questTimerCoroutine = StartCoroutine(ShowQuestNotification("GAME BẮT ĐẦU! TÌM CAKE 1"));
    }

    public void OnClickHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(true);
        if (startPanel) startPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnClickCloseHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TogglePause()
    {
        if (IsGameEnded) return;

        bool resume = Time.timeScale == 0f;
        Time.timeScale = resume ? 1f : 0f;

        if (pausePanel) pausePanel.SetActive(!resume);

        Cursor.visible = !resume;
        Cursor.lockState = resume ? CursorLockMode.Locked : CursorLockMode.None;
    }

    void CheckSmartLoseCondition()
    {
        if (IsGameEnded || playerController == null || playerTransform == null) return;
        if (!playerController.isGrounded) return;

        float pY = playerTransform.position.y;

        if (collectedCakes < 2 && pY < roofY - 2f) LoseGame("RƠI KHỎI TẦNG THƯỢNG!");
        else if (collectedCakes == 2 && pY < loseThresholdY) LoseGame("RƠI XUỐNG DƯỚI CẦU!");
        else if (collectedCakes == 3 && pY < groundY + 5f) LoseGame("CHẠM XUỐNG MẶT ĐẤT!");
        else if (collectedCakes >= 4 && pY < -15f) LoseGame("RƠI KHỎI THÀNH PHỐ!");
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (cakeText) cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes;
    }

    void UpdateTimerUI()
    {
        if (timerText) timerText.text = "Time: " + totalGameTimer.ToString("F1") + "s";
    }

    void UpdateDistanceUI()
    {
        if (playerTransform == null || distanceText == null) return;

        Transform target = GetTarget();
        if (target != null)
        {
            distanceText.text = "Next: " + Vector3.Distance(playerTransform.position, target.position).ToString("F1") + "m";
        }
    }

    Transform GetTarget()
    {
        int i = nextCakeIndex - 1;
        return (cakeTargets != null && i >= 0 && i < cakeTargets.Length) ? cakeTargets[i] : null;
    }

    public void SpawnFloatingScore(int value, bool isCombo = false)
    {
        if (!floatingScorePrefab || !uiOverlayParent) return;

        GameObject g = Instantiate(floatingScorePrefab, uiOverlayParent);
        g.transform.localPosition = Vector3.zero;

        TextMeshProUGUI txt = g.GetComponent<TextMeshProUGUI>();
        if (txt)
        {
            txt.text = (value >= 0 ? "+" : "") + value + (isCombo ? " COMBO!" : "");
            txt.color = isCombo ? Color.yellow : (value >= 0 ? Color.white : Color.red);
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

    public bool CanCollectCake(int i)
    {
        return isGameStarted && !IsGameEnded && i == nextCakeIndex;
    }

    public void ShowWrongOrderHint(int index)
    {
        if (hintText)
        {
            hintText.text = "Hãy ăn Cake " + nextCakeIndex + " trước!";

            if (hintTimerCoroutine != null) StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = StartCoroutine(HideHintAfterDelay(10f));
        }
    }

    IEnumerator SummonAssassinCar()
    {
        if (playerTransform == null || assassinCar == null) yield break;

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
                    new Vector3(assassinCar.transform.position.x, 0f, assassinCar.transform.position.z),
                    new Vector3(playerTransform.position.x, 0f, playerTransform.position.z)
                );

                Vector3 dirToPlayer = playerTransform.position - assassinCar.transform.position;
                bool hasPassedPlayer = Vector3.Dot(assassinCar.transform.forward, dirToPlayer) < 0f;

                if ((distanceToPlayer < 5f || hasPassedPlayer) && !startBraking)
                {
                    startBraking = true;
                    if (brakeScreechSound)
                    {
                        AudioSource.PlayClipAtPoint(brakeScreechSound, assassinCar.transform.position);
                    }
                }

                if (startBraking)
                {
                    currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * brakeIntensity);
                }

                if (distanceToPlayer < 2.5f && !IsGameEnded)
                {
                    ImmediateLoseGame("BẠN VỪA BỊ XE TÔNG TRÚNG!");
                    break;
                }

                yield return null;
            }

            yield return new WaitForSeconds(3f);
            assassinCar.SetActive(false);
        }
    }

    public void ImmediateLoseGame(string reason = "BẠN ĐÃ THẤT BẠI!")
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        if (cameraTransform != null)
        {
            StartCoroutine(CameraShake(0.3f, 0.4f));
        }

        if (loseReasonText) loseReasonText.text = "LÍ DO: " + reason + "\\nScore: " + score;
        if (continueButton) continueButton.SetActive(score >= continueCost);
        if (losePanel) losePanel.SetActive(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            if (losePanelGroup) losePanelGroup.alpha = t;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
