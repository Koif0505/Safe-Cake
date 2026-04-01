using UnityEngine;

public class CakeCollect : MonoBehaviour
{
    public int cakeIndex = 1;
    public int scoreValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        // Chỉ xử lý nếu là người chơi (Player) chạm vào
        if (!other.CompareTag("Player")) return;

        // Kiểm tra xem cái bánh này có đúng thứ tự không
        if (GameManager.Instance.CanCollectCake(cakeIndex))
        {
            GameManager.Instance.CollectCake(cakeIndex, scoreValue);
            gameObject.SetActive(false);
        }
        else
        {
            // Nếu sai thứ tự, gọi hàm báo lỗi bên GameManager
            GameManager.Instance.ShowWrongOrderHint(cakeIndex);
        }
    }
}