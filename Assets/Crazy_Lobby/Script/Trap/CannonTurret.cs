using Fusion;
using UnityEngine;

// Tự động gắn NetworkObject khi kéo script này vào model
[RequireComponent(typeof(NetworkObject))]
public class CannonTurret : NetworkBehaviour
{
    [Header("Các bộ phận của pháo")]
    [Tooltip("Trục xoay trái/phải (Pan)")]
    public Transform swivel; 
    [Tooltip("Nòng pháo ngẩng lên/xuống (Tilt)")]
    public Transform barrel; 
    [Tooltip("Vị trí đạn bay ra")]
    public Transform firePoint; 

    [Header("Cài đặt pháo")]
    public float rotationSpeed = 8f;
    public float fireRate = 1.5f; // Thời gian giữa 2 lần bắn (giây)
    public float attackRange = 20f;
    
    [Tooltip("Layer của người chơi để pháo nhận diện")]
    public LayerMask playerLayer; 
    [Tooltip("Prefab đạn (Bắt buộc phải có NetworkObject)")]
    public NetworkPrefabRef projectilePrefab; 

    // Các biến đồng bộ mạng (Chỉ State Authority được quyền ghi)
    [Networked] private Vector3 TargetPos { get; set; }
    [Networked] private NetworkBool HasTarget { get; set; }
    [Networked] private TickTimer FireTimer { get; set; }

    public override void FixedUpdateNetwork()
    {
        // Chỉ Host/Server (State Authority) mới được xử lý logic tìm mục tiêu và sinh đạn
        if (!HasStateAuthority) return;

        FindClosestPlayer();

        if (HasTarget)
        {
            // Kiểm tra xem cooldown bắn đã xong chưa
            if (FireTimer.ExpiredOrNotRunning(Runner))
            {
                Fire();
            }
        }
    }

    private void FindClosestPlayer()
    {
        // Nếu bạn quên thiết lập playerLayer trong Inspector, dùng OverlapSphere không filter layer
        Collider[] playersInRadius;
        if (playerLayer == 0)
            playersInRadius = Physics.OverlapSphere(transform.position, attackRange);
        else
            playersInRadius = Physics.OverlapSphere(transform.position, attackRange, playerLayer);

        float closestDistance = Mathf.Infinity;
        Transform closestPlayer = null;

        foreach (var col in playersInRadius)
        {
            // DEBUG: In ra để xem pháo đã quét thấy vật thể nào và có coi đó là người chơi không
            bool isPlayer = col.TryGetComponent<NetworkCharacterController>(out _);
            Debug.Log($"Pháo quét thấy '{col.name}'. Có phải người chơi không? -> {isPlayer}", col.gameObject);

            // Cần kiểm tra xem collider đó có thực sự là người chơi không
            if (!isPlayer) continue;

            float distance = (transform.position - col.transform.position).sqrMagnitude; // Dùng sqrMagnitude để tối ưu hiệu suất
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = col.transform;
            }
        }

        if (closestPlayer != null)
        {
            // Tùy chỉnh: Bạn có thể cộng thêm Vector3.up * 1f để pháo nhắm vào ngực thay vì dưới chân người chơi
            TargetPos = closestPlayer.position + Vector3.up * 1f; 
            HasTarget = true;
        }
        else
        {
            HasTarget = false;
        }
    }

    private void Fire()
    {
        // Reset timer cooldown
        FireTimer = TickTimer.CreateFromSeconds(Runner, fireRate);

        // Sinh đạn trên mạng (Đồng bộ cho mọi người chơi). 
        // Đạn sẽ do Host (StateAuthority) quản lý.
        Runner.Spawn(projectilePrefab, firePoint.position, firePoint.rotation, Object.StateAuthority);
    }

    // Render chạy ở Update thông thường trên MỌI CLIENT để hình ảnh xoay nòng mượt mà
    public override void Render()
    {
        if (HasTarget)
        {
            AimAtTarget();
        }
    }

    private void AimAtTarget()
    {
        // 1. Xoay trục ngang (Swivel - Chỉ xoay quanh trục Y)
        if (swivel != null)
        {
            Vector3 swivelDir = TargetPos - swivel.position;
            swivelDir.y = 0; // Khóa trục Y để nó không bị ngẩng lên
            if (swivelDir != Vector3.zero)
            {
                Quaternion targetSwivelRot = Quaternion.LookRotation(swivelDir);
                swivel.rotation = Quaternion.Slerp(swivel.rotation, targetSwivelRot, Time.deltaTime * rotationSpeed);
            }
        }

        // 2. Xoay nòng pháo (Barrel - Chỉ xoay quanh trục X cục bộ)
        if (barrel != null)
        {
            // Chuyển vị trí mục tiêu về không gian cục bộ (Local Space) của Swivel
            Vector3 localTargetPos = swivel.InverseTransformPoint(TargetPos);
            
            // Tính góc nghiêng bằng toán học (Atan2)
            float angleX = Mathf.Atan2(localTargetPos.y, localTargetPos.z) * Mathf.Rad2Deg;
            
            // Giới hạn góc để nòng không bị quay quá tay (ví dụ ngẩng tối đa 45 độ, chúc xuống -10 độ)
            angleX = Mathf.Clamp(angleX, -10f, 45f);

            // Tạo rotation cục bộ. 
            // Lưu ý: Tùy thuộc vào việc nòng súng của bạn được export từ Blender/Maya ra sao,
            // bạn có thể phải đổi dấu thành -angleX nếu nòng pháo bị xoay ngược.
            Quaternion localBarrelRot = Quaternion.Euler(-angleX, 0, 0); 
            
            barrel.localRotation = Quaternion.Slerp(barrel.localRotation, localBarrelRot, Time.deltaTime * rotationSpeed);
        }
    }

    // Vẽ vòng tròn tầm bắn trong Editor để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}