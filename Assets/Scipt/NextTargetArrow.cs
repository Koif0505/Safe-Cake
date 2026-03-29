using UnityEngine;

public class NextTargetArrow : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cameraTransform;
    public Transform arrowVisual;

    [Header("Attach To Screen")]
    public Vector3 normalLocalPosition = new Vector3(0f, 0.25f, 1.2f);
    public Vector3 warningLocalPosition = new Vector3(0f, 0.4f, 1.2f);

    [Header("Arrow Rotation Offset")]
    public Vector3 arrowRotationOffset = Vector3.zero;

    [Header("Distance")]
    public float hideDistance = 3f;
    public float warningDistance = 20f;

    [Header("Wrong Direction")]
    [Range(-1f, 1f)]
    public float wrongDirectionDotThreshold = 0.2f;

    [Header("Animation")]
    public float rotationSmooth = 8f;
    public float bounceSpeed = 6f;
    public float bounceHeight = 0.12f;
    public float pulseSpeed = 8f;
    public float pulseScale = 0.12f;

    private Vector3 baseScale;

    void Start()
    {
        if (arrowVisual != null)
        {
            baseScale = arrowVisual.localScale;
        }

        if (cameraTransform != null)
        {
            transform.SetParent(cameraTransform);
            transform.localPosition = normalLocalPosition;
            transform.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (player == null || cameraTransform == null || arrowVisual == null) return;

        Transform target = GetCurrentTarget();
        if (target == null)
        {
            arrowVisual.gameObject.SetActive(false);
            return;
        }

        Vector3 toTarget = target.position - player.position;
        float distance = toTarget.magnitude;

        // 1) Gần cake thì ẩn
        if (distance <= hideDistance)
        {
            arrowVisual.gameObject.SetActive(false);
            return;
        }

        arrowVisual.gameObject.SetActive(true);

        // 2) Luôn hướng về mục tiêu tiếp theo
        RotateArrowTowardTarget(target);

        // 3) Kiểm tra có đang đi sai hướng / quá xa không
        bool isWrongDirection = CheckWrongDirection(target);
        bool isFarAway = distance >= warningDistance;

        if (isWrongDirection || isFarAway)
        {
            ApplyWarningState();
        }
        else
        {
            ApplyNormalState();
        }
    }

    Transform GetCurrentTarget()
    {
        if (GameManager.Instance.cakeTargets == null) return null;

        int index = GameManager.Instance.nextCakeIndex - 1;

        if (index >= 0 && index < GameManager.Instance.cakeTargets.Length)
        {
            return GameManager.Instance.cakeTargets[index];
        }

        return null;
    }

    void RotateArrowTowardTarget(Transform target)
    {
        Vector3 direction = target.position - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);

        // bù xoay nếu model arrow bị lệch đầu mũi tên
        Quaternion offsetRotation = Quaternion.Euler(arrowRotationOffset);

        Quaternion finalRotation = lookRotation * offsetRotation;

        arrowVisual.rotation = Quaternion.Lerp(
            arrowVisual.rotation,
            finalRotation,
            Time.deltaTime * rotationSmooth
        );
    }

    bool CheckWrongDirection(Transform target)
    {
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 targetDir = target.position - player.position;
        targetDir.y = 0f;
        targetDir.Normalize();

        float dot = Vector3.Dot(camForward, targetDir);

        return dot < wrongDirectionDotThreshold;
    }

    void ApplyNormalState()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            normalLocalPosition,
            Time.deltaTime * 8f
        );

        arrowVisual.localScale = Vector3.Lerp(
            arrowVisual.localScale,
            baseScale,
            Time.deltaTime * 8f
        );
    }

    void ApplyWarningState()
    {
        float bounce = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
        Vector3 targetPos = warningLocalPosition + new Vector3(0f, bounce, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Time.deltaTime * 10f
        );

        float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * pulseScale;

        arrowVisual.localScale = Vector3.Lerp(
            arrowVisual.localScale,
            baseScale * pulse,
            Time.deltaTime * 10f
        );
    }
}