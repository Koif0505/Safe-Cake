using UnityEngine;

public class CameraFollowVR : MonoBehaviour
{
    public Transform neckBone; // Kéo xương Neck vào đây
    public Vector3 offset = new Vector3(0, 0, 0.15f); // Đẩy ra trước một chút để không thấy "ruột" nhân vật

    void LateUpdate() // Dùng LateUpdate để chạy sau khi hoạt ảnh đã xong, giúp hết giật
    {
        if (neckBone != null)
        {
            // CHỈ LẤY VỊ TRÍ: Camera sẽ di chuyển theo cổ
            transform.position = neckBone.position + neckBone.TransformDirection(offset);

            // KHÔNG LẤY XOAY: Xoay sẽ do Tracked Pose Driver (Kính VR) 
            // hoặc Script xoay của cậu tự lo. 
        }
    }
}