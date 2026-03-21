using UnityEngine;
using TMPro; // Thư viện để điều khiển TextMeshPro

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText; // Kéo ScoreText vào đây
    private int currentScore = 0;

    public void AddScore(int points)
    {
        currentScore += points;
        scoreText.text = "Score: " + currentScore;
        Debug.Log("Điểm hiện tại: " + currentScore);
    }
}