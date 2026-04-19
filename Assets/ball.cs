using Fusion;
using UnityEngine;

public class BallObstaclePhysics : NetworkBehaviour
{
    [Header("Cài đặt va chạm")]
    [Tooltip("Thời gian nạn nhân bị ngã/choáng")]
    public float knockDownDuration = 3f;

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Chỉ Host/Server mới có quyền xử lý va chạm để đảm bảo đồng bộ
        if (!Object.HasStateAuthority) return;

        // Lấy NetworkObject của đối tượng vừa bị bóng tông trúng
        var targetNetObj = collision.gameObject.GetComponentInParent<NetworkObject>();

        // 2. Lọc mục tiêu: Chỉ xét Player hoặc Enemy
        if (targetNetObj != null && (targetNetObj.CompareTag("Player") || targetNetObj.CompareTag("Enemy")))
        {
            // 3. Khóa mục tiêu: Lấy interface IStunnable và gây choáng (giống hệt StunItem của bạn)
            IStunnable stunnable = targetNetObj.GetComponentInParent<IStunnable>();
            if (stunnable != null)
            {
                stunnable.ApplyStun(knockDownDuration);
            }

            // 4. Báo cho tất cả các máy (Client) phát Animation ngã và đổi Layer
            RPC_PlayDieAnimAndKnockdown(targetNetObj);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDieAnimAndKnockdown(NetworkObject targetObj)
    {
        if (targetObj != null)
        {
            // Phát Animation "die" (hoặc "Fall")
            var animator = targetObj.GetComponentInChildren<Animator>();
            if (animator != null) 
            {
                animator.SetTrigger("die"); 
            }

            // Tạm thời chuyển Layer của nạn nhân để trái bóng có thể lăn sượt qua
            // (Nên đảm bảo bạn có logic chuyển Layer lại bình thường sau khi hết Stun)
            targetObj.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); 
        }
    }
}