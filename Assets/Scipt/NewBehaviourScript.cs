using UnityEngine;

public class InputDebugAll : MonoBehaviour
{
    void Update()
    {
        // Gamepad buttons
        for (int i = 0; i <= 19; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
            {
                Debug.Log("Joystick button: " + i);
            }
        }

        // Mouse
        if (Input.GetMouseButtonDown(0)) Debug.Log("Mouse Left");
        if (Input.GetMouseButtonDown(1)) Debug.Log("Mouse Right");
        if (Input.GetMouseButtonDown(2)) Debug.Log("Mouse Middle");

        // Some common keyboard-like keys
        if (Input.anyKeyDown)
        {
            Debug.Log("Any key down detected");
        }

        // Axes
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");

        if (Mathf.Abs(h) > 0.1f) Debug.Log("Horizontal: " + h);
        if (Mathf.Abs(v) > 0.1f) Debug.Log("Vertical: " + v);
        if (Mathf.Abs(mx) > 0.1f) Debug.Log("Mouse X: " + mx);
        if (Mathf.Abs(my) > 0.1f) Debug.Log("Mouse Y: " + my);
    }
}