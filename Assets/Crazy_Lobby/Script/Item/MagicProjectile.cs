using Fusion;
using UnityEngine;
using Crazy_Lobby.Player;

namespace Crazy_Lobby.Item
{
    [RequireComponent(typeof(NetworkObject))]
    public class MagicProjectile : NetworkBehaviour
    {
        public float speed = 25f; 
        public float lifeTime = 3f;
        public int damage = 1;
        public float rotationSpeed = 360f; // Tốc độ xoay (độ/giây)

        [Networked] public NetworkId OwnerId { get; set; }
        [Networked] private TickTimer LifeTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }

            // Bỏ qua va chạm với người bắn
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

            // Di chuyển thẳng về phía trước
            transform.position += transform.forward * speed * Runner.DeltaTime;
            
            // Xoay tự động quanh trục tiến
            transform.Rotate(Vector3.forward, rotationSpeed * Runner.DeltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            // Kiểm tra xem có trúng player không
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                var playerObj = health.GetComponent<NetworkObject>();
                if (playerObj != null && playerObj.Id != OwnerId)
                {
                    health.RPC_TakeDamage(damage);
                    Runner.Despawn(Object);
                }
            }
            else if (!other.isTrigger)
            {
                // Trúng tường hoặc vật cản
                Runner.Despawn(Object);
            }
        }
    }
}
