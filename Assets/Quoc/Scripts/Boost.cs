using Fusion;
using UnityEngine;

public class SpeedBoostItem : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Khi người chơi chạm vào vật phẩm
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            // Chỉ Host/Server xử lý logic ăn vật phẩm để tránh gian lận
            if (Object != null && Object.HasStateAuthority)
            {
                player.ApplySpeedBoost();
                
                // Xoá vật phẩm khỏi map trên tất cả các máy (Despawn)
                Runner.Despawn(Object);
            }
        }
    }
}