using Fusion;
using UnityEngine;

public class PlayerInputHandler : NetworkBehaviour
{
    private PlayerItem playerItem;

    private void Awake()
    {
        playerItem = GetComponent<PlayerItem>();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            playerItem.RPC_UseItem();
        }
    }
}