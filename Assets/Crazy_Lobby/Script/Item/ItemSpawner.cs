using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Environment
{
    public class MapItemSpawner : NetworkBehaviour
    {
        [Header("--- CẤU HÌNH VẬT PHẨM ---")]
        [Tooltip("Kéo các Prefab vật phẩm vào đây")]
        public NetworkPrefabRef[] itemPrefabs; 

        [Header("--- CẤU HÌNH SỐ LƯỢNG ---")]
        [Tooltip("Số lượng tối đa vật phẩm spawner này được phép tạo ra. (Ví dụ: 1, 2, 10...)")]
        public int maxSpawnCount = 1;

        [Tooltip("Nếu tích vào đây, nó sẽ spawn mãi mãi (bỏ qua maxSpawnCount)")]
        public bool isInfinite = false;

        [Header("--- CẤU HÌNH THỜI GIAN ---")]
        [Tooltip("Bao lâu thì đẻ ra 1 vật phẩm? (Giây)")]
        public float spawnInterval = 10f; 

        // Biến mạng lưu số lượng đã spawn để đồng bộ giữa các người chơi
        [Networked] private int SpawnedSoFar { get; set; }
        
        // Bộ đếm thời gian mạng
        [Networked] private TickTimer SpawnTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                // Reset số lượng về 0 khi bắt đầu
                SpawnedSoFar = 0;
                // Đặt thời gian spawn lần đầu (sau 1 giây)
                SpawnTimer = TickTimer.CreateFromSeconds(Runner, 1f);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            // Kiểm tra nếu timer hết hạn
            if (SpawnTimer.Expired(Runner))
            {
                // KIỂM TRẢ ĐIỀU KIỆN: Nếu là vô hạn HOẶC chưa đẻ đủ số lượng tối đa
                if (isInfinite || SpawnedSoFar < maxSpawnCount)
                {
                    SpawnRandomItem();
                    SpawnedSoFar++; // Tăng số lượng đã đẻ lên 1

                    // Quyết định có đặt timer cho lần tiếp theo không
                    if (isInfinite || SpawnedSoFar < maxSpawnCount)
                    {
                        SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
                    }
                    else
                    {
                        // Đã đẻ đủ số lượng -> Ngừng đếm
                        SpawnTimer = TickTimer.None;
                        Debug.Log($"[Spawner] {gameObject.name} đã hoàn thành nhiệm vụ (Đã đẻ đủ {maxSpawnCount} cái).");
                    }
                }
            }
        }

        private void SpawnRandomItem()
        {
            if (itemPrefabs == null || itemPrefabs.Length == 0) return;

            // Bốc ngẫu nhiên item từ mảng
            int randomIndex = Random.Range(0, itemPrefabs.Length);
            NetworkPrefabRef prefabToSpawn = itemPrefabs[randomIndex];

            if (!prefabToSpawn.IsValid) return;

            Vector3 spawnPos = transform.position + Vector3.up * 1f;

            // Thực hiện Spawn
            Runner.Spawn(
                prefabToSpawn, 
                spawnPos, 
                Quaternion.identity, 
                null 
            );

            Debug.Log($"[Spawner] {gameObject.name} vừa spawn item ngẫu nhiên. Tổng cộng đã spawn: {SpawnedSoFar + 1}");
        }

        private void OnDrawGizmos()
        {
            // Vẽ màu khác nhau để bạn dễ phân biệt trong Editor
            Gizmos.color = isInfinite ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.5f);
            
            // Vẽ thêm icon nhỏ để biết nó là loại gì
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                isInfinite ? "Infinite Spawner" : $"Max: {maxSpawnCount}");
            #endif
        }
    }
}