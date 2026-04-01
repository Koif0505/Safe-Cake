using UnityEngine;
using System.Collections.Generic;

public class CarPathFollower : MonoBehaviour
{
    public Transform pathParent;
    public float speedToP3 = 25f; // Tốc độ từ P2 tới P3 (Nên để nhanh)
    public AudioSource crashSoundSource; // Kéo thành phần Audio Source của cái xe vào đây

    private List<Transform> waypoints = new List<Transform>();
    private int currentPointIndex = 0;
    private int stage = 0; // 0: Idle, 1: P1->P2, 2: P2->P3
    private float moveTimer = 0f;
    private bool isMoving = false;
    private Vector3 startPos;

    void Start()
    {
        // Tự động tìm các điểm Point1, Point2, Point3
        if (pathParent == null)
        {
            Debug.LogError("LỖI: Bạn chưa kéo Object Path vào ô Path Parent trên cái xe!");
            return;
        }

        foreach (Transform child in pathParent) waypoints.Add(child);

        if (waypoints.Count < 3)
        {
            Debug.LogError("LỖI: Object Path của bạn đang trống không, hoặc thiếu điểm (cần ít nhất 3 điểm)!");
            return;
        }

        // Đặt xe ở vị trí Point 1 ngay từ đầu
        transform.position = waypoints[0].position;
    }

    // Hàm này sẽ được GameManager gọi khi bạn trả lời xong câu đố
    public void StartCarAttack()
    {
        if (waypoints.Count < 3) return;
        isMoving = true;
        stage = 1;
        moveTimer = 0f;
        startPos = transform.position;
    }

    // Hàm này sẽ được GameManager gọi khi bạn bấm nút Hồi sinh (Continue)
    public void StopAndDestroy()
    {
        isMoving = false;
        gameObject.SetActive(false); // Biến mất khi Continue
    }

    void Update()
    {
        if (!isMoving) return;

        if (stage == 1) // Lao từ P1 tới P2 trong đúng 3 giây
        {
            moveTimer += Time.deltaTime;
            float percent = moveTimer / 3f;
            transform.position = Vector3.Lerp(startPos, waypoints[1].position, percent);

            // Xoay hướng xe nhìn về P2
            transform.LookAt(waypoints[1]);

            if (moveTimer >= 3f)
            {
                stage = 2; // Đã đến P2, chuyển sang P3
                moveTimer = 0f;
            }
        }
        else if (stage == 2) // Lao tới P3 theo điều kiện
        {
            // Luôn xoay hướng nhìn về P3
            transform.LookAt(waypoints[2]);

            // Lao tới Point 3 theo tốc độ nhanh
            transform.position = Vector3.MoveTowards(transform.position, waypoints[2].position, speedToP3 * Time.deltaTime);

            // Đến đích Point 3 thì dừng
            if (Vector3.Distance(transform.position, waypoints[2].position) < 0.5f)
            {
                isMoving = false;
            }
        }
    }

    // XỬ LÝ ĐÂM XE: BẠN PHẢI TÍCH Ô "IS TRIGGER" TRÊN BOX COLLIDER CỦA CÁI XE
    private void OnTriggerEnter(Collider other)
    {
        // Đảm bảo nhân vật của bạn có Tag là "Player"
        if (other.CompareTag("Player"))
        {
            isMoving = false; // Khựng lại khi đâm trúng

            // PHÁT RA TIẾNG ĐÂM XE NGAY LẬP TỨC
            if (crashSoundSource != null && crashSoundSource.clip != null)
            {
                crashSoundSource.PlayOneShot(crashSoundSource.clip); // PlayOneShot cho trùng tiếng
            }

            // GỌI GAME OVER NGAY LẬP TỨC (Dùng hàm mới ImmediateLoseGame trong GameManager bên dưới)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ImmediateLoseGame("BẠN VỪA BỊ XE TÔNG TRÚNG!");
            }
        }
    }
}