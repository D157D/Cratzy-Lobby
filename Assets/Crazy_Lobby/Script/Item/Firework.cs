using Crazy_Lobby.Item;
using Fusion;
using UnityEngine;

namespace Cratzy_Lobby.Item
{
    public class Firework : Items
    {
        public NetworkPrefabRef _firework;
        public Firework() : base("Firework", 5f, "Shoot a firework directly to other player") { }

        public override void Use(Items item)
        {
            
        }

        private void SelectPlayer()
        {
            
        }
    }
}