using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveableLights : MonoBehaviour
{
    [SerializeField] List<Transform> lightsToMove;
    private Dictionary<Transform, int> lightStates = new Dictionary<Transform, int>();

    private void Start()
    {
        // Track state of each light (0 = start, 1 = +45, 2 = +90)
        foreach (var light in lightsToMove)
        {
            lightStates[light] = 0;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("triggered");
        MoveLights();
        CheckSolution();
    }

    void MoveLights()
    {
        foreach (var light in lightsToMove)
        {
            int state = lightStates[light];

            if (state == 0)
            {
                // step 1: rotate +45
                light.Rotate(Vector3.up, 45);
                lightStates[light] = 1;
            }
            else if (state == 1)
            {
                // step 2: rotate +45 again (total +90)
                light.Rotate(Vector3.up, 45);
                lightStates[light] = 2;
            }
            else if (state == 2)
            {
                // step 3: back to start (reset rotation)
                light.localRotation = Quaternion.identity;
                lightStates[light] = 0;
            }
        }
    }

    void CheckSolution()
    {
        //check if the lights are in the correct spot (ana knows if they are)
    }
}