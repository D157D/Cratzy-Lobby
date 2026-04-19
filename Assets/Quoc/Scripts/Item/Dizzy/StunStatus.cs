using Fusion;
using UnityEngine;

public class StunStatus : NetworkBehaviour, IStunnable
{
    [Networked] public TickTimer StunTimer { get; set; }
    
    public bool IsStunned => !StunTimer.ExpiredOrNotRunning(Runner);

    // Hàm này CHỈ CHẠY 1 LẦN DUY NHẤT lúc vừa đạp trúng bom
    public void ApplyStun(float duration)
    {
        if (Object.HasStateAuthority)
        {
            StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
            
            // Gọi Animation ở đây thì nó chỉ chạy 1 lần, không bị kẹt, không tốn băng thông!
            RPC_PlayStunAnim(); 
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayStunAnim()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null) 
        {
            // Truyền tên trigger của m vào
            animator.SetTrigger("die"); 
        }
    }
}