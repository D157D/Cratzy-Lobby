using UnityEngine;
using UnityEngine.AI;
using Crazy_Lobby.Player;
using Crazy_Lobby.Item;
using Fusion;

[RequireComponent(typeof(NetworkCharacterController))]
public class EnemyPatrol : NetworkBehaviour
{
    public enum PatrolMode
    {
        Random,
        FixedPoints
    }

    private NavMeshAgent agent;
    private NetworkCharacterController _ncc;
    private CharacterAnimation _characterAnimation;

    public PatrolMode currentMode = PatrolMode.Random;

    public float patrolRadius = 20f; 
    
    public float patrolTimeout = 10f; 

    public float visionRange = 10f;

    public float moveSpeed = 3f;   
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
    [Networked] private TickTimer attackTimer { get; set; }

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        _ncc = GetComponent<NetworkCharacterController>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
        agent.updateRotation = false; 
        agent.updatePosition = false; 

        _ncc.maxSpeed = moveSpeed;
        _ncc.acceleration = acceleration;
        _ncc.braking = braking;
        _ncc.rotationSpeed = 15f; 

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


    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        agent.nextPosition = transform.position;

        FindAndChaseClosestPlayer();

        Vector3 moveDirection = Vector3.zero;

        if (agent.hasPath || agent.pathPending)
        {
            Vector3 targetDir = agent.steeringTarget - transform.position;
            targetDir.y = 0f; 
            if (targetDir.sqrMagnitude > 0.001f)
            {
                moveDirection = targetDir.normalized;
            }
        }

        if (isChasing && targetPlayer != null)
        {
            AttackPlayerIfPossible(targetPlayer);
        }

        if (!isChasing && !agent.pathPending)
        {
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
                    if (isUnreachable || isTimeout)
                    {
                        if (TryGetValidPatrolPoint(out Vector3 newPoint))
                        {
                            fixedPoints[currentPointIndex] = newPoint;
                        }
                    }
                    
                    currentPointIndex = (currentPointIndex + 1) % fixedPoints.Length;
                    agent.SetDestination(fixedPoints[currentPointIndex]);
                    currentPatrolTimer = 0f; 
                }
            }
        }

        _ncc.Move(moveDirection);
    }

    private void AttackPlayerIfPossible(Transform targetTransform)
    {
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        
        if (distance <= attackRange)
        {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 targetPos = targetTransform.position + Vector3.up;
            Vector3 direction = (targetPos - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange))
            {
                var hitPlayer = hit.transform.GetComponentInParent<NetworkCharacterController>();
                if (hitPlayer != null && hitPlayer.transform == targetTransform)
                {
                    if (ItemManager.Instance != null && ItemManager.Instance.fireworkProjectilePrefab.IsValid)
                    {
                        Quaternion randomRot = Quaternion.Euler(Random.Range(-60f, 60f), Random.Range(0f, 360f), Random.Range(-60f, 60f));
                        Runner.Spawn(ItemManager.Instance.fireworkProjectilePrefab,
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
            isChasing = true;
            targetPlayer = closestPlayer;
            agent.SetDestination(closestPlayer.position);
        }
        else
        {
            targetPlayer = null;
            if (isChasing)
            {
                isChasing = false;
                currentPatrolTimer = 0f; 
                if (currentMode == PatrolMode.Random)
                {
                    SetRandomDestination();
                }
                else
                {
                    agent.SetDestination(fixedPoints[currentPointIndex]);
                }
            }
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
            else
            {
                fixedPoints[i] = transform.position;
            }
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