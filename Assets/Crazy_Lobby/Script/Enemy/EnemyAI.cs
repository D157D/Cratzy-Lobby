using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class EnemyPatrol : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform[] waypoints;
    private int currentIndex = 0;

    public float visionRange = 10f;

    public float patrolSpeed = 2f;   
    public float chaseSpeed = 5f;   

    private Transform player;
    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        waypoints = GameObject.FindGameObjectsWithTag("Waypoint")
                    .Select(w => w.transform)
                    .ToArray();

        player = GameObject.FindGameObjectWithTag("Player").transform;

        agent.speed = patrolSpeed;

        MoveToNextPoint();
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= visionRange)
        {
            isChasing = true;
            agent.speed = chaseSpeed;       
            agent.destination = player.position;
            return;
        }
        else
        {
            isChasing = false;
            agent.speed = patrolSpeed;      
        }

        if (!isChasing && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            MoveToNextPoint();
        }
    }

    void MoveToNextPoint()
    {
        if (waypoints.Length == 0) return;

        agent.destination = waypoints[currentIndex].position;

        currentIndex++;

        if (currentIndex >= waypoints.Length)
            currentIndex = 0;
    }
}