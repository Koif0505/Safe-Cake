using UnityEngine;

public class RightHandController : MonoBehaviour
{
    public float movementSpeed = 3.5f; // Tốc độ di chuyển của Nhi
    public float interactionDistance = 2.0f; // Khoảng cách để có thể nhặt bánh
    public Transform playerCamera; // Tham chiếu đến Main Camera
    public ScoreManager scoreManager; // Kéo vật thể GameManager vào đây

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. Nhận tín hiệu di chuyển từ Joystick hoặc WASD
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 2. Tính toán hướng đi dựa theo hướng mắt nhìn VR
        Vector3 forward = playerCamera.forward;
        Vector3 right = playerCamera.right;
        forward.y = 0; right.y = 0; // Giữ thăng bằng trên ván

        Vector3 move = (forward * z + right * x).normalized;
        controller.SimpleMove(move * movementSpeed);

        // 3. Nhấn nút Action (Nút A hoặc Space) để nhặt bánh
        if (Input.GetButtonDown("Action"))
        {
            CheckForCake();
        }
    }

    void CheckForCake()
    {
        // Tìm tất cả các vật thể có Tag là Cake trong Scene
        GameObject[] cakes = GameObject.FindGameObjectsWithTag("Cake");

        foreach (GameObject cake in cakes)
        {
            // Kiểm tra khoảng cách giữa Nhi và cái bánh
            if (Vector3.Distance(transform.position, cake.transform.position) < interactionDistance)
            {
                Debug.Log("Ăn bánh thành công!");

                // Lệnh quan trọng: Gọi hàm cộng điểm từ ScoreManager
                if (scoreManager != null)
                {
                    scoreManager.AddScore(10); // Mỗi cái bánh được 10 điểm
                }

                cake.SetActive(false); // Bánh biến mất
                break; // Thoát vòng lặp sau khi ăn được 1 cái bánh để tránh lỗi
            }
        }
    }
}