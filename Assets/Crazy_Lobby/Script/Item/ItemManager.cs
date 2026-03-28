using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Item
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }
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