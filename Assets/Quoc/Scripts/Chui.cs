using Fusion;
using UnityEngine;

public class PickupItem : NetworkBehaviour
{
    public int itemAmount = 1;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<NetworkObject>();
        if (player != null)
        {
            var playerItem = player.GetComponent<PlayerItem>();
            if (playerItem != null)
            {
                playerItem.RPC_AddItem(itemAmount);
                Runner.Despawn(Object);
            }
        }
    }
}