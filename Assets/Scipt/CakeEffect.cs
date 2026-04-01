using UnityEngine;
using System.Collections;

public class CakeEffect : MonoBehaviour
{
    private Vector3 originalPos;
    private Material mat;

    void Start()
    {
        originalPos = transform.position;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) mat = rend.material;
    }

    public void TriggerEffect(float duration, float heightMultiplier, bool useGlow)
    {
        if (gameObject.activeSelf)
        {
            StopAllCoroutines();
            StartCoroutine(EffectRoutine(duration, heightMultiplier, useGlow));
        }
    }

    IEnumerator EffectRoutine(float duration, float heightMul, bool useGlow)
    {
        if (useGlow && mat != null)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.yellow * 2f);
        }
        else if (mat != null)
        {
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
        }

        float elapsed = 0;
        while (elapsed < duration)
        {
            // Nhảy lên xuống nhịp nhàng
            float newY = originalPos.y + Mathf.Abs(Mathf.Sin(Time.time * 5f)) * (0.6f * heightMul);
            transform.position = new Vector3(originalPos.x, newY, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        if (mat != null) mat.SetColor("_EmissionColor", Color.black);
    }
}