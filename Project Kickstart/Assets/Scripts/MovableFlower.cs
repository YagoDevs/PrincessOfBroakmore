using UnityEngine;

public class MovableFlower : MonoBehaviour
{
    [Header("Configurações da Flor Móvel")]
    [SerializeField] private Flower flower; // Referência à flor nesta caixa
    [SerializeField] private Transform[] possibleTargets; // Alvos possíveis (paredes, porta, etc.)
    [SerializeField] private int currentTargetIndex = 0;
    
    [Header("Configurações de Movimento")]
    [SerializeField] private float detectionRange = 2f; // Distância para detectar alvos
    [SerializeField] private LayerMask targetLayer = 1; // Layer dos alvos
    
    [Header("Controle Manual")]
    [SerializeField] private KeyCode nextTargetKey = KeyCode.E; // Tecla para mudar alvo
    [SerializeField] private bool useManualControl = true; // Se pode mudar alvo manualmente
    
    [Header("Detecção Automática")]
    [SerializeField] private bool useAutoDetection = true; // Se detecta alvos automaticamente
    [SerializeField] private bool prioritizeDoors = true; // Se prioriza portas quando próximo
    [SerializeField] private float doorPriorityDistance = 3f; // Distância para priorizar portas
    
    [Header("Ativação Automática")]
    [SerializeField] private bool autoActivateWhenReceivingLight = true; // Se ativa automaticamente ao receber luz
    [SerializeField] private float lightCheckInterval = 0.1f; // Intervalo para verificar luz
    
    private Vector3 lastPosition;
    private bool wasReceivingLight = false;
    private float lastLightCheck = 0f;
    
    private void Start()
    {
        // Configurar flor se não estiver configurada
        if (flower == null)
        {
            flower = GetComponentInChildren<Flower>();
        }
        
        if (flower == null)
        {
            Debug.LogWarning($"MovableFlower {gameObject.name}: Flor não encontrada!");
            return;
        }
        
        // Configurar alvo inicial
        UpdateFlowerTarget();
        
        lastPosition = transform.position;
        
        Debug.Log($"[MOVABLE FLOWER] {gameObject.name} inicializada com {possibleTargets.Length} alvos possíveis");
    }
    
    private void Update()
    {
        // Verificar se está recebendo luz (ativação automática)
        if (autoActivateWhenReceivingLight && Time.time > lastLightCheck + lightCheckInterval)
        {
            CheckForIncomingLight();
            lastLightCheck = Time.time;
        }
        
        // Verificar se a caixa se moveu
        if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            OnBoxMoved();
            lastPosition = transform.position;
        }
        
        // Controle manual de mudança de alvo (só se a flor estiver ativa)
        if (useManualControl && Input.GetKeyDown(nextTargetKey) && flower != null && flower.IsActivated)
        {
            CycleToNextTarget();
        }
        
        // Detecção automática de alvos próximos
        if (useAutoDetection)
        {
            DetectNearbyTargets();
        }
    }
    
    private void CheckForIncomingLight()
    {
        bool isReceivingLight = false;
        
        // Verificar se alguma flor está emitindo luz para esta caixa/flor
        Flower[] allFlowers = FindObjectsOfType<Flower>();
        
        foreach (Flower otherFlower in allFlowers)
        {
            if (otherFlower != flower && otherFlower.IsActivated)
            {
                // Verificar se esta flor/caixa é o alvo da outra flor
                if (otherFlower.CurrentTarget == transform || 
                    (flower != null && otherFlower.CurrentTarget == flower.transform))
                {
                    isReceivingLight = true;
                    break;
                }
            }
        }
        
        // Se começou a receber luz, ativar a flor
        if (isReceivingLight && !wasReceivingLight)
        {
            ActivateFlower();
        }
        // Se parou de receber luz, desativar a flor
        else if (!isReceivingLight && wasReceivingLight)
        {
            DeactivateFlower();
        }
        
        wasReceivingLight = isReceivingLight;
    }
    
    private void ActivateFlower()
    {
        if (flower != null && !flower.IsActivated)
        {
            flower.ReceiveLight();
            Debug.Log($"[MOVABLE FLOWER] Flor da caixa {gameObject.name} ativada automaticamente!");
        }
    }
    
    private void DeactivateFlower()
    {
        if (flower != null && flower.IsActivated)
        {
            flower.DeactivateFlower();
            Debug.Log($"[MOVABLE FLOWER] Flor da caixa {gameObject.name} desativada (perdeu luz de entrada)");
        }
    }
    
    private void OnBoxMoved()
    {
        Debug.Log($"[MOVABLE FLOWER] Caixa {gameObject.name} se moveu para {transform.position}");
        
        // Se usar detecção automática, verificar alvos próximos
        if (useAutoDetection)
        {
            CheckNearbyTargetsAndSwitch();
        }
        
        // Atualizar alvo da flor (mantém a mesma direção)
        UpdateFlowerTarget();
    }
    
    private void CycleToNextTarget()
    {
        if (possibleTargets.Length == 0) return;
        
        currentTargetIndex = (currentTargetIndex + 1) % possibleTargets.Length;
        UpdateFlowerTarget();
        
        Debug.Log($"[MOVABLE FLOWER] Mudou para alvo {currentTargetIndex}: {GetCurrentTargetName()}");
    }
    
    private void UpdateFlowerTarget()
    {
        if (flower == null || possibleTargets.Length == 0) return;
        
        Transform currentTarget = possibleTargets[currentTargetIndex];
        if (currentTarget != null)
        {
            flower.ChangeTarget(currentTarget);
            
            // Se a flor estiver ativa, força a atualização da luz
            if (flower.IsActivated)
            {
                // A flor vai automaticamente atualizar sua luz para o novo alvo
                Debug.Log($"[MOVABLE FLOWER] Luz redirecionada para {currentTarget.name}");
            }
            
            // Verificar se o alvo atual é uma porta
            CheckIfTargetIsDoor(currentTarget);
        }
    }
    
    private void CheckNearbyTargetsAndSwitch()
    {
        if (possibleTargets.Length == 0) 
        {
            Debug.LogWarning($"[MOVABLE FLOWER] {gameObject.name}: Nenhum alvo configurado!");
            return;
        }
        
        // Se priorizar portas, verificar se há uma porta próxima
        if (prioritizeDoors)
        {
            int doorIndex = FindNearestDoor();
            if (doorIndex != -1 && doorIndex != currentTargetIndex)
            {
                Debug.Log($"[MOVABLE FLOWER] 🔄 Mudando de alvo {currentTargetIndex} ({GetCurrentTargetName()}) para porta {doorIndex}");
                currentTargetIndex = doorIndex;
                Debug.Log($"[MOVABLE FLOWER] ✅ Mudança automática para porta próxima: {GetCurrentTargetName()}");
                return;
            }
            else if (doorIndex != -1)
            {
                Debug.Log($"[MOVABLE FLOWER] ✅ Já está apontando para a porta mais próxima: {GetCurrentTargetName()}");
            }
        }
        
        // Buscar novos alvos próximos
        DetectNearbyTargets();
    }
    
    private int FindNearestDoor()
    {
        int nearestDoorIndex = -1;
        float nearestDistance = float.MaxValue;
        
        Debug.Log($"[MOVABLE FLOWER] Procurando portas próximas... Distância máxima: {doorPriorityDistance}");
        Debug.Log($"[MOVABLE FLOWER] Lista de alvos: {string.Join(", ", System.Array.ConvertAll(possibleTargets, t => t?.name ?? "null"))}");
        
        for (int i = 0; i < possibleTargets.Length; i++)
        {
            if (possibleTargets[i] == null) 
            {
                Debug.Log($"[MOVABLE FLOWER] Alvo {i} é null!");
                continue;
            }
            
            // Verificar se é uma porta
            Door door = possibleTargets[i].GetComponent<Door>();
            float distance = Vector3.Distance(transform.position, possibleTargets[i].position);
            
            Debug.Log($"[MOVABLE FLOWER] Alvo {i}: {possibleTargets[i].name} - Distância: {distance:F2} - É porta: {(door != null)}");
            
            if (door != null)
            {
                if (distance <= doorPriorityDistance && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestDoorIndex = i;
                    Debug.Log($"[MOVABLE FLOWER] Nova porta mais próxima: {possibleTargets[i].name} (distância: {distance:F2})");
                }
            }
        }
        
        Debug.Log($"[MOVABLE FLOWER] Porta mais próxima encontrada: {(nearestDoorIndex != -1 ? possibleTargets[nearestDoorIndex].name : "Nenhuma")}");
        return nearestDoorIndex;
    }
    
    private void DetectNearbyTargets()
    {
        // Buscar alvos próximos automaticamente
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);
        
        foreach (Collider obj in nearbyObjects)
        {
            // Verificar se é um alvo válido que não está na lista
            if (IsValidTarget(obj.transform) && !IsTargetInList(obj.transform))
            {
                AddTarget(obj.transform);
            }
        }
    }
    
    private bool IsValidTarget(Transform target)
    {
        // Verificar se o objeto tem componentes que indicam que é um alvo válido
        return target.GetComponent<Door>() != null || 
               target.CompareTag("LightTarget") || 
               target.name.ToLower().Contains("target");
    }
    
    private bool IsTargetInList(Transform target)
    {
        foreach (Transform t in possibleTargets)
        {
            if (t == target) return true;
        }
        return false;
    }
    
    private void AddTarget(Transform newTarget)
    {
        // Expandir array de alvos
        Transform[] newArray = new Transform[possibleTargets.Length + 1];
        for (int i = 0; i < possibleTargets.Length; i++)
        {
            newArray[i] = possibleTargets[i];
        }
        newArray[possibleTargets.Length] = newTarget;
        possibleTargets = newArray;
        
        Debug.Log($"[MOVABLE FLOWER] Novo alvo adicionado: {newTarget.name}");
    }
    
    private void CheckIfTargetIsDoor(Transform target)
    {
        Door door = target.GetComponent<Door>();
        if (door != null)
        {
            Debug.Log($"[MOVABLE FLOWER] Luz apontando para porta {door.name}!");
            // A porta será ativada automaticamente pelo sistema de luz
        }
    }
    
    private string GetCurrentTargetName()
    {
        if (possibleTargets.Length == 0) return "Nenhum";
        Transform target = possibleTargets[currentTargetIndex];
        return target != null ? target.name : "null";
    }
    
    // Métodos públicos para controle externo
    public void SetTargetIndex(int index)
    {
        if (index >= 0 && index < possibleTargets.Length)
        {
            currentTargetIndex = index;
            UpdateFlowerTarget();
        }
    }
    
    public void AddPossibleTarget(Transform target)
    {
        if (!IsTargetInList(target))
        {
            AddTarget(target);
        }
    }
    
    public Transform GetCurrentTarget()
    {
        if (possibleTargets.Length == 0) return null;
        return possibleTargets[currentTargetIndex];
    }
    
    // Visualização no editor
    private void OnDrawGizmosSelected()
    {
        // Desenhar range de detecção geral
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Desenhar range de prioridade para portas
        if (prioritizeDoors)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, doorPriorityDistance);
        }
        
        // Desenhar linhas para alvos possíveis
        if (possibleTargets != null)
        {
            for (int i = 0; i < possibleTargets.Length; i++)
            {
                if (possibleTargets[i] != null)
                {
                    // Cor especial para portas
                    Door door = possibleTargets[i].GetComponent<Door>();
                    if (door != null)
                    {
                        Gizmos.color = (i == currentTargetIndex) ? Color.cyan : Color.blue;
                    }
                    else
                    {
                        Gizmos.color = (i == currentTargetIndex) ? Color.green : Color.gray;
                    }
                    
                    Gizmos.DrawLine(transform.position, possibleTargets[i].position);
                    
                    // Desenhar esfera no alvo se for o atual
                    if (i == currentTargetIndex)
                    {
                        Gizmos.DrawWireSphere(possibleTargets[i].position, 0.5f);
                    }
                }
            }
        }
    }
}
