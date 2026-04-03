using UnityEngine;
using System.Collections.Generic;

public class CarPathFollower : MonoBehaviour
{
    public Transform pathParent;
    public float speedToP3 = 50f; // Gấp đôi 25f
    public AudioSource crashSoundSource;

    private List<Transform> waypoints = new List<Transform>();
    // Đã xóa dòng private int currentPointIndex = 0; ở đây để hết Warning
    private int stage = 0; // 0: Idle, 1: P1->P2, 2: P2->P3
    private float moveTimer = 0f;
    private bool isMoving = false;
    private Vector3 startPos;

    void Start()
    {
        if (pathParent == null)
        {
            Debug.LogError("LỖI: Bạn chưa kéo Object Path vào ô Path Parent trên cái xe!");
            return;
        }

        foreach (Transform child in pathParent)
            waypoints.Add(child);

        if (waypoints.Count < 3)
        {
            Debug.LogError("LỖI: Object Path của bạn đang trống không, hoặc thiếu điểm (cần ít nhất 3 điểm)!");
            return;
        }

        transform.position = waypoints[0].position;
    }

    public void StartCarAttack()
    {
        if (waypoints.Count < 3) return;

        isMoving = true;
        stage = 1;
        moveTimer = 0f;
        startPos = transform.position;
    }

    public void StopAndDestroy()
    {
        isMoving = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isMoving) return;

        if (stage == 1) // P1 -> P2 nhanh gấp đôi: 3s -> 1.5s
        {
            moveTimer += Time.deltaTime;
            float percent = moveTimer / 1.5f;
            transform.position = Vector3.Lerp(startPos, waypoints[1].position, percent);

            transform.LookAt(waypoints[1]);

            if (moveTimer >= 1.5f)
            {
                stage = 2;
                moveTimer = 0f;
            }
        }
        else if (stage == 2) // P2 -> P3
        {
            transform.LookAt(waypoints[2]);
            transform.position = Vector3.MoveTowards(
                transform.position,
                waypoints[2].position,
                speedToP3 * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, waypoints[2].position) < 0.5f)
            {
                isMoving = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isMoving = false;

            if (crashSoundSource != null && crashSoundSource.clip != null)
            {
                crashSoundSource.PlayOneShot(crashSoundSource.clip);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ImmediateLoseGame("BẠN VỪA BỊ XE TÔNG TRÚNG!");
            }
        }
    }
}