using Crazy_Lobby.Item;
using Fusion;
using UnityEngine;

namespace Cratzy_Lobby.Item
{
    public class Firework : Items
    {
        public NetworkPrefabRef _firework;
        public Firework(string _ability, float _timer, string _des) : base(_ability, _timer, _des)
        {
            _ability = "Firework";
            _timer = 5f;
            _des = "Shoot a firework directly to other player";
        }

        public override void Use(Items item)
        {
            
        }
    }
}