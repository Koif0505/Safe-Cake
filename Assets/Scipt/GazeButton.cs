using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))] // Cần có BoxCollider để Raycast chạm vào trong VR
public class GazeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float gazeDuration = 2.0f;
    public UnityEvent onClick;
    public Image loadingBar; // Kéo một UI Image (Type: Filled, Radial 360) vào đây

    private bool isGazing = false;
    private float gazeTimer = 0f;

    void Start()
    {
        if (loadingBar != null) loadingBar.fillAmount = 0;

        // Tự động thêm BoxCollider vừa với RectTransform nếu chưa có
        BoxCollider col = GetComponent<BoxCollider>();
        RectTransform rect = GetComponent<RectTransform>();
        if (col != null && rect != null)
        {
            col.size = new Vector3(rect.rect.width, rect.rect.height, 1f);
        }
    }

    void Update()
    {
        if (isGazing)
        {
            gazeTimer += Time.unscaledDeltaTime; // Chạy cả khi pause game
            if (loadingBar != null) loadingBar.fillAmount = gazeTimer / gazeDuration;

            if (gazeTimer >= gazeDuration)
            {
                isGazing = false;
                if (loadingBar != null) loadingBar.fillAmount = 0;
                onClick.Invoke(); // Kích hoạt nút
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isGazing = true;
        gazeTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isGazing = false;
        gazeTimer = 0f;
        if (loadingBar != null) loadingBar.fillAmount = 0;
    }
}