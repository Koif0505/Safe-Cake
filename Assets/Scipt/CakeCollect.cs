using UnityEngine;

public class CakeCollect : MonoBehaviour
{
    public int cakeIndex = 1;
    public int scoreValue = 100;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CanCollectCake(cakeIndex))
        {
            collected = true;
            GameManager.Instance.CollectCake(cakeIndex, scoreValue);
            gameObject.SetActive(false);
        }
        else
        {
            GameManager.Instance.ShowWrongOrderHint(cakeIndex);
        }
    }
}