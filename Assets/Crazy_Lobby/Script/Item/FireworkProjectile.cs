using Fusion;
using UnityEngine;
using Crazy_Lobby.Player;

namespace Crazy_Lobby.Item
{
    [RequireComponent(typeof(NetworkObject))]
    public class FireworkProjectile : NetworkBehaviour
    {
        [Header("Cài đặt đạn")]
        public float speed = 15f; // Tốc độ bay
        public float turnSpeed = 5f; // Tốc độ bẻ lái (Heat-seeking)
        public float lifeTime = 5f; // Thời gian tồn tại tối đa
        internal NetworkId OwnerId;

        // Biến mạng lưu ID của mục tiêu để đạn tự động bay tới hoặc đuổi theo
        [Networked] public NetworkId TargetId { get; set; }
        
        [Networked] private TickTimer LifeTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }

            // Ignore owner collision
            var ownerObj = Runner.FindObject(OwnerId);
            if (ownerObj != null)
            {
                var ownerColliders = ownerObj.GetComponentsInChildren<Collider>();
                var myCollider = GetComponent<Collider>();

                foreach (var col in ownerColliders)
                {
                    Physics.IgnoreCollision(myCollider, col);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                if (LifeTimer.Expired(Runner))
                {
                    Runner.Despawn(Object);
                    return;
                }
            }
            if (TargetId.IsValid)
            {
                NetworkObject targetObj = Runner.FindObject(TargetId);
                if (targetObj != null)
                {
                    Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.5f; // Nhắm vào ngực
                    Vector3 direction = (targetPos - transform.position).normalized;
                    
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * turnSpeed);
                    }
                }
            }

            transform.position += transform.up * speed * Runner.DeltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority)
                return;

            var networkObject = other.GetComponentInParent<NetworkObject>();

            if (networkObject != null)
            {
                var playerHealth = networkObject.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    if (playerHealth.Object.Id == OwnerId)
                        return;

                    playerHealth.RPC_TakeDamage(1);
                    Runner.Despawn(Object);
                    return;
                }
            }

            if (other.TryGetComponent<NetworkCharacterController>(out var ncc) && ncc.Object.Id == OwnerId)
            {
                return;
            }

            Runner.Despawn(Object);
        }
    }
}