using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Item
{
    public class ItemManager : MonoBehaviour
    {
        // Tạo Singleton để có thể gọi từ bất kỳ đâu: ItemManager.Instance...
        public static ItemManager Instance { get; private set; }

        [Header("Item Prefabs")]
        [Tooltip("Prefab đạn pháo hoa (Bắt buộc phải có NetworkObject)")]
        public NetworkPrefabRef fireworkProjectilePrefab;

        private void Awake()
        {
            // Thiết lập Singleton
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}