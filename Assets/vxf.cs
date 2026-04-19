using Fusion;
using UnityEngine;

public class AutoDespawnVFX : NetworkBehaviour
{
    [Tooltip("Thời gian tồn tại của tia sét (giây)")]
    public float lifeTime = 1f; 

    [Networked] private TickTimer lifeTimer { get; set; }

    public override void Spawned()
    {
        // Chỉ Server mới có quyền đếm ngược thời gian xóa
        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Khi hết giờ, Server sẽ ra lệnh xóa tia sét khỏi mọi máy tính
        if (Object.HasStateAuthority && lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }
}