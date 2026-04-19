using Fusion;
using UnityEngine;
using Crazy_Lobby.Player;
using UnityEngine.SceneManagement;
using Crazy_Lobby.Player.Components;
using Crazy_Lobby.Enemy; // 👉 Đã thêm thư viện Enemy để nhận diện EnemyAI

namespace Crazy_Lobby.Item
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(AudioSource))]
    public class FireworkProjectile : NetworkBehaviour
    {
        [Header("Projectile Settings (Trạng thái Đạn)")]
        private float speed = 15f; 
        private float turnSpeed = 5f; 
        private float lifeTime = 5f;
        private float shootUpDuration = 0.5f;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private GameObject smokeTrailPrefab;

        [Header("Pickup Settings (Trạng thái Vật phẩm)")]
        private int ammoAmount = 1;             
        private float rotateSpeed = 90f;        
        private float floatSpeed = 2f;          
        private float floatAmplitude = 0.3f;    
        [Header("Audio")]
        private AudioSource audioSource;
        public AudioClip flyingSound;
        public AudioClip explosionSound;
        [Networked] public NetworkId OwnerId { get; set; }
        [Networked] public NetworkId TargetId { get; set; }
        
        [Networked] private TickTimer LifeTimer { get; set; }
        [Networked] private TickTimer ShootUpTimer { get; set; }
        private bool _isSpawnReady = false;
        private GameObject _spawnedSmokeTrail;
        private NetworkObject _cachedTarget;
        private bool _isInLobby;
        private Vector3 _startPosition;

        private bool IsProjectile => OwnerId.IsValid; 

        public void Awake()
        {
            audioSource = GetComponent<AudioSource>();    
        }
        public override void Spawned()
        {
            _isSpawnReady = true;
            
            _isInLobby = SceneManager.GetActiveScene().name == "Login_Crazy";
            _startPosition = transform.position; 

            if (IsProjectile)
            {
                if (HasStateAuthority)
                {
                    LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
                    ShootUpTimer = TickTimer.CreateFromSeconds(Runner, shootUpDuration);
                }

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

                if (smokeTrailPrefab != null)
                {
                    Quaternion smokeRot = Quaternion.LookRotation(-transform.up);
                    _spawnedSmokeTrail = Instantiate(smokeTrailPrefab, transform.position, smokeRot, transform);
                }

                if(audioSource != null && flyingSound != null)
                {
                    audioSource.clip = flyingSound;
                    audioSource.loop = true;
                    audioSource.spatialBlend = 1f;
                    audioSource.Play();
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (IsProjectile)
            {
                if (HasStateAuthority && LifeTimer.Expired(Runner))
                {
                    ExplodeAndDespawn();
                    return;
                }

                if (TargetId.IsValid && ShootUpTimer.Expired(Runner))
                {
                    if (_cachedTarget == null) _cachedTarget = Runner.FindObject(TargetId);

                    if (_cachedTarget != null)
                    {
                        Vector3 targetPos = _cachedTarget.transform.position + Vector3.up * 1.2f;
                        Vector3 direction = (targetPos - transform.position).normalized;
                        
                        if (direction != Vector3.zero)
                        {
                            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * turnSpeed);
                        }

                        float distance = Vector3.Distance(transform.position, targetPos);
                        if (HasStateAuthority && distance < 1.5f)
                        {
                            ApplyDamage(_cachedTarget);
                            ExplodeAndDespawn();
                            return;
                        }
                    }
                }

                // Bay tới trước
                transform.position += transform.up * speed * Runner.DeltaTime;
            }
            else
            {
                transform.Rotate(Vector3.up, rotateSpeed * Runner.DeltaTime);
                float newY = _startPosition.y + Mathf.Sin(Runner.SimulationTime * floatSpeed) * floatAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if(!_isSpawnReady) return;
            if (!HasStateAuthority) return;

            if (IsProjectile)
            {
                var health = other.GetComponentInParent<PlayerHealth>();
                if (health != null && health.TryGetComponent(out NetworkObject playerObj))
                {
                    if (playerObj.Id != OwnerId && (!TargetId.IsValid || playerObj.Id == TargetId))
                    {
                        ApplyDamage(playerObj);
                        ExplodeAndDespawn();
                        return;
                    }
                }

                // 👉 Đã đổi từ EnemyPatrol sang EnemyAI
                var enemy = other.GetComponentInParent<EnemyAI>();
                if (enemy != null && enemy.TryGetComponent(out NetworkObject enemyObj))
                {
                    if (enemyObj.Id != OwnerId && (!TargetId.IsValid || enemyObj.Id == TargetId))
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
            else
            {
                var playerCombat = other.GetComponentInParent<PlayerCombat>();
                if (playerCombat != null)
                {
                    playerCombat.FireworkCount += ammoAmount;
                    playerCombat.RPC_PickUpItem("Firework", ammoAmount); 
                    Runner.Despawn(Object); 
                }
            }
        }

        private void ApplyDamage(NetworkObject target)
        {
            var health = target.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                var targetAnimator = target.GetComponentInChildren<Animator>();
                if (targetAnimator != null) targetAnimator.SetTrigger("die");

                var playerCombat = target.GetComponentInParent<PlayerCombat>();
                if (playerCombat != null) playerCombat.ApplyStun(2f); 
                
                if (_isInLobby) return; 

                Debug.Log($"[Firework] Gây sát thương cho Player: {target.Id}");
                health.RPC_TakeDamage(1);
                return;
            }

            // 👉 Đã đổi từ EnemyPatrol sang EnemyAI
            var enemy = target.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                Debug.Log($"[Firework] Gây sát thương cho Enemy: {target.Id}");
                // Hàm RPC_TakeDamage bên EnemyAI đã có sẵn lệnh gọi Animator.SetTrigger("die") rồi
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

            if(explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
            }

            Runner.Despawn(Object);
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_spawnedSmokeTrail != null) Destroy(_spawnedSmokeTrail);
            _cachedTarget = null; 
        }

        public override void Render()
        {
            if (IsProjectile && !HasStateAuthority && !HasInputAuthority && TargetId.IsValid && ShootUpTimer.Expired(Runner))
            {
                if (_cachedTarget == null) _cachedTarget = Runner.FindObject(TargetId);

                if (_cachedTarget != null)
                {
                    Vector3 targetPos = _cachedTarget.transform.position + Vector3.up * 1.2f;
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