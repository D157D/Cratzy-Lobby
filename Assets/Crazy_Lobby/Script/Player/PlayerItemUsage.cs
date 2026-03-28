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
            // Logic chỉ được thực thi trên State Authority (Host/Server)
            if (!_player.HasStateAuthority) return;

            Debug.Log("3. Host/Server đang xử lý bắn pháo hoa...");
            NetworkId targetId = FindClosestPlayerTarget();

            // Bắt buộc phải có mục tiêu mới cho phép bắn pháo hoa
            if (!targetId.IsValid)
            {
                Debug.LogWarning("Không có mục tiêu trong tầm, hủy thao tác bắn pháo hoa.");
                return;
            }

            if (ItemManager.Instance != null && ItemManager.Instance.fireworkProjectilePrefab.IsValid)
            {
                Debug.Log($"4. Sinh đạn thành công! Mục tiêu khoá mục tiêu ID: {targetId}");
                _player.Runner.Spawn(ItemManager.Instance.fireworkProjectilePrefab,
                    _player.transform.position + Vector3.up, // Sinh ra ở phía trên đầu người chơi
                    Quaternion.identity,
                    _player.Object.InputAuthority,
                    (runner, obj) =>
                    {
                        // Khởi tạo projectile sau khi sinh ra
                        var firework = obj.GetComponent<FireworkProjectile>();
                        if (firework != null)
                        {
                            firework.TargetId = targetId;
                            // Gán OwnerId để đạn biết ai là người bắn và bỏ qua va chạm với người đó
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

            // Dùng danh sách người chơi đang hoạt động để tìm mục tiêu
            foreach (var p in PlayerController.ActivePlayers)
            {
                // Không nhắm vào chính mình, đối tượng null hoặc người chơi đã chết
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