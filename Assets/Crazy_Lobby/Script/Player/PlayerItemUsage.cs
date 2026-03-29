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

            NetworkId targetId = FindClosestPlayerTarget();

            if (!targetId.IsValid)
            {
                return;
            }

            if (ItemManager.Instance != null && ItemManager.Instance.fireworkProjectilePrefab.IsValid)
            {
                _player.Runner.Spawn(ItemManager.Instance.fireworkProjectilePrefab,
                    _player.transform.position + Vector3.up, 
                    Quaternion.identity,
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

        private NetworkId FindClosestPlayerTarget()
        {
            float closestDistSqr = float.MaxValue;
            PlayerController closestPlayer = null;

            foreach (var p in PlayerController.ActivePlayers)
            {
                if (p == null || p.Object == _player.Object || p.IsDead) continue;

                float distSqr = (_player.transform.position - p.transform.position).sqrMagnitude;
                if (distSqr < closestDistSqr && distSqr < _targetingRange * _targetingRange)
                {
                    closestDistSqr = distSqr;
                    closestPlayer = p;
                }
            }

            return closestPlayer != null ? closestPlayer.Object.Id : default;
        }
    }
}