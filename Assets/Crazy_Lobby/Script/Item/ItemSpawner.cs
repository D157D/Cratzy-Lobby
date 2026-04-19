using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Environment
{
    public class MapItemSpawner : NetworkBehaviour
    {
        [Header("--- CẤU HÌNH SPAWNER ---")]
        [Tooltip("Kéo TẤT CẢ các Prefab vật phẩm vào danh sách này")]
        public NetworkPrefabRef[] itemPrefabs; 
        
        [Tooltip("Bao lâu thì đẻ ra 1 vật phẩm? (Giây)")]
        public float spawnInterval = 10f; 

        // Bộ đếm thời gian
        [Networked] private TickTimer SpawnTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                // Cho sinh đồ ngay lần đầu tiên sau 1 giây
                SpawnTimer = TickTimer.CreateFromSeconds(Runner, 1f);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            // Đếm giờ đẻ đồ
            if (SpawnTimer.Expired(Runner))
            {
                SpawnRandomItem();
                SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
            }
        }

        private void SpawnRandomItem()
        {
            if (itemPrefabs == null || itemPrefabs.Length == 0)
            {
                Debug.LogWarning("[Spawner] Danh sách Item trống trơn! Hãy kéo Prefab vào.");
                return;
            }

            // 1. Quay xổ số: Bốc ngẫu nhiên 1 loại đồ trong danh sách
            int randomIndex = Random.Range(0, itemPrefabs.Length);
            NetworkPrefabRef prefabToSpawn = itemPrefabs[randomIndex];

            if (!prefabToSpawn.IsValid) return;

            // 2. TỌA ĐỘ CỐ ĐỊNH: Ngay tại vị trí cục Spawner (nhích lên 1m cho khỏi lún sàn)
            Vector3 spawnPos = transform.position + Vector3.up * 1f;

            // 3. Đẻ ra đồ
            Runner.Spawn(
                prefabToSpawn, 
                spawnPos, 
                Quaternion.identity, 
                null 
            );

            Debug.Log($"[Spawner] Đã spawn item (Index: {randomIndex}) TẠI CHỖ: {spawnPos}");
        }

        // Vẽ một khối cầu nhỏ màu xanh ngay tại tâm để bạn dễ căn chỉnh trên Unity
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.5f);
        }
    }
}