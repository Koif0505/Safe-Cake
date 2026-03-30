using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public Transform leftDoor;
    public Transform rightDoor;

    public float openDelay = 1.5f; // Đợi 1.5 giây SAU KHI bấm Start
    public float openSpeed = 2f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private bool hasStartedOpening = false; // Cờ để đảm bảo chỉ chạy mở cửa 1 lần

    void Start()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("DoorController: Chưa gán Left Door hoặc Right Door trong Inspector.");
            return;
        }

        // Lưu vị trí đóng ban đầu
        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;

        // Thiết lập vị trí mở
        leftOpenPos = new Vector3(-1.2f, leftClosedPos.y, leftClosedPos.z);
        rightOpenPos = new Vector3(1.2f, rightClosedPos.y, rightClosedPos.z);

        // Đảm bảo lúc mới vào game cửa luôn đóng
        leftDoor.localPosition = leftClosedPos;
        rightDoor.localPosition = rightClosedPos;
    }

    void Update()
    {
        // Kiểm tra: Nếu GameManager đã Start và cửa chưa bắt đầu mở thì mới chạy
        if (GameManager.Instance != null && GameManager.Instance.isGameStarted && !hasStartedOpening)
        {
            hasStartedOpening = true;
            StartCoroutine(OpenDoorRoutine());
        }
    }

    IEnumerator OpenDoorRoutine()
    {
        // Chờ 1.5 giây tính từ lúc nhấn nút Start
        yield return new WaitForSeconds(openDelay);

        while (Vector3.Distance(leftDoor.localPosition, leftOpenPos) > 0.01f ||
               Vector3.Distance(rightDoor.localPosition, rightOpenPos) > 0.01f)
        {
            leftDoor.localPosition = Vector3.Lerp(
                leftDoor.localPosition,
                leftOpenPos,
                Time.deltaTime * openSpeed
            );

            rightDoor.localPosition = Vector3.Lerp(
                rightDoor.localPosition,
                rightOpenPos,
                Time.deltaTime * openSpeed
            );

            yield return null;
        }

        leftDoor.localPosition = leftOpenPos;
        rightDoor.localPosition = rightOpenPos;
        Debug.Log("Cửa thang máy đã mở hoàn toàn.");
    }
}