using System;
using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public struct PlayerHealthStruct : INetworkStruct
    {
        public int maxHealth;
        public int currentHealth;
    }

    [Networked]
    public PlayerHealthStruct playerHealthStruct { get; set; }

    [Networked]
    public NetworkBool IsDead { get; private set; }

    public event Action<int, int> OnHealthUpdated;
    public static event Action<PlayerHealth> OnLocalPlayerSpawned;
    public event Action OnDeath;

    public static PlayerHealth LocalPlayer; 

    private ChangeDetector _changeDetector;

    [SerializeField] private int _initialMaxHealth = 3;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            var hp = new PlayerHealthStruct { maxHealth = _initialMaxHealth, currentHealth = _initialMaxHealth };
            playerHealthStruct = hp;
        }

        OnHealthUpdated?.Invoke(playerHealthStruct.currentHealth, playerHealthStruct.maxHealth);

        if (HasInputAuthority)
        {
            LocalPlayer = this; 
            
            OnLocalPlayerSpawned?.Invoke(this); 
        }

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            LocalPlayer = null; 
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(playerHealthStruct):
                    OnHealthUpdated?.Invoke(playerHealthStruct.currentHealth, playerHealthStruct.maxHealth);
                    break;
                
                case nameof(IsDead):
                    if (IsDead)
                    {
                        OnDeath?.Invoke();
                    }
                    break;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage)
    {
        // Debug.Log($"[PlayerHealth] RPC_TakeDamage được gọi trên {Object.Id} với sát thương: {damage}. IsDead: {IsDead}");

        if (IsDead)
        {
            // Debug.Log($"[PlayerHealth] Người chơi {Object.Id} đã chết, bỏ qua sát thương.");
            return;
        }

        var hp = playerHealthStruct;
        // Debug.Log($"[PlayerHealth] Máu hiện tại của {Object.Id} trước khi nhận sát thương: {hp.currentHealth}/{hp.maxHealth}");
        hp.currentHealth -= damage;
        if (hp.currentHealth < 0) hp.currentHealth = 0;
        playerHealthStruct = hp;
        // Debug.Log($"[PlayerHealth] Máu hiện tại của {Object.Id} sau khi nhận sát thương: {playerHealthStruct.currentHealth}/{playerHealthStruct.maxHealth}");

        if (playerHealthStruct.currentHealth <= 0 && !IsDead)
        {
            // Debug.Log($"[PlayerHealth] Người chơi {Object.Id} hết máu. Đang chuyển trạng thái sang chết.");
            IsDead = true;
            Die();
        }
    }
    void HealthChangedCallback()
    {
        OnHealthUpdated?.Invoke(playerHealthStruct.currentHealth, playerHealthStruct.maxHealth);
    }
    private void Die()
    {
        // Debug.Log($"Player {Object.Id} died on server.");
    }
}