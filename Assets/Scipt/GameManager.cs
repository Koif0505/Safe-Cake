using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Config")]
    public bool useTimer = false;
    public float timeRemaining = 180f;
    public int totalCakes = 10;

    [Header("Runtime")]
    public int collectedCakes = 0;
    public int score = 0;
    public bool IsGameEnded { get; private set; } = false;
    private bool finishUnlocked = false;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI cakeText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI hintText;

    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI loseScoreText;

    [Header("Scene Objects")]
    public GameObject finishFlag;
    public Light directionalLight;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ApplyModeSettings();
        UpdateUI();

        if (finishFlag != null) finishFlag.SetActive(false);
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

    void ApplyModeSettings()
    {
        if (GameSession.selectedMode == "Safe")
        {
            useTimer = false;
            timeRemaining = 0f;
            totalCakes = 10;
            SetHint("Collect all cakes carefully.");
        }
        else if (GameSession.selectedMode == "Challenge")
        {
            useTimer = true;
            timeRemaining = 180f;
            totalCakes = 10;
            SetHint("Collect all cakes before time runs out!");
        }
    }

    public void CollectCake(int scoreValue)
    {
        if (IsGameEnded) return;

        collectedCakes++;
        score += scoreValue;

        TriggerProgressionEvents();
        UpdateUI();

        if (collectedCakes >= totalCakes)
        {
            UnlockFinish();
        }
    }

    void TriggerProgressionEvents()
    {
        if (GameSession.selectedMode == "Safe")
        {
            if (collectedCakes == 3)
                SetHint("Be careful on narrow beams.");

            if (collectedCakes == 5)
                SetHint("The wind is getting stronger.");

            if (collectedCakes == 8)
            {
                SetHint("It's getting darker...");
                MakeSceneDarker();
            }
        }
        else if (GameSession.selectedMode == "Challenge")
        {
            if (collectedCakes == 3)
                SetHint("Hurry up!");

            if (collectedCakes == 6)
            {
                SetHint("The challenge is getting harder!");
                MakeSceneDarker();
            }

            if (collectedCakes == 8)
                SetHint("Almost there!");
        }
    }

    void UnlockFinish()
    {
        finishUnlocked = true;

        if (finishFlag != null)
            finishFlag.SetActive(true);

        SetHint("Go to Finish!");
    }

    public void ReachFinishFlag()
    {
        if (IsGameEnded) return;

        if (finishUnlocked)
            WinGame();
    }

    void MakeSceneDarker()
    {
        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Max(0.2f, directionalLight.intensity * 0.5f);
        }
    }

    public void WinGame()
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        int finalScore = score + 500;

        if (useTimer)
            finalScore += Mathf.RoundToInt(timeRemaining * 5f);

        if (winScoreText != null)
            winScoreText.text = "Final Score: " + finalScore;

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

    void SetHint(string message)
    {
        if (hintText != null)
            hintText.text = message;
    }

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