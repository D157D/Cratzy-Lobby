using UnityEngine;
using UnityEngine.AI;
using Crazy_Lobby.Player;
using Crazy_Lobby.Item;
using Fusion;
using System.Collections.Generic;
using Crazy_Lobby.Enemy;

public class EnemyPatrol : NetworkBehaviour // Removed RequireComponent as it's not always needed if spawned dynamically
{
    public enum PatrolMode
    {
        Random,
        FixedPoints
    }
    public static readonly List<EnemyPatrol> ActiveEnemies = new List<EnemyPatrol>();

    private NavMeshAgent agent;
    private NetworkCharacterController _ncc;
    private CharacterAnimation _characterAnimation;
    private EnemyCharacterHandler _characterHandler;

    public PatrolMode currentMode = PatrolMode.Random;

    public float patrolRadius = 20f; 
    
    public float patrolTimeout = 10f; 

    public float visionRange = 10f;

    public float moveSpeed = 3f;   
    public float rotationSpeed = 15f; // Tốc độ quay của kẻ địch khi nhắm mục tiêu
    public float acceleration = 100f;
    public float braking = 100f;

    private Transform targetPlayer;
    private bool isChasing = false;
    private Vector3[] fixedPoints = new Vector3[3];
    private int currentPointIndex = 0;
    private float currentPatrolTimer = 0f;

    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float attackCooldown = 3f;
    [Networked] private NetworkBool IsAttacking { get; set; } // Trạng thái kẻ địch đang tấn công
    [Networked] private TickTimer postAttackPatrolTimer { get; set; } // Thời gian kẻ địch tuần tra sau khi tấn công
    [Networked] private TickTimer attackTimer { get; set; }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        _ncc = GetComponent<NetworkCharacterController>();
        _characterHandler = GetComponent<EnemyCharacterHandler>();

        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
        
        if (_characterHandler != null)
        {
            _characterHandler.OnModelChanged += HandleModelChanged;
            // If model is already spawned, update animator
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
                _characterAnimation.SetAnimator(animator);
        }

        agent.updateRotation = false; 
        agent.updatePosition = false; 

        _ncc.maxSpeed = moveSpeed;
        _ncc.acceleration = acceleration;
        _ncc.braking = braking;

        if (currentMode == PatrolMode.FixedPoints)
        {
            GenerateFixedPoints();
            agent.SetDestination(fixedPoints[currentPointIndex]);
            currentPatrolTimer = 0f;
        }
        else
        {
            SetRandomDestination();
        }
    }

    private void HandleModelChanged(GameObject newModel)
    {
        if (_characterAnimation != null)
        {
            var animator = newModel.GetComponentInChildren<Animator>();
            if (animator == null) animator = newModel.GetComponent<Animator>();
            _characterAnimation.SetAnimator(animator);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_characterHandler != null)
        {
            _characterHandler.OnModelChanged -= HandleModelChanged;
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        agent.nextPosition = transform.position; // Đồng bộ vị trí NavMeshAgent với Fusion

        Vector3 moveDirection = Vector3.zero; // Khởi tạo moveDirection

        if (IsAttacking)
        {
            // Quay về phía người chơi mục tiêu
            if (targetPlayer != null)
            {
                Vector3 lookDirection = targetPlayer.position - transform.position;
                lookDirection.y = 0; // Giữ cho việc quay chỉ trên mặt phẳng ngang
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * rotationSpeed);
                }
            }

            // Nếu thời gian hồi chiêu tấn công đã hết, kết thúc trạng thái tấn công và quay lại tuần tra
            if (attackTimer.Expired(Runner))
                EndAttackAndResumePatrol();
            
            // Không di chuyển trong khi tấn công, nên moveDirection vẫn là Vector3.zero
        }
        else if (postAttackPatrolTimer.IsRunning)
        {
            // Kẻ địch đang trong thời gian tuần tra sau khi tấn công, bỏ qua việc tìm người chơi
            HandlePatrolMovement(out moveDirection);
        }
        else // Không tấn công và không trong thời gian hồi chiêu sau tấn công
        {
            FindAndChaseClosestPlayer(); // Tìm và quyết định có đuổi theo/tấn công không

            if (isChasing && targetPlayer != null)
            {
                // Nếu đang đuổi theo, đặt đích đến là người chơi
                agent.SetDestination(targetPlayer.position);
                if (agent.hasPath || agent.pathPending)
                {
                    Vector3 targetDir = agent.steeringTarget - transform.position;
                    targetDir.y = 0f;
                    if (targetDir.sqrMagnitude > 0.001f)
                    {
                        moveDirection = targetDir.normalized;
                    }
                }
            }
            else // Không đuổi theo (không tìm thấy người chơi hoặc vừa kết thúc đuổi theo)
            {
                HandlePatrolMovement(out moveDirection); // Tiếp tục/bắt đầu tuần tra bình thường
            }
        }

        _ncc.Move(moveDirection);
    }

    private void TryAttackPlayer(Transform targetTransform)
    {
        // Nếu không có mục tiêu, đang trong thời gian hồi chiêu, hoặc đã trong trạng thái tấn công, thì không làm gì
        if (targetTransform == null || attackTimer.IsRunning || IsAttacking) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        
        if (distance <= attackRange)
        {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 targetPos = targetTransform.position + Vector3.up * 1.2f; // Nhắm vào người chơi

            // Kẻ địch sẽ quay mặt về hướng mục tiêu trong FixedUpdateNetwork khi IsAttacking là true.
            // Ở đây chỉ kiểm tra tầm nhìn và bắn.
            Vector3 direction = (targetPos - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange))
            {
                var hitPlayer = hit.transform.GetComponentInParent<NetworkCharacterController>();
                if (hitPlayer != null && hitPlayer.transform == targetTransform)
                {
                    IsAttacking = true; // Đặt trạng thái đang tấn công. FixedUpdateNetwork sẽ xử lý dừng di chuyển và quay.
                    agent.isStopped = true; // Dừng NavMeshAgent

                    if (ItemManager.Instance != null && ItemManager.Instance.fireworkProjectilePrefab.IsValid)
                    {
                        Quaternion randomRot = Quaternion.Euler(
                            Random.Range(-60f, 60f),
                            Random.Range(0f, 360f),
                            Random.Range(-60f, 60f));

                        Runner.Spawn(
                            ItemManager.Instance.fireworkProjectilePrefab,
                            origin, 
                            randomRot,
                            Object.StateAuthority,
                            (runner, obj) =>
                            {
                                var firework = obj.GetComponent<FireworkProjectile>();
                                if (firework != null)
                                {
                                    firework.TargetId = hitPlayer.Object.Id;
                                    firework.OwnerId = Object.Id;
                                }
                            });
                    }

                    attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown); // Đặt thời gian hồi chiêu
                }
            }
        }
    }

    private void FindAndChaseClosestPlayer()
    {
        Collider[] playersInRadius = Physics.OverlapSphere(transform.position, visionRange);
        float closestDistanceSqr = Mathf.Infinity;
        Transform closestPlayer = null;

        // Tìm người chơi gần nhất trong tầm nhìn
        foreach (var col in playersInRadius)
        {
            if (col.CompareTag("Player"))
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
            targetPlayer = closestPlayer; // Luôn đặt mục tiêu nếu tìm thấy

            // Thử tấn công người chơi. TryAttackPlayer sẽ đặt IsAttacking = true nếu thành công.
            TryAttackPlayer(closestPlayer);

            // Nếu vừa bắt đầu tấn công (IsAttacking là true), hoặc đang tấn công, thì không đuổi theo
            if (IsAttacking)
            {
                isChasing = false; // Đang trong trạng thái tấn công, không phải đuổi theo
                // NavMeshAgent đã được dừng bởi TryAttackPlayer.
                // Di chuyển sẽ được xử lý bởi khối IsAttacking trong FixedUpdateNetwork.
            }
            else
            {
                // Nếu tìm thấy người chơi nhưng không thể tấn công (ví dụ: đang hồi chiêu), thì đuổi theo
                isChasing = true;
                // agent.SetDestination sẽ được gọi trong FixedUpdateNetwork nếu isChasing là true
            }
        }
        else // Không tìm thấy người chơi trong tầm nhìn
        {
            targetPlayer = null; // Xóa mục tiêu
            isChasing = false; // Dừng đuổi theo
        }
    }
    
    private void EndAttackAndResumePatrol()
    {
        IsAttacking = false; // Kết thúc trạng thái tấn công
        agent.isStopped = false; // Tiếp tục di chuyển NavMeshAgent
        postAttackPatrolTimer = TickTimer.CreateFromSeconds(Runner, 5f); // Bắt đầu thời gian tuần tra sau tấn công (5 giây)
        isChasing = false; // Dừng đuổi theo

        if (currentMode == PatrolMode.Random)
        {
            SetRandomDestination();
        }
        else
        {
            agent.SetDestination(fixedPoints[currentPointIndex]);
        }
        currentPatrolTimer = 0f; // Đặt lại bộ đếm thời gian tuần tra để tìm điểm đến mới ngay lập tức.
    }

    private void HandlePatrolMovement(out Vector3 moveDirection)
    {
        moveDirection = Vector3.zero;

        if (agent.hasPath || agent.pathPending)
        {
            Vector3 targetDir = agent.steeringTarget - transform.position;
            targetDir.y = 0f;
            if (targetDir.sqrMagnitude > 0.001f)
            {
                moveDirection = targetDir.normalized;
            }
        }

        currentPatrolTimer += Runner.DeltaTime;

        bool isUnreachable = agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid;
        bool isTimeout = currentPatrolTimer >= patrolTimeout;

        if (agent.remainingDistance < 0.5f || isUnreachable || isTimeout)
        {
            if (currentMode == PatrolMode.Random)
            {
                SetRandomDestination();
            }
            else
            {
                currentPointIndex = (currentPointIndex + 1) % fixedPoints.Length;
                agent.SetDestination(fixedPoints[currentPointIndex]);
            }
            currentPatrolTimer = 0f; // Đặt lại bộ đếm thời gian tuần tra
        }
    }

    public override void Render()
    {
        if (_characterAnimation != null && _ncc != null)
        {
            _characterAnimation.UpdateMoveAnimation(_ncc.Velocity, moveSpeed);
        }
    }

    void GenerateFixedPoints()
    {
        for (int i = 0; i < 3; i++)
        {
            if (TryGetValidPatrolPoint(out Vector3 point))
            {
                fixedPoints[i] = point;
            }
            // Fallback nếu không tìm được điểm hợp lệ, đặt tại vị trí hiện tại
            else
            {
                fixedPoints[i] = transform.position;
            }
            // Đảm bảo NavMeshAgent có đường đi hợp lệ đến điểm cố định ban đầu
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, fixedPoints[i], NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                agent.SetDestination(fixedPoints[i]);
        }
    }

    void SetRandomDestination()
    {
        if (TryGetValidPatrolPoint(out Vector3 point))
        {
            agent.SetDestination(point);
            currentPatrolTimer = 0f; 
        }
    }

    bool TryGetValidPatrolPoint(out Vector3 result)
    {
        for (int i = 0; i < 15; i++) 
        {
            Vector2 randomPlane = Random.insideUnitCircle * patrolRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomPlane.x, 0, randomPlane.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        result = new Vector3(hit.position.x, transform.position.y, hit.position.z);
                        return true;
                    }
                }
            }
        }

        result = transform.position;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        if (Application.isPlaying)
        {
            if (currentMode == PatrolMode.FixedPoints && fixedPoints != null && fixedPoints.Length > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < fixedPoints.Length; i++)
                {
                    Gizmos.DrawSphere(fixedPoints[i], 0.5f);
                    int nextIndex = (i + 1) % fixedPoints.Length;
                    Gizmos.DrawLine(fixedPoints[i], fixedPoints[nextIndex]);
                }
            }
            else if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(agent.destination, 0.5f);
                Gizmos.DrawLine(transform.position, agent.steeringTarget);
            }
        }
    }
}