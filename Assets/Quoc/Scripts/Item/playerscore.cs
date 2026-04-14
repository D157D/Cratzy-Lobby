using Fusion;
using UnityEngine;

public class PlayerItem : NetworkBehaviour
{
    [Networked] public int ItemCount { get; set; }

    // RPC để cộng item (server authoritative)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddItem(int amount)
    {
        ItemCount += amount;
        Debug.Log($"Item hiện có: {ItemCount}");
    }

    // RPC để dùng item
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UseItem()
    {
        if (ItemCount > 0)
        {
            ItemCount--;

            Debug.Log($"Đã dùng item! Còn lại: {ItemCount}");

            // 👉 Thêm effect ở đây (heal, buff, skill...)
        }
        else
        {
            Debug.Log("Không có item để dùng!");
        }
    }
}