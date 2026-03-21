using UnityEngine;

public class CakeCollect : MonoBehaviour
{
    public int scoreValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();

            if (scoreManager != null)
            {
                scoreManager.AddScore(scoreValue);
            }

            gameObject.SetActive(false);
        }
    }
}