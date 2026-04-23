using Fusion;
using UnityEngine;
using Crazy_Lobby.Player;
using Crazy_Lobby.Player.Components;
using Crazy_Lobby.Enemy; 

namespace Crazy_Lobby.Item
{
    [RequireComponent(typeof(NetworkObject))]
    public class MagicProjectile : NetworkBehaviour
    {
        [Header("Spell Settings (Chế độ Kỹ năng)")]
        public float freezeRadius = 5f;   // <-- THÊM: Bán kính tác dụng của phép thuật
        public float freezeDuration = 3f; 
        public int damageAmount = 1;      
        public GameObject iceBlockPrefab; 
        public float spellLifeTime = 1.0f; 

        [Header("Audio")]
        public AudioClip castSound;       

        [Header("Pickup Settings (Chế độ Vật phẩm)")]
        public int ammoAmount = 1;              
        public float rotateSpeed = 90f;         
        public float floatSpeed = 2f;           
        public float floatAmplitude = 0.3f;
        
        // ID của người/quái vật đã tung phép
        [Networked] public NetworkId OwnerId { get; set; }
        [Networked] private TickTimer DespawnTimer { get; set; }
        
        private Vector3 _startPosition;
        private bool _isSpawnReady = false;

        private bool IsProjectile => OwnerId.IsValid; 

        public override void Spawned()
        {
            _isSpawnReady = true;
            _startPosition = transform.position;

            if (IsProjectile)
            {
                if (castSound != null) 
                {
                    Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
                    AudioSource.PlayClipAtPoint(castSound, soundPos, 1.0f);
                }

                if (HasStateAuthority)
                {
                    ExecuteAreaFreeze(); // <-- GỌI HÀM MỚI: Chỉ đóng băng khu vực gần
                    DespawnTimer = TickTimer.CreateFromSeconds(Runner, spellLifeTime);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (IsProjectile)
            {
                if (HasStateAuthority && DespawnTimer.Expired(Runner))
                {
                    Runner.Despawn(Object);
                }
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
            if (!_isSpawnReady || !HasStateAuthority) return;

            if (IsProjectile) return; 

            var playerCombat = other.GetComponentInParent<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.MagicCount += ammoAmount;
                playerCombat.RPC_PickUpItem("Magic", ammoAmount); 
                Runner.Despawn(Object); 
            }
        }

        // <-- HÀM ĐÃ ĐƯỢC CHỈNH SỬA
        private void ExecuteAreaFreeze()
        {
            // Quét Người Chơi
            foreach (var player in PlayerController.ActivePlayers)
            {
                if (player == null || player.IsDead || player.Object.Id == OwnerId) continue;
                
                // Kiểm tra khoảng cách
                if (Vector3.Distance(transform.position, player.transform.position) <= freezeRadius)
                {
                    ApplyFreezeToTarget(player.gameObject, player.transform.position);
                }
            }

            // Quét Quái Vật
            foreach (var enemy in EnemyAI.ActiveEnemies)
            {
                if (enemy == null || enemy.IsDead || enemy.Object.Id == OwnerId) continue;
                
                // Kiểm tra khoảng cách
                if (Vector3.Distance(transform.position, enemy.transform.position) <= freezeRadius)
                {
                    ApplyFreezeToTarget(enemy.gameObject, enemy.transform.position);
                }
            }
        }

        private void ApplyFreezeToTarget(GameObject target, Vector3 position)
        {
            if (target.TryGetComponent<PlayerHealth>(out var health)) health.RPC_TakeDamage(damageAmount);
            if (target.TryGetComponent<PlayerCombat>(out var combat)) combat.ApplyStun(freezeDuration); 
            if (target.TryGetComponent<EnemyAI>(out var enemy)) enemy.RPC_TakeDamage(damageAmount);

            if (target.TryGetComponent<NetworkObject>(out var targetNetObj))
            {
                RPC_PlayStunAnim(targetNetObj);
            }

            RPC_ShowIceBlockLocal(position);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowIceBlockLocal(Vector3 targetPosition)
        {
            if (iceBlockPrefab != null)
            {
                GameObject iceEffect = Instantiate(iceBlockPrefab, targetPosition, Quaternion.identity);
                Destroy(iceEffect, freezeDuration); 
            }
        }

        // <-- THÊM: Vẽ vòng tròn tầm ảnh hưởng trong Editor để dễ canh chỉnh
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f); // Màu Cyan trong suốt
            Gizmos.DrawWireSphere(transform.position, freezeRadius);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayStunAnim(NetworkObject targetObj)
        {
            if (targetObj != null)
            {
                // Tìm Animator trên đối tượng mục tiêu thay vì tìm trên cục đạn
                var animator = targetObj.GetComponentInChildren<Animator>();
                if (animator != null) 
                {
                    animator.SetTrigger("die"); 
                }
            }
        }
    }
}