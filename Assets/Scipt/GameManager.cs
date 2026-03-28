using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    public Transform playerTransform;

    [Header("Game State")]
    public int score = 0;
    public int collectedCakes = 0;
    public int totalCakes = 10;
    public int nextCakeIndex = 1;
    public bool IsGameEnded { get; private set; } = false;

    [Header("Checkpoint")]
    private Vector3 lastCheckpoint;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI cakeText;
    public TextMeshProUGUI hintText;

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
        lastCheckpoint = playerTransform.position;

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);

        UpdateUI();
        SetHint("Go to Cake 1");
    }

    void Update()
    {
        if (IsGameEnded) return;

        // R?i xu?ng
        if (playerTransform.position.y < -10f)
        {
            Respawn(15);
        }
    }

    // ================= CAKE =================

    public bool CanCollectCake(int index)
    {
        return index == nextCakeIndex;
    }

    public void CollectCake(int index, int scoreValue)
    {
        if (IsGameEnded) return;
        if (index != nextCakeIndex) return;

        collectedCakes++;
        score += scoreValue;
        nextCakeIndex++;

        // checkpoint
        lastCheckpoint = playerTransform.position;

        UpdateUI();

        if (collectedCakes >= totalCakes)
        {
            SetHint("Go to Finish!");
        }
        else
        {
            SetHint("Go to Cake " + nextCakeIndex);
        }
    }

    public void ShowWrongOrderHint(int index)
    {
        SetHint("Collect Cake " + nextCakeIndex + " first!");
    }

    // ================= RESPAWN =================

    public void Respawn(int penalty)
    {
        if (IsGameEnded) return;

        score -= penalty;
        if (score < 0) score = 0;

        playerTransform.position = lastCheckpoint;

        SetHint("You failed... Try again!");
        UpdateUI();
    }

    // ================= WIN / LOSE =================

    public void WinGame()
    {
        IsGameEnded = true;

        if (winScoreText)
            winScoreText.text = "Score: " + score;

        if (winPanel)
            winPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoseGame()
    {
        IsGameEnded = true;

        if (loseScoreText)
            loseScoreText.text = "Score: " + score;

        if (losePanel)
            losePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ================= UI =================

    void SetHint(string msg)
    {
        if (hintText) hintText.text = msg;
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (cakeText) cakeText.text = "Cakes: " + collectedCakes + "/" + totalCakes;
    }
}