using UnityEngine;
using UnityEngine.AI;
using Fusion;
using System.Collections;
using System.Collections.Generic;

public class EnemyPatroll : NetworkBehaviour
{
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

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        agent = GetComponent<NavMeshAgent>();

        agent.autoTraverseOffMeshLink = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.3f;

        GameObject[] waypoints = GameObject.FindGameObjectsWithTag(waypointTag);

        if (waypoints.Length == 0)
        {
            Debug.LogWarning("No patrol points found!");
            return;
        }

        // 👉 add tất cả waypoint
        foreach (var wp in waypoints)
        {
            patrolPoints.Add(wp.transform);
            remainingPoints.Add(wp.transform);
        }

        // 👉 chọn điểm gần nhất để bắt đầu
        currentTarget = GetNearestPoint();

        // 👉 remove khỏi list chưa đi
        remainingPoints.Remove(currentTarget);

        agent.SetDestination(currentTarget.position);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (agent == null) return;

        if (agent.isOnOffMeshLink && !isJumping)
        {
            StartCoroutine(JumpAcrossLink());
            return;
        }

        Patrol();
    }

    void Patrol()
    {
        if (agent.pathPending || isJumping) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            // 👉 nếu hết waypoint → đứng yên
            if (remainingPoints.Count == 0)
            {
                Debug.Log("Patrol Finished!");
                agent.isStopped = true;
                return;
            }

            // 👉 random waypoint chưa đi
            int rand = Random.Range(0, remainingPoints.Count);
            currentTarget = remainingPoints[rand];

            remainingPoints.Remove(currentTarget);

            agent.SetDestination(currentTarget.position);
        }
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
}