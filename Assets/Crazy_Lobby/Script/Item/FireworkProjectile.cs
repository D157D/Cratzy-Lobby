using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Item
{
    [RequireComponent(typeof(NetworkObject))]
    public class FireworkProjectile : NetworkBehaviour
    {
        [Header("Cài đặt đạn")]
        public float speed = 15f; // Tốc độ bay
        public float turnSpeed = 5f; // Tốc độ bẻ lái (Heat-seeking)
        public float lifeTime = 5f; // Thời gian tồn tại tối đa

        // Biến mạng lưu ID của mục tiêu để đạn tự động bay tới hoặc đuổi theo
        [Networked] public NetworkId TargetId { get; set; }
        
        [Networked] private TickTimer LifeTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                // Đặt thời gian tự hủy để tránh rác trong Scene
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // Chỉ Server (State Authority) mới có quyền huỷ đạn
            if (HasStateAuthority)
            {
                if (LifeTimer.Expired(Runner))
                {
                    Runner.Despawn(Object);
                    return;
                }
            }

            // Các Client cũng sẽ chạy logic bên dưới để tự mô phỏng đạn bay, giúp họ nhìn thấy đạn di chuyển mượt mà.

            // Nếu có mục tiêu, liên tục cập nhật hướng bay về phía mục tiêu (Heat-seeking)
            if (TargetId.IsValid)
            {
                NetworkObject targetObj = Runner.FindObject(TargetId);
                if (targetObj != null)
                {
                    Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.5f; // Nhắm vào ngực
                    Vector3 direction = (targetPos - transform.position).normalized;
                    
                    if (direction != Vector3.zero)
                    {
                        // Pháo có đầu hướng lên trên (trục Y), ta bẻ trục Y hướng về mục tiêu bằng cách thêm góc 90 độ X
                        Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                        // Xoay dần dần về phía mục tiêu
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * turnSpeed);
                    }
                }
            }

            // Do ta đã bẻ đầu pháo (trục Y) hướng tới mục tiêu, dùng transform.up để bay tới trước
            transform.position += transform.up * speed * Runner.DeltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Code xử lý khi viên đạn chạm vào người chơi khác (nổ, mất máu) ở đây...
        }
    }
}