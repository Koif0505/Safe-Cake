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

        [Header("Turn Settings")]
        public float turnSpeed = 120f;
        public bool allowJoystickTurn = false;

        [Header("Audio & Effects (Keep Asset Logic)")]
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

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        void Start()
        {
            controller = GetComponent<CharacterController>();

            if (cam == null && Camera.main != null)
                cam = Camera.main.transform;

            AssignAnimationIDs();

#if UNITY_EDITOR || UNITY_STANDALONE
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
#endif
        }

        void Update()
        {
            if (GameManager.Instance != null &&
                (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded))
            {
                return;
            }

            HandleLookByPlatform();
            HandleMovement();
            SyncAnimator();
        }

        void HandleLookByPlatform()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandlePCLook();
#else
            HandleMobileBodyFollowCamera();
#endif
        }

        void HandlePCLook()
        {
            if (cam == null) return;

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);

            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        void HandleMobileBodyFollowCamera()
        {
            if (cam == null) return;

            Vector3 flatForward = cam.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            transform.rotation = targetRotation;
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;

                if (animator != null)
                    animator.SetBool(_animIDJump, false);
            }

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move = Vector3.zero;

            // D = đi tới
            if (Input.GetKey(KeyCode.JoystickButton3) ||
                Input.GetKey(KeyCode.UpArrow) ||
                Input.GetKey(KeyCode.W))
            {
                move += forward;
            }

            // B = trái
            if (Input.GetKey(KeyCode.JoystickButton1) ||
                Input.GetKey(KeyCode.LeftArrow) ||
                Input.GetKey(KeyCode.A))
            {
                move -= right;
            }

            // A = phải
            if (Input.GetKey(KeyCode.JoystickButton0) ||
                Input.GetKey(KeyCode.RightArrow) ||
                Input.GetKey(KeyCode.D))
            {
                move += right;
            }

            move = Vector3.ClampMagnitude(move, 1f);

            float targetSpeed = Input.GetKey(KeyCode.LeftShift)
                ? moveSpeed * runMultiplier
                : moveSpeed;

            if (move == Vector3.zero)
                targetSpeed = 0f;

            controller.Move(move * targetSpeed * Time.deltaTime);

            bool jumpPressed =
                Input.GetKeyDown(KeyCode.JoystickButton2) ||
                Input.GetKeyDown(KeyCode.Space);

            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (animator != null)
                    animator.SetBool(_animIDJump, true);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetSpeed,
                Time.deltaTime * SpeedChangeRate
            );
        }

        void SyncAnimator()
        {
            if (animator == null) return;

            animator.SetFloat(_animIDSpeed, _animationBlend);
            animator.SetBool(_animIDGrounded, isGrounded);
            animator.SetFloat(_animIDMotionSpeed, 1f);

            if (!isGrounded && velocity.y < -5f)
                animator.SetBool(_animIDFreeFall, true);
            else
                animator.SetBool(_animIDFreeFall, false);
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

            if (animator != null)
                animator.SetBool(_animIDJump, true);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f &&
                FootstepAudioClips != null &&
                FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(
                    FootstepAudioClips[index],
                    transform.position,
                    FootstepAudioVolume
                );
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f &&
                LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(
                    LandingAudioClip,
                    transform.position,
                    FootstepAudioVolume
                );
            }
        }
    }
}