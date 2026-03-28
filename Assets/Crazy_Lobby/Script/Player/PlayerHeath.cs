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

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            var hp = new PlayerHealthStruct { maxHealth = 5, currentHealth = 5 };
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
        if (IsDead) return;

        var hp = playerHealthStruct;
        hp.currentHealth -= damage;
        if (hp.currentHealth < 0) hp.currentHealth = 0;
        playerHealthStruct = hp;

        if (playerHealthStruct.currentHealth <= 0 && !IsDead)
        {
            IsDead = true;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Player {Object.Id} died on server.");
    }
}