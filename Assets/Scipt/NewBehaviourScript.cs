using UnityEngine;

public class InputDebugButtons : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) Debug.Log("Mouse Left");
        if (Input.GetMouseButtonDown(1)) Debug.Log("Mouse Right");
        if (Input.GetMouseButtonDown(2)) Debug.Log("Mouse Middle");

        if (Input.GetKeyDown(KeyCode.Return)) Debug.Log("Enter");
        if (Input.GetKeyDown(KeyCode.Space)) Debug.Log("Space");
        if (Input.GetKeyDown(KeyCode.Escape)) Debug.Log("Escape");
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Debug.Log("LeftArrow");
        if (Input.GetKeyDown(KeyCode.RightArrow)) Debug.Log("RightArrow");
        if (Input.GetKeyDown(KeyCode.UpArrow)) Debug.Log("UpArrow");
        if (Input.GetKeyDown(KeyCode.DownArrow)) Debug.Log("DownArrow");

        for (int i = 0; i <= 19; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
            {
                Debug.Log("Joystick button: " + i);
            }
        }
    }
}