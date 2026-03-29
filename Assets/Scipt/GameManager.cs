using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;
using System.Collections; // Thêm dòng này vào đầu script

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Transform playerTransform;

    [Header("Game State")]
    public int score = 0;
    public int collectedCakes = 0;
    public int totalCakes = 6;
    public int nextCakeIndex = 1;
    public bool IsGameEnded { get; private set; } = false;

    [Header("Checkpoint")]
    private Vector3 lastCheckpoint;
    public int fallPenalty = 15;

    [Header("Targets")]
    public Transform[] cakeTargets;

    [Header("Day Night")]
    public DayNight dayNight;
    private bool switchedToNight = false;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI cakeText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI distanceText;
    public GameObject riddlePanel;
    public TextMeshProUGUI riddleTimerText;

    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI loseScoreText;

    [Header("Question")]
    public bool cake6HintUnlocked = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        lastCheckpoint = playerTransform.position;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (dayNight != null)
        {
            dayNight.SetDayMode();
        }

        UpdateUI();
        UpdateDistanceUI();
        SetHint("Go to Cake 1");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (riddlePanel != null) riddlePanel.SetActive(false); // Thêm dòng này!
    }

    void Update()
    {
        if (IsGameEnded) return;

        if (playerTransform != null && playerTransform.position.y < -10f)
        {
            Respawn(fallPenalty);
        }

        UpdateDistanceUI();
    }

    // ================= CAKE =================

    public bool CanCollectCake(int index)
    {
        if (IsGameEnded) return false;
        return index == nextCakeIndex;
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (IsGameEnded) return;
        if (index != nextCakeIndex) return;

        collectedCakes++;
        score += scoreValue;

        lastCheckpoint = playerTransform.position;

        nextCakeIndex++;

        TriggerProgressionEvents();

        UpdateUI();
        UpdateDistanceUI();

        if (collectedCakes >= totalCakes)
        {
            if (dayNight != null)
            {
                dayNight.SetDayMode();
                switchedToNight = false;
            }

            WinGame();
        }
        else
        {
            SetHint("Go to Cake " + nextCakeIndex);
        }
    }

    public void ShowWrongOrderHint(int index)
    {
        if (IsGameEnded) return;
        SetHint("Collect Cake " + nextCakeIndex + " first!");
    }

    void TriggerProgressionEvents()
    {
        switch (collectedCakes)
        {
            case 1:
                SetHint("Cake 2: Nhảy ra tấm ván, leo lên sân thượng!");
                break;
            case 2:
                SetHint("Cake 3: Nhảy xuống mái dưới, nhìn bên phải cửa thang máy!");
                // Kích hoạt mũi tên chỉ hướng nếu bạn làm xong
                break;
            case 3:
                SetHint("Cake 4: Rơi tự do xuống tầng dưới, tìm đường hẹp!");
                break;
            case 4:
                SetHint("CẢNH BÁO: Trời tối! Cake 5 đang ở giữa đường tử thần.");
                if (dayNight != null) dayNight.SetNightMode();
                break;
            case 5:
                // Dừng game 10s để giải đố
                StartCoroutine(RiddleEvent());
                break;
        }
    }

    IEnumerator RiddleEvent()
    {
        Time.timeScale = 0; // Ngừng thời gian
        riddlePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float timer = 10f;
        while (timer > 0)
        {
            timer -= Time.unscaledDeltaTime; // Dùng unscaled vì timeScale = 0
            riddleTimerText.text = "Thời gian: " + Mathf.Ceil(timer) + "s";
            yield return null;
        }
        // Nếu hết giờ mà chưa chọn -> Thua
        if (!cake6HintUnlocked) LoseGame();
    }

    // ================= DISTANCE UI =================

    void UpdateDistanceUI()
    {
        if (distanceText == null || playerTransform == null)
            return;

        Transform currentTarget = GetCurrentCakeTarget();

        if (currentTarget == null)
        {
            distanceText.text = "";
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, currentTarget.position);
        distanceText.text = "Next Cake: " + distance.ToString("F1") + " m";
    }

    Transform GetCurrentCakeTarget()
    {
        if (cakeTargets == null || cakeTargets.Length == 0)
            return null;

        int targetIndex = nextCakeIndex - 1;

        if (targetIndex >= 0 && targetIndex < cakeTargets.Length)
            return cakeTargets[targetIndex];

        return null;
    }

    // ================= RESPAWN =================

    public void Respawn(int penalty)
    {
        if (IsGameEnded) return;

        score -= penalty;
        if (score < 0) score = 0;

        if (playerTransform != null)
        {
            CharacterController controller = playerTransform.GetComponent<CharacterController>();

            if (controller != null)
            {
                controller.enabled = false;
                playerTransform.position = lastCheckpoint;
                controller.enabled = true;
            }
            else
            {
                playerTransform.position = lastCheckpoint;
            }
        }

        SetHint("You failed... Try again!");
        UpdateUI();
        UpdateDistanceUI();
    }

    // ================= SCORE HELPERS =================

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    public void SubtractScore(int amount)
    {
        score -= amount;
        if (score < 0) score = 0;
        UpdateUI();
    }

    // ================= HINT HELPERS =================

    public void UnlockCake6Hint()
    {
        cake6HintUnlocked = true;
    }

    public void SetPublicHint(string msg)
    {
        SetHint(msg);
    }

    // ================= WIN / LOSE =================

    public void WinGame()
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        if (winScoreText != null)
            winScoreText.text = "Score: " + score;

        if (winPanel != null)
            winPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoseGame()
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        if (loseScoreText != null)
            loseScoreText.text = "Score: " + score;

        if (losePanel != null)
            losePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ================= UI =================

    void SetHint(string msg)
    {
        if (hintText != null)
            hintText.text = msg;
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (cakeText != null)
            cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes;
    }

    // ================= BUTTONS =================

    public void RestartScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("1.MainMenu");
    }
    // Hàm này gán vào Button Trả lời đúng
    public void OnClickCorrectAnswer()
    {
        cake6HintUnlocked = true; // Mở khóa hint
        Time.timeScale = 1f; // Chạy lại thời gian
        riddlePanel.SetActive(false); // Ẩn bảng câu hỏi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetHint("Chính xác! Cake 6 đang dính trên một cái cây gần ghế đá.");
    }

    // Hàm này gán vào Button Trả lời sai
    public void OnClickWrongAnswer()
    {
        SubtractScore(10); // Phạt điểm
        SetHint("Sai rồi! Thử lại nhanh lên, sắp hết giờ!");
    }
}