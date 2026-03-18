using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Items
{
    public class Item : NetworkBehaviour
    {
        public string itemName = "Ball";
        public int amount = 1;

        public override void Spawned()
        {
            
        }

        public override void FixedUpdateNetwork()
        {
            transform.Rotate(0, 90 * Runner.DeltaTime, 0);
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (!HasStateAuthority) return;

            if(collider.CompareTag("Player"))
            {
                PlayerController player = collider.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    player.RPC_PickUpItem(itemName, amount);
                    
                    Runner.Despawn(Object);
                }
            }
        }
    }
}