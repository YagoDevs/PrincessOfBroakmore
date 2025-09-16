using System;
using UnityEngine;

/// <summary>
/// Enumeration to identify available dimensions
/// </summary>
public enum DimensionType
{
    DimensionA,
    DimensionB
}

/// <summary>
/// Main game dimension manager.
/// Controls the global state of the current dimension and notifies all relevant objects when there's a change.
/// </summary>
public class DimensionManager : MonoBehaviour
{
    [Header("Dimension Settings")]
    [SerializeField] private DimensionType currentDimension = DimensionType.DimensionA;
    [SerializeField] private KeyCode switchDimensionKey = KeyCode.Tab;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Singleton pattern for global access
    public static DimensionManager Instance { get; private set; }
    
    // Public property to access current dimension
    public DimensionType CurrentDimension => currentDimension;
    
    // Event triggered when dimension changes
    public static event Action<DimensionType> OnDimensionChanged;
    
    // Specific event for objects that need to know about changes
    public static event Action<DimensionType, DimensionType> OnDimensionSwitched;

    private void Awake()
    {
        // Singleton implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Notify all objects about initial dimension
        NotifyDimensionChange();
        
        if (showDebugInfo)
        {
            Debug.Log($"DimensionManager started. Current dimension: {currentDimension}");
        }
    }

    private void Update()
    {
        // Check if player pressed key to switch dimension
        if (Input.GetKeyDown(switchDimensionKey))
        {
            SwitchDimension();
        }
    }

    /// <summary>
    /// Switch to the other dimension
    /// </summary>
    public void SwitchDimension()
    {
        DimensionType previousDimension = currentDimension;
        
        // Alternate between dimensions
        currentDimension = currentDimension == DimensionType.DimensionA 
            ? DimensionType.DimensionB 
            : DimensionType.DimensionA;
        
        if (showDebugInfo)
        {
            Debug.Log($"Dimension changed from {previousDimension} to {currentDimension}");
        }
        
        // Play dimension switch sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDimensionSwitchSound(previousDimension, currentDimension);
        }
        
        // Notify all objects about the change
        NotifyDimensionChange();
        NotifyDimensionSwitch(previousDimension, currentDimension);
    }

    /// <summary>
    /// Set a specific dimension
    /// </summary>
    /// <param name="newDimension">New dimension to be set</param>
    public void SetDimension(DimensionType newDimension)
    {
        if (currentDimension == newDimension)
            return;
            
        DimensionType previousDimension = currentDimension;
        currentDimension = newDimension;
        
        if (showDebugInfo)
        {
            Debug.Log($"Dimension set from {previousDimension} to {currentDimension}");
        }
        
        // Play dimension switch sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDimensionSwitchSound(previousDimension, currentDimension);
        }
        
        NotifyDimensionChange();
        NotifyDimensionSwitch(previousDimension, currentDimension);
    }

    /// <summary>
    /// Notify all listeners about dimension change
    /// </summary>
    private void NotifyDimensionChange()
    {
        OnDimensionChanged?.Invoke(currentDimension);
    }

    /// <summary>
    /// Notify about specific dimension switch (with previous and new dimension)
    /// </summary>
    private void NotifyDimensionSwitch(DimensionType from, DimensionType to)
    {
        OnDimensionSwitched?.Invoke(from, to);
    }

    /// <summary>
    /// Check if current dimension is the specified one
    /// </summary>
    /// <param name="dimension">Dimension to check</param>
    /// <returns>True if it's the current dimension</returns>
    public bool IsCurrentDimension(DimensionType dimension)
    {
        return currentDimension == dimension;
    }

    private void OnDestroy()
    {
        // Clear events when destroying object
        OnDimensionChanged = null;
        OnDimensionSwitched = null;
    }

    private void OnGUI()
    {
        if (!showDebugInfo)
            return;
            
        // Simple debug interface
        GUI.Box(new Rect(10, 10, 200, 60), "Dimension Manager");
        GUI.Label(new Rect(20, 30, 180, 20), $"Current Dimension: {currentDimension}");
        GUI.Label(new Rect(20, 50, 180, 20), $"Key: {switchDimensionKey}");
    }
}
