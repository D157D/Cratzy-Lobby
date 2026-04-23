using Fusion;
using UnityEngine;

public class SpeedBoostStatus : NetworkBehaviour
{
    [Networked] public TickTimer BoostTimer { get; set; }
    
    [Header("Cấu hình")]
    public float speedMultiplier = 1.8f; // Tăng 80% tốc độ

    public bool IsBoosting => !BoostTimer.ExpiredOrNotRunning(Runner);

    public void ApplyBoost(float duration)
    {
        if (Object.HasStateAuthority)
        {
            BoostTimer = TickTimer.CreateFromSeconds(Runner, duration);
            RPC_PlayBoostVFX();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBoostVFX()
    {
        // Bạn có thể kích hoạt hiệu ứng luồng gió hoặc TrailRenderer ở đây
        Debug.Log("Đang tăng tốc!");
    }
}