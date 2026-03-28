using Fusion;
using UnityEngine;
using Crazy_Lobby.Player;

namespace Crazy_Lobby.Item
{
    [RequireComponent(typeof(NetworkObject))]
    public class FireworkProjectile : NetworkBehaviour
    {
        public float speed = 15f; 
        public float turnSpeed = 5f; 
        public float lifeTime = 5f;

        [Networked] public NetworkId OwnerId { get; set; }
        [Networked] public NetworkId TargetId { get; set; }
        
        [Networked] private TickTimer LifeTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }

            if (OwnerId.IsValid)
            {
                var ownerObj = Runner.FindObject(OwnerId);
                if (ownerObj != null)
                {
                    var ownerColliders = ownerObj.GetComponentsInChildren<Collider>();
                    var myCollider = GetComponent<Collider>();

                    if (myCollider != null)
                    {
                        foreach (var col in ownerColliders)
                        {
                            Physics.IgnoreCollision(myCollider, col);
                        }
                    }
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && LifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            if (TargetId.IsValid)
            {
                NetworkObject targetObj = Runner.FindObject(TargetId);
                if (targetObj != null)
                {
                    // 1. Cập nhật hướng xoay để đuổi theo mục tiêu
                    Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.2f; // Nhắm vào người
                    Vector3 direction = (targetPos - transform.position).normalized;
                    
                    if (direction != Vector3.zero)
                    {
                        // Xoay trục Up của đạn về phía mục tiêu
                        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * turnSpeed);
                    }

                    // 2. Kiểm tra khoảng cách để kích nổ (Thay vì SphereCast)
                    float distance = Vector3.Distance(transform.position, targetPos);
                    if (HasStateAuthority && distance < 1.5f) // Tăng nhẹ khoảng cách để bù trừ lag
                    {
                        ApplyDamage(targetObj);
                        return;
                    }
                }
            }

            // 3. Di chuyển đạn tiến lên
            transform.position += transform.up * speed * Runner.DeltaTime;
        }

        // Tách riêng hàm gây sát thương để quản lý
        private void ApplyDamage(NetworkObject target)
        {
            var health = target.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                Debug.Log($"[Firework] Gây sát thương cho: {target.Id}");
                health.RPC_TakeDamage(1);
            }
            Runner.Despawn(Object);
        }
    }
}