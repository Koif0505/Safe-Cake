using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private bool inputHandled = false;
    private bool isRiddlePassed = false;

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

    [Header("Lose Y-Levels")]
    public float startRoofLimitY = -10f;
    public float groundLimitY = -180f;
    public CanvasGroup losePanelGroup;

    [Header("Win Celebration")]
    public GameObject fireworksObject;
    public string jumpAnimationParam = "WinJump";

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
    public GameObject startPanel;
    public GameObject howToPlayPanel; // ĐÃ THÊM LẠI - FIX LỖI CS0103
    public GameObject riddlePanel;
    public TextMeshProUGUI riddleTimerText;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject pausePanel;
    public TextMeshProUGUI loseReasonText;

    [Header("Effects")]
    public GameObject floatingScorePrefab;
    public Transform uiOverlayParent;
    public AudioClip collectSound;
    public TextMeshProUGUI questNotificationText;

    void Awake() { Instance = this; }

    void Start()
    {
        playerController = playerTransform.GetComponent<CharacterController>();
        playerScript = playerTransform.GetComponent<CharacterControlHybrid>();
        Time.timeScale = 1f;
        if (losePanel) { losePanel.SetActive(false); if (losePanelGroup) losePanelGroup.alpha = 0; }
        if (pausePanel) pausePanel.SetActive(false);
        UpdateUI();
    }

    void Update()
    {
        if (!isGameStarted || IsGameEnded || (riddlePanel && riddlePanel.activeSelf))
        {
            HandleMenuInput();
            return;
        }
        CheckSmartLoseCondition();
        UpdateDistanceUI();
        UpdateTimerUI();
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7)) TogglePause();
    }

    void HandleMenuInput()
    {
        float hAxis = Input.GetAxisRaw("Horizontal");
        bool leftK = Input.GetKeyDown(KeyCode.A) || hAxis < -0.5f;
        bool rightK = Input.GetKeyDown(KeyCode.D) || hAxis > 0.5f;

        if (!inputHandled && (leftK || rightK))
        {
            if (riddlePanel && riddlePanel.activeSelf) { ProcessRiddle(leftK); inputHandled = true; }
            else if (!isGameStarted && rightK) { OnClickStart(); inputHandled = true; }
            else if (IsGameEnded && leftK) { RestartScene(); inputHandled = true; }
        }
        if (Mathf.Abs(hAxis) < 0.1f) inputHandled = false;
    }

    // --- FIX LỖI CS7036: Thêm giá trị mặc định cho reason ---
    public void LoseGame(string reason = "BẠN ĐÃ THẤT BẠI!")
    {
        if (IsGameEnded) return;
        IsGameEnded = true;
        StartCoroutine(LoseRoutine(reason));
    }

    IEnumerator LoseRoutine(string reason)
    {
        yield return new WaitForSeconds(1.0f);
        if (losePanel) losePanel.SetActive(true);
        if (loseReasonText) loseReasonText.text = reason;
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            if (losePanelGroup) losePanelGroup.alpha = t;
            yield return null;
        }
        Time.timeScale = 0;
    }

    void CheckSmartLoseCondition()
    {
        if (IsGameEnded || !playerController.isGrounded) return;
        float pY = playerTransform.position.y;
        if (collectedCakes < 2 && pY < startRoofLimitY) LoseGame("BẠN VỪA RƠI KHỎI TÒA NHÀ!");
        else if (collectedCakes >= 2 && collectedCakes < 4 && pY < groundLimitY) LoseGame("BẠN ĐÃ CHẠM XUỐNG ĐẤT!");
    }

    public void HitByCar() { LoseGame("BẠN VỪA BỊ XE TÔNG TRÚNG!"); }

    void ProcessRiddle(bool correct) { StartCoroutine(RiddleResult(correct)); }
    IEnumerator RiddleResult(bool correct)
    {
        Image leftImg = riddlePanel.transform.GetChild(0).GetComponent<Image>();
        Image rightImg = riddlePanel.transform.GetChild(1).GetComponent<Image>();
        if (correct)
        {
            if (leftImg) leftImg.color = Color.green;
            score += 10; SpawnFloatingScore(10, false);
        }
        else
        {
            if (rightImg) rightImg.color = Color.red;
            score = Mathf.Max(0, score - 10); SpawnFloatingScore(-10, false);
        }
        yield return new WaitForSecondsRealtime(1.0f);
        riddlePanel.SetActive(false);
        Time.timeScale = 1f;
        isRiddlePassed = true;
        if (hintText) hintText.text = "Hint: Cake 6 ở gốc cây có hàng ghế đá.";
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (!CanCollectCake(index)) return;
        score += scoreValue; collectedCakes++;
        SpawnFloatingScore(scoreValue, false);
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
        if (collectedCakes >= totalCakes) StartCoroutine(WinCelebration());
        else TriggerQuestEvents();
    }

    IEnumerator WinCelebration()
    {
        if (dayNight) dayNight.SetDayMode();
        IsGameEnded = true;
        if (playerScript) playerScript.enabled = false;
        if (mainVRCamera) mainVRCamera.gameObject.SetActive(false);
        if (winCamera) { winCamera.gameObject.SetActive(true); winCamera.transform.LookAt(playerTransform.position + Vector3.up); }
        if (fireworksObject) fireworksObject.SetActive(true);
        yield return new WaitForSeconds(5.0f);
        if (winPanel) winPanel.SetActive(true);
    }

    public void OnClickStart() { isGameStarted = true; if (startPanel) startPanel.SetActive(false); Cursor.lockState = CursorLockMode.Locked; }

    // --- FIX LỖI CS0103: Thêm lại hàm này ---
    public void OnClickHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(true);
        if (startPanel) startPanel.SetActive(false);
    }

    public void OnClickCloseHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);
    }

    void UpdateUI() { if (scoreText) scoreText.text = "Score: " + score; if (cakeText) cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes; }
    void UpdateTimerUI() { if (timerText) timerText.text = "Time: " + Time.timeSinceLevelLoad.ToString("F1") + "s"; }
    void UpdateDistanceUI() { Transform t = GetTarget(); if (t && distanceText) distanceText.text = "Next: " + Vector3.Distance(playerTransform.position, t.position).ToString("F1") + "m"; }
    Transform GetTarget() { int i = nextCakeIndex - 1; return (i >= 0 && i < cakeTargets.Length) ? cakeTargets[i] : null; }
    void SpawnFloatingScore(int v, bool c) { if (floatingScorePrefab && uiOverlayParent) { GameObject g = Instantiate(floatingScorePrefab, uiOverlayParent); g.transform.localPosition = Vector3.zero; TextMeshProUGUI txt = g.GetComponent<TextMeshProUGUI>(); if (txt) { txt.text = (v > 0 ? "+" : "") + v; txt.color = v > 0 ? Color.yellow : Color.red; } } }
    public void RestartScene() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void TogglePause() { if (IsGameEnded) return; bool p = Time.timeScale == 0; Time.timeScale = p ? 1f : 0f; if (pausePanel) pausePanel.SetActive(!p); }
    public bool CanCollectCake(int i) => isGameStarted && !IsGameEnded && i == nextCakeIndex;

    void TriggerQuestEvents()
    {
        string m = ""; switch (collectedCakes)
        {
            case 1: m = "NHẢY QUA TẤM VÁN ĂN CAKE 2"; break;
            case 2: m = "XUỐNG ĐƯỜNG TÌM CAKE 3!"; break;
            case 4: if (dayNight) dayNight.SetNightMode(); break;
            case 5: Time.timeScale = 0; riddlePanel.SetActive(true); break;
        }
        if (questNotificationText && m != "") StartCoroutine(ShowQuest(m));
    }
    IEnumerator ShowQuest(string m) { questNotificationText.text = m; questNotificationText.gameObject.SetActive(true); yield return new WaitForSeconds(2.5f); questNotificationText.gameObject.SetActive(false); }
    public void ShowWrongOrderHint(int i) { if (!IsGameEnded && hintText != null) hintText.text = "Hãy ăn Cake " + nextCakeIndex + " trước!"; }
}