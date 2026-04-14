using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Item
{
    public class ShootItemPickup : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float rotationSpeed = 100f;
        
        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Runner.DeltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            // Check if it's a player
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                if (!player.CanShoot)
                {
                    player.CanShoot = true;
                    Debug.Log($"Player {player.Object.Id} picked up shoot item!");
                    Runner.Despawn(Object);
                }
                return;
            }

            // Check if it's an enemy
            EnemyPatrol enemy = other.GetComponentInParent<EnemyPatrol>();
            if (enemy != null)
            {
                if (!enemy.CanShoot)
                {
                    enemy.CanShoot = true;
                    Debug.Log($"Enemy {enemy.Object.Id} picked up shoot item!");
                    Runner.Despawn(Object);
                }
                return;
            }
        }
    }
}
