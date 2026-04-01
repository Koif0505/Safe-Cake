using UnityEngine;

public class VRMovement : MonoBehaviour
{
    public float moveSpeed = 4.0f;
    public float jumpForce = 6.0f;
    private CharacterController controller;
    private Vector3 velocity;
    private Transform camTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
    }

    void Update()
    {
        if (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded) return;

        // 1. ?I T?I: Gi? nút D (Joystick Button 3)
        if (Input.GetKey(KeyCode.JoystickButton3))
        {
            Vector3 forward = camTransform.forward;
            forward.y = 0; // Không cho bay lên tr?i khi nhìn lên
            controller.Move(forward.normalized * moveSpeed * Time.deltaTime);
        }

        // 2. NH?Y: B?m nút C (Joystick Button 2)
        if (controller.isGrounded)
        {
            velocity.y = -2f;
            if (Input.GetKeyDown(KeyCode.JoystickButton2))
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
            }
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}