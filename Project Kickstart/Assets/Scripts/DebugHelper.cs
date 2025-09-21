using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    [Header("Debug da Caixa Móvel")]
    [SerializeField] private MovableFlower movableFlower;
    [SerializeField] private Door targetDoor;
    
    [Header("Controles de Debug")]
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
        Debug.Log("=== DEBUG DA CAIXA MÓVEL ===");
        
        if (movableFlower == null)
        {
            Debug.LogError("❌ MovableFlower não configurada!");
            return;
        }
        
        // Verificar configurações da caixa
        Debug.Log($"📦 Caixa: {movableFlower.name}");
        Debug.Log($"📍 Posição: {movableFlower.transform.position}");
        
        // Verificar flor
        Flower flower = movableFlower.GetComponentInChildren<Flower>();
        if (flower != null)
        {
            Debug.Log($"🌸 Flor: {flower.name}");
            Debug.Log($"🔌 Flor ativada: {flower.IsActivated}");
            Debug.Log($"🎯 Alvo atual: {flower.CurrentTarget?.name}");
        }
        else
        {
            Debug.LogError("❌ Flor não encontrada na caixa!");
        }
        
        // Verificar porta
        if (targetDoor != null)
        {
            Debug.Log($"🚪 Porta: {targetDoor.name}");
            Debug.Log($"📍 Posição da porta: {targetDoor.transform.position}");
            Debug.Log($"🔌 Porta recebendo luz: {targetDoor.IsReceivingLight}");
            Debug.Log($"🚪 Porta aberta: {targetDoor.IsOpen}");
            
            float distance = Vector3.Distance(movableFlower.transform.position, targetDoor.transform.position);
            Debug.Log($"📏 Distância da porta: {distance:F2}");
        }
        
        Debug.Log("=== FIM DO DEBUG ===");
    }
    
    // Visualização no editor
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
