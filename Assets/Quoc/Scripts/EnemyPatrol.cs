using UnityEngine;
using UnityEngine.AI;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using Crazy_Lobby.Player;
using Crazy_Lobby.Item; 

public class EnemyPatrol : NetworkBehaviour
{
    public static readonly List<EnemyPatrol> ActiveLobbyEnemies = new List<EnemyPatrol>();

    [Header("Patrol Settings")]
    public string waypointTag = "Waypoint";
    public float patrolSpeed = 2f;

    private List<Transform> patrolPoints = new List<Transform>();
    private List<Transform> remainingPoints = new List<Transform>();
    private Transform currentTarget;

    [Header("NavMesh")]
    private NavMeshAgent agent;

    [Header("Jump Settings")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.5f;
    private bool isJumping = false;

    [Header("Combat & Item Settings")]
    public float visionRange = 15f;      
    public float attackRange = 15f;      
    
    [Networked] public int FireworkCount { get; set; } 
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer attackTimer { get; set; } 
    [Networked] private TickTimer StunTimer { get; set; }

    private CharacterAnimation _characterAnimation;
    private Transform targetPlayer;
    private bool isSeekingItem = false;

    public override void Spawned()
    {
        ActiveLobbyEnemies.Add(this);

        if (!HasStateAuthority) return;

        agent = GetComponent<NavMeshAgent>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
        
        agent.autoTraverseOffMeshLink = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.3f;

        GameObject[] waypoints = GameObject.FindGameObjectsWithTag(waypointTag);
        if (waypoints.Length == 0) return;

        foreach (var wp in waypoints)
        {
            patrolPoints.Add(wp.transform);
            remainingPoints.Add(wp.transform);
        }

        currentTarget = GetNearestPoint();
        remainingPoints.Remove(currentTarget);

        if (currentTarget != null) agent.SetDestination(currentTarget.position);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ActiveLobbyEnemies.Remove(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (agent == null) return;

        if (IsDead || !StunTimer.ExpiredOrNotRunning(Runner))
        {
            if (!agent.isStopped) agent.isStopped = true;
            return;
        }

        if (agent.isStopped) agent.isStopped = false;

        if (attackTimer.IsRunning && targetPlayer != null)
        {
            agent.updateRotation = false; 
            Vector3 lookDir = targetPlayer.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Runner.DeltaTime * 15f);
            }
        }
        else
        {
            agent.updateRotation = true; 
        }

        HandleCombatAndItems();

        if (isSeekingItem) return; 

        if (agent.isOnOffMeshLink && !isJumping)
        {
            StartCoroutine(JumpAcrossLink());
            RPC_PlayJumpAnim(); 
            return;
        }

        Patrol();
    }

    private void HandleCombatAndItems()
    {
        if (FireworkCount > 0 && attackTimer.ExpiredOrNotRunning(Runner))
        {
            Transform randomTarget = GetRandomTarget();
            if (randomTarget != null)
            {
                isSeekingItem = false;
                StartAttack(randomTarget);
            }
        }

        if (FireworkCount <= 0)
        {
            Transform nearbyItem = GetClosestItem();
            if (nearbyItem != null)
            {
                isSeekingItem = true;
                
                agent.SetDestination(nearbyItem.position);
                
                if (Vector3.Distance(transform.position, nearbyItem.position) < 1.5f)
                {
                    var itemPickup = nearbyItem.GetComponentInParent<FireworkProjectile>();
                    if (itemPickup != null && !itemPickup.OwnerId.IsValid)
                    {
                        FireworkCount += 3; 
                        Runner.Despawn(itemPickup.Object);  
                        
                        isSeekingItem = false;
                        if (currentTarget != null) agent.SetDestination(currentTarget.position); 
                    }
                }
                return;
            }
        }

        // 3. ĐI BÌNH THƯỜNG
        isSeekingItem = false;
        if (currentTarget != null && agent.destination != currentTarget.position) 
        {
            agent.SetDestination(currentTarget.position);
        }
    }

    private void StartAttack(Transform target)
    {
        targetPlayer = target; 
        FireworkCount--;
        
        RPC_PlayAttackAnim(); 

        if (ItemManager.Instance != null && ItemManager.Instance.fireworkProjectilePrefab.IsValid)
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f + transform.forward * 1f;
            var hitNetworkObject = target.GetComponentInParent<NetworkObject>();

            Runner.Spawn(
                ItemManager.Instance.fireworkProjectilePrefab,
                origin, 
                transform.rotation,
                Object.StateAuthority,
                (runner, obj) =>
                {
                    var firework = obj.GetComponent<FireworkProjectile>();
                    if (firework != null)
                    {
                        firework.TargetId = hitNetworkObject != null ? hitNetworkObject.Id : default;
                        firework.OwnerId = Object.Id; 
                    }
                });
        }

        attackTimer = TickTimer.CreateFromSeconds(Runner, 2f);
    }

    void Patrol()
    {
        if (agent.pathPending || isJumping) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (remainingPoints.Count == 0) return;

            int rand = Random.Range(0, remainingPoints.Count);
            currentTarget = remainingPoints[rand];
            remainingPoints.Remove(currentTarget);
            agent.SetDestination(currentTarget.position);
        }
    }

    private Transform GetRandomTarget()
    {
        List<Transform> potentialTargets = new List<Transform>();

        foreach (var p in PlayerController.ActivePlayers)
        {
            if (p == null || p.IsDead) continue;
            float distSqr = (transform.position - p.transform.position).sqrMagnitude;
            if (distSqr < attackRange * attackRange) potentialTargets.Add(p.transform);
        }

        foreach (var e in ActiveLobbyEnemies)
        {
            if (e == null || e == this || e.IsDead) continue; 
            float distSqr = (transform.position - e.transform.position).sqrMagnitude;
            if (distSqr < attackRange * attackRange) potentialTargets.Add(e.transform);
        }

        if (potentialTargets.Count > 0)
        {
            return potentialTargets[Random.Range(0, potentialTargets.Count)];
        }
        return null; 
    }

    private Transform GetClosestItem()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange);
        Transform closest = null;
        float minDistSqr = Mathf.Infinity;

        foreach (var hit in hits)
        {
            var item = hit.GetComponentInParent<FireworkProjectile>();
            if (item != null)
            {
                if (item.Object == null || !item.Object.IsValid) continue;
                if (item.OwnerId.IsValid) continue;

                float distSqr = (transform.position - item.transform.position).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closest = item.transform;
                }
            }
        }
        return closest;
    }

    Transform GetNearestPoint()
    {
        Transform nearest = null;
        float minDist = Mathf.Infinity;
        foreach (var point in patrolPoints)
        {
            float dist = Vector3.Distance(transform.position, point.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = point;
            }
        }
        return nearest;
    }

    IEnumerator JumpAcrossLink()
    {
        isJumping = true;
        agent.isStopped = true;

        OffMeshLinkData link = agent.currentOffMeshLinkData;
        Vector3 start = agent.transform.position;
        Vector3 end = link.endPos;
        float t = 0f;

        while (t < 1f)
        {
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            agent.transform.position = pos;

            t += Time.deltaTime / jumpDuration;
            yield return null;
        }

        agent.transform.position = end;
        agent.CompleteOffMeshLink();
        agent.isStopped = false;
        isJumping = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage)
    {
        if (IsDead) return;
        StunTimer = TickTimer.CreateFromSeconds(Runner, 2f);
        RPC_PlayHitAnim();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitAnim()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.SetTrigger("die"); 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackAnim()
    {
        if (_characterAnimation != null) _characterAnimation.TriggerAttack();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayJumpAnim()
    {
        if (_characterAnimation != null) _characterAnimation.TriggerJump();
    }

    public override void Render()
    {
        if (_characterAnimation != null && agent != null)
        {
            _characterAnimation.UpdateMoveAnimation(agent.velocity, patrolSpeed);
        }
    }
}