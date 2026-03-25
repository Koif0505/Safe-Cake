using UnityEngine;

namespace FCG
{
    public class CharacterControl : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float runMultiplier = 1.2f;
        public float mouseSensitivity = 100f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;

        private float xRotation = 0f;
        private float yRotation = 0f;

        private Transform cam;
        private CharacterController controller;

        private Vector3 velocity;
        private bool isGrounded;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            controller = GetComponent<CharacterController>();
            cam = transform.Find("Camera");

            if (cam == null)
            {
                Debug.LogError("Camera child not found inside First Person Player.");
            }

            cam.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
                return;

            HandleLook();
            HandleMovement();
        }

        void HandleLook()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);

            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
#endif
        }

        void HandleMovement()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            move = Vector3.ClampMagnitude(move, 1f);

            float finalSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * runMultiplier : moveSpeed;
            controller.Move(move * finalSpeed * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
#endif
        }
    }
}