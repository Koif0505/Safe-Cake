using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCamera;

    void Start()
    {
        // Tìm camera chính. Cache lại để tối ưu cho Mobile
        mainCamera = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Làm cho Canvas luôn xoay mặt về phía Camera
        // Cộng thêm transform.position để Canvas không bị lật ngược (đặc thù của UI World Space)
        transform.LookAt(transform.position + mainCamera.forward);
    }
}