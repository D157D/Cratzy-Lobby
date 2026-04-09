using Crazy_Lobby.Item;
using Fusion;
using UnityEngine;

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

            NetworkId targetId = FindClosestTarget();

            if (!targetId.IsValid)
            {
                return;
            }

            if (ItemManager.Instance != null && ItemManager.Instance.fireworkProjectilePrefab.IsValid)
            {
                Quaternion randomRot = Quaternion.Euler(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-30f, 30f));
                _player.Runner.Spawn(ItemManager.Instance.fireworkProjectilePrefab,
                    _player.transform.position + Vector3.up, 
                    randomRot,
                    _player.Object.StateAuthority,
                    (runner, obj) =>
                    {
                        var firework = obj.GetComponent<FireworkProjectile>();
                        if (firework != null)
                        {
                            firework.TargetId = targetId;
                            firework.OwnerId = _player.Object.Id;
                        }
                    });
            }
            else
            {
                Debug.LogError("LỖI: Không tìm thấy ItemManager hoặc chưa gắn Prefab Firework vào ItemManager!");
            }
        }

        public void UseMagic()
        {
            if (!_player.HasStateAuthority) return;

            if (ItemManager.Instance != null && ItemManager.Instance.Magic.IsValid)
            {
                // Bắn về phía trước của người chơi
                Vector3 spawnPos = _player.transform.position + _player.transform.forward * 1.5f + Vector3.up * 1.2f;
                Quaternion spawnRot = _player.transform.rotation;

                _player.Runner.Spawn(ItemManager.Instance.Magic,
                    spawnPos,
                    spawnRot,
                    _player.Object.StateAuthority,
                    (runner, obj) =>
                    {
                        var magic = obj.GetComponent<Crazy_Lobby.Item.MagicProjectile>();
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

            // Kiểm tra list Players
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

            // Kiểm tra list Enemies
            foreach (var e in EnemyPatrol.ActiveEnemies)
            {
                if (e == null || e.Object == null) continue;

                // Kiểm tra xem enemy đã chết chưa (nếu có PlayerHealth)
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