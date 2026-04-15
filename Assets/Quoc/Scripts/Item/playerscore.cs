using Crazy_Lobby.Item;
using Fusion;
using UnityEngine;

public class PlayerItem : NetworkBehaviour
{
    [Networked] public int ItemCount { get; set; }

    private readonly float _targetingRange = 50f;

    // RPC để cộng item (server authoritative)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddItem(int amount)
    {
        ItemCount += amount;
        Debug.Log($"Item hiện có: {ItemCount}");
    }

    // RPC để dùng item – bắn FireworkProjectile
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UseItem()
    {
        if (ItemCount <= 0)
        {
            Debug.Log("Không có item để dùng!");
            return;
        }

        if (ItemManager.Instance == null || !ItemManager.Instance.fireworkProjectilePrefab.IsValid)
        {
            Debug.LogError("LỖI: Không tìm thấy ItemManager hoặc chưa gắn Prefab Firework!");
            return;
        }

        ItemCount--;
        Debug.Log($"Đã dùng item! Còn lại: {ItemCount}");

        // Tìm target gần nhất
        NetworkId targetId = FindClosestTarget();

        // Tính góc spawn: có target → góc ngẫu nhiên (firework tự truy đuổi)
        //                  không có target → bắn thẳng hướng player đang nhìn
        Quaternion spawnRot;
        if (targetId.IsValid)
        {
            spawnRot = Quaternion.Euler(
                Random.Range(-30f, 30f),
                Random.Range(0f, 360f),
                Random.Range(-30f, 30f)
            );
        }
        else
        {
            spawnRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(90f, 0f, 0f);
        }

        Runner.Spawn(
            ItemManager.Instance.fireworkProjectilePrefab,
            transform.position + Vector3.up,
            spawnRot,
            Object.StateAuthority,
            (runner, obj) =>
            {
                var firework = obj.GetComponent<FireworkProjectile>();
                if (firework != null)
                {
                    firework.OwnerId = Object.Id;
                    firework.TargetId = targetId; // default nếu không có target
                }
            });
    }

    private NetworkId FindClosestTarget()
    {
        float closestDistSqr = _targetingRange * _targetingRange;
        NetworkObject closestObj = null;

        // Tìm trong các PlayerController đang active
        foreach (var p in PlayerController.ActivePlayers)
        {
            if (p == null || p.Object == Object || p.IsDead) continue;

            float distSqr = (transform.position - p.transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closestObj = p.Object;
            }
        }

        return closestObj != null ? closestObj.Id : default;
    }
}