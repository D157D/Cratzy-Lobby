using Fusion;
using UnityEngine;
using System.Collections.Generic;
using Crazy_Lobby.Player; // Nhớ thêm các namespace của bạn
using Crazy_Lobby.Enemy;

public class FinishLine : NetworkBehaviour
{
    private HashSet<NetworkId> finishedEntities = new HashSet<NetworkId>();

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            var networkObject = other.GetComponentInParent<NetworkObject>();

            if (networkObject != null)
            {
                if (finishedEntities.Contains(networkObject.Id)) return;

                finishedEntities.Add(networkObject.Id);

                // 👉 GỌI LỆNH DỪNG DI CHUYỂN
                // Thử tìm PlayerController
                if (other.TryGetComponent<PlayerController>(out var player))
                {
                    player.SetFinished();
                }
                // Thử tìm EnemyAI
                else if (other.GetComponentInParent<EnemyAI>() is EnemyAI enemy)
                {
                    enemy.SetFinished();
                }

                // Báo cho GameManager cộng điểm
                if (CountdownController.Instance != null)
                {
                    CountdownController.Instance.AddFinishedPlayer();
                }

                Debug.Log($"[FinishLine] {other.name} đã về đích và bị khóa di chuyển.");
            }
        }
    }
}