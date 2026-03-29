using UnityEngine;
using TMPro;

public class FloatingScore : MonoBehaviour
{
    void Start() { Destroy(gameObject, 1f); } // Bi?n m?t sau 1 giây
    void Update()
    {
        // Bay lên trên và m? d?n
        transform.localPosition += new Vector3(0, 100f * Time.deltaTime, 0);
    }
}