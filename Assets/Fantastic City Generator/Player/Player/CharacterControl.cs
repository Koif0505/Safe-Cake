using UnityEngine;

namespace FCG
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControlHybrid : MonoBehaviour
    {
        [Header("Movement Settings (Hybrid Logic)")]
        public float moveSpeed = 6f;
        public float runMultiplier = 1.2f;
        public float mouseSensitivity = 100f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;

        [Header("Audio & Effects (Keep Asset Logic)")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        public float SpeedChangeRate = 10.0f;

        [Header("References")]
        public Animator animator;
        public Transform cam;

        // Private variables
        private float xRotation = 0f;
        private float yRotation = 0f;
        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;

        private int _animIDSpeed, _animIDGrounded, _animIDJump, _animIDFreeFall, _animIDMotionSpeed;
        private float _animationBlend;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            if (cam == null) cam = transform.Find("Camera");
            if (cam == null) cam = Camera.main.transform;

            AssignAnimationIDs();

            // Khóa chuột trên máy tính
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            // Kiểm tra trạng thái GameManager
            if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded))
                return;

            HandleLook();
            HandleMovement();
            SyncAnimator();
        }

        void HandleLook()
        {
            // --- LOGIC CHO MÁY TÍNH ---
            // Mouse X xoay người (Y-Axis), Mouse Y xoay camera (X-Axis)
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);

            // Lưu ý: Trên VR, Component "Tracked Pose Driver" trên Camera sẽ tự động ghi đè góc quay này 
            // dựa trên cảm biến điện thoại, nên không lo bị xung đột.
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
                if (animator != null) animator.SetBool(_animIDJump, false);
            }

            // 1. LẤY INPUT MẶC ĐỊNH (Keyboard WASD / Joystick Axis)
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // 2. GÁN NÚT CONTROLLER VR (Theo yêu cầu của cậu - Chế độ @+C)
            // Nút A (Tiến) -> Z = 1
            if (Input.GetKey(KeyCode.JoystickButton0)) moveZ = 1f;
            // Nút B (Lùi) -> Z = -1
            if (Input.GetKey(KeyCode.JoystickButton1)) moveZ = -1f;
            // Nút C (Trái) -> X = -1
            if (Input.GetKey(KeyCode.JoystickButton2)) moveX = -1f;
            // Nút D (Phải) -> X = 1
            if (Input.GetKey(KeyCode.JoystickButton3)) moveX = 1f;

            // 3. TÍNH TOÁN DI CHUYỂN THEO HƯỚNG NHÌN (Cam forward)
            // Nhìn hướng nào, bấm Tiến (A) sẽ đi về hướng đó.
            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            move = Vector3.ClampMagnitude(move, 1f);

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * runMultiplier : moveSpeed;
            if (move == Vector3.zero) targetSpeed = 0f;

            controller.Move(move * targetSpeed * Time.deltaTime);

            // 4. NHẢY (Phím Space HOẶC Nút @ trên tay cầm)
            // Nút @ thường là JoystickButton4 hoặc JoystickButton10 trên Android
            bool jumpPressed = Input.GetKeyDown(KeyCode.Space) ||
                               Input.GetKeyDown(KeyCode.JoystickButton4) ||
                               Input.GetKeyDown(KeyCode.JoystickButton10);

            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetBool(_animIDJump, true);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Cập nhật biến Blend cho Animation
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        }

        void SyncAnimator()
        {
            if (animator == null) return;
            animator.SetFloat(_animIDSpeed, _animationBlend);
            animator.SetBool(_animIDGrounded, isGrounded);
            animator.SetFloat(_animIDMotionSpeed, 1f);

            if (!isGrounded && velocity.y < -5f) animator.SetBool(_animIDFreeFall, true);
            else animator.SetBool(_animIDFreeFall, false);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        // --- EVENTS TỪ ANIMATION (GIỮ NGUYÊN) ---
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
            }
        }
    }
}