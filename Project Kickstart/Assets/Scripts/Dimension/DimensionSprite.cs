using UnityEngine;

/// <summary>
/// Script for objects that change sprite depending on current dimension.
/// Example: a tree that has different appearance in each dimension.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DimensionSprite : MonoBehaviour
{
    [Header("Sprites per Dimension")]
    [SerializeField] private Sprite spriteForDimensionA;
    [SerializeField] private Sprite spriteForDimensionB;
    
    [Header("Optional Settings")]
    [SerializeField] private bool changeColor = false;
    [SerializeField] private Color colorForDimensionA = Color.white;
    [SerializeField] private Color colorForDimensionB = Color.white;
    
    [Header("Transition Animation")]
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private SpriteRenderer spriteRenderer;
    private bool isTransitioning = false;
    
    // Animation variables
    private float transitionTimer = 0f;
    private Sprite targetSprite;
    private Color targetColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // If DimensionManager already exists, apply current dimension immediately
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionSprite(DimensionManager.Instance.CurrentDimension, false);
        }
        
        // Subscribe to dimension change event
        DimensionManager.OnDimensionChanged += OnDimensionChanged;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: DimensionSprite initialized");
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
    /// Called when dimension changes
    /// </summary>
    /// <param name="newDimension">New active dimension</param>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        ApplyDimensionSprite(newDimension, useTransitionAnimation);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Dimension changed to {newDimension}");
        }
    }

    /// <summary>
    /// Apply sprite and color corresponding to dimension
    /// </summary>
    /// <param name="dimension">Dimension to apply</param>
    /// <param name="animated">Whether to use transition animation</param>
    private void ApplyDimensionSprite(DimensionType dimension, bool animated = true)
    {
        // Determine sprite and color for the dimension
        Sprite newSprite = dimension == DimensionType.DimensionA ? spriteForDimensionA : spriteForDimensionB;
        Color newColor = changeColor ? 
            (dimension == DimensionType.DimensionA ? colorForDimensionA : colorForDimensionB) : 
            spriteRenderer.color;

        // Check if change is necessary
        if (spriteRenderer.sprite == newSprite && spriteRenderer.color == newColor)
            return;

        if (animated && useTransitionAnimation && Application.isPlaying)
        {
            StartTransition(newSprite, newColor);
        }
        else
        {
            // Immediate application
            spriteRenderer.sprite = newSprite;
            if (changeColor)
            {
                spriteRenderer.color = newColor;
            }
        }
    }

    /// <summary>
    /// Start an animated transition to new sprite/color
    /// </summary>
    /// <param name="newSprite">New sprite</param>
    /// <param name="newColor">New color</param>
    private void StartTransition(Sprite newSprite, Color newColor)
    {
        if (isTransitioning)
        {
            // If already transitioning, complete current transition immediately
            CompleteTransition();
        }

        targetSprite = newSprite;
        targetColor = newColor;
        transitionTimer = 0f;
        isTransitioning = true;
    }

    /// <summary>
    /// Process transition animation
    /// </summary>
    private void ProcessTransition()
    {
        transitionTimer += Time.deltaTime;
        float progress = transitionTimer / transitionDuration;
        
        if (progress >= 1f)
        {
            CompleteTransition();
            return;
        }

        // Apply animation curve
        float curveValue = transitionCurve.Evaluate(progress);

        // Fade animation to swap sprite in the middle of transition
        if (progress >= 0.5f && spriteRenderer.sprite != targetSprite)
        {
            spriteRenderer.sprite = targetSprite;
        }

        // Interpolate color if necessary
        if (changeColor)
        {
            Color startColor = progress < 0.5f ? spriteRenderer.color : targetColor;
            Color endColor = targetColor;
            
            // Fade out and fade in
            float alpha = progress < 0.5f ? 
                Mathf.Lerp(1f, 0f, curveValue * 2f) : 
                Mathf.Lerp(0f, 1f, (curveValue - 0.5f) * 2f);
                
            Color currentColor = Color.Lerp(startColor, endColor, progress);
            currentColor.a = alpha;
            spriteRenderer.color = currentColor;
        }
        else
        {
            // Only sprite fade
            float alpha = progress < 0.5f ? 
                Mathf.Lerp(1f, 0f, curveValue * 2f) : 
                Mathf.Lerp(0f, 1f, (curveValue - 0.5f) * 2f);
                
            Color currentColor = spriteRenderer.color;
            currentColor.a = alpha;
            spriteRenderer.color = currentColor;
        }
    }

    /// <summary>
    /// Complete transition and restore final values
    /// </summary>
    private void CompleteTransition()
    {
        isTransitioning = false;
        spriteRenderer.sprite = targetSprite;
        
        if (changeColor)
        {
            spriteRenderer.color = targetColor;
        }
        else
        {
            Color currentColor = spriteRenderer.color;
            currentColor.a = 1f;
            spriteRenderer.color = currentColor;
        }
    }

    /// <summary>
    /// Force immediate application of current dimension
    /// </summary>
    [ContextMenu("Apply Current Dimension")]
    public void ForceApplyCurrentDimension()
    {
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionSprite(DimensionManager.Instance.CurrentDimension, false);
        }
    }

    /// <summary>
    /// Validate if sprites were configured correctly
    /// </summary>
    private void OnValidate()
    {
        if (spriteForDimensionA == null)
        {
            Debug.LogWarning($"{gameObject.name}: Sprite for Dimension A was not defined!");
        }
        
        if (spriteForDimensionB == null)
        {
            Debug.LogWarning($"{gameObject.name}: Sprite for Dimension B was not defined!");
        }
    }

    private void OnDestroy()
    {
        // Remove event subscription when destroying
        DimensionManager.OnDimensionChanged -= OnDimensionChanged;
    }
}
