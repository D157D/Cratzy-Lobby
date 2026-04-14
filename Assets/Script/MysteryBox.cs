using UnityEngine;
using Fusion;

public class MysteryBox : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (Runner == null || !Object || !Object.HasStateAuthority) return;

        if (other.TryGetComponent<PlayerItem>(out var playerItem))
        {
            if (playerItem.IsInventoryFull())
            {
                Debug.Log("Inventory đầy!");
                return;
            }

            ItemType randomItem = GetRandomItem();

            playerItem.AddItem(randomItem);

            Debug.Log($"[MysteryBox] Player nhận: {randomItem}");

            Runner.Despawn(Object);
        }
    }

    private ItemType GetRandomItem()
    {
        int rand = Random.Range(0, 2);

        switch (rand)
        {
            case 0: return ItemType.Shield;
            case 1: return ItemType.Boom;
        }

        return ItemType.None;
    }
}