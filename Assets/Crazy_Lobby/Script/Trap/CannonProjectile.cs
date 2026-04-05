using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Trap
{
    public class CannonProjectile : NetworkBehaviour
    {
        [Header("Thông số đạn")]
        public float speed = 20f;
        public float lifeTime = 3f;
        public float knockbackForce = 15f;

        [Networked] private TickTimer LifeTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            // Hủy đạn sau một khoảng thời gian để dọn dẹp bộ nhớ
            if (LifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            // Di chuyển đạn về phía trước theo thời gian của server (đồng bộ cho tất cả)
            transform.position += transform.forward * speed * Runner.DeltaTime;
        }

        // Xử lý va chạm
        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            // Nếu trúng người chơi, đẩy lùi người chơi tương tự như cái Búa (Hammer)
            if (other.TryGetComponent<NetworkCharacterController>(out var ncc))
            {
                Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;
                knockbackDirection.y = 0;
                ncc.Velocity += knockbackDirection * knockbackForce;
                
                // Hủy đạn sau khi trúng mục tiêu
                Runner.Despawn(Object);
            }
        }
    }
}