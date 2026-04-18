using Fusion;
using UnityEngine;
using Crazy_Lobby.Player.Components; // Hoặc thư viện chứa Enemy nếu cần
// Nếu EnemyPatrol của bạn nằm ở thư mục khác, hãy thêm using vào đây, ví dụ:
// using Crazy_Lobby.Enemy;

namespace Crazy_Lobby.Player
{
    public class CameraTargetLock : NetworkBehaviour
    {
        [Header("Cài đặt khóa mục tiêu")]
        public float lockRange = 20f;
        public KeyCode lockKey = KeyCode.Mouse0;
        public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

        private Transform _targetPlayer;
        private Camera _mainCamera;
        private CameraP _cameraScript;

        public Transform TargetPlayer => _targetPlayer;

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                _mainCamera = Camera.main;
                _cameraScript = FindObjectOfType<CameraP>();
            }
        }

        private void Update()
        {
            if (!HasInputAuthority || _mainCamera == null) return;

            if (Input.GetKeyDown(lockKey))
            {
                FindAndLockNearestPlayer();
            }
            else if (Input.GetKeyUp(lockKey))
            {
                UnlockTarget();
            }
        }

        public void UnlockTarget()
        {
            if (_targetPlayer != null)
            {
                _targetPlayer = null;
                Debug.Log("Đã hủy khóa mục tiêu.");
            }
            if (_cameraScript != null)
            {
                _cameraScript.IsTargetLocked = false;
            }
        }

        [System.Obsolete]
        private void LateUpdate()
        {
            if (!HasInputAuthority || _mainCamera == null || _targetPlayer == null) return;

            if (Vector3.Distance(transform.position, _targetPlayer.position) > lockRange)
            {
                UnlockTarget();
                return;
            }

            if (_cameraScript == null) _cameraScript = FindObjectOfType<CameraP>();

            if (_cameraScript != null) _cameraScript.IsTargetLocked = true;

            Vector3 directionToTarget = (_targetPlayer.position + targetOffset) - (transform.position + targetOffset);
            if (directionToTarget != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(directionToTarget);

                if (_cameraScript != null)
                {
                    _cameraScript.SetYawPitch(lookRot.eulerAngles.y, lookRot.eulerAngles.x);
                }
                else
                {
                    _mainCamera.transform.rotation = lookRot;
                }
            }
        }

        // 👉 HÀM KIỂM TRA MỤC TIÊU HỢP LỆ (NGƯỜI + QUÁI)
        private bool IsValidTarget(Collider col)
        {
            // Bỏ qua chính bản thân mình
            if (col.transform.root == transform.root) return false;

            // Kiểm tra xem có phải là Người chơi khác không
            if (col.GetComponentInParent<PlayerController>() != null) return true;

            // Kiểm tra xem có phải là Quái vật không (Bao gồm cả 2 loại quái của bạn)
            if (col.GetComponentInParent<EnemyAI>() != null) return true;
            if (col.GetComponentInParent<EnemyPatrol>() != null) return true;

            return false;
        }

        public void LockOnNearestPlayerInSight()
        {
            float closestDistanceSqr = lockRange * lockRange;
            Transform closestTarget = null;

            Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange);

            foreach (var col in colliders)
            {
                // Gọi hàm kiểm tra mục tiêu hợp lệ ở đây
                if (!IsValidTarget(col)) continue;

                Vector3 viewportPos = _mainCamera.WorldToViewportPoint(col.transform.position + targetOffset);
                bool isInSight = viewportPos.z > 0 && viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1;

                if (isInSight)
                {
                    if (!Physics.Linecast(transform.position + targetOffset, col.transform.position + targetOffset, out RaycastHit hit) || hit.transform.root == col.transform.root)
                    {
                        float distanceSqr = (transform.position - col.transform.position).sqrMagnitude;
                        if (distanceSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distanceSqr;
                            closestTarget = col.transform;
                        }
                    }
                }
            }

            if (closestTarget != null)
            {
                _targetPlayer = closestTarget;
                Debug.Log($"[Skill] Đã khóa camera vào mục tiêu gần nhất trong tầm nhìn: {_targetPlayer.name}");
            }
        }

        private void FindAndLockNearestPlayer()
        {
            float closestDistanceSqr = lockRange * lockRange;
            Transform closestTarget = null;

            Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange);

            foreach (var col in colliders)
            {
                // Gọi hàm kiểm tra mục tiêu hợp lệ ở đây
                if (!IsValidTarget(col)) continue;

                float distanceSqr = (transform.position - col.transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestTarget = col.transform;
                }
            }

            if (closestTarget != null)
            {
                _targetPlayer = closestTarget;
            }
        }
    }
}