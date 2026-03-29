using UnityEngine;
using UnityEngine.UI;

public class UIArrowPointer : MonoBehaviour
{
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // 1. Kiểm tra nếu game kết thúc hoặc GameManager chưa sẵn sàng thì ẩn mũi tên
        if (GameManager.Instance == null || GameManager.Instance.IsGameEnded)
        {
            gameObject.SetActive(false);
            return;
        }

        // 2. Lấy mục tiêu Cake hiện tại
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // 3. Tính hướng từ Player đến Cake
        Vector3 dir = target.position - GameManager.Instance.playerTransform.position;

        // 4. Chuyển hướng đó về không gian của Camera (để xoay theo hướng nhìn)
        Vector3 localDir = GameManager.Instance.cameraTransform.InverseTransformDirection(dir);

        // Chỉ quan tâm đến hướng Trái/Phải (X) và Trên/Dưới (Y) trên màn hình
        float angle = Mathf.Atan2(localDir.x, localDir.y) * Mathf.Rad2Deg;

        // 5. Xoay mũi tên
        // Thêm +180 vì cái DropdownArrow của bạn mặc định nó chỉ xuống dưới (6 giờ)
        rectTransform.localRotation = Quaternion.Euler(0, 0, -angle + 180f);
    }

    Transform GetCurrentTarget()
    {
        int idx = GameManager.Instance.nextCakeIndex - 1;
        if (GameManager.Instance.cakeTargets != null && idx >= 0 && idx < GameManager.Instance.cakeTargets.Length)
        {
            return GameManager.Instance.cakeTargets[idx];
        }
        return null;
    }
}