using UnityEngine;

/// <summary>
/// Script for objects that only exist in a specific dimension.
/// Example: a door that only exists in Dimension A, a platform that only exists in Dimension B.
/// </summary>
public class DimensionObject : MonoBehaviour
{
    [Header("Dimension Configuration")]
    [SerializeField] private DimensionType activeDimension = DimensionType.DimensionA;
    [SerializeField] private bool startActive = true;
    
    [Header("Hiding Mode")]
    [SerializeField] private HidingMode hidingMode = HidingMode.SetActive;
    [SerializeField] private bool disableColliders = true;
    [SerializeField] private bool disableRenderers = true;
    
    [Header("Transition Animation")]
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public enum HidingMode
    {
        SetActive,      // Activate/deactivate GameObject completely
        SetVisible,     // Only control visibility (renderers and colliders)
        SetTransparent  // Use transparency to hide/show
    }

    // Components for control
    private Collider2D[] colliders2D;
    private Collider[] colliders3D;
    private Renderer[] renderers;
    private SpriteRenderer[] spriteRenderers;
    
    // State and animation
    private bool isCurrentlyActive = false;
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Vector3 originalScale;
    private Color[] originalColors;
    
    // Values for transition
    private bool targetActiveState;

    private void Awake()
    {
        // Cache components
        colliders2D = GetComponentsInChildren<Collider2D>();
        colliders3D = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        // Save original scale and colors
        originalScale = transform.localScale;
        CacheOriginalColors();
    }

    private void Start()
    {
        // Define initial state
        isCurrentlyActive = startActive;
        
        // If DimensionManager already exists, apply the current dimension immediately
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension, false);
        }
        else
        {
            // Apply initial state if there is no manager yet
            SetObjectState(isCurrentlyActive, false);
        }
        
        // Subscribe to dimension change event
        DimensionManager.OnDimensionChanged += OnDimensionChanged;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: DimensionObject started for dimension {activeDimension}");
        }
    }

    private void Update()
    {
        // Process transition animation if active
        if (isTransitioning && useTransitionAnimation)
        {
            ProcessTransition();
        }
    }

    /// <summary>
    /// Store original colors of sprites
    /// </summary>
    private void CacheOriginalColors()
    {
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    /// <summary>
    /// Called when dimension changes
    /// </summary>
    /// <param name="newDimension">New active dimension</param>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        ApplyDimensionState(newDimension, useTransitionAnimation);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Dimensão mudou para {newDimension}. Objeto deve estar {(newDimension == activeDimension ? "ativo" : "inativo")}");
        }
    }

    /// <summary>
    /// Apply the state corresponding to the dimension
    /// </summary>
    /// <param name="dimension">Current dimension</param>
    /// <param name="animated">Whether to use animation</param>
    private void ApplyDimensionState(DimensionType dimension, bool animated = true)
    {
        bool shouldBeActive = (dimension == activeDimension);
        
        if (isCurrentlyActive == shouldBeActive && !isTransitioning)
            return;

        SetObjectState(shouldBeActive, animated);
    }

    /// <summary>
    /// Set the object's active/inactive state
    /// </summary>
    /// <param name="active">Whether the object should be active</param>
    /// <param name="animated">Whether to use animation</param>
    private void SetObjectState(bool active, bool animated = true)
    {
        if (animated && useTransitionAnimation && Application.isPlaying)
        {
            StartTransition(active);
        }
        else
        {
            // Immediate application
            ApplyStateImmediate(active);
        }
        
        isCurrentlyActive = active;
    }

    /// <summary>
    /// Apply the state immediately without animation
    /// </summary>
    /// <param name="active">Active state</param>
    private void ApplyStateImmediate(bool active)
    {
        switch (hidingMode)
        {
            case HidingMode.SetActive:
                gameObject.SetActive(active);
                break;
                
            case HidingMode.SetVisible:
                SetColliders(active && disableColliders);
                SetRenderers(active && disableRenderers);
                break;
                
            case HidingMode.SetTransparent:
                SetColliders(active && disableColliders);
                SetTransparency(active ? 1f : 0f);
                break;
        }
        
        // Restore original scale if necessary
        if (active)
        {
            transform.localScale = originalScale;
        }
    }

    /// <summary>
    /// Start an animated transition
    /// </summary>
    /// <param name="targetActive">Target state</param>
    private void StartTransition(bool targetActive)
    {
        if (isTransitioning)
        {
            CompleteTransition();
        }

        targetActiveState = targetActive;
        transitionTimer = 0f;
        isTransitioning = true;
        
        // If appearing, ensure the object is visible for the animation
        if (targetActive)
        {
            switch (hidingMode)
            {
                case HidingMode.SetActive:
                    if (!gameObject.activeInHierarchy)
                        gameObject.SetActive(true);
                    break;
                    
                case HidingMode.SetVisible:
                    SetRenderers(true);
                    break;
                    
                case HidingMode.SetTransparent:
                    // Transparency will be controlled in the animation
                    break;
            }
        }
    }

    /// <summary>
    /// Process the transition animation
    /// </summary>
    private void ProcessTransition()
    {
        transitionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(transitionTimer / transitionDuration);
        
        if (progress >= 1f)
        {
            CompleteTransition();
            return;
        }

        // Reverse progress if disappearing
        float animationProgress = targetActiveState ? progress : (1f - progress);
        
        // Apply animation curves
        float scaleValue = scaleCurve.Evaluate(animationProgress);
        float alphaValue = alphaCurve.Evaluate(animationProgress);
        
        // Scale animation
        transform.localScale = originalScale * scaleValue;
        
        // Transparency animation for transparent mode
        if (hidingMode == HidingMode.SetTransparent)
        {
            SetTransparency(alphaValue);
        }
        else if (hidingMode == HidingMode.SetVisible)
        {
            // For visible mode, only use transparency as animation
            SetTransparency(alphaValue);
        }
    }

    /// <summary>
    /// Complete the transition
    /// </summary>
    private void CompleteTransition()
    {
        isTransitioning = false;
        ApplyStateImmediate(targetActiveState);
    }

    /// <summary>
    /// Control sprite transparency
    /// </summary>
    /// <param name="alpha">Alpha value (0-1)</param>
    private void SetTransparency(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color color = originalColors[i];
                color.a = alpha;
                spriteRenderers[i].color = color;
            }
        }
    }

    /// <summary>
    /// Enable/disable colliders
    /// </summary>
    /// <param name="enabled">Colliders state</param>
    private void SetColliders(bool enabled)
    {
        foreach (var collider in colliders2D)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
        
        foreach (var collider in colliders3D)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    /// <summary>
    /// Enable/disable renderers
    /// </summary>
    /// <param name="enabled">Renderers state</param>
    private void SetRenderers(bool enabled)
    {
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }

    /// <summary>
    /// Force applying the state for the current dimension
    /// </summary>
    [ContextMenu("Apply Current State")]
    public void ForceApplyCurrentState()
    {
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension, false);
        }
    }

    /// <summary>
    /// Toggle this object's active dimension
    /// </summary>
    [ContextMenu("Toggle Active Dimension")]
    public void ToggleActiveDimension()
    {
        activeDimension = activeDimension == DimensionType.DimensionA ? 
            DimensionType.DimensionB : DimensionType.DimensionA;
            
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension, false);
        }
    }

    private void OnDestroy()
    {
        // Remove event subscription on destroy
        DimensionManager.OnDimensionChanged -= OnDimensionChanged;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw visual indicator of the active dimension
        Gizmos.color = activeDimension == DimensionType.DimensionA ? Color.red : Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
