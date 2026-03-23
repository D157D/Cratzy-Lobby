using Crazy_Lobby.Item;
using Fusion;
using UnityEngine;

namespace Cratzy_Lobby.Item
{
    public class Firework : Items
    {
        public Firework() : base("Firework", 5f, "Shoot a firework directly to other player") { }

        public override void Use(PlayerController player)
        {
            if (HasStateAuthority)
            {
                if (ItemManager.Instance != null)
                {
                    Vector3 startPos = player.transform.position + Vector3.up * 1f;
                    Vector3 shootDirection = player.transform.forward;
                    
                    if (player.CurrentTargetId.IsValid)
                    {
                        NetworkObject targetObj = Runner.FindObject(player.CurrentTargetId);
                        if (targetObj != null)
                        {
                            Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.5f;
                            Vector3 dirToTarget = (targetPos - startPos).normalized;
                            if (dirToTarget.sqrMagnitude > 0.001f)
                            {
                                shootDirection = dirToTarget;
                            }
                        }
                    }

                    Vector3 spawnPos = startPos + shootDirection * 1.5f;
                    
                    Quaternion spawnRotation = Quaternion.LookRotation(shootDirection) * Quaternion.Euler(90f, 0f, 0f);
                    
                    Runner.Spawn(ItemManager.Instance.fireworkProjectilePrefab, spawnPos, spawnRotation, Object.StateAuthority, (runner, obj) => {
                        if (obj.TryGetComponent<Crazy_Lobby.Item.FireworkProjectile>(out var projectile))
                        {
                            projectile.TargetId = player.CurrentTargetId;
                        }
                    });
                }
                else
                {
                    Debug.LogError("Không tìm thấy ItemManager trong Scene! Hãy tạo một GameObject và gắn script ItemManager vào.");
                }
            }
        }
    }
}