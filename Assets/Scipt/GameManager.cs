using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int totalCakes = 1;
    public bool useTimer = false;
    public float timeRemaining = 120f;
    public bool requireFinishFlag = false;

    public int collectedCakes = 0;
    public int score = 0;
    public bool IsGameEnded { get; private set; } = false;
    public bool allCakesCollected = false;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI cakeText;
    public TextMeshProUGUI timerText;

    public GameObject winPanel;
    public GameObject losePanel;

    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI loseScoreText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
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

    public void WinGame()
    {
        if (IsGameEnded) return;

        IsGameEnded = true;

        if (useTimer)
        {
            score += Mathf.RoundToInt(timeRemaining * 5f);
        }

        score += 500;
        UpdateUI();

        if (winScoreText != null)
            winScoreText.text = "Final Score: " + score;

        if (winPanel != null)
            winPanel.SetActive(true);

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

        Debug.Log("GAME OVER");
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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