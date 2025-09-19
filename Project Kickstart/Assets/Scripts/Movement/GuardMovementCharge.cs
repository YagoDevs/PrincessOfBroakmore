using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardMovementCharge : MonoBehaviour
{
    public enum GuardState { Patrolling, Chasing, Returning }
    private GuardState currentState = GuardState.Patrolling;

    [Header("Movement")]
    public NavMeshAgent agent;
    public List<Transform> waypoints;
    public bool loop = true;
    public float stoppingDistance = 0.5f;
    private int currentIndex = 0;
    private Vector3 lastPatrolPosition;

    [Header("Vision")]
    public Transform eyes;
    public float viewRadius = 8f;
    public float viewAngle = 60f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    private Transform currentTarget;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (waypoints.Count > 0)
            SetDestination(waypoints[currentIndex]);
    }

    void Update()
    {
        switch (currentState)
        {
            case GuardState.Patrolling:
                Patrol();
                DetectTargets();
                break;
            case GuardState.Chasing:
                ChaseTarget();
                break;
            case GuardState.Returning:
                ReturnToPatrol();
                break;
        }
    }

    void Patrol()
    {
        if (waypoints.Count == 0 || agent.pathPending) return;

        if (agent.remainingDistance <= stoppingDistance && !agent.pathPending)
        {
            NextWaypoint();
        }
    }

    void SetDestination(Transform target)
    {
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    void NextWaypoint()
    {
        currentIndex++;

        if (currentIndex >= waypoints.Count)
        {
            if (loop)
                currentIndex = 0;
            else
                return;
        }

        SetDestination(waypoints[currentIndex]);
    }

    void DetectTargets()
    {
        Collider[] targetsInView = Physics.OverlapSphere(eyes.position, viewRadius, targetMask);

        foreach (Collider target in targetsInView)
        {
            Vector3 dirToTarget = (target.transform.position - eyes.position).normalized;
            float angleToTarget = Vector3.Angle(eyes.forward, dirToTarget);

            if (angleToTarget < viewAngle / 2f)
            {
                if (!Physics.Raycast(eyes.position, dirToTarget, out RaycastHit hit, viewRadius, obstacleMask))
                {
                    currentTarget = target.transform;
                    lastPatrolPosition = transform.position;
                    currentState = GuardState.Chasing;
                    return;
                }
            }
        }
    }

    void ChaseTarget()
    {
        if (currentTarget == null)
        {
            currentState = GuardState.Returning;
            return;
        }

        agent.SetDestination(currentTarget.position);

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget > viewRadius * 1.5f)
        {
            currentTarget = null;
            currentState = GuardState.Returning;
        }
    }

    void ReturnToPatrol()
    {
        agent.SetDestination(lastPatrolPosition);

        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            currentState = GuardState.Patrolling;
            SetDestination(waypoints[currentIndex]);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (eyes == null) return;

        Gizmos.color = new Color(1, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(eyes.position, viewRadius);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * eyes.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * eyes.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyes.position, eyes.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(eyes.position, eyes.position + rightBoundary * viewRadius);
    }
}