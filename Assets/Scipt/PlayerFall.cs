using UnityEngine;

public class PlayerFall : MonoBehaviour
{
    public float fallHeight = -10f;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
            return;

        if (transform.position.y < fallHeight)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseGame();
            }
        }
    }
}