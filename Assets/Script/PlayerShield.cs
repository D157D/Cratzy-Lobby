using UnityEngine;
using Fusion;

public class PlayerShield : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject shieldVisual;

    [Networked] public NetworkBool hasShield { get; set; }   // Có item hay không

    [Networked, OnChangedRender(nameof(OnShieldChanged))]
    public NetworkBool isActive { get; set; }                // Đang bật khiên

    private TickTimer shieldTimer;

    // ================= INIT =================
    public override void Spawned()
    {
        // Đảm bảo trạng thái ban đầu
        if (shieldVisual != null)
            shieldVisual.SetActive(false);
    }

    // ================= HIỂN THỊ =================
    void OnShieldChanged()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(isActive);
        }

        Debug.Log("Shield trạng thái: " + isActive);
    }

    // ================= SERVER LOGIC =================
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Hết thời gian thì tắt
        if (isActive && shieldTimer.Expired(Runner))
        {
            isActive = false;
            Debug.Log("Shield OFF");
        }
    }

    // ================= KÍCH HOẠT =================
    public void ActivateShield()
    {
        if (Object.HasStateAuthority)
        {
            ActivateOnServer();
        }
        else
        {
            RPC_ActivateShield();
        }
    }

    // ================= RPC =================
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_ActivateShield()
    {
        ActivateOnServer();
    }

    // ================= SERVER THỰC THI =================
    void ActivateOnServer()
    {
        if (!hasShield || isActive) return;

        isActive = true;
        hasShield = false;

        shieldTimer = TickTimer.CreateFromSeconds(Runner, 3f);

        Debug.Log("Shield Activated!");
    }

    // ================= 🔥 QUAN TRỌNG (CHO BOMB) =================
    public bool IsShieldActive()
    {
        return isActive;
    }
}