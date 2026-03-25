using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int totalCakes = 3;
    public bool useTimer = false;
    public float timeRemaining = 120f;
    public bool requireFinishFlag = false;

    [Header("Runtime")]
    public int collectedCakes = 0;
    public int score = 0;
    public bool IsGameEnded { get; private set; } = false;
    public bool allCakesCollected = false;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI cakeText;
    public TextMeshProUGUI timerText;

    public GameObject winPanel;
    public GameObject losePanel;

    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI loseScoreText;

    [Header("Optional")]
    public GameObject finishFlag;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ApplyModeSettings();
        UpdateUI();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (IsGameEnded) return;

        if (useTimer)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                LoseGame();
            }

            UpdateUI();
        }
    }

    // =============================
    // MODE SETTINGS (NEW VERSION)
    // =============================
    void ApplyModeSettings()
    {
        if (GameSession.selectedMode == "Safe")
        {
            useTimer = false;
            totalCakes = 3;
            timeRemaining = 0f;
            requireFinishFlag = false;
        }
        else if (GameSession.selectedMode == "Challenge")
        {
            useTimer = true;
            totalCakes = 5;
            timeRemaining = 120f;
            requireFinishFlag = true;
        }

        if (finishFlag != null)
        {
            finishFlag.SetActive(requireFinishFlag);
        }

        Debug.Log("Mode: " + GameSession.selectedMode +
                  " | Cakes: " + totalCakes +
                  " | Timer: " + useTimer +
                  " | RequireFlag: " + requireFinishFlag);
    }

    // =============================
    // GAMEPLAY
    // =============================
    public void CollectCake(int scoreValue)
    {
        if (IsGameEnded) return;

        collectedCakes++;
        score += scoreValue;

        if (collectedCakes >= totalCakes)
        {
            allCakesCollected = true;

            if (!requireFinishFlag)
            {
                WinGame();
            }
            else
            {
                Debug.Log("All cakes collected. Go to finish flag.");
            }
        }

        UpdateUI();
    }

    public void ReachFinishFlag()
    {
        if (IsGameEnded) return;

        if (requireFinishFlag && allCakesCollected)
        {
            WinGame();
        }
    }

    // =============================
    // WIN / LOSE
    // =============================
    public void WinGame()
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        // Bonus score
        score += 500;

        if (useTimer)
        {
            score += Mathf.RoundToInt(timeRemaining * 5f);
        }

        UpdateUI();

        if (winScoreText != null)
            winScoreText.text = "Final Score: " + score;

        if (winPanel != null)
            winPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("YOU WIN");
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

        Debug.Log("GAME OVER");
    }

    // =============================
    // RESTART
    // =============================
    public void RestartScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =============================
    // UI UPDATE
    // =============================
    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (cakeText != null)
            cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes;

        if (timerText != null)
        {
            if (useTimer)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timerText.text = $"Time: {minutes:00}:{seconds:00}";
            }
            else
            {
                timerText.text = "";
            }
        }
    }
}