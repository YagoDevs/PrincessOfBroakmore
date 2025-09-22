using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    [Header("Phase Configuration")]
    [SerializeField] private Transform princess; // Reference to the princess
    [SerializeField] private Transform door; // Door that triggers the phase change
    [SerializeField] private Transform phase1StartPoint; // Where princess starts in phase 1
    [SerializeField] private Transform phase2TeleportPoint; // Where princess goes in phase 2
    
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera; // Main camera reference
    [SerializeField] private Transform phase1CameraPosition; // Camera position for phase 1
    [SerializeField] private Transform phase2CameraPosition; // Camera position for phase 2
    [SerializeField] private float cameraTransitionSpeed = 2f; // How fast camera moves
    
    [Header("Phase Change Detection")]
    [SerializeField] private float detectionRange = 1f; // Distance to detect princess near door
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // State
    private bool hasChangedPhase = false;
    private bool isTransitioningCamera = false;
    private Vector3 cameraStartPosition;
    private Vector3 cameraTargetPosition;
    private float transitionProgress = 0f;
    
    private void Start()
    {
        // Validate references
        if (princess == null)
        {
            Debug.LogError("PhaseManager: Princess reference not set!");
            return;
        }
        
        if (door == null)
        {
            Debug.LogError("PhaseManager: Door reference not set!");
            return;
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("PhaseManager: No main camera found!");
                return;
            }
        }
        
        // Set initial camera position for phase 1
        if (phase1CameraPosition != null)
        {
            mainCamera.transform.position = phase1CameraPosition.position;
            mainCamera.transform.rotation = phase1CameraPosition.rotation;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("PhaseManager: Started in Phase 1");
        }
    }
    
    private void Update()
    {
        // Only check for phase change if not already changed
        if (!hasChangedPhase)
        {
            CheckForPhaseChange();
        }
        
        // Handle camera transition
        if (isTransitioningCamera)
        {
            UpdateCameraTransition();
        }
    }
    
    private void CheckForPhaseChange()
    {
        // Check if princess is close enough to the door
        float distanceToDoor = Vector3.Distance(princess.position, door.position);
        
        if (distanceToDoor <= detectionRange)
        {
            ChangeToPhase2();
        }
        
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every second
        {
            Debug.Log($"PhaseManager: Distance to door: {distanceToDoor:F2}");
        }
    }
    
    private void ChangeToPhase2()
    {
        if (hasChangedPhase) return;
        
        hasChangedPhase = true;
        
        if (showDebugInfo)
        {
            Debug.Log("PhaseManager: Changing to Phase 2!");
        }
        
        // Teleport princess to phase 2
        if (phase2TeleportPoint != null)
        {
            princess.position = phase2TeleportPoint.position;
            princess.rotation = phase2TeleportPoint.rotation;
            
            if (showDebugInfo)
            {
                Debug.Log($"PhaseManager: Princess teleported to {phase2TeleportPoint.position}");
            }
        }
        else
        {
            Debug.LogError("PhaseManager: Phase 2 teleport point not set!");
        }
        
        // Start camera transition to phase 2
        if (phase2CameraPosition != null)
        {
            StartCameraTransition(phase2CameraPosition);
        }
        else
        {
            Debug.LogError("PhaseManager: Phase 2 camera position not set!");
        }
    }
    
    private void StartCameraTransition(Transform targetCameraPosition)
    {
        isTransitioningCamera = true;
        transitionProgress = 0f;
        
        cameraStartPosition = mainCamera.transform.position;
        cameraTargetPosition = targetCameraPosition.position;
        
        if (showDebugInfo)
        {
            Debug.Log("PhaseManager: Starting camera transition to Phase 2");
        }
    }
    
    private void UpdateCameraTransition()
    {
        transitionProgress += Time.deltaTime * cameraTransitionSpeed;
        
        if (transitionProgress >= 1f)
        {
            // Transition complete
            mainCamera.transform.position = cameraTargetPosition;
            mainCamera.transform.rotation = phase2CameraPosition.rotation;
            isTransitioningCamera = false;
            transitionProgress = 1f;
            
            if (showDebugInfo)
            {
                Debug.Log("PhaseManager: Camera transition complete!");
            }
        }
        else
        {
            // Smooth transition
            mainCamera.transform.position = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, transitionProgress);
            
            // Smooth rotation transition
            Quaternion startRotation = mainCamera.transform.rotation;
            Quaternion targetRotation = phase2CameraPosition.rotation;
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, transitionProgress);
        }
    }
    
    // Public method to manually trigger phase change (for testing)
    [ContextMenu("Force Change to Phase 2")]
    public void ForceChangeToPhase2()
    {
        if (!hasChangedPhase)
        {
            ChangeToPhase2();
        }
    }
    
    // Public method to reset to phase 1 (for testing)
    [ContextMenu("Reset to Phase 1")]
    public void ResetToPhase1()
    {
        hasChangedPhase = false;
        isTransitioningCamera = false;
        transitionProgress = 0f;
        
        // Reset princess position
        if (phase1StartPoint != null)
        {
            princess.position = phase1StartPoint.position;
            princess.rotation = phase1StartPoint.rotation;
        }
        
        // Reset camera position
        if (phase1CameraPosition != null)
        {
            mainCamera.transform.position = phase1CameraPosition.position;
            mainCamera.transform.rotation = phase1CameraPosition.rotation;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("PhaseManager: Reset to Phase 1");
        }
    }
    
    // Visual debugging
    private void OnDrawGizmosSelected()
    {
        if (door != null)
        {
            // Draw detection range around door
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(door.position, detectionRange);
        }
        
        // Draw phase points
        if (phase1StartPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(phase1StartPoint.position, 0.5f);
            Gizmos.DrawIcon(phase1StartPoint.position + Vector3.up, "d_winbtn_mac_max", true);
        }
        
        if (phase2TeleportPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(phase2TeleportPoint.position, 0.5f);
            Gizmos.DrawIcon(phase2TeleportPoint.position + Vector3.up, "d_winbtn_mac_max", true);
        }
        
        // Draw camera positions
        if (phase1CameraPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(phase1CameraPosition.position, Vector3.one * 0.3f);
        }
        
        if (phase2CameraPosition != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(phase2CameraPosition.position, Vector3.one * 0.3f);
        }
    }
}
