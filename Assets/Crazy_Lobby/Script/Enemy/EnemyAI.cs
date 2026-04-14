using UnityEngine;
using UnityEngine.AI;
using Crazy_Lobby.Player;
using Crazy_Lobby.Item;
using Fusion;
using System.Collections.Generic;
using Crazy_Lobby.Enemy;

public class EnemyPatrol : NetworkBehaviour
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
    public float rotationSpeed = 15f;
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
    [Networked] private NetworkBool IsAttacking { get; set; }
    [Networked] private TickTimer postAttackPatrolTimer { get; set; }
    [Networked] private TickTimer attackTimer { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer StunTimer { get; set; }

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

        ActiveEnemies.Add(this);
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
        ActiveEnemies.Remove(this);
    }


    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsDead || !StunTimer.ExpiredOrNotRunning(Runner))
        {
            _ncc.Move(Vector3.zero);
            return;
        }

        agent.nextPosition = transform.position;

        Vector3 moveDirection = Vector3.zero;

        if (IsAttacking)
        {
            if (targetPlayer != null)
            {
                Vector3 lookDirection = targetPlayer.position - transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * rotationSpeed);
                }
            }

            if (attackTimer.Expired(Runner))
                EndAttackAndResumePatrol();
            
        }
        else if (postAttackPatrolTimer.IsRunning)
        {
            HandlePatrolMovement(out moveDirection);
        }
        else
        {
            FindAndChaseClosestPlayer();

            if (isChasing && targetPlayer != null)
            {
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
            else
            {
                HandlePatrolMovement(out moveDirection);
            }
        }

        _ncc.Move(moveDirection);
    }

    private void TryAttackPlayer(Transform targetTransform)
    {
        if (targetTransform == null || attackTimer.IsRunning || IsAttacking) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        
        if (distance <= attackRange)
        {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 targetPos = targetTransform.position + Vector3.up * 1.2f;

            Vector3 direction = (targetPos - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange))
            {
                var hitPlayer = hit.transform.GetComponentInParent<NetworkCharacterController>();
                if (hitPlayer != null && hitPlayer.transform == targetTransform)
                {
                    IsAttacking = true;
                    agent.isStopped = true;

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

                    attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
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
            targetPlayer = closestPlayer;

            TryAttackPlayer(closestPlayer);

            if (IsAttacking)
            {
                isChasing = false;
            }
            else
            {
                isChasing = true;
            }
        }
        else
        {
            targetPlayer = null;
            isChasing = false;
        }
    }
    
    private void EndAttackAndResumePatrol()
    {
        IsAttacking = false;
        agent.isStopped = false;
        postAttackPatrolTimer = TickTimer.CreateFromSeconds(Runner, 5f);
        isChasing = false;

        if (currentMode == PatrolMode.Random)
        {
            SetRandomDestination();
        }
        else
        {
            agent.SetDestination(fixedPoints[currentPointIndex]);
        }
        currentPatrolTimer = 0f;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage)
    {
        if (IsDead) return;

        StunTimer = TickTimer.CreateFromSeconds(Runner, 3f);
        
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("die");
        }
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