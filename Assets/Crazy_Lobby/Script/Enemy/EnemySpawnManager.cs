using Fusion;
using UnityEngine;
using System.Collections.Generic;

namespace Crazy_Lobby.Enemy
{
    public class EnemySpawnManager : NetworkBehaviour
    {
        [Networked] public NetworkBool IsPrivateRoom { get; set; }

        [Header("Cài đặt Spawn Kẻ địch")]
        public NetworkPrefabRef enemyPrefab;
        public Transform[] spawnPoints;
        public Transform[] gameScenePatrolPoints; // Các điểm tuần tra cố định cho màn chơi
        public int maxEnemies = 5;
        public float spawnInterval = 5f;

        [Networked] private TickTimer spawnTimer { get; set; }
        [Networked] private int spawnedEnemyCount { get; set; }

        // Danh sách các kẻ địch đang hoạt động, để dễ dàng quản lý (ví dụ: khi phòng đóng)
        private readonly List<NetworkObject> _activeEnemies = new List<NetworkObject>();

        public override void Spawned()
        {
            // Chỉ Host/Server mới quản lý việc spawn kẻ địch
            if (!HasStateAuthority) return;

            Debug.Log($"[EnemySpawnManager] Spawned. IsPrivateRoom: {IsPrivateRoom}");

            if (!IsPrivateRoom)
            {
                // Bắt đầu timer spawn nếu không phải phòng riêng tư
                spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            // Nếu là phòng riêng tư, không làm gì cả
            if (IsPrivateRoom) return;

            // Spawn kẻ địch dần dần nếu timer hết hạn và chưa đạt số lượng tối đa
            if (spawnTimer.Expired(Runner) && spawnedEnemyCount < maxEnemies)
            {
                SpawnEnemy();
                spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval); // Reset timer
            }
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab.IsValid && spawnPoints.Length > 0)
            {
                Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                NetworkObject newEnemy = Runner.Spawn(enemyPrefab, randomSpawnPoint.position, randomSpawnPoint.rotation, Object.StateAuthority);
                _activeEnemies.Add(newEnemy);
                spawnedEnemyCount++;
                Debug.Log($"[EnemySpawnManager] Đã spawn kẻ địch thứ {spawnedEnemyCount} tại {randomSpawnPoint.position}");
            }
        }
    }
}