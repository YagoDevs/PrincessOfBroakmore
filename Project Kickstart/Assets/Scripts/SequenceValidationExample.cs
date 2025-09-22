using UnityEngine;

/// <summary>
/// Script de exemplo para demonstrar como funciona a validação de sequência de flores.
/// Este script é apenas para demonstração e pode ser removido após os testes.
/// </summary>
public class SequenceValidationExample : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool showTestInfo = true;
    
    private void Start()
    {
        if (showTestInfo)
        {
            LogValidationExamples();
        }
    }
    
    private void LogValidationExamples()
    {
        Debug.Log("=== SEQUENCE VALIDATION EXAMPLES ===");
        Debug.Log("");
        
        Debug.Log("✅ CASO VÁLIDO:");
        Debug.Log("Torcha -> Flor1 -> Flor2 -> Flor3 -> FlorBox -> Porta");
        Debug.Log("- Todas as flores estão ativadas");
        Debug.Log("- Sequência completa e contínua");
        Debug.Log("- Resultado: PORTA ABRE");
        Debug.Log("");
        
        Debug.Log("❌ CASO INVÁLIDO 1:");
        Debug.Log("Torcha -> Flor1 -> Flor2 -> Parede");
        Debug.Log("Flor3 -> FlorBox -> Porta (desconectada)");
        Debug.Log("- Sequência quebrada na Flor2 (aponta para parede)");
        Debug.Log("- Flor3 não recebe luz da sequência principal");
        Debug.Log("- Resultado: PORTA NÃO ABRE");
        Debug.Log("");
        
        Debug.Log("❌ CASO INVÁLIDO 2:");
        Debug.Log("Torcha -> Flor1 -> Flor2 (desativada) -> Flor3 -> FlorBox -> Porta");
        Debug.Log("- Flor2 não está ativada (não recebeu luz)");
        Debug.Log("- Sequência quebrada na Flor2");
        Debug.Log("- Resultado: PORTA NÃO ABRE");
        Debug.Log("");
        
        Debug.Log("❌ CASO INVÁLIDO 3:");
        Debug.Log("Torcha -> Flor1 -> Objeto qualquer");
        Debug.Log("(Alguém ativa manualmente) Flor3 -> FlorBox -> Porta");
        Debug.Log("- Não há conexão entre a torcha e a Flor3");
        Debug.Log("- Sequência não é contínua da fonte");
        Debug.Log("- Resultado: PORTA NÃO ABRE");
        Debug.Log("");
        
        Debug.Log("🔍 COMO FUNCIONA A VALIDAÇÃO:");
        Debug.Log("1. Sistema encontra todas as tochas ativas");
        Debug.Log("2. Para cada tocha, segue a cadeia de flores conectadas");
        Debug.Log("3. Verifica se cada flor na cadeia está ativada");
        Debug.Log("4. Só considera válido se a cadeia chegar até a porta");
        Debug.Log("5. Se qualquer elo estiver quebrado, a porta não abre");
        Debug.Log("");
        
        Debug.Log("=======================================");
    }
    
    // Método para testar a validação manualmente no editor
    [ContextMenu("Test Validation")]
    private void TestValidation()
    {
        Door[] doors = FindObjectsOfType<Door>();
        
        if (doors.Length == 0)
        {
            Debug.LogWarning("Nenhuma porta encontrada para testar!");
            return;
        }
        
        foreach (Door door in doors)
        {
            Debug.Log($"Testando porta: {door.name}");
            Debug.Log($"Requer luz: {door.RequiresLight}");
            Debug.Log($"Recebendo luz: {door.IsReceivingLight}");
            Debug.Log($"Está aberta: {door.IsOpen}");
            Debug.Log("---");
        }
    }
    
    // Método para mostrar informações sobre flores ativas
    [ContextMenu("Show Flower Status")]
    private void ShowFlowerStatus()
    {
        Flower[] flowers = FindObjectsOfType<Flower>();
        
        Debug.Log("=== STATUS DAS FLORES ===");
        
        foreach (Flower flower in flowers)
        {
            string targetName = flower.CurrentTarget != null ? flower.CurrentTarget.name : "NENHUM";
            Debug.Log($"Flor: {flower.name} | Ativada: {flower.IsActivated} | Alvo: {targetName}");
        }
        
        Debug.Log("========================");
    }
    
    // Método para mostrar informações sobre tochas
    [ContextMenu("Show Torch Status")]
    private void ShowTorchStatus()
    {
        Torch[] torches = FindObjectsOfType<Torch>();
        
        Debug.Log("=== STATUS DAS TOCHAS ===");
        
        foreach (Torch torch in torches)
        {
            string targetName = torch.CurrentTarget != null ? torch.CurrentTarget.name : "NENHUM";
            Debug.Log($"Tocha: {torch.name} | Alvo: {targetName}");
        }
        
        Debug.Log("=========================");
    }
}
