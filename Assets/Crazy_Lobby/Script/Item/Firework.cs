using Crazy_Lobby.Item;
using Fusion;
using UnityEngine;

namespace Cratzy_Lobby.Item
{
    public class Firework : Items
    {
        public Firework() : base("Firework", 5f, "Shoot a firework directly to other player") { }
        // public override void FixedUpdateNetwork()
        // {
        //     if (data.UseItem)
        //     {
        //         // Nếu đang khoá mục tiêu, lập tức xoay mặt nhân vật về phía mục tiêu
        //         if (CurrentTargetId.IsValid)
        //         {
        //             NetworkObject targetObj = Runner.FindObject(CurrentTargetId);
        //             if (targetObj != null)
        //             {
        //                 Vector3 dirToTarget = targetObj.transform.position - transform.position;
        //                 dirToTarget.y = 0; // Cố định trục Y để nhân vật không bị ngửa ra sau
        //                 if (dirToTarget != Vector3.zero)
        //                 {
        //                     transform.rotation = Quaternion.LookRotation(dirToTarget);
        //                 }
        //             }
        //         }

        //         Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        //         foreach (var hitCollider in hitColliders)
        //         {
        //             if (hitCollider.TryGetComponent<Crazy_Lobby.Item.Items>(out var nearbyItem))
        //             {
        //                 nearbyItem.Use(this);
        //                 break; 
        //             }
        //         }
        //     }
        // }
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