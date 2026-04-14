using UnityEngine;
using Fusion;

public class MysteryBoxSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef boxPrefab; // Kéo Prefab MysteryBox vào đây
    [Networked] private TickTimer respawnTimer { get; set; }
    [Networked] private NetworkObject currentBox { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Nếu hiện tại không có hộp và chưa bắt đầu đếm ngược
        if (currentBox == null && respawnTimer.ExpiredOrNotRunning(Runner))
        {
            // Bắt đầu đếm ngược 15 giây
            respawnTimer = TickTimer.CreateFromSeconds(Runner, 15f);
        }

        // Khi hết thời gian đếm ngược, hồi sinh hộp
        if (currentBox == null && respawnTimer.Expired(Runner))
        {
            currentBox = Runner.Spawn(boxPrefab, transform.position, transform.rotation);
            respawnTimer = TickTimer.None; // Reset timer
        }
    }
}