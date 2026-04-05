using UnityEngine;

namespace FCG
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControlHybrid : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 6f;
        public float gravity = -20f;
        public float jumpHeight = 1.2f;
        public float mouseSensitivity = 100f;

        [Header("References")]
        public Animator animator;
        public Transform camTransform; // Kéo MainCamera Left vào đây

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private float xRotation = 0f;
        private float _animationBlend;

        private int _animIDSpeed, _animIDGrounded, _animIDJump, _animIDFreeFall;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            if (camTransform == null && Camera.main != null) camTransform = Camera.main.transform;
            AssignAnimationIDs();
        }

        void Update()
        {
            if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded)) return;

            HandleRotation(); // Rocker đóng vai trò con chuột
            HandleMovement(); // Nút A,B,C,D đóng vai trò di chuyển
            SyncAnimator();
        }

        void HandleRotation()
        {
            // Trong Mode Mouse (@+D), Rocker sẽ phát tín hiệu Mouse X/Y
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);

            if (camTransform != null)
                camTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f) velocity.y = -2f;

            Vector3 move = Vector3.zero;

            // D - Đi lên (JoystickButton3 hoặc W)
            if (Input.GetKey(KeyCode.JoystickButton3) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                move += transform.forward;

            // B - Sang trái (JoystickButton1 hoặc A)
            if (Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                move -= transform.right;

            // A - Sang phải (JoystickButton0 hoặc D)
            if (Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                move += transform.right;

            // C - Nhảy (JoystickButton2 hoặc Space)
            if (Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.Space))
            {
                if (isGrounded)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (animator != null) animator.SetBool(_animIDJump, true);
                }
            }

            controller.Move(move * moveSpeed * Time.deltaTime);

            // Trọng lực
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Animation
            float targetSpeed = move.magnitude > 0 ? moveSpeed : 0;
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * 10f);
        }

        void SyncAnimator()
        {
            if (animator == null) return;
            animator.SetFloat(_animIDSpeed, _animationBlend);
            animator.SetBool(_animIDGrounded, isGrounded);
            animator.SetBool(_animIDFreeFall, !isGrounded && velocity.y < -5f);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
        }
    }
}