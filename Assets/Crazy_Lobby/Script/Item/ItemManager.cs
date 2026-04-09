using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Item
{
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }
        public NetworkPrefabRef fireworkProjectilePrefab;
        public NetworkPrefabRef Magic;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);  
        }
    }
}