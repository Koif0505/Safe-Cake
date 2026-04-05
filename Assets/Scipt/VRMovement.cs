using UnityEngine;

public class VRMovement : MonoBehaviour
{
    public float moveSpeed = 6.0f;
    public float jumpForce = 6.0f;
    private CharacterController controller;
    private Vector3 velocity;
    public Transform camTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (camTransform == null) camTransform = Camera.main.transform;
    }

    void Update()
    {
        if (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded) return;

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        // 1. TIẾN/LÙI: Sử dụng cần Analog trái (Axis Vertical)
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = transform.forward * vertical;

        // 2. SANG TRÁI/PHẢI: Nút Vuông (Button 2) và Tròn (Button 1)
        if (Input.GetKey(KeyCode.JoystickButton2)) move -= transform.right; // Vuông
        if (Input.GetKey(KeyCode.JoystickButton1)) move += transform.right; // Tròn

        controller.Move(move.normalized * moveSpeed * Time.deltaTime);

        // 3. NHẢY: Bấm nút Tam giác (Joystick Button 3)
        if (isGrounded && Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    private bool isGrounded;
}