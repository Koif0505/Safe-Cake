using UnityEngine;

namespace FCG
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControlHybrid : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 6f;
        public float runMultiplier = 1.2f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;

        [Header("PC Look Settings")]
        public float mouseSensitivity = 100f;

        [Header("Audio & Effects")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        public float SpeedChangeRate = 10.0f;

        [Header("References")]
        public Animator animator;
        public Transform cam;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private float _animationBlend;
        private float xRotation = 0f;

        private int _animIDSpeed, _animIDGrounded, _animIDJump, _animIDFreeFall, _animIDMotionSpeed;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            AssignAnimationIDs();

#if UNITY_EDITOR || UNITY_STANDALONE
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
#endif
        }

        void Update()
        {
            if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded)) return;

            HandlePCLook();
            HandleMovement();
            SyncAnimator();
        }

        void HandlePCLook()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            // Chỉ PC mới dùng chuột xoay Camera. Mobile VR thì Cardboard tự lo phần xoay Camera.
            if (cam == null) return;
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);

            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
#endif
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
                if (animator != null) animator.SetBool(_animIDJump, false);
            }

            // LẤY HƯỚNG TỪ CAMERA (Áp dụng cho cả PC và VR)
            Vector3 forward = cam != null ? cam.forward : transform.forward;
            Vector3 right = cam != null ? cam.right : transform.right;

            // QUAN TRỌNG: Bỏ trục Y và Normalize để sải bước luôn dài và nhanh y hệt PC
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move = Vector3.zero;

            // D = ĐI TỚI
            if (Input.GetKey(KeyCode.JoystickButton3) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                move += forward;

            // B = TRÁI
            if (Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                move -= right;

            // A = PHẢI
            if (Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                move += right;

            if (move.magnitude > 1f) move.Normalize();

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * runMultiplier : moveSpeed;
            if (move == Vector3.zero) targetSpeed = 0f;

            // Di chuyển nhân vật
            controller.Move(move * targetSpeed * Time.deltaTime);

            // XOAY MODEL NHÂN VẬT (Chỉ xoay hình ảnh 3D, không xoay Camera để tránh lỗi chóng mặt VR)
            if (move != Vector3.zero && animator != null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, targetRotation, Time.deltaTime * 12f);
            }

            // C = NHẢY
            bool jumpPressed = Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.Space);
            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetBool(_animIDJump, true);
            }

            // Rơi xuống
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        }

        void SyncAnimator()
        {
            if (animator == null) return;
            animator.SetFloat(_animIDSpeed, _animationBlend);
            animator.SetBool(_animIDGrounded, isGrounded);
            animator.SetFloat(_animIDMotionSpeed, 1f);
            animator.SetBool(_animIDFreeFall, !isGrounded && velocity.y < -5f);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        public void Jump()
        {
            if (!controller.isGrounded) return;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null) animator.SetBool(_animIDJump, true);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips != null && FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
            }
        }
    }
}