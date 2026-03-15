using UnityEngine;
using UnityEngine.AI;
using Crazy_Lobby.Player;
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

    public float patrolRadius = 20f; // Bán kính giới hạn cho mỗi lần chọn điểm đi ngẫu nhiên
    
    public float patrolTimeout = 10f; // Thời gian tối đa để đến một điểm tuần tra

    public float visionRange = 10f;

    public float moveSpeed = 3f;   // Dùng chung một tốc độ cho mọi trường hợp
    public float acceleration = 100f;
    public float braking = 100f;

    private Transform player;
    private bool isChasing = false;
    private Vector3[] fixedPoints = new Vector3[3];
    private int currentPointIndex = 0;
    private float currentPatrolTimer = 0f;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        _ncc = GetComponent<NetworkCharacterController>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
        agent.updateRotation = false; // Tắt tự động xoay để tự điều khiển bằng code
        agent.updatePosition = false; // Tắt tự động di chuyển để NetworkCharacterController quản lý

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // Tắt NavMeshAgent trên các máy Client, chỉ cho phép Server/Host (State Authority) xử lý AI
        if (!HasStateAuthority)
        {
            agent.enabled = false;
            return;
        }

        _ncc.maxSpeed = moveSpeed;
        _ncc.acceleration = acceleration;
        _ncc.braking = braking;
        _ncc.rotationSpeed = 15f; // Chỉnh tốc độ xoay mặt tự động

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

        // Cập nhật vị trí NavMeshAgent theo vị trí thực tế của NetworkCharacterController
        agent.nextPosition = transform.position;

        Vector3 moveDirection = Vector3.zero;

        if (agent.hasPath || agent.pathPending)
        {
            Vector3 targetDir = agent.steeringTarget - transform.position;
            targetDir.y = 0f; // Bỏ qua trục y để nhân vật không bị nghiêng lên/xuống
            if (targetDir.sqrMagnitude > 0.001f)
            {
                moveDirection = targetDir.normalized;
            }
        }

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= visionRange)
            {
                isChasing = true;
                agent.SetDestination(player.position);
            }
            else
            {
                // Nếu vừa mới mất dấu người chơi
                if (isChasing)
                {
                    isChasing = false;
                    currentPatrolTimer = 0f; // Reset lại timer khi bắt đầu tuần tra lại
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

        if (!isChasing && !agent.pathPending)
        {
            currentPatrolTimer += Runner.DeltaTime;

            // Kiểm tra xem đã đến đích hoặc đường đi bị chặn hoàn toàn không thể tới được
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
                        // Thay thế điểm bị lỗi/quá thời gian bằng một điểm mới
                        if (TryGetValidPatrolPoint(out Vector3 newPoint))
                        {
                            fixedPoints[currentPointIndex] = newPoint;
                        }
                    }
                    
                    currentPointIndex = (currentPointIndex + 1) % fixedPoints.Length;
                    agent.SetDestination(fixedPoints[currentPointIndex]);
                    currentPatrolTimer = 0f; // Reset timer
                }
            }
        }

        // Uỷ quyền việc di chuyển và xoay người cho NetworkCharacterController
        _ncc.Move(moveDirection);
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
            currentPatrolTimer = 0f; // Reset timer sau khi tìm được điểm đến hợp lệ
        }
    }

    bool TryGetValidPatrolPoint(out Vector3 result)
    {
        for (int i = 0; i < 15; i++) // Thử sinh ngẫu nhiên tối đa 15 lần để tìm được điểm tốt nhất
        {
            Vector2 randomPlane = Random.insideUnitCircle * patrolRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomPlane.x, 0, randomPlane.y);

            NavMeshHit hit;
            // Quét trong bán kính nhỏ (2f) để tránh bị bắt dính vào bề mặt sau bức tường
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                // Tính toán đường đi để đảm bảo không bị kẹt hay cách nhau bởi vật cản không thể vượt qua
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
        // Vẽ tầm nhìn (Màu Đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Vẽ bán kính di chuyển ngẫu nhiên (Màu Xanh Dương)
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