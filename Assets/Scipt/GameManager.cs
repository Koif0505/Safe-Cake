using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;

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
    public int nextCakeIndex = 1;
    public bool IsGameEnded { get; private set; } = false;
    private bool finishUnlocked = false;
    private bool switchedToNight = false;

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
    public DayNight dayNight;

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

        if (dayNight != null)
        {
            dayNight.SetDayMode();
        }

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
            SetHint("Collect Cake 1 first.");
        }
        else if (GameSession.selectedMode == "Challenge")
        {
            useTimer = true;
            timeRemaining = 180f;
            totalCakes = 10;
            SetHint("Collect Cake 1 first. Hurry up!");
        }
    }

    public bool CanCollectCake(int cakeIndex)
    {
        return !IsGameEnded && cakeIndex == nextCakeIndex;
    }

    public void CollectCake(int cakeIndex, int scoreValue)
    {
        if (IsGameEnded) return;
        if (cakeIndex != nextCakeIndex) return;

        collectedCakes++;
        score += scoreValue;
        nextCakeIndex++;

        TriggerProgressionEvents();
        UpdateUI();

        if (collectedCakes >= totalCakes)
        {
            UnlockFinish();
        }
        else
        {
            SetHint("Go to Cake " + nextCakeIndex + "!");
        }
    }

    public void ShowWrongOrderHint(int wrongCakeIndex)
    {
        if (IsGameEnded) return;
        SetHint("You must collect Cake " + nextCakeIndex + " first!");
    }

    void TriggerProgressionEvents()
    {
        if (GameSession.selectedMode == "Safe")
        {
            if (collectedCakes == 3)
            {
                SetHint("Keep going carefully. Go to Cake " + nextCakeIndex + "!");
            }

            if (collectedCakes == 5 && !switchedToNight)
            {
                SetHint("It is getting dark... Go to Cake " + nextCakeIndex + "!");
                if (dayNight != null)
                {
                    dayNight.SetNightMode();
                    switchedToNight = true;
                }
            }

            if (collectedCakes == 8)
            {
                SetHint("Almost there. Go to Cake " + nextCakeIndex + "!");
            }
        }
        else if (GameSession.selectedMode == "Challenge")
        {
            if (collectedCakes == 3)
            {
                SetHint("Hurry up! Go to Cake " + nextCakeIndex + "!");
            }

            if (collectedCakes == 5 && !switchedToNight)
            {
                SetHint("Night has fallen. Go to Cake " + nextCakeIndex + "!");
                if (dayNight != null)
                {
                    dayNight.SetNightMode();
                    switchedToNight = true;
                }
            }

            if (collectedCakes == 8)
            {
                SetHint("Almost there! Go to Cake " + nextCakeIndex + "!");
            }
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