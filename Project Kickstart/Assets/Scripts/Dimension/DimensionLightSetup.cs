using UnityEngine;

/// <summary>
/// Utility script to help setup DimensionLight components for multiple lights at once.
/// Use this to quickly configure all lights in your scene for proper dimension behavior.
/// </summary>
public class DimensionLightSetup : MonoBehaviour
{
    [Header("Bulk Setup Configuration")]
    [SerializeField] private bool setupFlowersInScene = true;
    [SerializeField] private bool setupTorchesInScene = true;
    [SerializeField] private bool setupLightsInScene = true;
    
    [Header("Default Dimension Assignment")]
    [SerializeField] private DimensionType defaultDimensionForFlowers = DimensionType.DimensionA;
    [SerializeField] private DimensionType defaultDimensionForTorches = DimensionType.DimensionA;
    [SerializeField] private DimensionType defaultDimensionForLights = DimensionType.DimensionA;
    
    [Header("Smart Assignment")]
    [SerializeField] private bool useSmartAssignment = true; // Try to guess dimension based on object names
    [SerializeField] private string dimensionAKeyword = "DimensionA"; // Objects with this in name go to Dimension A
    [SerializeField] private string dimensionBKeyword = "DimensionB"; // Objects with this in name go to Dimension B
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [ContextMenu("Setup All Lights in Scene")]
    private void SetupAllLightsInScene()
    {
        int setupCount = 0;
        
        Debug.Log("=== DIMENSION LIGHT SETUP STARTED ===");
        
        // Setup Flowers
        if (setupFlowersInScene)
        {
            Flower[] flowers = FindObjectsOfType<Flower>();
            foreach (Flower flower in flowers)
            {
                if (SetupDimensionLight(flower.gameObject, "Flower"))
                {
                    setupCount++;
                }
            }
        }
        
        // Setup Torches
        if (setupTorchesInScene)
        {
            Torch[] torches = FindObjectsOfType<Torch>();
            foreach (Torch torch in torches)
            {
                if (SetupDimensionLight(torch.gameObject, "Torch"))
                {
                    setupCount++;
                }
            }
        }
        
        // Setup standalone Lights
        if (setupLightsInScene)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                // Skip lights that are children of Flowers or Torches (already handled above)
                if (light.GetComponentInParent<Flower>() == null && 
                    light.GetComponentInParent<Torch>() == null)
                {
                    if (SetupDimensionLight(light.gameObject, "Light"))
                    {
                        setupCount++;
                    }
                }
            }
        }
        
        Debug.Log($"=== SETUP COMPLETE: {setupCount} objects configured ===");
    }
    
    /// <summary>
    /// Setup DimensionLight component for a specific GameObject
    /// </summary>
    private bool SetupDimensionLight(GameObject obj, string objectType)
    {
        // Check if already has DimensionLight component
        DimensionLight existingDimensionLight = obj.GetComponent<DimensionLight>();
        if (existingDimensionLight != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[SETUP] {objectType} {obj.name} already has DimensionLight - skipping");
            }
            return false;
        }
        
        // Add DimensionLight component
        DimensionLight dimensionLight = obj.AddComponent<DimensionLight>();
        
        // Determine which dimension this should belong to
        DimensionType assignedDimension = DetermineDimension(obj, objectType);
        dimensionLight.SetDimension(assignedDimension);
        
        if (showDebugInfo)
        {
            Debug.Log($"[SETUP] ✅ Added DimensionLight to {objectType} {obj.name} - Dimension: {assignedDimension}");
        }
        
        return true;
    }
    
    /// <summary>
    /// Determine which dimension an object should belong to
    /// </summary>
    private DimensionType DetermineDimension(GameObject obj, string objectType)
    {
        if (useSmartAssignment)
        {
            string objName = obj.name.ToLower();
            
            // Check for dimension keywords in object name
            if (objName.Contains(dimensionAKeyword.ToLower()))
            {
                return DimensionType.DimensionA;
            }
            else if (objName.Contains(dimensionBKeyword.ToLower()))
            {
                return DimensionType.DimensionB;
            }
            
            // Check parent names
            Transform parent = obj.transform.parent;
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
        
        // Use default dimension based on object type
        switch (objectType)
        {
            case "Flower":
                return defaultDimensionForFlowers;
            case "Torch":
                return defaultDimensionForTorches;
            case "Light":
                return defaultDimensionForLights;
            default:
                return DimensionType.DimensionA;
        }
    }
    
    [ContextMenu("Remove All DimensionLight Components")]
    private void RemoveAllDimensionLights()
    {
        DimensionLight[] dimensionLights = FindObjectsOfType<DimensionLight>();
        int removedCount = 0;
        
        foreach (DimensionLight dl in dimensionLights)
        {
            if (dl != null)
            {
                DestroyImmediate(dl);
                removedCount++;
            }
        }
        
        Debug.Log($"[SETUP] Removed {removedCount} DimensionLight components");
    }
    
    [ContextMenu("List All Lights in Scene")]
    private void ListAllLightsInScene()
    {
        Debug.Log("=== ALL LIGHTS IN SCENE ===");
        
        Flower[] flowers = FindObjectsOfType<Flower>();
        Debug.Log($"Flowers found: {flowers.Length}");
        foreach (Flower flower in flowers)
        {
            bool hasDimensionLight = flower.GetComponent<DimensionLight>() != null;
            Debug.Log($"  - Flower: {flower.name} | Has DimensionLight: {hasDimensionLight}");
        }
        
        Torch[] torches = FindObjectsOfType<Torch>();
        Debug.Log($"Torches found: {torches.Length}");
        foreach (Torch torch in torches)
        {
            bool hasDimensionLight = torch.GetComponent<DimensionLight>() != null;
            Debug.Log($"  - Torch: {torch.name} | Has DimensionLight: {hasDimensionLight}");
        }
        
        Light[] lights = FindObjectsOfType<Light>();
        Debug.Log($"Unity Lights found: {lights.Length}");
        foreach (Light light in lights)
        {
            bool hasDimensionLight = light.GetComponent<DimensionLight>() != null;
            bool isChildOfFlowerOrTorch = (light.GetComponentInParent<Flower>() != null || 
                                         light.GetComponentInParent<Torch>() != null);
            Debug.Log($"  - Light: {light.name} | Has DimensionLight: {hasDimensionLight} | Child of Flower/Torch: {isChildOfFlowerOrTorch}");
        }
        
        Debug.Log("===========================");
    }
    
    [ContextMenu("Test Dimension Switch")]
    private void TestDimensionSwitch()
    {
        if (DimensionManager.Instance != null)
        {
            DimensionType currentDimension = DimensionManager.Instance.CurrentDimension;
            DimensionType newDimension = (currentDimension == DimensionType.DimensionA) ? 
                                        DimensionType.DimensionB : DimensionType.DimensionA;
            
            Debug.Log($"[TEST] Switching dimension from {currentDimension} to {newDimension}");
            DimensionManager.Instance.SetDimension(newDimension);
        }
        else
        {
            Debug.LogWarning("[TEST] No DimensionManager found in scene!");
        }
    }
}
