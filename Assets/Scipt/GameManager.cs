using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using FCG;

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
        if (collectedCakes == 1)
        {
            SetHint("Jump to the plank, then climb up to Cake 2.");
        }
        else if (collectedCakes == 2)
        {
            SetHint("Go down to the lower roof, then look to the right of the elevator-facing direction.");
        }
        else if (collectedCakes == 3)
        {
            SetHint("Drop down and move to the narrow high plank to reach Cake 4.");
        }
        else if (collectedCakes == 4)
        {
            SetHint("Night falls. Get ready for Cake 5.");

            if (dayNight != null && !switchedToNight)
            {
                dayNight.SetNightMode();
                switchedToNight = true;
            }
        }
        else if (collectedCakes == 5)
        {
            SetHint("Answer the question to unlock Cake 6.");
        }
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
}