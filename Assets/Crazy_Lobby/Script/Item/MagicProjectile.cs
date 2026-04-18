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
        [Header("Spell Settings")]
        public float freezeDuration = 3f; 
        public int damageAmount = 1;      
        public GameObject iceBlockPrefab; 
        public float spellLifeTime = 1.0f; 

        [Header("Audio")]
        public AudioClip castSound;       

        // ID của người/quái vật đã tung phép
        [Networked] public NetworkId OwnerId { get; set; }
        [Networked] private TickTimer DespawnTimer { get; set; }

        public override void Spawned()
        {
            if (castSound != null) 
                AudioSource.PlayClipAtPoint(castSound, transform.position);

            // Chỉ Server mới thực hiện tính toán logic đóng băng
            if (HasStateAuthority)
            {
                ExecuteGlobalFreeze();
                DespawnTimer = TickTimer.CreateFromSeconds(Runner, spellLifeTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && DespawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }

        private void ExecuteGlobalFreeze()
        {
            // --- 1. QUÉT NGƯỜI CHƠI ---
            foreach (var player in PlayerController.ActivePlayers)
            {
                // KIỂM TRA CHẶN CHỦ NHÂN:
                // Nếu ID của nhân vật này TRÙNG với OwnerId của phép thuật -> BỎ QUA
                if (player == null || player.IsDead || player.Object.Id == OwnerId) continue;

                // Thực thi logic đóng băng lên mục tiêu hợp lệ
                ApplyFreezeToTarget(player.gameObject, player.transform.position);
            }

            // --- 2. QUÉT QUÁI VẬT ---
            foreach (var enemy in EnemyAI.ActiveEnemies)
            {
                // Nếu chính con quái này tung phép -> BỎ QUA
                if (enemy == null || enemy.IsDead || enemy.Object.Id == OwnerId) continue;

                ApplyFreezeToTarget(enemy.gameObject, enemy.transform.position);
            }
        }

        private void ApplyFreezeToTarget(GameObject target, Vector3 position)
        {
            // Gây sát thương và Stun (Server side)
            if (target.TryGetComponent<PlayerHealth>(out var health)) health.RPC_TakeDamage(damageAmount);
            if (target.TryGetComponent<PlayerCombat>(out var combat)) combat.ApplyStun(freezeDuration);
            
            // Nếu là EnemyAI
            if (target.TryGetComponent<EnemyAI>(out var enemy)) enemy.RPC_TakeDamage(damageAmount);

            // Hiển thị cục băng bằng RPC (Tối ưu: chỉ hiện tại máy khách, không tốn tài nguyên mạng)
            RPC_ShowIceBlockLocal(position);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowIceBlockLocal(Vector3 targetPosition)
        {
            if (iceBlockPrefab != null)
            {
                GameObject iceEffect = Instantiate(iceBlockPrefab, targetPosition, Quaternion.identity);
                Destroy(iceEffect, freezeDuration); // Tự xóa sau khi hết đóng băng
            }
        }
    }
}