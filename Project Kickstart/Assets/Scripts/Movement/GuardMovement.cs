using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardMovement : MonoBehaviour
{
    public enum GuardState { Patrolling, Charging, Returning }
    private GuardState currentState = GuardState.Patrolling;

    public NavMeshAgent agent;
    public List<Transform> waypoints;
    public bool loop = true;
    public float stoppingDistance = 0.5f;
    private int currentIndex = 0;
    private Vector3 lastPatrolPosition;

    public Transform eyes;
    public float viewRadius = 8f;
    public float viewAngle = 60f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    public float chargeSpeed = 10f;
    public float chargeDuration = 1.5f;

    private Transform currentTarget;
    private Vector3 chargeDirection;
    private float chargeTimer;

    public Transform Shadow;

    [SerializeField] Animator animator;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (waypoints.Count > 0)
            SetDestination(waypoints[currentIndex]);
    }

    void Update()
    {
        Shadow.transform.position = transform.position;
        switch (currentState)
        {
            case GuardState.Patrolling:
                Patrol();
                DetectTargets();
                break;
            case GuardState.Charging:
                Charge();
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
            animator.SetBool("Walk", true);
        }
    }

    void SetDestination(Transform target)
    {
        if (target != null)
        {
            agent.isStopped = false;
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
                    chargeDirection = dirToTarget;
                    chargeTimer = chargeDuration;
                    agent.isStopped = true;
                    currentState = GuardState.Charging;
                    animator.SetBool("Charge", true);
                    return;
                }
            }
        }
    }

    void Charge()
    {
        animator.SetBool("Charge", true);
        animator.SetTrigger("Charging");
        if (chargeTimer > 0)
        {
            transform.position += chargeDirection * chargeSpeed * Time.deltaTime;
            chargeTimer -= Time.deltaTime;
            animator.SetBool("Charge", true);
        }
        else
        {
            currentState = GuardState.Returning;
        }
        animator.SetBool("Charge", false);
    }

    void ReturnToPatrol()
    {
        agent.isStopped = false;
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