using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WaypointMarker : MonoBehaviour
{
    public RectTransform containerRect;
    public TextMeshProUGUI meterText;
    public Vector3 offset = new Vector3(0, 2.5f, 0);
    private Image iconImage;

    void Start()
    {
        if (containerRect != null) iconImage = containerRect.GetComponentInChildren<Image>();
    }

    void Update()
    {
        // Kiểm tra an toàn để tránh lỗi NullReference
        if (GameManager.Instance == null || Camera.main == null) return;

        if (GameManager.Instance.IsGameEnded)
        {
            ToggleMarker(false);
            return;
        }

        Transform target = GetCurrentTarget();
        if (target == null)
        {
            ToggleMarker(false);
            return;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position + offset);

        if (screenPos.z > 0)
        {
            ToggleMarker(true);
            containerRect.position = screenPos;
            float distance = Vector3.Distance(GameManager.Instance.playerTransform.position, target.position);
            meterText.text = distance.ToString("F1") + "m";
        }
        else
        {
            ToggleMarker(false);
        }
    }

    Transform GetCurrentTarget()
    {
        int idx = GameManager.Instance.nextCakeIndex - 1;
        if (GameManager.Instance.cakeTargets != null && idx >= 0 && idx < GameManager.Instance.cakeTargets.Length)
            return GameManager.Instance.cakeTargets[idx];
        return null;
    }

    void ToggleMarker(bool status)
    {
        if (containerRect != null && containerRect.gameObject.activeSelf != status)
            containerRect.gameObject.SetActive(status);
    }
}