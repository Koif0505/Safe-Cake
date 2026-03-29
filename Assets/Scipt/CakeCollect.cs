using UnityEngine;

public class CakeCollect : MonoBehaviour
{
    public int cakeIndex = 1;
    public int scoreValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance.CanCollectCake(cakeIndex))
        {
            GameManager.Instance.CollectCake(cakeIndex, scoreValue);
            gameObject.SetActive(false);
        }
        else
        {
            GameManager.Instance.ShowWrongOrderHint(cakeIndex);
        }
    }
}