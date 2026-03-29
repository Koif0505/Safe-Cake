using UnityEngine;
using TMPro;

public class FloatingScore : MonoBehaviour
{
    public float moveSpeed = 100f;
    public float duration = 1.5f;

    private TextMeshProUGUI text;
    private float timer = 0f;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        // Lệnh này cực kỳ quan trọng để dọn dẹp bộ nhớ
        Destroy(gameObject, duration);
    }

    void Update()
    {
        // Bay lên theo trục Y của màn hình
        transform.localPosition += Vector3.up * moveSpeed * Time.deltaTime;

        // Mờ dần theo thời gian
        timer += Time.deltaTime;
        if (text != null)
        {
            Color c = text.color;
            c.a = Mathf.Lerp(1, 0, timer / duration);
            text.color = c;
        }
    }
}