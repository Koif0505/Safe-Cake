using UnityEngine;
using System.Text;

public class ControllerFullProbe : MonoBehaviour
{
    [Header("Logging")]
    public bool logMouseAxes = true;
    public bool logCommonAxes = true;
    public bool logJoystickButtons = true;
    public bool logKeyboardKeys = true;
    public bool logJoystickNamesOnStart = true;
    public bool logOnlyOnChange = true;

    [Header("Axis Thresholds")]
    public float axisThreshold = 0.2f;
    public float mouseThreshold = 0.2f;

    private float lastHorizontal;
    private float lastVertical;
    private float lastMouseX;
    private float lastMouseY;
    private float lastMouseScroll;

    private readonly KeyCode[] commonKeys = new KeyCode[]
    {
        KeyCode.Space,
        KeyCode.Return,
        KeyCode.Escape,
        KeyCode.Backspace,
        KeyCode.Tab,

        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.UpArrow,
        KeyCode.DownArrow,

        KeyCode.A,
        KeyCode.B,
        KeyCode.C,
        KeyCode.D,
        KeyCode.W,
        KeyCode.S,
        KeyCode.Q,
        KeyCode.E,

        KeyCode.LeftShift,
        KeyCode.RightShift,
        KeyCode.LeftControl,
        KeyCode.RightControl,

        KeyCode.Alpha0,
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9
    };

    void Start()
    {
        Debug.Log("=== ControllerFullProbe START ===");

        if (logJoystickNamesOnStart)
        {
            string[] names = Input.GetJoystickNames();
            if (names == null || names.Length == 0)
            {
                Debug.Log("JoystickNames: none");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("JoystickNames:");
                for (int i = 0; i < names.Length; i++)
                {
                    sb.AppendLine($"[{i}] '{names[i]}'");
                }
                Debug.Log(sb.ToString());
            }
        }

        Debug.Log("Test guide:");
        Debug.Log("1) Nhấn từng mode trên tay cầm: @+A, @+B, @+C, @+D");
        Debug.Log("2) Sau mỗi mode, thử: gạt cần, bấm A/B/C/D, bấm nút phụ nếu có.");
        Debug.Log("3) Xem Console in ra kiểu input gì.");
    }

    void Update()
    {
        ProbeMouse();
        ProbeAxes();
        ProbeMouseButtons();
        ProbeJoystickButtons();
        ProbeKeyboardKeys();
    }

    void ProbeMouse()
    {
        if (!logMouseAxes) return;

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");
        float mw = Input.GetAxisRaw("Mouse ScrollWheel");

        bool changedEnough =
            Mathf.Abs(mx - lastMouseX) > 0.01f ||
            Mathf.Abs(my - lastMouseY) > 0.01f ||
            Mathf.Abs(mw - lastMouseScroll) > 0.01f;

        bool overThreshold =
            Mathf.Abs(mx) > mouseThreshold ||
            Mathf.Abs(my) > mouseThreshold ||
            Mathf.Abs(mw) > 0.01f;

        if (overThreshold && (!logOnlyOnChange || changedEnough))
        {
            Debug.Log($"MOUSE AXIS -> MouseX:{mx:F2}, MouseY:{my:F2}, Scroll:{mw:F2}");
        }

        lastMouseX = mx;
        lastMouseY = my;
        lastMouseScroll = mw;
    }

    void ProbeAxes()
    {
        if (!logCommonAxes) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool changedEnough =
            Mathf.Abs(h - lastHorizontal) > 0.01f ||
            Mathf.Abs(v - lastVertical) > 0.01f;

        bool overThreshold =
            Mathf.Abs(h) > axisThreshold ||
            Mathf.Abs(v) > axisThreshold;

        if (overThreshold && (!logOnlyOnChange || changedEnough))
        {
            Debug.Log($"COMMON AXIS -> Horizontal:{h:F2}, Vertical:{v:F2}");
        }

        lastHorizontal = h;
        lastVertical = v;
    }

    void ProbeMouseButtons()
    {
        if (Input.GetMouseButtonDown(0)) Debug.Log("MOUSE BUTTON -> Left");
        if (Input.GetMouseButtonDown(1)) Debug.Log("MOUSE BUTTON -> Right");
        if (Input.GetMouseButtonDown(2)) Debug.Log("MOUSE BUTTON -> Middle");
    }

    void ProbeJoystickButtons()
    {
        if (!logJoystickButtons) return;

        for (int i = 0; i <= 19; i++)
        {
            KeyCode code = (KeyCode)((int)KeyCode.JoystickButton0 + i);

            if (Input.GetKeyDown(code))
                Debug.Log($"JOYSTICK BUTTON DOWN -> {code} (index {i})");

            if (Input.GetKeyUp(code))
                Debug.Log($"JOYSTICK BUTTON UP -> {code} (index {i})");
        }
    }

    void ProbeKeyboardKeys()
    {
        if (!logKeyboardKeys) return;

        foreach (KeyCode key in commonKeys)
        {
            if (Input.GetKeyDown(key))
            {
                Debug.Log($"KEY DOWN -> {key}");
            }
        }

        if (Input.anyKeyDown)
        {
            Debug.Log("ANY KEY DOWN detected");
        }
    }
}