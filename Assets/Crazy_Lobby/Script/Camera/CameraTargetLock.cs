using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Player
{
    public class CameraTargetLock : NetworkBehaviour
    {
        [Header("Cài đặt khóa mục tiêu")]
        public float lockRange = 20f;
        public KeyCode lockKey = KeyCode.Mouse0; 
        public Vector3 targetOffset = new Vector3(0, 1.5f, 0); 

        private Transform _targetPlayer;
        private UnityEngine.Camera _mainCamera;
        private global::Camera _cameraScript;

        public Transform TargetPlayer => _targetPlayer;

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                _mainCamera = UnityEngine.Camera.main;
                _cameraScript = FindObjectOfType<global::Camera>();
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

        private void LateUpdate()
        {
            if (!HasInputAuthority || _mainCamera == null || _targetPlayer == null) return;

            if (Vector3.Distance(transform.position, _targetPlayer.position) > lockRange)
            {
                UnlockTarget();
                return;
            }

            if (_cameraScript == null) _cameraScript = FindObjectOfType<global::Camera>();

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

        public void LockOnNearestPlayerInSight()
        {
            float closestDistanceSqr = lockRange * lockRange;
            Transform closestPlayer = null;

            Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange);
            
            foreach (var col in colliders)
            {
                if (col.transform.root == transform.root) continue;

                if (col.GetComponentInParent<PlayerController>() != null)
                {
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
                                closestPlayer = col.transform;
                            }
                        }
                    }
                }
            }

            if (closestPlayer != null)
            {
                _targetPlayer = closestPlayer;
                Debug.Log($"[Skill] Đã khóa camera vào mục tiêu gần nhất trong tầm nhìn: {_targetPlayer.name}");
            }
        }

        private void FindAndLockNearestPlayer()
        {
            float closestDistanceSqr = lockRange * lockRange;
            Transform closestPlayer = null;

            Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange);
            
            foreach (var col in colliders)
            {
                if (col.transform.root == transform.root) continue;

                if (col.GetComponentInParent<PlayerController>() != null)
                {
                    float distanceSqr = (transform.position - col.transform.position).sqrMagnitude;
                    if (distanceSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distanceSqr;
                        closestPlayer = col.transform;
                    }
                }
            }

            if (closestPlayer != null)
            {
                _targetPlayer = closestPlayer;
            }
        }
    }
}