using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    [Header("Movable Box Debug")]
    [SerializeField] private MovableFlower movableFlower;
    [SerializeField] private Door targetDoor;
    
    [Header("Debug Controls")]
    [SerializeField] private KeyCode debugKey = KeyCode.F1;
    
    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            DebugMovableFlower();
        }
    }
    
    private void DebugMovableFlower()
    {
        Debug.Log("=== MOVABLE BOX DEBUG ===");
        
        if (movableFlower == null)
        {
            Debug.LogError("❌ MovableFlower not set!");
            return;
        }
        
        // Check box settings
        Debug.Log($"📦 Box: {movableFlower.name}");
        Debug.Log($"📍 Position: {movableFlower.transform.position}");
        
        // Check flower
        Flower flower = movableFlower.GetComponentInChildren<Flower>();
        if (flower != null)
        {
            Debug.Log($"🌸 Flower: {flower.name}");
            Debug.Log($"🔌 Flower activated: {flower.IsActivated}");
            Debug.Log($"🎯 Current target: {flower.CurrentTarget?.name}");
        }
        else
        {
            Debug.LogError("❌ Flower not found in the box!");
        }
        
        // Check door
        if (targetDoor != null)
        {
            Debug.Log($"🚪 Door: {targetDoor.name}");
            Debug.Log($"📍 Door position: {targetDoor.transform.position}");
            Debug.Log($"🔌 Door receiving light: {targetDoor.IsReceivingLight}");
            Debug.Log($"🚪 Door open: {targetDoor.IsOpen}");
            
            float distance = Vector3.Distance(movableFlower.transform.position, targetDoor.transform.position);
            Debug.Log($"📏 Distance to door: {distance:F2}");
        }
        
        Debug.Log("=== END OF DEBUG ===");
    }
    
    // Editor visualization
    private void OnDrawGizmos()
    {
        if (movableFlower != null && targetDoor != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(movableFlower.transform.position, targetDoor.transform.position);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(movableFlower.transform.position, 3f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetDoor.transform.position, 5f);
        }
    }
}
