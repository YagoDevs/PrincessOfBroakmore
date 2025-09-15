using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardMovement : MonoBehaviour
{
    public NavMeshAgent agent;
    public List<Transform> waypoints;
    public bool loop = true;
    public float stoppingDistance = 0.5f;
    private int currentIndex = 0;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (waypoints.Count > 0)
            SetDestination(waypoints[currentIndex]);
    }

    void Update()
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
}