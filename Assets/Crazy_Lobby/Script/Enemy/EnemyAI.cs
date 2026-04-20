using UnityEngine;
using UnityEngine.AI;
using Crazy_Lobby.Player;
using Crazy_Lobby.Item;
using Fusion;
using System.Collections.Generic;
using Crazy_Lobby.Enemy;
using UnityEngine.SceneManagement;

public class EnemyAI : NetworkBehaviour, IStunnable
{
    public enum PatrolMode { Random, FixedPoints }
    public static readonly List<EnemyAI> ActiveEnemies = new List<EnemyAI>();

    private NavMeshAgent agent;
    private NetworkCharacterController _ncc;
    private CharacterAnimation _characterAnimation;
    private EnemyCharacterHandler _characterHandler;

    public PatrolMode currentMode = PatrolMode.Random;
    public float patrolRadius = 20f;
    public float patrolTimeout = 10f;
    public float visionRange = 15f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 15f;
    public float acceleration = 100f;
    public float braking = 100f;

    private Transform targetPlayer;
    private bool isChasing = false;
    private bool isSeekingItem = false;
    private Vector3[] fixedPoints = new Vector3[3];
    private int currentPointIndex = 0;
    private float currentPatrolTimer = 0f;

    private bool _isInLobby;
    private Transform _realDestPos;

    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float attackCooldown = 5f;
    public float attackStandTime = 1.5f;

    // --- CÁC BIẾN NETWORK ---
    [Networked] public int MagicCount { get; set; }
    [Networked] public int FireworkCount { get; set; }
    [Networked] private NetworkBool IsAttacking { get; set; }
    [Networked] private TickTimer postAttackPatrolTimer { get; set; }
    [Networked] private TickTimer attackTimer { get; set; }
    [Networked] private TickTimer shootCooldownTimer { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer StunTimer { get; set; }
    [Networked] public bool HasFinished { get; set; }

    // Biến quan trọng để ẩn Enemy đồng bộ qua mạng
    [Networked, OnChangedRender(nameof(OnIsHiddenChanged))] 
    public NetworkBool IsHidden { get; set; }

    public bool IsInLobby => SceneManager.GetActiveScene().name == "Login_Crazy";

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        _ncc = GetComponent<NetworkCharacterController>();
        _characterHandler = GetComponent<EnemyCharacterHandler>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());

        if (_characterHandler != null)
        {
            _characterHandler.OnModelChanged += HandleModelChanged;
            var animator = GetComponentInChildren<Animator>();
            if (animator != null) _characterAnimation.SetAnimator(animator);
        }

        agent.updateRotation = false;
        agent.updatePosition = false;

        _ncc.maxSpeed = IsInLobby ? moveSpeed : 10;
        _ncc.acceleration = acceleration;
        _ncc.braking = braking;

        _isInLobby = IsInLobby;

        if (!_isInLobby)
        {
            GameObject destObj = GameObject.Find("RealDestPos");
            if (destObj != null)
            {
                _realDestPos = destObj.transform;
                agent.SetDestination(_realDestPos.position);
            }
        }
        else
        {
            if (currentMode == PatrolMode.FixedPoints)
            {
                GenerateFixedPoints();
                agent.SetDestination(fixedPoints[currentPointIndex]);
            }
            else SetRandomDestination();
        }

        ActiveEnemies.Add(this);
        
        // Kiểm tra trạng thái ẩn khi vừa spawn (dành cho late-joiner)
        OnIsHiddenChanged();
    }

    // --- HÀM XỬ LÝ ẨN ENEMY ---
    private void OnIsHiddenChanged()
    {
        // Ẩn Mesh (Visual) nhưng vẫn giữ Object để không lỗi Network
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = !IsHidden;

        // Tắt Collider để người chơi không va chạm với Enemy đã về đích
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = !IsHidden;

        // Nếu ẩn, dừng Agent để tiết kiệm hiệu năng
        if (IsHidden && agent != null) agent.isStopped = true;
    }

    public void SetFinished()
    {
        if (Object.HasStateAuthority)
        {
            HasFinished = true;
            IsHidden = true; // Kích hoạt đồng bộ ẩn trên toàn mạng
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || IsHidden) return; // Nếu đã ẩn thì không xử lý logic nữa

        if (!_isInLobby)
        {
            if (!CountdownController.IsGameStarted || HasFinished)
            {
                if (_ncc != null) _ncc.Move(Vector3.zero);
                return;
            }
        }

        if (IsDead || !StunTimer.ExpiredOrNotRunning(Runner))
        {
            _ncc.Move(Vector3.zero);
            return;
        }

        agent.nextPosition = transform.position;
        Vector3 moveDirection = Vector3.zero;

        if (IsAttacking)
        {
            HandleRotationTowardsTarget();
            if (attackTimer.Expired(Runner)) EndAttackAndResumePatrol();
        }
        else if (postAttackPatrolTimer.IsRunning)
        {
            HandlePatrolMovement(out moveDirection);
        }
        else
        {
            if (MagicCount > 0)
            {
                StartMagicAttack();
                _ncc.Move(Vector3.zero);
                return;
            }

            if (FireworkCount <= 0)
            {
                Transform nearbyItem = GetClosestItem(out bool isMagicItem);
                if (nearbyItem != null) GoToItem(nearbyItem, isMagicItem, out moveDirection);
                else HandlePatrolMovement(out moveDirection);
            }
            else
            {
                FindAndChaseClosestPlayer();
                if (isChasing && targetPlayer != null)
                {
                    agent.SetDestination(targetPlayer.position);
                    CalculateMoveDirFromAgent(out moveDirection);
                }
                else
                {
                    Transform nearbyItem = GetClosestItem(out bool isMagicItem);
                    if (nearbyItem != null) GoToItem(nearbyItem, isMagicItem, out moveDirection);
                    else HandlePatrolMovement(out moveDirection);
                }
            }
        }

        _ncc.Move(moveDirection);
    }

    private void HandleRotationTowardsTarget()
    {
        if (targetPlayer == null) return;
        Vector3 lookDirection = targetPlayer.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * rotationSpeed);
        }
    }

    private void CalculateMoveDirFromAgent(out Vector3 moveDirection)
    {
        moveDirection = Vector3.zero;
        if (agent.hasPath || agent.pathPending)
        {
            Vector3 targetDir = agent.steeringTarget - transform.position;
            targetDir.y = 0f;
            if (targetDir.sqrMagnitude > 0.001f) moveDirection = targetDir.normalized;
        }
    }

    // --- CÁC HÀM CÒN LẠI (GIỮ NGUYÊN LOGIC CỦA ÔNG) ---
    private void HandleModelChanged(GameObject newModel)
    {
        if (_characterAnimation != null)
        {
            var animator = newModel.GetComponentInChildren<Animator>() ?? newModel.GetComponent<Animator>();
            _characterAnimation.SetAnimator(animator);
        }
        OnIsHiddenChanged(); // Cập nhật lại trạng thái ẩn cho model mới
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_characterHandler != null) _characterHandler.OnModelChanged -= HandleModelChanged;
        ActiveEnemies.Remove(this);
    }

    private void GoToItem(Transform itemTransform, bool isMagicItem, out Vector3 moveDirection)
    {
        moveDirection = Vector3.zero;
        isSeekingItem = true;
        isChasing = false;
        targetPlayer = null;
        agent.SetDestination(itemTransform.position);

        if (Vector3.Distance(transform.position, itemTransform.position) < 1.5f)
        {
            var netObj = itemTransform.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.IsValid)
            {
                if (isMagicItem) MagicCount++; else FireworkCount += 3;
                Runner.Despawn(netObj);
            }
        }
        CalculateMoveDirFromAgent(out moveDirection);
    }

    private void StartMagicAttack()
    {
        MagicCount--;
        IsAttacking = true;
        agent.isStopped = true;
        RPC_PlayAttackAnim();

        if (ItemManager.Instance != null && ItemManager.Instance.Magic.IsValid)
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Runner.Spawn(ItemManager.Instance.Magic, origin, transform.rotation, Object.StateAuthority, (runner, obj) =>
            {
                var magic = obj.GetComponent<MagicProjectile>();
                if (magic != null) magic.OwnerId = Object.Id;
            });
        }
        attackTimer = TickTimer.CreateFromSeconds(Runner, attackStandTime);
        shootCooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
    }

    private Transform GetClosestItem(out bool isMagic)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange);
        Transform closest = null;
        float minDistSqr = Mathf.Infinity;
        isMagic = false;

        foreach (var hit in hits)
        {
            bool foundMagic = false;
            Transform itemTransform = null;

            var firework = hit.GetComponentInParent<FireworkProjectile>();
            if (firework != null && firework.Object != null && firework.Object.IsValid && !firework.OwnerId.IsValid)
                itemTransform = firework.transform;

            var magic = hit.GetComponentInParent<MagicProjectile>();
            if (magic != null && magic.Object != null && magic.Object.IsValid && !magic.OwnerId.IsValid)
            {
                itemTransform = magic.transform;
                foundMagic = true;
            }

            if (itemTransform != null)
            {
                float distSqr = (transform.position - itemTransform.position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closest = itemTransform;
                    isMagic = foundMagic;
                }
            }
        }
        return closest;
    }

    private void TryAttackPlayer(Transform targetTransform)
    {
        if (targetTransform == null || shootCooldownTimer.IsRunning || IsAttacking || FireworkCount <= 0) return;

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
                    FireworkCount--;
                    IsAttacking = true;
                    agent.isStopped = true;
                    RPC_PlayAttackAnim();

                    if (ItemManager.Instance != null && ItemManager.Instance.fireworkProjectilePrefab.IsValid)
                    {
                        Quaternion randomRot = Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-30f, 30f));
                        Runner.Spawn(ItemManager.Instance.fireworkProjectilePrefab, origin + transform.forward * 1f, randomRot, Object.StateAuthority, (runner, obj) =>
                        {
                            var firework = obj.GetComponent<FireworkProjectile>();
                            if (firework != null) { firework.TargetId = hitPlayer.Object.Id; firework.OwnerId = Object.Id; }
                        });
                    }
                    attackTimer = TickTimer.CreateFromSeconds(Runner, attackStandTime);
                    shootCooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
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
                if (distanceSqr < closestDistanceSqr) { closestDistanceSqr = distanceSqr; closestPlayer = col.transform; }
            }
        }

        if (closestPlayer != null)
        {
            targetPlayer = closestPlayer;
            TryAttackPlayer(closestPlayer);
            isChasing = !IsAttacking;
        }
        else { targetPlayer = null; isChasing = false; }
    }

    private void EndAttackAndResumePatrol()
    {
        IsAttacking = false;
        agent.isStopped = false;
        postAttackPatrolTimer = TickTimer.CreateFromSeconds(Runner, 5f);
        isChasing = false;

        if (!_isInLobby)
        {
            if (_realDestPos != null) agent.SetDestination(_realDestPos.position);
        }
        else
        {
            if (currentMode == PatrolMode.Random) SetRandomDestination();
            else agent.SetDestination(fixedPoints[currentPointIndex]);
        }
        currentPatrolTimer = 0f;
    }

    private void HandlePatrolMovement(out Vector3 moveDirection)
    {
        CalculateMoveDirFromAgent(out moveDirection);

        if (!_isInLobby)
        {
            if (_realDestPos != null && agent.destination != _realDestPos.position) agent.SetDestination(_realDestPos.position);
            return;
        }

        currentPatrolTimer += Runner.DeltaTime;
        bool isUnreachable = agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid;
        if (agent.remainingDistance < 0.5f || isUnreachable || currentPatrolTimer >= patrolTimeout)
        {
            if (currentMode == PatrolMode.Random) SetRandomDestination();
            else
            {
                currentPointIndex = (currentPointIndex + 1) % fixedPoints.Length;
                agent.SetDestination(fixedPoints[currentPointIndex]);
            }
            currentPatrolTimer = 0f;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage)
    {
        if (IsDead || IsHidden) return;
        StunTimer = TickTimer.CreateFromSeconds(Runner, 3f);
        RPC_PlayHitAnim();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackAnim() { if (_characterAnimation != null) _characterAnimation.TriggerAttack(); }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitAnim() { var anim = GetComponentInChildren<Animator>(); if (anim != null) anim.SetTrigger("die"); }

    public override void Render()
    {
        if (_characterAnimation != null && _ncc != null && !IsHidden)
            _characterAnimation.UpdateMoveAnimation(_ncc.Velocity, moveSpeed);
    }

    void GenerateFixedPoints()
    {
        for (int i = 0; i < 3; i++)
        {
            if (TryGetValidPatrolPoint(out Vector3 point)) fixedPoints[i] = point;
            else fixedPoints[i] = transform.position;
        }
    }

    void SetRandomDestination() { if (TryGetValidPatrolPoint(out Vector3 pt)) { agent.SetDestination(pt); currentPatrolTimer = 0f; } }

    bool TryGetValidPatrolPoint(out Vector3 result)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 rand = Random.insideUnitCircle * patrolRadius;
            Vector3 target = transform.position + new Vector3(rand.x, 0, rand.y);
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    result = new Vector3(hit.position.x, transform.position.y, hit.position.z);
                    return true;
                }
            }
        }
        result = transform.position; return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }

    public void ApplyStun(float duration)
    {
        if (Object.HasStateAuthority && !IsHidden)
        {
            StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
            RPC_PlayHitAnim();
        }
    }
}