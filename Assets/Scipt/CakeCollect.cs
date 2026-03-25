using UnityEngine;

public class CakeCollect : MonoBehaviour
{
    public int scoreValue = 100;
    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            collected = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectCake(scoreValue);
            }

            gameObject.SetActive(false);
        }
    }
}