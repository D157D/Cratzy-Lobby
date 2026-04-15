using Crazy_Lobby.Item;
using Fusion;
using UnityEngine;
using Crazy_Lobby.Enemy;

namespace Crazy_Lobby.Player.Components
{
    public class PlayerItemUsage
    {
        private readonly PlayerController _player;
        private readonly float _targetingRange = 50f;

        public PlayerItemUsage(PlayerController player)
        {
            _player = player;
        }

        public void UseFirework()
        {
            if (!_player.HasStateAuthority) return;

            if (ItemManager.Instance == null || !ItemManager.Instance.fireworkProjectilePrefab.IsValid)
            {
                Debug.LogError("LỖI: Không tìm thấy ItemManager hoặc chưa gắn Prefab Firework!");
                return;
            }

            NetworkId targetId = FindClosestTarget();

            // Nếu có target: bắn với góc ngẫu nhiên và tự truy đuổi
            // Nếu không có target: bắn thẳng về phía trước của player
            Quaternion spawnRot;
            if (targetId.IsValid)
            {
                spawnRot = Quaternion.Euler(
                    Random.Range(-30f, 30f),
                    Random.Range(0f, 360f),
                    Random.Range(-30f, 30f)
                );
            }
            else
            {
                // Bắn thẳng về hướng player đang nhìn (forward = up của firework vì projectile bay theo trục Y)
                spawnRot = Quaternion.LookRotation(_player.transform.forward) * Quaternion.Euler(90f, 0f, 0f);
            }

            _player.Runner.Spawn(
                ItemManager.Instance.fireworkProjectilePrefab,
                _player.transform.position + Vector3.up,
                spawnRot,
                _player.Object.StateAuthority,
                (runner, obj) =>
                {
                    var firework = obj.GetComponent<FireworkProjectile>();
                    if (firework != null)
                    {
                        firework.TargetId = targetId; // Có thể là default nếu không có target
                        firework.OwnerId = _player.Object.Id;
                    }
                });
        }

        public void UseMagic()
        {
            if (!_player.HasStateAuthority) return;

            if (ItemManager.Instance != null && ItemManager.Instance.Magic.IsValid)
            {
                Vector3 spawnPos = _player.transform.position 
                    + _player.transform.forward * 1.5f 
                    + Vector3.up * 1.2f;

                Quaternion spawnRot = _player.transform.rotation;

                _player.Runner.Spawn(
                    ItemManager.Instance.Magic,
                    spawnPos,
                    spawnRot,
                    _player.Object.StateAuthority,
                    (runner, obj) =>
                    {
                        var magic = obj.GetComponent<MagicProjectile>();
                        if (magic != null)
                        {
                            magic.OwnerId = _player.Object.Id;
                        }
                    });
            }
            else
            {
                Debug.LogWarning("Chưa gắn prefab Magic trong ItemManager!");
            }
        }

        private NetworkId FindClosestTarget()
        {
            float closestDistSqr = float.MaxValue;
            NetworkObject closestObj = null;

            // Player
            foreach (var p in PlayerController.ActivePlayers)
            {
                if (p == null || p.Object == _player.Object || p.IsDead) continue;

                float distSqr = (_player.transform.position - p.transform.position).sqrMagnitude;
                if (distSqr < closestDistSqr && distSqr < _targetingRange * _targetingRange)
                {
                    closestDistSqr = distSqr;
                    closestObj = p.Object;
                }
            }

            // Enemy (FIXED)
            foreach (var e in EnemyPatrol.ActiveEnemies)
            {
                if (e == null || e.Object == null || e.Object.Id == _player.Object.Id) continue;

                var health = e.GetComponent<PlayerHealth>();
                if (health != null && health.IsDead) continue;

                float distSqr = (_player.transform.position - e.transform.position).sqrMagnitude;
                if (distSqr < closestDistSqr && distSqr < _targetingRange * _targetingRange)
                {
                    closestDistSqr = distSqr;
                    closestObj = e.Object;
                }
            }

            return closestObj != null ? closestObj.Id : default;
        }
    }
}