using UnityEngine;

namespace FCG
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControlHybrid : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 6f;
        public float runMultiplier = 1.2f;
        public float mouseSensitivity = 100f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;

        [Header("Audio & Effects")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        public float SpeedChangeRate = 10.0f;

        [Header("References")]
        public Animator animator;
        public Transform cam; // Kéo Main Camera vào đây

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

            // Tự động tìm Camera nếu chưa gán
            if (cam == null) cam = transform.Find("Camera");
            if (cam == null) cam = Camera.main.transform;

            AssignAnimationIDs();

            // Khóa chuột trên máy tính để quay tâm
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            // Không cho di chuyển nếu game chưa bắt đầu hoặc đã kết thúc
            if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded))
                return;

            HandleLook();
            HandleMovement();
            SyncAnimator();
        }

        void HandleLook()
        {
            // Nếu đang chạy trên PC (để test hoặc chơi thường)
            // Lưu ý: Trên điện thoại, "Tracked Pose Driver" sẽ tự động ghi đè xRotation của Cam
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f); // Giới hạn góc nhìn lên xuống

            // Quay Camera lên xuống (X-axis)
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Quay thân nhân vật trái phải (Y-axis)
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

            // 1. Lấy Input mặc định (Keyboard WASD / Joystick)
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // 2. Ép thêm nút Controller VR (Chế độ @+C)
            if (Input.GetKey(KeyCode.JoystickButton0)) moveZ = 1f;  // Nút A -> Tiến
            if (Input.GetKey(KeyCode.JoystickButton1)) moveZ = -1f; // Nút B -> Lùi
            if (Input.GetKey(KeyCode.JoystickButton2)) moveX = -1f; // Nút C -> Trái
            if (Input.GetKey(KeyCode.JoystickButton3)) moveX = 1f;  // Nút D -> Phải

            // Tính hướng di chuyển dựa trên hướng nhân vật đang đứng
            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            move = Vector3.ClampMagnitude(move, 1f);

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * runMultiplier : moveSpeed;
            if (move == Vector3.zero) targetSpeed = 0f;

            controller.Move(move * targetSpeed * Time.deltaTime);

            // 3. Nhảy (Space hoặc Nút @ / Nút cò tay cầm)
            bool jumpPressed = Input.GetKeyDown(KeyCode.Space) ||
                               Input.GetKeyDown(KeyCode.JoystickButton4) ||
                               Input.GetKeyDown(KeyCode.JoystickButton10);

            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetBool(_animIDJump, true);
            }

            // Áp dụng trọng lực
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Đồng bộ Animation
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

        // Các Event âm thanh gọi từ Animation của nhân vật
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
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
        }
    }
}