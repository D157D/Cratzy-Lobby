using Fusion; // BẮT BUỘC CÓ để dùng NetworkCharacterController
using UnityEngine;

public class LHS_Respawn2 : MonoBehaviour
{
    [SerializeField] Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Kiểm tra xem ai vừa rớt xuống
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            // 2. Tìm component điều khiển mạng (Dùng GetComponentInParent đề phòng Collider nằm ở object con)
            var ncc = other.GetComponentInParent<NetworkCharacterController>();
            
            if (ncc != null)
            {
                // 3. Dịch chuyển chuẩn xác bằng lệnh Teleport của Fusion
                ncc.Teleport(respawnPoint.position);
                Debug.Log($"Đã cứu {other.name} về điểm Respawn!");
            }
            else
            {
                // Backup: Nếu rớt xuống là một object offline bình thường không có mạng
                other.transform.position = respawnPoint.position;
            }
        }
    }
}