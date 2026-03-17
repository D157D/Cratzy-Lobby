using Fusion;
using Unity.VisualScripting;
using UnityEngine;

namespace Crazy_Lobby.Items
{
    public class Item : NetworkBehaviour
    {
        public override void Spawned()
        {
            
        }

        public override void FixedUpdateNetwork()
        {
            
        }

        private void OnTriggerEnter(Collider collider)
        {
            if(collider.CompareTag("Player"))
            {
                
            }
        }
    }
}