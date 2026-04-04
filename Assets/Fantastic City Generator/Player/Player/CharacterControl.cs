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

        [Header("Look Settings")]
        public float verticalClamp = 70f;

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
        private float animationBlend;
        private float xRotation = 0f;

        private int animIDSpeed;
        private int animIDGrounded;
        private int animIDJump;
        private int animIDFreeFall;
        private int animIDMotionSpeed;

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
            if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.IsGameEnded))
                return;

            HandleLook();
            HandleMovement();
            SyncAnimator();
        }

        void HandleLook()
        {
            if (cam == null || UnifiedPlayerInput.Instance == null)
                return;

            float lookX = UnifiedPlayerInput.Instance.GetLookX(Time.deltaTime);
            float lookY = UnifiedPlayerInput.Instance.GetLookY(Time.deltaTime);

            xRotation -= lookY;
            xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * lookX);
        }

        void HandleMovement()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
                if (animator != null) animator.SetBool(animIDJump, false);
            }

            Vector3 forward = cam != null ? cam.forward : transform.forward;
            Vector3 right = cam != null ? cam.right : transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move = Vector3.zero;
            UnifiedPlayerInput input = UnifiedPlayerInput.Instance;

            if (input != null)
            {
                if (input.ForwardHeld()) move += forward;
                if (input.LeftHeld()) move -= right;
                if (input.RightHeld()) move += right;
            }
            else
            {
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) move += forward;
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) move -= right;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) move += right;
            }

            if (move.magnitude > 1f) move.Normalize();

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * runMultiplier : moveSpeed;
            if (move == Vector3.zero) targetSpeed = 0f;

            controller.Move(move * targetSpeed * Time.deltaTime);

            if (move != Vector3.zero && animator != null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, targetRotation, Time.deltaTime * 12f);
            }

            bool jumpPressed = input != null
                ? input.JumpPressedThisFrame()
                : Input.GetKeyDown(KeyCode.Space);

            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetBool(animIDJump, true);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        }

        void SyncAnimator()
        {
            if (animator == null) return;
            animator.SetFloat(animIDSpeed, animationBlend);
            animator.SetBool(animIDGrounded, isGrounded);
            animator.SetFloat(animIDMotionSpeed, 1f);
            animator.SetBool(animIDFreeFall, !isGrounded && velocity.y < -5f);
        }

        void AssignAnimationIDs()
        {
            animIDSpeed = Animator.StringToHash("Speed");
            animIDGrounded = Animator.StringToHash("Grounded");
            animIDJump = Animator.StringToHash("Jump");
            animIDFreeFall = Animator.StringToHash("FreeFall");
            animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        public void Jump()
        {
            if (!controller.isGrounded) return;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null) animator.SetBool(animIDJump, true);
        }

        void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips != null && FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }

        void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
            }
        }
    }
}
