using Fusion;
using UnityEngine;

public class StunItem : NetworkBehaviour
{
    public float radius = 7f;
    public float duration = 3f;
    public LayerMask affectedLayers;

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        // Tìm NetworkObject của người chạm vào
        var picker = other.GetComponentInParent<NetworkObject>();

        if (picker != null && (picker.CompareTag("Player") || picker.CompareTag("Enemy")))
        {
            ExecuteStun(picker);
            Runner.Despawn(Object); // Biến mất sau khi nhặt
        }
    }

    private void ExecuteStun(NetworkObject picker)
    {
        // Quét tất cả vật thể trong phạm vi 3D
        Collider[] targets = Physics.OverlapSphere(transform.position, radius, affectedLayers);

        foreach (var t in targets)
        {
            // Lấy interface IStunnable từ Component StunStatus
            IStunnable stunnable = t.GetComponentInParent<IStunnable>();

            if (stunnable != null)
            {
                var targetNetObj = t.GetComponentInParent<NetworkObject>();
                // Không làm choáng chính người nhặt
                if (targetNetObj != null && targetNetObj == picker) continue;

                stunnable.ApplyStun(duration);
            }
        }
    }
}