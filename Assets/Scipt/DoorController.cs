using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public Transform leftDoor;
    public Transform rightDoor;

    public float openDelay = 1.5f;
    public float openSpeed = 2f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    void Start()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("DoorController: Chua gan Left Door hoac Right Door trong Inspector.");
            return;
        }

        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;

        leftOpenPos = new Vector3(-1.2f, leftClosedPos.y, leftClosedPos.z);
        rightOpenPos = new Vector3(1.2f, rightClosedPos.y, rightClosedPos.z);

        StartCoroutine(OpenDoorRoutine());
    }

    IEnumerator OpenDoorRoutine()
    {
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
    }
}