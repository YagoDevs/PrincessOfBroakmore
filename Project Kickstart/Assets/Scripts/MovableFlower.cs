using UnityEngine;

public class MovableFlower : MonoBehaviour
{
    [Header("Movable Flower Settings")]
    [SerializeField] private Flower flower; // Reference to the flower in this box
    [SerializeField] private Transform[] possibleTargets; // Possible targets (walls, door, etc.)
    [SerializeField] private int currentTargetIndex = 0;
    
    [Header("Movement Settings")]
    [SerializeField] private float detectionRange = 2f; // Distance to detect targets
    [SerializeField] private LayerMask targetLayer = 1; // Layer of targets
    
    [Header("Manual Control")]
    [SerializeField] private KeyCode nextTargetKey = KeyCode.E; // Key to change target
    [SerializeField] private bool useManualControl = true; // Whether target can be changed manually
    
    [Header("Automatic Detection")]
    [SerializeField] private bool useAutoDetection = true; // Whether to detect targets automatically
    [SerializeField] private bool prioritizeDoors = true; // Whether to prioritize doors when nearby
    [SerializeField] private float doorPriorityDistance = 3f; // Distance to prioritize doors
    
    [Header("Automatic Activation")]
    [SerializeField] private bool autoActivateWhenReceivingLight = true; // Whether to auto-activate when receiving light
    [SerializeField] private float lightCheckInterval = 0.1f; // Interval to check for light
    
    private Vector3 lastPosition;
    private bool wasReceivingLight = false;
    private float lastLightCheck = 0f;
    
    private void Start()
    {
        // Configure flower if not set
        if (flower == null)
        {
            flower = GetComponentInChildren<Flower>();
        }
        
        if (flower == null)
        {
            Debug.LogWarning($"MovableFlower {gameObject.name}: Flower not found!");
            return;
        }
        
        // Configure initial target
        UpdateFlowerTarget();
        
        lastPosition = transform.position;
        
        Debug.Log($"[MOVABLE FLOWER] {gameObject.name} initialized with {possibleTargets.Length} possible targets");
    }
    
    private void Update()
    {
        // Check if receiving light (automatic activation)
        if (autoActivateWhenReceivingLight && Time.time > lastLightCheck + lightCheckInterval)
        {
            CheckForIncomingLight();
            lastLightCheck = Time.time;
        }
        
        // Check if the box moved
        if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            OnBoxMoved();
            lastPosition = transform.position;
        }
        
        // Manual target change (only if the flower is active)
        if (useManualControl && Input.GetKeyDown(nextTargetKey) && flower != null && flower.IsActivated)
        {
            CycleToNextTarget();
        }
        
        // Automatic detection of nearby targets
        if (useAutoDetection)
        {
            DetectNearbyTargets();
        }
    }
    
    private void CheckForIncomingLight()
    {
        bool isReceivingLight = false;
        
        // Check if any flower is emitting light to this box/flower
        Flower[] allFlowers = FindObjectsOfType<Flower>();
        
        foreach (Flower otherFlower in allFlowers)
        {
            if (otherFlower != flower && otherFlower.IsActivated)
            {
                // Check if this flower/box is the target of the other flower
                if (otherFlower.CurrentTarget == transform || 
                    (flower != null && otherFlower.CurrentTarget == flower.transform))
                {
                    isReceivingLight = true;
                    break;
                }
            }
        }
        
        // If it started receiving light, activate the flower
        if (isReceivingLight && !wasReceivingLight)
        {
            ActivateFlower();
        }
        // If it stopped receiving light, deactivate the flower
        else if (!isReceivingLight && wasReceivingLight)
        {
            DeactivateFlower();
        }
        
        wasReceivingLight = isReceivingLight;
    }
    
    private void ActivateFlower()
    {
        if (flower != null && !flower.IsActivated)
        {
            flower.ReceiveLight();
            Debug.Log($"[MOVABLE FLOWER] Box flower {gameObject.name} automatically activated!");
        }
    }
    
    private void DeactivateFlower()
    {
        if (flower != null && flower.IsActivated)
        {
            flower.DeactivateFlower();
            Debug.Log($"[MOVABLE FLOWER] Box flower {gameObject.name} deactivated (lost incoming light)");
        }
    }
    
    private void OnBoxMoved()
    {
        Debug.Log($"[MOVABLE FLOWER] Box {gameObject.name} moved to {transform.position}");
        
        // If using automatic detection, check nearby targets
        if (useAutoDetection)
        {
            CheckNearbyTargetsAndSwitch();
        }
        
        // Update flower target (keeps the same direction)
        UpdateFlowerTarget();
    }
    
    private void CycleToNextTarget()
    {
        if (possibleTargets.Length == 0) return;
        
        currentTargetIndex = (currentTargetIndex + 1) % possibleTargets.Length;
        UpdateFlowerTarget();
        
        Debug.Log($"[MOVABLE FLOWER] Switched to target {currentTargetIndex}: {GetCurrentTargetName()}");
    }
    
    private void UpdateFlowerTarget()
    {
        if (flower == null || possibleTargets.Length == 0) return;
        
        Transform currentTarget = possibleTargets[currentTargetIndex];
        if (currentTarget != null)
        {
            flower.ChangeTarget(currentTarget);
            
            // If the flower is active, force light update
            if (flower.IsActivated)
            {
                // The flower will automatically update its light to the new target
                Debug.Log($"[MOVABLE FLOWER] Light redirected to {currentTarget.name}");
            }
            
            // Check if the current target is a door
            CheckIfTargetIsDoor(currentTarget);
        }
    }
    
    private void CheckNearbyTargetsAndSwitch()
    {
        if (possibleTargets.Length == 0) 
        {
            Debug.LogWarning($"[MOVABLE FLOWER] {gameObject.name}: No target configured!");
            return;
        }
        
        // If prioritizing doors, check if there is a nearby door
        if (prioritizeDoors)
        {
            int doorIndex = FindNearestDoor();
            if (doorIndex != -1 && doorIndex != currentTargetIndex)
            {
                Debug.Log($"[MOVABLE FLOWER] 🔄 Changing from target {currentTargetIndex} ({GetCurrentTargetName()}) to door {doorIndex}");
                currentTargetIndex = doorIndex;
                Debug.Log($"[MOVABLE FLOWER] ✅ Automatic switch to nearby door: {GetCurrentTargetName()}");
                return;
            }
            else if (doorIndex != -1)
            {
                Debug.Log($"[MOVABLE FLOWER] ✅ Already pointing to the nearest door: {GetCurrentTargetName()}");
            }
        }
        
        // Search for new nearby targets
        DetectNearbyTargets();
    }
    
    private int FindNearestDoor()
    {
        int nearestDoorIndex = -1;
        float nearestDistance = float.MaxValue;
        
        Debug.Log($"[MOVABLE FLOWER] Searching for nearby doors... Max distance: {doorPriorityDistance}");
        Debug.Log($"[MOVABLE FLOWER] Target list: {string.Join(", ", System.Array.ConvertAll(possibleTargets, t => t?.name ?? "null"))}");
        
        for (int i = 0; i < possibleTargets.Length; i++)
        {
            if (possibleTargets[i] == null) 
            {
                Debug.Log($"[MOVABLE FLOWER] Target {i} is null!");
                continue;
            }
            
            // Check if it is a door
            Door door = possibleTargets[i].GetComponent<Door>();
            float distance = Vector3.Distance(transform.position, possibleTargets[i].position);
            
            Debug.Log($"[MOVABLE FLOWER] Target {i}: {possibleTargets[i].name} - Distance: {distance:F2} - Is door: {(door != null)}");
            
            if (door != null)
            {
                if (distance <= doorPriorityDistance && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestDoorIndex = i;
                    Debug.Log($"[MOVABLE FLOWER] New nearest door: {possibleTargets[i].name} (distance: {distance:F2})");
                }
            }
        }
        
        Debug.Log($"[MOVABLE FLOWER] Nearest door found: {(nearestDoorIndex != -1 ? possibleTargets[nearestDoorIndex].name : "None")}");
        return nearestDoorIndex;
    }
    
    private void DetectNearbyTargets()
    {
        // Automatically search for nearby targets
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);
        
        foreach (Collider obj in nearbyObjects)
        {
            // Check if it is a valid target that is not in the list
            if (IsValidTarget(obj.transform) && !IsTargetInList(obj.transform))
            {
                AddTarget(obj.transform);
            }
        }
    }
    
    private bool IsValidTarget(Transform target)
    {
        // Check if the object has components indicating it is a valid target
        return target.GetComponent<Door>() != null || 
               target.CompareTag("LightTarget") || 
               target.name.ToLower().Contains("target");
    }
    
    private bool IsTargetInList(Transform target)
    {
        foreach (Transform t in possibleTargets)
        {
            if (t == target) return true;
        }
        return false;
    }
    
    private void AddTarget(Transform newTarget)
    {
        // Expand target array
        Transform[] newArray = new Transform[possibleTargets.Length + 1];
        for (int i = 0; i < possibleTargets.Length; i++)
        {
            newArray[i] = possibleTargets[i];
        }
        newArray[possibleTargets.Length] = newTarget;
        possibleTargets = newArray;
        
        Debug.Log($"[MOVABLE FLOWER] New target added: {newTarget.name}");
    }
    
    private void CheckIfTargetIsDoor(Transform target)
    {
        Door door = target.GetComponent<Door>();
        if (door != null)
        {
            Debug.Log($"[MOVABLE FLOWER] Light pointing to door {door.name}!");
            // The door will be activated automatically by the light system
        }
    }
    
    private string GetCurrentTargetName()
    {
        if (possibleTargets.Length == 0) return "None";
        Transform target = possibleTargets[currentTargetIndex];
        return target != null ? target.name : "null";
    }
    
    // Public methods for external control
    public void SetTargetIndex(int index)
    {
        if (index >= 0 && index < possibleTargets.Length)
        {
            currentTargetIndex = index;
            UpdateFlowerTarget();
        }
    }
    
    public void AddPossibleTarget(Transform target)
    {
        if (!IsTargetInList(target))
        {
            AddTarget(target);
        }
    }
    
    public Transform GetCurrentTarget()
    {
        if (possibleTargets.Length == 0) return null;
        return possibleTargets[currentTargetIndex];
    }
    
    // Editor visualization
    private void OnDrawGizmosSelected()
    {
        // Draw general detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw priority range for doors
        if (prioritizeDoors)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, doorPriorityDistance);
        }
        
        // Draw lines for possible targets
        if (possibleTargets != null)
        {
            for (int i = 0; i < possibleTargets.Length; i++)
            {
                if (possibleTargets[i] != null)
                {
                    // Special color for doors
                    Door door = possibleTargets[i].GetComponent<Door>();
                    if (door != null)
                    {
                        Gizmos.color = (i == currentTargetIndex) ? Color.cyan : Color.blue;
                    }
                    else
                    {
                        Gizmos.color = (i == currentTargetIndex) ? Color.green : Color.gray;
                    }
                    
                    Gizmos.DrawLine(transform.position, possibleTargets[i].position);
                    
                    // Draw a sphere at the target if it is the current one
                    if (i == currentTargetIndex)
                    {
                        Gizmos.DrawWireSphere(possibleTargets[i].position, 0.5f);
                    }
                }
            }
        }
    }
}
