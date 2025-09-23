using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Platform : MonoBehaviour
{
    [Header("Platform Settings")]
    [SerializeField] private Torch torch; // Reference to the torch it controls
    [SerializeField] private Transform newTarget; // New target for the torch when activated
    
    [Header("Direct Flower Connection")]
    [SerializeField] private Flower sourceFlower; // Flower that will emit light
    [SerializeField] private Flower targetFlower; // Flower that will receive light
    [SerializeField] private bool useDirectFlowerConnection = false; // Whether to use direct connection instead of the torch
    
    [Header("Direction Cycle System")]
    [SerializeField] private Flower flowerToCycle; // Flower that will have its direction changed
    [SerializeField] private bool useCycleMode = false; // Whether to use cycle mode
    
    [Header("Interaction Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool canBeDeactivated = true; // Whether it can be deactivated on exit
    
    [Header("Dimension Settings")]
    [SerializeField] private DimensionType activeDimension = DimensionType.DimensionA; // Which dimension this platform belongs to
    [SerializeField] private bool onlyWorkInCorrectDimension = true; // Whether platform only works in its assigned dimension
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject activatedEffect; // Effect when activated
    [SerializeField] private Material activatedMaterial; // Material when activated
    [SerializeField] private Material deactivatedMaterial; // Material when deactivated
    [SerializeField] private GameObject activatedModel; // Model when activated
    [SerializeField] private GameObject deactivatedModel; // Model when deactivated
    
    [Header("Movement Settings")]
    [SerializeField] private float depthOffset = 0.1f; // How much the platform goes down
    [SerializeField] private float animationSpeed = 5f; // Animation speed
    
    private Renderer platformRenderer;
    private Transform originalTorchTarget; // Stores the original target of the torch
    private Transform originalSourceTarget; // Stores the original target of the source flower
    private Vector3 originalPosition; // Original position of the platform
    private Vector3 targetPosition; // Target position of the platform
    private bool isMoving = false; // Whether it is moving
    private bool playerOnPlatform = false; // Track if player is currently on platform
    private bool hasPlayedMovementSound = false; // Track if movement sound has been played for current movement

    private void Start()
    {
        // Configure trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"Platform {gameObject.name}: Collider not found! Adding BoxCollider...");
            gameObject.AddComponent<BoxCollider>().isTrigger = true;
        }

        // Get renderer for visual changes
        platformRenderer = GetComponent<Renderer>();
        
        // Store torch original target
        if (torch != null)
        {
            originalTorchTarget = torch.CurrentTarget;
        }
        
        // Store source flower original target
        if (sourceFlower != null)
        {
            originalSourceTarget = sourceFlower.CurrentTarget;
        }
        
        // Store original position
        originalPosition = transform.position;
        targetPosition = originalPosition;
        
        // Set initial state
        UpdateVisualState();
        
        // Subscribe to dimension changes to handle platform state when player is on it
        DimensionManager.OnDimensionChanged += OnDimensionChanged;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from dimension changes
        DimensionManager.OnDimensionChanged -= OnDimensionChanged;
    }
    
    /// <summary>
    /// Called when dimension changes - handle platform deactivation if player is on it
    /// </summary>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        // If player is on platform and we're changing to a dimension where this platform shouldn't work
        if (playerOnPlatform && onlyWorkInCorrectDimension && newDimension != activeDimension)
        {
            Debug.Log($"[PLATFORM DEBUG] Dimension changed to {newDimension}. Platform {gameObject.name} belongs to {activeDimension}. Force deactivating platform since player is on it.");
            
            // Force deactivate the platform since it shouldn't work in the new dimension
            if (isActivated)
            {
                DeactivatePlatform();
            }
        }
    }

    private void Update()
    {
        // Animate platform movement
        if (isMoving)
        {
            // Play movement sound once when movement starts
            if (!hasPlayedMovementSound)
            {
                PlayMovementSound();
                hasPlayedMovementSound = true;
            }
            
            transform.position = Vector3.Lerp(transform.position, targetPosition, animationSpeed * Time.deltaTime);
            
            // Stop moving when close enough
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                hasPlayedMovementSound = false; // Reset for next movement
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PLATFORM DEBUG] Something entered trigger: {other.name} with tag: {other.tag}");
        
        if (other.CompareTag(playerTag) && !isActivated)
        {
            // Check dimension before activating
            if (onlyWorkInCorrectDimension && !IsInCorrectDimension())
            {
                Debug.Log($"[PLATFORM DEBUG] Platform {gameObject.name} is in dimension {activeDimension}, but current dimension is {GetCurrentDimension()}. Platform will NOT activate.");
                return;
            }
            
            Debug.Log($"[PLATFORM DEBUG] Player detected in correct dimension! Activating platform...");
            playerOnPlatform = true;
            ActivatePlatform();
        }
        else if (!other.CompareTag(playerTag))
        {
            Debug.Log($"[PLATFORM DEBUG] Object {other.name} does not have tag '{playerTag}'");
        }
        else if (isActivated)
        {
            Debug.Log($"[PLATFORM DEBUG] Platform already activated");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[PLATFORM DEBUG] Something exited trigger: {other.name}");
        
        if (other.CompareTag(playerTag) && isActivated && canBeDeactivated)
        {
            // Check dimension before deactivating
            if (onlyWorkInCorrectDimension && !IsInCorrectDimension())
            {
                Debug.Log($"[PLATFORM DEBUG] Platform {gameObject.name} not in correct dimension, ignoring exit.");
                return;
            }
            
            Debug.Log($"[PLATFORM DEBUG] Player exited! Deactivating platform...");
            playerOnPlatform = false;
            DeactivatePlatform();
        }
    }

    private void ActivatePlatform()
    {
        Debug.Log($"[PLATFORM DEBUG] Trying to activate platform {gameObject.name}");
        
        isActivated = true;
        Debug.Log($"[PLATFORM DEBUG] State changed to activated");
        
        if (useCycleMode)
        {
            // Mode: Flower direction cycle
            if (flowerToCycle == null)
            {
                Debug.LogWarning($"[PLATFORM DEBUG] Platform {gameObject.name}: Flower to cycle not set!");
                return;
            }
            
            // Cycle to next direction
            flowerToCycle.CycleToNextDirection();
            Debug.Log($"[PLATFORM DEBUG] Flower {flowerToCycle.name} cycled to direction: {flowerToCycle.CurrentDirectionName}");
            
            // If the flower is not active, activate it
            if (!flowerToCycle.IsActivated)
            {
                flowerToCycle.ReceiveLight();
                Debug.Log($"[PLATFORM DEBUG] Flower {flowerToCycle.name} activated");
            }
        }
        else if (useDirectFlowerConnection)
        {
            // Mode: Direct connection between flowers
            if (sourceFlower == null || targetFlower == null)
            {
                Debug.LogWarning($"[PLATFORM DEBUG] Platform {gameObject.name}: Flowers not set for direct connection!");
                return;
            }
            
            // Change source flower target to destination flower
            sourceFlower.ChangeTarget(targetFlower.transform);
            Debug.Log($"[PLATFORM DEBUG] Flower {sourceFlower.name} redirected to: {targetFlower.name}");
            
            // Activate the source flower if it is not active
            if (!sourceFlower.IsActivated)
            {
                sourceFlower.ReceiveLight();
                Debug.Log($"[PLATFORM DEBUG] Flower {sourceFlower.name} activated");
            }
        }
        else
        {
            // Mode: Torch control
            if (torch == null)
            {
                Debug.LogWarning($"[PLATFORM DEBUG] Platform {gameObject.name}: Torch not set!");
                return;
            }
            
            // Change torch target
            torch.SetTargetFlower(newTarget);
            Debug.Log($"[PLATFORM DEBUG] Torch redirected to: {(newTarget != null ? newTarget.name : "null")}");
        }
        
        // Lower the platform
        targetPosition = originalPosition - Vector3.up * depthOffset;
        isMoving = true;
        Debug.Log($"[PLATFORM DEBUG] Original position: {originalPosition}, New position: {targetPosition}");
        
        // Update visuals
        UpdateVisualState();
        Debug.Log($"[PLATFORM DEBUG] Visuals updated");
        
        Debug.Log($"[PLATFORM DEBUG] Platform {gameObject.name} activated successfully!");
    }

    private void DeactivatePlatform()
    {
        isActivated = false;
        
        if (useCycleMode)
        {
            // Mode: Direction cycle - do NOTHING on exit
            Debug.Log($"Platform {gameObject.name} deactivated! Cycle mode - keeping current direction of flower {flowerToCycle?.name}");
            // Do not reset flower direction!
        }
        else if (useDirectFlowerConnection)
        {
            // Mode: Direct connection between flowers
            if (sourceFlower != null)
            {
                // Restore original target of the source flower silently (no sound)
                sourceFlower.ChangeTargetSilently(originalSourceTarget);
                Debug.Log($"Platform {gameObject.name} deactivated! Flower {sourceFlower.name} restored to original target (silently).");
            }
        }
        else
        {
            // Mode: Torch control
            if (torch != null)
            {
                // Restore original torch target silently (no activation sound)
                torch.SetTargetFlowerSilently(originalTorchTarget);
                Debug.Log($"Platform {gameObject.name} deactivated! Torch restored to original target (silently).");
            }
        }
        
        // Move platform back to original position
        targetPosition = originalPosition;
        isMoving = true;
        
        // Update visuals
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        Debug.Log($"[PLATFORM DEBUG] Updating visuals - Activated state: {isActivated}");
        
        // Toggle visual effect
        if (activatedEffect != null)
        {
            activatedEffect.SetActive(isActivated);
            Debug.Log($"[PLATFORM DEBUG] Effect enabled: {isActivated}");
        }
        
        // Change material if configured
        if (platformRenderer != null)
        {
            if (isActivated && activatedMaterial != null)
            {
                platformRenderer.material = activatedMaterial;
                Debug.Log($"[PLATFORM DEBUG] Material changed to activated");
            }
            else if (!isActivated && deactivatedMaterial != null)
            {
                platformRenderer.material = deactivatedMaterial;
                Debug.Log($"[PLATFORM DEBUG] Material changed to deactivated");
            }
        }
        
        // Toggle model if configured
        if (activatedModel != null && deactivatedModel != null)
        {
            activatedModel.SetActive(isActivated);
            deactivatedModel.SetActive(!isActivated);
            Debug.Log($"[PLATFORM DEBUG] Models updated - Activated: {isActivated}, Deactivated: {!isActivated}");
        }
        else
        {
            Debug.Log($"[PLATFORM DEBUG] Models not configured - ActivatedModel: {(activatedModel != null ? "OK" : "NULL")}, DeactivatedModel: {(deactivatedModel != null ? "OK" : "NULL")}");
        }
    }

    // Method to configure torch via script
    public void SetTorch(Torch newTorch)
    {
        torch = newTorch;
        if (torch != null)
        {
            originalTorchTarget = torch.CurrentTarget;
        }
    }

    // Method to configure new target via script
    public void SetNewTarget(Transform target)
    {
        newTarget = target;
        
        // If platform is already activated, update immediately
        if (isActivated && torch != null)
        {
            torch.SetTargetFlower(newTarget);
        }
    }

    // Methods to configure models via script
    public void SetActivatedModel(GameObject model)
    {
        activatedModel = model;
        UpdateVisualState();
    }

    public void SetDeactivatedModel(GameObject model)
    {
        deactivatedModel = model;
        UpdateVisualState();
    }

    // Method to configure depth offset
    public void SetDepthOffset(float offset)
    {
        depthOffset = offset;
    }

    // Method to configure animation speed
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }

    // Methods to force activation/deactivation
    public void ForceActivate()
    {
        if (!isActivated)
        {
            ActivatePlatform();
        }
    }

    public void ForceDeactivate()
    {
        if (isActivated)
        {
            DeactivatePlatform();
        }
    }

    private void OnValidate()
    {
        // Ensure collider is trigger in the editor
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// Check if the platform is in the correct dimension to be activated
    /// </summary>
    private bool IsInCorrectDimension()
    {
        if (DimensionManager.Instance == null)
        {
            Debug.LogWarning($"[PLATFORM DEBUG] No DimensionManager found! Platform {gameObject.name} will work regardless of dimension.");
            return true; // If no dimension manager, allow activation
        }
        
        return DimensionManager.Instance.CurrentDimension == activeDimension;
    }
    
    /// <summary>
    /// Get the current dimension (for debug purposes)
    /// </summary>
    private DimensionType GetCurrentDimension()
    {
        if (DimensionManager.Instance == null)
        {
            return DimensionType.DimensionA; // Default
        }
        
        return DimensionManager.Instance.CurrentDimension;
    }
    
    /// <summary>
    /// Manually set which dimension this platform belongs to
    /// </summary>
    public void SetDimension(DimensionType dimension)
    {
        activeDimension = dimension;
        Debug.Log($"[PLATFORM DEBUG] Platform {gameObject.name} assigned to dimension {dimension}");
    }
    
    /// <summary>
    /// Enable or disable dimension checking
    /// </summary>
    public void SetDimensionCheckEnabled(bool enabled)
    {
        onlyWorkInCorrectDimension = enabled;
        Debug.Log($"[PLATFORM DEBUG] Platform {gameObject.name} dimension checking: {(enabled ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Play movement sound based on platform direction
    /// </summary>
    private void PlayMovementSound()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[PLATFORM AUDIO] AudioManager not found for platform {gameObject.name}!");
            return;
        }
        
        // Determine if platform is moving down (activated) or up (deactivated)
        bool isMovingDown = (targetPosition.y < originalPosition.y);
        
        // Play the movement sound
        AudioManager.Instance.PlayPlatformMovementSound(transform.position, isMovingDown);
        
        Debug.Log($"[PLATFORM AUDIO] Playing movement sound for platform {gameObject.name} - Moving {(isMovingDown ? "DOWN" : "UP")}");
    }

    // Properties for external access
    public bool IsActivated => isActivated;
    public Torch AssociatedTorch => torch;
    public Transform NewTarget => newTarget;
    public DimensionType GetDimension => activeDimension;
    public bool IsDimensionCheckEnabled => onlyWorkInCorrectDimension;
}
