using UnityEngine;

/// <summary>
/// Utility script to help setup platforms for proper dimension behavior.
/// Use this to quickly configure all platforms in your scene so they only work in their assigned dimension.
/// </summary>
public class DimensionPlatformSetup : MonoBehaviour
{
    [Header("Bulk Setup Configuration")]
    [SerializeField] private bool setupPlatformsInScene = true;
    [SerializeField] private bool enableDimensionCheckByDefault = true;
    
    [Header("Default Dimension Assignment")]
    [SerializeField] private DimensionType defaultDimensionForPlatforms = DimensionType.DimensionA;
    
    [Header("Smart Assignment")]
    [SerializeField] private bool useSmartAssignment = true; // Try to guess dimension based on object names
    [SerializeField] private string dimensionAKeyword = "DimensionA"; // Objects with this in name go to Dimension A
    [SerializeField] private string dimensionBKeyword = "DimensionB"; // Objects with this in name go to Dimension B
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [ContextMenu("Setup All Platforms in Scene")]
    private void SetupAllPlatformsInScene()
    {
        if (!setupPlatformsInScene)
        {
            Debug.LogWarning("[PLATFORM SETUP] Platform setup is disabled. Enable 'Setup Platforms In Scene' first.");
            return;
        }
        
        Platform[] platforms = FindObjectsOfType<Platform>();
        int setupCount = 0;
        
        Debug.Log("=== PLATFORM DIMENSION SETUP STARTED ===");
        Debug.Log($"Found {platforms.Length} platforms in scene");
        
        foreach (Platform platform in platforms)
        {
            if (SetupPlatformDimension(platform))
            {
                setupCount++;
            }
        }
        
        Debug.Log($"=== SETUP COMPLETE: {setupCount} platforms configured ===");
    }
    
    /// <summary>
    /// Setup dimension behavior for a specific platform
    /// </summary>
    private bool SetupPlatformDimension(Platform platform)
    {
        if (platform == null) return false;
        
        // Determine which dimension this platform should belong to
        DimensionType assignedDimension = DeterminePlatformDimension(platform.gameObject);
        
        // Configure the platform
        platform.SetDimension(assignedDimension);
        platform.SetDimensionCheckEnabled(enableDimensionCheckByDefault);
        
        if (showDebugInfo)
        {
            Debug.Log($"[PLATFORM SETUP] ✅ Platform {platform.name} configured for dimension {assignedDimension} | Check enabled: {enableDimensionCheckByDefault}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Determine which dimension a platform should belong to
    /// </summary>
    private DimensionType DeterminePlatformDimension(GameObject platformObj)
    {
        if (useSmartAssignment)
        {
            string objName = platformObj.name.ToLower();
            
            // Check for dimension keywords in platform name
            if (objName.Contains(dimensionAKeyword.ToLower()))
            {
                return DimensionType.DimensionA;
            }
            else if (objName.Contains(dimensionBKeyword.ToLower()))
            {
                return DimensionType.DimensionB;
            }
            
            // Check parent names
            Transform parent = platformObj.transform.parent;
            while (parent != null)
            {
                string parentName = parent.name.ToLower();
                if (parentName.Contains(dimensionAKeyword.ToLower()))
                {
                    return DimensionType.DimensionA;
                }
                else if (parentName.Contains(dimensionBKeyword.ToLower()))
                {
                    return DimensionType.DimensionB;
                }
                parent = parent.parent;
            }
        }
        
        // Use default dimension
        return defaultDimensionForPlatforms;
    }
    
    [ContextMenu("List All Platforms in Scene")]
    private void ListAllPlatformsInScene()
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        
        Debug.Log("=== ALL PLATFORMS IN SCENE ===");
        Debug.Log($"Total platforms found: {platforms.Length}");
        
        foreach (Platform platform in platforms)
        {
            string dimensionInfo = $"Dimension: {platform.GetDimension}";
            string checkInfo = $"Check Enabled: {platform.IsDimensionCheckEnabled}";
            string activeInfo = $"Currently Active: {platform.IsActivated}";
            
            Debug.Log($"  - Platform: {platform.name} | {dimensionInfo} | {checkInfo} | {activeInfo}");
        }
        
        Debug.Log("==============================");
    }
    
    [ContextMenu("Disable All Platform Dimension Checks")]
    private void DisableAllPlatformDimensionChecks()
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        int disabledCount = 0;
        
        foreach (Platform platform in platforms)
        {
            platform.SetDimensionCheckEnabled(false);
            disabledCount++;
        }
        
        Debug.Log($"[PLATFORM SETUP] Disabled dimension checking for {disabledCount} platforms");
        Debug.LogWarning("⚠️ Platforms will now work in ANY dimension - this might not be what you want!");
    }
    
    [ContextMenu("Enable All Platform Dimension Checks")]
    private void EnableAllPlatformDimensionChecks()
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        int enabledCount = 0;
        
        foreach (Platform platform in platforms)
        {
            platform.SetDimensionCheckEnabled(true);
            enabledCount++;
        }
        
        Debug.Log($"[PLATFORM SETUP] Enabled dimension checking for {enabledCount} platforms");
    }
    
    [ContextMenu("Test Dimension Switch")]
    private void TestDimensionSwitch()
    {
        if (DimensionManager.Instance != null)
        {
            DimensionType currentDimension = DimensionManager.Instance.CurrentDimension;
            DimensionType newDimension = (currentDimension == DimensionType.DimensionA) ? 
                                        DimensionType.DimensionB : DimensionType.DimensionA;
            
            Debug.Log($"[PLATFORM TEST] Switching dimension from {currentDimension} to {newDimension}");
            DimensionManager.Instance.SetDimension(newDimension);
            
            // List which platforms should work in the new dimension
            Platform[] platforms = FindObjectsOfType<Platform>();
            int activeCount = 0;
            int inactiveCount = 0;
            
            foreach (Platform platform in platforms)
            {
                bool shouldWork = (platform.GetDimension == newDimension) || !platform.IsDimensionCheckEnabled;
                if (shouldWork)
                {
                    activeCount++;
                    Debug.Log($"  ✅ Platform {platform.name} should work in {newDimension}");
                }
                else
                {
                    inactiveCount++;
                    Debug.Log($"  ❌ Platform {platform.name} will NOT work in {newDimension} (belongs to {platform.GetDimension})");
                }
            }
            
            Debug.Log($"[PLATFORM TEST] Result: {activeCount} platforms active, {inactiveCount} platforms inactive in {newDimension}");
        }
        else
        {
            Debug.LogWarning("[PLATFORM TEST] No DimensionManager found in scene!");
        }
    }
    
    [ContextMenu("Set All Platforms to Dimension A")]
    private void SetAllPlatformsToDimensionA()
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        foreach (Platform platform in platforms)
        {
            platform.SetDimension(DimensionType.DimensionA);
        }
        Debug.Log($"[PLATFORM SETUP] Set {platforms.Length} platforms to Dimension A");
    }
    
    [ContextMenu("Set All Platforms to Dimension B")]
    private void SetAllPlatformsToDimensionB()
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        foreach (Platform platform in platforms)
        {
            platform.SetDimension(DimensionType.DimensionB);
        }
        Debug.Log($"[PLATFORM SETUP] Set {platforms.Length} platforms to Dimension B");
    }
}
