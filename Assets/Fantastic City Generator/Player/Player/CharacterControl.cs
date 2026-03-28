using UnityEngine;

namespace FCG
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControlHybrid : MonoBehaviour
    {
        [Header("Movement Settings (Your Logic)")]
        public float moveSpeed = 6f;
        public float runMultiplier = 1.2f;
        public float mouseSensitivity = 100f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;

        [Header("Audio & Effects (Asset Logic)")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
        public float SpeedChangeRate = 10.0f;

        [Header("References")]
        public Animator animator; // Kéo PlayerArmature vào đây
        public Transform cam;

        // Private variables - Movement
        private float xRotation = 0f;
        private float yRotation = 0f;
        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;

        // Private variables - Animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private float _animationBlend;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            controller = GetComponent<CharacterController>();

            if (cam == null) cam = transform.Find("Camera");

            // Khởi tạo ID hiệu ứng từ Asset
            AssignAnimationIDs();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameEnded)
                return;

            HandleLook();
            HandleMovement();
            SyncAnimator(); // Đồng bộ hóa hoạt ảnh và âm thanh
        }

        void HandleLook()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);

            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
                // Hiệu ứng tiếp đất (Landing)
                if (animator != null) animator.SetBool(_animIDJump, false);
            }

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // Giữ nguyên cách đi của bạn (Strafe chuẩn FPS)
            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            move = Vector3.ClampMagnitude(move, 1f);

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * runMultiplier : moveSpeed;

            // Nếu không bấm phím di chuyển, tốc độ về 0
            if (move == Vector3.zero) targetSpeed = 0f;

            controller.Move(move * targetSpeed * Time.deltaTime);

            // Nhảy
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetBool(_animIDJump, true);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Tính toán Animation Blend (để mượt mà như asset)
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        }

        void SyncAnimator()
        {
            if (animator == null) return;

            // Truyền các thông số vào Animator của PlayerArmature
            animator.SetFloat(_animIDSpeed, _animationBlend);
            animator.SetBool(_animIDGrounded, isGrounded);
            animator.SetFloat(_animIDMotionSpeed, 1f); // Mặc định tốc độ di chuyển hoạt ảnh

            if (!isGrounded && velocity.y < -5f)
            {
                animator.SetBool(_animIDFreeFall, true);
            }
            else
            {
                animator.SetBool(_animIDFreeFall, false);
            }
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        // --- CÁC HÀM ÂM THANH TỪ ASSET (Starter Assets) ---
        // Lưu ý: Các hàm này sẽ được gọi tự động bởi Animation Events trên Model của bạn
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
                }
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