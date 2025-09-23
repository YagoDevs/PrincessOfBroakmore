using UnityEngine;

/// <summary>
/// Component to integrate lights (Flowers and Torches) with the dimension system.
/// Automatically activates/deactivates lights based on the current dimension.
/// </summary>
public class DimensionLight : MonoBehaviour
{
    [Header("Dimension Configuration")]
    [SerializeField] private DimensionType activeDimension = DimensionType.DimensionA;
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Header("Light Components")]
    [SerializeField] private Flower flower;
    [SerializeField] private Torch torch;
    [SerializeField] private Light[] pointLights; // Additional Unity lights
    [SerializeField] private LineRenderer[] lineRenderers; // Light rays
    [SerializeField] private ParticleSystem[] particleSystems; // Light effects
    
    [Header("Control Options")]
    [SerializeField] private bool controlFlowerActivation = true; // Control flower on/off
    [SerializeField] private bool controlTorchLight = true; // Control torch light
    [SerializeField] private bool controlPointLights = true; // Control Unity lights
    [SerializeField] private bool controlVisualEffects = true; // Control visual effects
    [SerializeField] private bool resetFlowerOnDimensionChange = true; // Reset flower state when changing dimensions
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // State tracking
    private bool isCurrentlyActive = false;
    private bool wasFlowerActiveBefore = false;
    
    private void Awake()
    {
        // Auto-setup components if enabled
        if (autoSetupOnStart)
        {
            AutoSetupComponents();
        }
    }
    
    private void Start()
    {
        // Subscribe to dimension changes
        DimensionManager.OnDimensionChanged += OnDimensionChanged;
        
        // Apply initial dimension state
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[DIMENSION LIGHT] {gameObject.name} initialized for dimension {activeDimension}");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        DimensionManager.OnDimensionChanged -= OnDimensionChanged;
    }
    
    /// <summary>
    /// Automatically find and setup light components in this object
    /// </summary>
    private void AutoSetupComponents()
    {
        // Find flower component
        if (flower == null)
        {
            flower = GetComponent<Flower>();
            if (flower == null)
            {
                flower = GetComponentInChildren<Flower>();
            }
        }
        
        // Find torch component
        if (torch == null)
        {
            torch = GetComponent<Torch>();
            if (torch == null)
            {
                torch = GetComponentInChildren<Torch>();
            }
        }
        
        // Find all lights
        if (pointLights == null || pointLights.Length == 0)
        {
            pointLights = GetComponentsInChildren<Light>();
        }
        
        // Find all line renderers
        if (lineRenderers == null || lineRenderers.Length == 0)
        {
            lineRenderers = GetComponentsInChildren<LineRenderer>();
        }
        
        // Find all particle systems
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[DIMENSION LIGHT] Auto-setup found: Flower={flower != null}, Torch={torch != null}, " +
                     $"Lights={pointLights.Length}, LineRenderers={lineRenderers.Length}, Particles={particleSystems.Length}");
        }
    }
    
    /// <summary>
    /// Called when dimension changes
    /// </summary>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        ApplyDimensionState(newDimension);
        
        if (showDebugInfo)
        {
            Debug.Log($"[DIMENSION LIGHT] {gameObject.name}: Dimension changed to {newDimension}. " +
                     $"Should be active: {newDimension == activeDimension}");
        }
    }
    
    /// <summary>
    /// Apply the dimension state to all light components
    /// </summary>
    private void ApplyDimensionState(DimensionType dimension)
    {
        bool shouldBeActive = (dimension == activeDimension);
        
        if (isCurrentlyActive == shouldBeActive)
            return;
            
        isCurrentlyActive = shouldBeActive;
        
        if (shouldBeActive)
        {
            ActivateLightComponents();
        }
        else
        {
            DeactivateLightComponents();
        }
    }
    
    /// <summary>
    /// Activate all light components for this dimension
    /// </summary>
    private void ActivateLightComponents()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[DIMENSION LIGHT] Activating light components for {gameObject.name}");
        }
        
        // Activate flower
        if (controlFlowerActivation && flower != null)
        {
            // Only reactivate if it was active before, or if it's a fresh start
            if (wasFlowerActiveBefore || !resetFlowerOnDimensionChange)
            {
                if (!flower.IsActivated)
                {
                    flower.ReceiveLight();
                    if (showDebugInfo) Debug.Log($"[DIMENSION LIGHT] Flower {flower.name} reactivated");
                }
            }
        }
        
        // Activate torch
        if (controlTorchLight && torch != null)
        {
            torch.ActivateTorch();
            if (showDebugInfo) Debug.Log($"[DIMENSION LIGHT] Torch {torch.name} activated");
        }
        
        // Activate Unity lights
        if (controlPointLights && pointLights != null)
        {
            foreach (Light light in pointLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }
        }
        
        // Activate visual effects
        if (controlVisualEffects)
        {
            // Line renderers
            if (lineRenderers != null)
            {
                foreach (LineRenderer lr in lineRenderers)
                {
                    if (lr != null)
                    {
                        lr.enabled = true;
                    }
                }
            }
            
            // Particle systems
            if (particleSystems != null)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps != null)
                    {
                        var emission = ps.emission;
                        emission.enabled = true;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Deactivate all light components for this dimension
    /// </summary>
    private void DeactivateLightComponents()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[DIMENSION LIGHT] Deactivating light components for {gameObject.name}");
        }
        
        // Store flower state before deactivating
        if (flower != null)
        {
            wasFlowerActiveBefore = flower.IsActivated;
        }
        
        // Deactivate flower
        if (controlFlowerActivation && flower != null)
        {
            if (flower.IsActivated)
            {
                flower.DeactivateFlower();
                if (showDebugInfo) Debug.Log($"[DIMENSION LIGHT] Flower {flower.name} deactivated");
            }
        }
        
        // Deactivate torch (only visual, not the actual torch object)
        if (controlTorchLight && torch != null)
        {
            // Note: Torch doesn't have a built-in deactivate method, so we control its light components
            Light torchLight = torch.GetComponentInChildren<Light>();
            if (torchLight != null)
            {
                torchLight.enabled = false;
            }
        }
        
        // Deactivate Unity lights
        if (controlPointLights && pointLights != null)
        {
            foreach (Light light in pointLights)
            {
                if (light != null)
                {
                    light.enabled = false;
                }
            }
        }
        
        // Deactivate visual effects
        if (controlVisualEffects)
        {
            // Line renderers
            if (lineRenderers != null)
            {
                foreach (LineRenderer lr in lineRenderers)
                {
                    if (lr != null)
                    {
                        lr.enabled = false;
                    }
                }
            }
            
            // Particle systems
            if (particleSystems != null)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps != null)
                    {
                        var emission = ps.emission;
                        emission.enabled = false;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Manually set which dimension this light belongs to
    /// </summary>
    public void SetDimension(DimensionType dimension)
    {
        activeDimension = dimension;
        
        // Reapply current dimension state
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension);
        }
    }
    
    /// <summary>
    /// Check if this light is currently active
    /// </summary>
    public bool IsActive => isCurrentlyActive;
    
    /// <summary>
    /// Get the dimension this light belongs to
    /// </summary>
    public DimensionType GetDimension => activeDimension;
    
    // Context menu methods for testing
    [ContextMenu("Force Activate")]
    private void ForceActivate()
    {
        ActivateLightComponents();
    }
    
    [ContextMenu("Force Deactivate")]
    private void ForceDeactivate()
    {
        DeactivateLightComponents();
    }
    
    [ContextMenu("Test Dimension Switch")]
    private void TestDimensionSwitch()
    {
        DimensionType testDimension = (activeDimension == DimensionType.DimensionA) ? 
                                     DimensionType.DimensionB : DimensionType.DimensionA;
        
        Debug.Log($"[DIMENSION LIGHT] Testing switch to {testDimension}");
        ApplyDimensionState(testDimension);
    }
    
    [ContextMenu("Auto Setup Components")]
    private void ManualAutoSetup()
    {
        AutoSetupComponents();
        Debug.Log("[DIMENSION LIGHT] Manual auto-setup completed!");
    }
}
