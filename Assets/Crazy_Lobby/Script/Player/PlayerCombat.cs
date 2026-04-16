using Fusion;
using UnityEngine;
using Crazy_Lobby.Item;
using Crazy_Lobby.UI;

namespace Crazy_Lobby.Player.Components
{
    public class PlayerCombat : NetworkBehaviour
    {
        [Header("Item Settings")]
        public float itemCooldown = 3f;
        public float magicCooldown = 1f;

        [Networked] private TickTimer ItemCooldownTimer { get; set; }
        [Networked] private TickTimer MagicCooldownTimer { get; set; }
        
        [Networked] public int FireworkCount { get; set; }
        [Networked] public int MagicCount { get; set; }
        [Networked] public TickTimer StunTimer {get; set;}
        public bool IsStunned => StunTimer.IsRunning && !StunTimer.Expired(Runner);
        private PlayerItemUsage _playerItemUsage;
        private PlayerAudio _playerAudio;

        public void Initialize(PlayerController player, PlayerAudio audio)
        {
            _playerItemUsage = new PlayerItemUsage(player);
            _playerAudio = audio;
        }

        public void ApplyStun(float duration)
        {
            if(HasInputAuthority)
            {
                StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
            }
        }

        public void ProcessCombatInput(NetworkInputData data, bool isInLobby, CharacterAnimation animation)
        {
            if (!HasStateAuthority) return;

            if(IsStunned) return;

            if (data.UseItem)
            {
                if (isInLobby || FireworkCount > 0)
                {
                    if (ItemCooldownTimer.ExpiredOrNotRunning(Runner))
                    {
                        _playerItemUsage.UseFirework();
                        ItemCooldownTimer = TickTimer.CreateFromSeconds(Runner, itemCooldown);

                        if (_playerAudio != null) _playerAudio.PlayShoot(Runner.IsForward);

                        if (!isInLobby) FireworkCount--;
                    }
                }
            }

            if (data.Magic)
            {
                if (isInLobby || MagicCount > 0)
                {
                    if (MagicCooldownTimer.ExpiredOrNotRunning(Runner))
                    {
                        _playerItemUsage.UseMagic();
                        MagicCooldownTimer = TickTimer.CreateFromSeconds(Runner, magicCooldown);
                        animation.TriggerAttack();

                        if (_playerAudio != null) _playerAudio.PlayShoot(Runner.IsForward);

                        if (!isInLobby) MagicCount--;
                    }
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        public void RPC_PickUpItem(string itemName, int amount)
        {
            Debug.Log($"Bạn vừa nhặt được: {amount} {itemName}");
            
            if(ItemUIManager.Instance != null)
            {
                ItemUIManager.Instance.ShowItemPickup(itemName, amount);
            }
        }
    }
}