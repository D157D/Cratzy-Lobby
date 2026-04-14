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

        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private GameObject smokeTrailPrefab;
        private GameObject _spawnedSmokeTrail;

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

            if (smokeTrailPrefab != null)
            {
                _spawnedSmokeTrail = Instantiate(smokeTrailPrefab, transform.position, transform.rotation, transform);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && LifeTimer.Expired(Runner))
            {
                ExplodeAndDespawn();
                return;
            }

            if (TargetId.IsValid && ShootUpTimer.Expired(Runner))
            {
                NetworkObject targetObj = Runner.FindObject(TargetId);
                if (targetObj != null)
                {
                    Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.2f;
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
                        ExplodeAndDespawn();
                        return;
                    }
                }
            }

            transform.position += transform.up * speed * Runner.DeltaTime;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                var playerObj = health.GetComponent<NetworkObject>();
                if (playerObj != null && playerObj.Id != OwnerId && (!TargetId.IsValid || playerObj.Id == TargetId))
                {
                    ApplyDamage(playerObj);
                    ExplodeAndDespawn();
                    return;
                }
            }

            var enemy = other.GetComponentInParent<EnemyPatrol>();
            if (enemy != null)
            {
                var enemyObj = enemy.GetComponent<NetworkObject>();
                if (enemyObj != null && enemyObj.Id != OwnerId && (!TargetId.IsValid || enemyObj.Id == TargetId))
                {
                    ApplyDamage(enemyObj);
                    ExplodeAndDespawn();
                    return;
                }
            }
            else if (!other.isTrigger)
            {
                ExplodeAndDespawn();
                return;
            }
        }

        private void ApplyDamage(NetworkObject target)
        {

            var health = target.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                Debug.Log($"[Firework] Gây sát thương cho Player: {target.Id}");
                health.RPC_TakeDamage(1);

                var targetAnimator = target.GetComponentInChildren<Animator>();
                if (targetAnimator != null)
                {
                    targetAnimator.SetTrigger("die");
                }
                return;
            }

            var enemy = target.GetComponentInParent<EnemyPatrol>();
            if (enemy != null)
            {
                Debug.Log($"[Firework] Gây sát thương cho Enemy: {target.Id}");
                enemy.RPC_TakeDamage(1);
                
                return;
            }
        }

        private void ExplodeAndDespawn()
        {
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }
            Runner.Despawn(Object);
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_spawnedSmokeTrail != null)
            {
                Destroy(_spawnedSmokeTrail);
            }
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