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
                Runner.Despawn(Object);
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
                        return;
                    }
                }
            }

            transform.position += transform.up * speed * Runner.DeltaTime;
        }

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