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
        public float shootUpDuration = 0.5f;

        [Networked] public NetworkId OwnerId { get; set; }
        [Networked] public NetworkId TargetId { get; set; }
        
        [Networked] private TickTimer LifeTimer { get; set; }
        [Networked] private TickTimer ShootUpTimer { get; set; }

        [SerializeField] private GameObject explosionEffectPrefab; // Hiệu ứng phát nổ

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
                ShootUpTimer = TickTimer.CreateFromSeconds(Runner, shootUpDuration);
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
                ExplodeAndDespawn(); // Phát nổ khi hết thời gian sống
                return;
            }

            if (TargetId.IsValid && ShootUpTimer.Expired(Runner))
            {
                NetworkObject targetObj = Runner.FindObject(TargetId);
                if (targetObj != null)
                {
                    Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.2f; // Nhắm vào người
                    Vector3 direction = (targetPos - transform.position).normalized;
                    
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * turnSpeed);
                    }

                    float distance = Vector3.Distance(transform.position, targetPos);
                    if (HasStateAuthority && distance < 1.5f)
                    {
                        ApplyDamage(targetObj);
                        ExplodeAndDespawn(); // Phát nổ khi tiếp cận mục tiêu
                        return;
                    }
                }
            }

            transform.position += transform.up * speed * Runner.DeltaTime;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            // Kiểm tra xem có trúng player không
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                var playerObj = health.GetComponent<NetworkObject>();
                // Đảm bảo không phải người bắn và là mục tiêu nếu TargetId hợp lệ, hoặc bất kỳ người chơi nào nếu TargetId không hợp lệ
                if (playerObj != null && playerObj.Id != OwnerId && (!TargetId.IsValid || playerObj.Id == TargetId))
                {
                    ApplyDamage(playerObj);
                    ExplodeAndDespawn(); // Phát nổ khi va chạm với người chơi
                    return;
                }
            }
            // Nếu trúng tường hoặc vật cản (không phải trigger)
            else if (!other.isTrigger)
            {
                ExplodeAndDespawn(); // Phát nổ khi va chạm với vật cản
                return;
            }
        }

        private void ApplyDamage(NetworkObject target)
        {
            // Debug.Log($"[Firework] ApplyDamage called on {target.Id}");
            // Sát thương được gây ra, nhưng việc hủy vật thể sẽ được xử lý bởi ExplodeAndDespawn
            // Việc tách biệt này đảm bảo sát thương chỉ được áp dụng một lần và hiệu ứng nổ/hủy diễn ra nhất quán.

            var health = target.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                Debug.Log($"[Firework] Gây sát thương cho: {target.Id}");
                health.RPC_TakeDamage(1);
            }
        }

        private void ExplodeAndDespawn()
        {
            if (explosionEffectPrefab != null)
            {
                // Khởi tạo hiệu ứng phát nổ. Đây là hiệu ứng hình ảnh chỉ hiển thị ở phía client.
                // Nếu hiệu ứng nổ cần được đồng bộ qua mạng (ví dụ: để gây sát thương diện rộng), bạn sẽ cần sử dụng Runner.Spawn.
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }
            Runner.Despawn(Object); // Hủy đạn sau khi phát nổ
        }

        public override void Render()
        {
            if (!HasStateAuthority && !HasInputAuthority && TargetId.IsValid && ShootUpTimer.Expired(Runner))
            {
                NetworkObject targetObj = Runner.FindObject(TargetId);
                if (targetObj != null)
                {
                    Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.2f;
                    Vector3 direction = (targetPos - transform.position).normalized;
                    
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                    }
                }
            }
        }
    }
}