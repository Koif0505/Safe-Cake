using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UnifiedPlayerInput : MonoBehaviour
{
    public static UnifiedPlayerInput Instance { get; private set; }

    [Header("Look Settings")]
    public float gamepadLookSpeed = 180f;
    public float mouseLookSensitivity = 100f;
    public bool invertY = false;

    [Header("Movement Thresholds")]
    [Range(0.05f, 1f)] public float stickDeadzone = 0.2f;

    [Header("Legacy Joystick Mapping")]
    public int legacyButtonRight = 0;
    public int legacyButtonLeft = 1;
    public int legacyButtonJump = 2;
    public int legacyButtonForward = 3;
    public int legacyButtonPause = 7;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public float GetLookX(float deltaTime)
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            float stickX = Gamepad.current.rightStick.ReadValue().x;
            if (Mathf.Abs(stickX) > stickDeadzone)
                return stickX * gamepadLookSpeed * deltaTime;
        }
#endif

        float mouseX = Input.GetAxisRaw("Mouse X");
        return mouseX * mouseLookSensitivity * deltaTime;
    }

    public float GetLookY(float deltaTime)
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            float stickY = Gamepad.current.rightStick.ReadValue().y;
            if (Mathf.Abs(stickY) > stickDeadzone)
            {
                float value = stickY * gamepadLookSpeed * deltaTime;
                return invertY ? -value : value;
            }
        }
#endif

        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseLookSensitivity * deltaTime;
        return invertY ? -mouseY : mouseY;
    }

    public bool ForwardHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 move = Gamepad.current.leftStick.ReadValue();
            bool dpadUp = Gamepad.current.dpad.up.isPressed;
            return move.y > stickDeadzone || dpadUp;
        }
#endif

        return Input.GetKey(KeyCode.UpArrow) ||
               Input.GetKey(KeyCode.W) ||
               GetLegacyJoy(legacyButtonForward);
    }

    public bool LeftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 move = Gamepad.current.leftStick.ReadValue();
            bool dpadLeft = Gamepad.current.dpad.left.isPressed;
            return move.x < -stickDeadzone || dpadLeft;
        }
#endif

        return Input.GetKey(KeyCode.LeftArrow) ||
               Input.GetKey(KeyCode.A) ||
               GetLegacyJoy(legacyButtonLeft);
    }

    public bool RightHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 move = Gamepad.current.leftStick.ReadValue();
            bool dpadRight = Gamepad.current.dpad.right.isPressed;
            return move.x > stickDeadzone || dpadRight;
        }
#endif

        return Input.GetKey(KeyCode.RightArrow) ||
               Input.GetKey(KeyCode.D) ||
               GetLegacyJoy(legacyButtonRight);
    }

    public bool JumpPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            return Gamepad.current.buttonSouth.wasPressedThisFrame ||
                   Gamepad.current.rightShoulder.wasPressedThisFrame;
        }
#endif

        return Input.GetKeyDown(KeyCode.Space) || GetLegacyJoyDown(legacyButtonJump);
    }

    public bool MenuLeftPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 move = Gamepad.current.leftStick.ReadValue();
            return Gamepad.current.dpad.left.wasPressedThisFrame ||
                   Gamepad.current.leftShoulder.wasPressedThisFrame ||
                   move.x < -0.75f;
        }
#endif

        return Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.A) ||
               GetLegacyJoyDown(legacyButtonLeft);
    }

    public bool MenuRightPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 move = Gamepad.current.leftStick.ReadValue();
            return Gamepad.current.dpad.right.wasPressedThisFrame ||
                   Gamepad.current.rightShoulder.wasPressedThisFrame ||
                   move.x > 0.75f;
        }
#endif

        return Input.GetKeyDown(KeyCode.RightArrow) ||
               Input.GetKeyDown(KeyCode.D) ||
               GetLegacyJoyDown(legacyButtonRight);
    }

    public bool MenuDownPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 move = Gamepad.current.leftStick.ReadValue();
            return Gamepad.current.dpad.down.wasPressedThisFrame || move.y < -0.75f;
        }
#endif

        return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
    }

    public bool PausePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            return Gamepad.current.startButton.wasPressedThisFrame;
        }
#endif

        return Input.GetKeyDown(KeyCode.Escape) || GetLegacyJoyDown(legacyButtonPause);
    }

    public bool IsNavigationIdle()
    {
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 move = Gamepad.current.leftStick.ReadValue();
            return move.sqrMagnitude < 0.01f &&
                   !Gamepad.current.dpad.left.isPressed &&
                   !Gamepad.current.dpad.right.isPressed &&
                   !Gamepad.current.dpad.down.isPressed;
        }
#endif

        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.1f &&
               Mathf.Abs(Input.GetAxisRaw("Vertical")) < 0.1f &&
               !Input.anyKey;
    }

    bool GetLegacyJoy(int index)
    {
        if (index < 0) return false;
        return Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + index));
    }

    bool GetLegacyJoyDown(int index)
    {
        if (index < 0) return false;
        return Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + index));
    }
}
