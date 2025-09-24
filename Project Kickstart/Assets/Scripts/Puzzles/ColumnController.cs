using UnityEngine;
using System.Collections.Generic;

public class ColumnController : MonoBehaviour
{
    [Header("Column Settings")]
    [SerializeField] private int columnID; // 1, 2, 3, or 4
    [SerializeField] private Transform columnMesh; // O mesh da coluna que vai rotacionar
    [SerializeField] private Transform[] torches; // Array das 2 tochas
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationStep = 90f; // Graus por rotação (corrigido para 90°)
    [SerializeField] private float rotationDuration = 0.4f; // Duração de cada passo de rotação em segundos
    public enum RotationAxis { X, Y, Z }
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z; // Eixo de rotação (padrão Z conforme solicitado)
    
    [Header("Connection Settings")]
    [SerializeField] private int[] targetColumns; // Colunas que esta deve se conectar
    [SerializeField] private float connectionRange = 10f; // Distância máxima para conexão
    [SerializeField] private float connectionAngleTolerance = 5f; // Tolerância do ângulo
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showGizmos = true;
    
    // Estado atual
    private int currentStepIndex = 0; // Índice do passo atual (0, 1, 2, 3 para 0°, 90°, 180°, 270°)
    private float currentRotation = 0f;
    private bool isRotating = false;
    private bool[] isConnectedTo; // Array indicando conexões ativas
    private ColumnPuzzleManager puzzleManager;
    private List<TorchLight> torchLightComponents = new List<TorchLight>();
    
    // Propriedades públicas
    public int ColumnID => columnID;
    public bool IsRotating => isRotating;
    public float CurrentRotation => currentRotation;
    
    private void Start()
    {
        // Inicializar array de conexões
        isConnectedTo = new bool[5]; // Índice 0 não usado, 1-4 para colunas
        
        // Encontrar o puzzle manager
        puzzleManager = FindObjectOfType<ColumnPuzzleManager>();
        
        // Validar configuração
        ValidateSetup();
        
        // Se columnMesh não estiver atribuído, usar o próprio transform da coluna
        if (columnMesh == null)
        {
            columnMesh = transform;
            if (showDebugInfo)
            {
                Debug.Log($"Column {columnID}: columnMesh not set. Using self transform.");
            }
        }

        // Forçar posição inicial para 0° e atualizar mesh
        currentStepIndex = 0;
        currentRotation = 0f;
        
        // Força o mesh para rotação inicial preservando X=-90°
        if (columnMesh != null)
        {
            // Forçar rotação específica: X=-90, Y=0, Z=0
            Vector3 targetEuler = new Vector3(-90f, 0f, 0f);
            columnMesh.localEulerAngles = targetEuler;
            
            if (showDebugInfo)
            {
                Debug.Log($"Column {columnID}: Set initial rotation to X=-90° Y=0° Z=0° (fixed orientation)");
            }
        }

        // Garantir configuração automática das tochas (Light + TorchLight)
        EnsureTorchSetup();
        
        // Snap das tochas para ângulos exatos
        SnapTorchesToExactAngles();

        // Verificar conexões iniciais
        CheckConnections();
        
        if (showDebugInfo)
        {
            Debug.Log($"Column {columnID} initialized at rotation {currentRotation}°");
        }
    }
    
    private void ValidateSetup()
    {
        if (columnMesh == null)
        {
            Debug.LogError($"Column {columnID}: columnMesh not assigned!");
        }
        
        if (torches == null || torches.Length != 2)
        {
            Debug.LogError($"Column {columnID}: Must have exactly 2 torches assigned!");
        }
        
        if (targetColumns == null || targetColumns.Length != 2)
        {
            Debug.LogError($"Column {columnID}: Must have exactly 2 target columns assigned!");
        }
    }
    
    /// <summary>
    /// Garante que cada tocha tenha Light e TorchLight configurados automaticamente
    /// </summary>
    private void EnsureTorchSetup()
    {
        torchLightComponents.Clear();

        if (torches == null) return;

        foreach (Transform torch in torches)
        {
            if (torch == null) continue;

            // Garantir componente Light
            var light = torch.GetComponent<Light>();
            if (light == null)
            {
                light = torch.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 10f;
                light.intensity = 0.5f;
                light.enabled = false; // começa desligada
            }

            // Garantir componente TorchLight
            var torchLight = torch.GetComponent<TorchLight>();
            if (torchLight == null)
            {
                torchLight = torch.gameObject.AddComponent<TorchLight>();
            }

            // Assegurar que TorchLight conhece sua Light
            if (torchLight.GetLight() == null)
            {
                // Força reobter o componente agora atribuído
                // TorchLight pega o Light via GetComponent no Start, mas já garantimos aqui
            }

            torchLightComponents.Add(torchLight);
        }
    }

    /// <summary>
    /// Rotaciona a coluna em 45 graus
    /// </summary>
    public void RotateColumn()
    {
        if (isRotating) return;
        
        StartCoroutine(PerformRotation());
    }
    
    [ContextMenu("Debug Column State")]
    public void DebugColumnState()
    {
        Debug.Log($"=== COLUMN {columnID} STATE ===");
        Debug.Log($"currentStepIndex: {currentStepIndex}");
        Debug.Log($"currentRotation: {currentRotation}°");
        Debug.Log($"Expected rotation for step {currentStepIndex}: {currentStepIndex * 90f}°");
        if (columnMesh != null)
        {
            Vector3 actualRotation = columnMesh.localEulerAngles;
            Debug.Log($"Actual mesh rotation: X={actualRotation.x:F1}° Y={actualRotation.y:F1}° Z={actualRotation.z:F1}°");
        }
        Debug.Log($"Is rotating: {isRotating}");
        
        // Debug das tochas
        if (torches != null)
        {
            for (int i = 0; i < torches.Length; i++)
            {
                if (torches[i] != null)
                {
                    Vector3 torchRot = torches[i].localEulerAngles;
                    float torchWorldAngle = torchRot.z + currentRotation;
                    torchWorldAngle = Mathf.Repeat(torchWorldAngle, 360f);
                    Debug.Log($"Torch {i}: local Z={torchRot.z:F1}° | world Z={torchWorldAngle:F1}°");
                }
            }
        }
        
        // Debug das conexões
        if (targetColumns != null)
        {
            for (int i = 0; i < targetColumns.Length; i++)
            {
                int targetID = targetColumns[i];
                bool connected = (targetID >= 0 && targetID < isConnectedTo.Length) ? isConnectedTo[targetID] : false;
                Debug.Log($"Connection to Column {targetID}: {connected}");
            }
        }
        
        Debug.Log($"===========================");
    }
    
    [ContextMenu("Force Check All Connections")]
    public void ForceCheckConnections()
    {
        Debug.Log($"=== FORCING CONNECTION CHECK FOR COLUMN {columnID} ===");
        CheckConnections();
    }
    
    /// <summary>
    /// Força as tochas para ângulos exatos (0, 90, 180, 270)
    /// </summary>
    private void SnapTorchesToExactAngles()
    {
        if (torches == null) return;
        
        for (int i = 0; i < torches.Length; i++)
        {
            if (torches[i] != null)
            {
                Vector3 currentEuler = torches[i].localEulerAngles;
                float originalZ = currentEuler.z;
                
                // Encontrar o ângulo mais próximo de 0, 90, 180, 270
                float[] snapAngles = { 0f, 90f, 180f, 270f };
                float closestAngle = snapAngles[0];
                float minDifference = Mathf.Abs(Mathf.DeltaAngle(originalZ, snapAngles[0]));
                
                foreach (float angle in snapAngles)
                {
                    float difference = Mathf.Abs(Mathf.DeltaAngle(originalZ, angle));
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        closestAngle = angle;
                    }
                }
                
                // Aplicar ângulo correto
                currentEuler.z = closestAngle;
                torches[i].localEulerAngles = currentEuler;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Column {columnID}: Torch {i} snapped from {originalZ:F1}° to {closestAngle}°");
                }
            }
        }
    }
    
    private System.Collections.IEnumerator PerformRotation()
    {
        isRotating = true;
        
        // Validar sincronização antes de calcular próximo passo
        float expectedRotation = currentStepIndex * 90f;
        if (Mathf.Abs(currentRotation - expectedRotation) > 5f)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"Column {columnID}: Sync issue! currentStepIndex={currentStepIndex} expects {expectedRotation}° but currentRotation={currentRotation}°. Fixing...");
            }
            currentRotation = expectedRotation;
            ApplyRotationToMesh(currentRotation);
        }
        
        // Calcular próximo passo sequencial (0 -> 1 -> 2 -> 3 -> 0)
        int nextStepIndex = (currentStepIndex + 1) % 4;
        float targetRotation = nextStepIndex * 90f;
        
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, rotationDuration);
        
        if (showDebugInfo)
        {
            Debug.Log($"Column {columnID}: Step {currentStepIndex} -> {nextStepIndex} | Current rotation {currentRotation}° -> Target {targetRotation}°");
            Debug.Log($"Column {columnID}: All possible rotations: Step0=0°, Step1=90°, Step2=180°, Step3=270°");
        }
        
        // Tocar som de rotação da plataforma
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlatformMovementSound(transform.position, false);
        }
        
        // Aplicar rotação diretamente sem interpolação para evitar números quebrados
        currentStepIndex = nextStepIndex;
        currentRotation = targetRotation;
        
        // Debug detalhado ANTES da aplicação
        if (showDebugInfo)
        {
            Debug.Log($"Column {columnID}: BEFORE applying - currentStepIndex={currentStepIndex}, currentRotation={currentRotation}°");
        }
        
        // Aplicar rotação exata (0, 90, 180, 270)
        ApplyRotationToMesh(targetRotation);
        
        // Debug detalhado APÓS a aplicação
        if (showDebugInfo && columnMesh != null)
        {
            Vector3 actualRotation = columnMesh.localEulerAngles;
            Debug.Log($"Column {columnID}: AFTER applying - mesh rotation X={actualRotation.x:F1}° Y={actualRotation.y:F1}° Z={actualRotation.z:F1}°");
        }
        
        // Simular tempo de rotação se necessário
        if (duration > 0.01f)
        {
            yield return new WaitForSeconds(duration * 0.1f); // Tempo reduzido para feedback visual
        }
        
        isRotating = false;
        
        // Verificar novas conexões após rotação
        CheckConnections();
        
        if (showDebugInfo)
        {
            Debug.Log($"Column {columnID}: Rotation completed at step {currentStepIndex} ({currentRotation}°)");
        }
    }
    
    /// <summary>
    /// Aplica rotação ao mesh da coluna
    /// </summary>
    /// <param name="angle">Ângulo específico (opcional, usa currentRotation se não especificado)</param>
    private void ApplyRotationToMesh(float? angle = null)
    {
        if (columnMesh == null) return;
        
        float rotationAngle = angle ?? currentRotation;
        
        // Sempre manter X=-90, Y=0, e apenas alterar Z
        Vector3 targetRotation = new Vector3(-90f, 0f, rotationAngle);
        
        // Aplicar diretamente sem interpolação
        columnMesh.localEulerAngles = targetRotation;
        
        if (showDebugInfo)
        {
            Vector3 actualRotation = columnMesh.localEulerAngles;
            Debug.Log($"Column {columnID}: Applied rotation X={actualRotation.x:F1}° Y={actualRotation.y:F1}° Z={actualRotation.z:F1}° (target Z={rotationAngle}°)");
        }
    }
    
    /// <summary>
    /// Verifica conexões com outras colunas
    /// </summary>
    public void CheckConnections()
    {
        if (puzzleManager == null) return;
        
        // Reset conexões
        for (int i = 0; i < isConnectedTo.Length; i++)
        {
            isConnectedTo[i] = false;
        }
        
        // Verificar cada coluna alvo
        foreach (int targetColumnID in targetColumns)
        {
            ColumnController targetColumn = puzzleManager.GetColumn(targetColumnID);
            if (targetColumn != null)
            {
                bool connected = CheckConnectionToColumn(targetColumn);
                isConnectedTo[targetColumnID] = connected;
                
                if (showDebugInfo && connected)
                {
                    Debug.Log($"Column {columnID} connected to Column {targetColumnID}");
                }
            }
        }
        
        // Atualizar luzes das tochas
        UpdateTorchLights();
        
        // Notificar puzzle manager
        if (puzzleManager != null)
        {
            puzzleManager.OnColumnConnectionsChanged();
        }
    }
    
    /// <summary>
    /// Verifica se está conectado com uma coluna específica
    /// </summary>
    private bool CheckConnectionToColumn(ColumnController targetColumn)
    {
        if (targetColumn == null) return false;
        
        // Calcular direção para a coluna alvo
        Vector3 directionToTarget = (targetColumn.transform.position - transform.position).normalized;
        
        // Calcular ângulo da direção no plano XY (eixo Z)
        float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.y) * Mathf.Rad2Deg;
        if (targetAngle < 0) targetAngle += 360;
        
        if (showDebugInfo)
        {
            Debug.Log($"Column {columnID}: Target angle to Column {targetColumn.ColumnID} = {targetAngle:F1}°");
        }
        
        // Verificar se alguma das tochas está apontando para a direção correta
        foreach (Transform torch in torches)
        {
            if (torch == null) continue;
            
            // Ângulo absoluto da tocha (orientação inicial + rotação da coluna)
            float torchLocalAngle = torch.localEulerAngles.z;
            float torchWorldAngle = torchLocalAngle + currentRotation;
            
            // Normalizar ângulo
            torchWorldAngle = Mathf.Repeat(torchWorldAngle, 360f);
            
            if (showDebugInfo)
            {
                Debug.Log($"Column {columnID}: Torch local={torchLocalAngle:F1}° + column={currentRotation:F1}° = world={torchWorldAngle:F1}°");
            }
            
            // Verificar se está dentro da tolerância
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(torchWorldAngle, targetAngle));
            
            if (showDebugInfo)
            {
                Debug.Log($"Column {columnID}: Torch angle difference to Column {targetColumn.ColumnID} = {angleDifference:F1}° (tolerance={connectionAngleTolerance}°)");
            }
            
            if (angleDifference <= connectionAngleTolerance)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Column {columnID}: Torch IS pointing to Column {targetColumn.ColumnID}. Checking if target points back...");
                }
                
                // Verificar se a coluna alvo também está apontando de volta
                bool targetPointsBack = targetColumn.IsPointingTowards(this);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Column {columnID}: Target Column {targetColumn.ColumnID} points back: {targetPointsBack}");
                }
                
                if (targetPointsBack)
                {
                    return true;
                }
            }
            else if (showDebugInfo)
            {
                Debug.Log($"Column {columnID}: Torch NOT pointing to Column {targetColumn.ColumnID} (difference too large)");
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Verifica se esta coluna está apontando para outra coluna
    /// </summary>
    public bool IsPointingTowards(ColumnController targetColumn)
    {
        if (targetColumn == null) return false;
        
        // Calcular direção para a coluna alvo
        Vector3 directionToTarget = (targetColumn.transform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.y) * Mathf.Rad2Deg;
        if (targetAngle < 0) targetAngle += 360;
        
        // Verificar cada tocha
        foreach (Transform torch in torches)
        {
            if (torch == null) continue;
            
            float torchLocalAngle = torch.localEulerAngles.z;
            float torchWorldAngle = Mathf.Repeat(torchLocalAngle + currentRotation, 360f);
            
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(torchWorldAngle, targetAngle));
            
            if (angleDifference <= connectionAngleTolerance)
            {
                return true;
            }
        }
        
        return false;
    }

    
    /// <summary>
    /// Atualiza as luzes das tochas baseado na rotação atual
    /// </summary>
    private void UpdateTorchLights()
    {
        // Definir rotações onde as tochas devem estar acesas para cada coluna
        Dictionary<int, float[]> torchActivationRotations = new Dictionary<int, float[]>
        {
            { 1, new float[] { 0f } },      // Coluna 1: tocha acesa quando Z = 0°
            { 2, new float[] { 90f } },     // Coluna 2: tocha acesa quando Z = 90°
            { 3, new float[] { 90f } },     // Coluna 3: tocha acesa quando Z = 90°
            { 4, new float[] { 180f } }     // Coluna 4: tocha acesa quando Z = 180°
        };

        if (!torchActivationRotations.ContainsKey(columnID))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"Column {columnID}: No torch activation rules defined!");
            }
            return;
        }

        float[] activationAngles = torchActivationRotations[columnID];
        float normalizedCurrentRotation = ((currentRotation % 360f) + 360f) % 360f;

        // Verificar se a rotação atual corresponde a alguma das rotações de ativação
        bool shouldTorchesBeActive = false;
        foreach (float targetAngle in activationAngles)
        {
            float normalizedTarget = ((targetAngle % 360f) + 360f) % 360f;
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(normalizedCurrentRotation, normalizedTarget));

            if (angleDifference <= 5f) // Tolerância de 5°
            {
                shouldTorchesBeActive = true;
                break;
            }
        }

        // Garantir setup
        if (torchLightComponents == null || torchLightComponents.Count == 0)
        {
            EnsureTorchSetup();
        }

        // Ativar/desativar todas as tochas baseado na rotação
        for (int i = 0; i < torchLightComponents.Count; i++)
        {
            var tl = torchLightComponents[i];
            if (tl == null) continue;
            tl.SetConnectionState(shouldTorchesBeActive);
        }

        if (showDebugInfo)
        {
            Debug.Log($"Column {columnID}: Rotation={normalizedCurrentRotation:F1}°, Torches={shouldTorchesBeActive}");
        }
    }
    
    /// <summary>
    /// Verifica se todas as conexões necessárias estão ativas
    /// </summary>
    public bool AreAllConnectionsActive()
    {
        foreach (int targetColumnID in targetColumns)
        {
            if (!isConnectedTo[targetColumnID])
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Verifica se pelo menos uma tocha está acesa
    /// </summary>
    /// <returns>True se qualquer tocha estiver acesa</returns>
    public bool HasAnyTorchLit()
    {
        if (torches == null) return false;
        
        foreach (Transform torch in torches)
        {
            if (torch == null) continue;
            
            TorchLight torchLight = torch.GetComponent<TorchLight>();
            if (torchLight != null && torchLight.IsLit)
            {
                return true;
            }
            
            // Fallback: verificar se a Light está ativa
            Light light = torch.GetComponent<Light>();
            if (light != null && light.enabled && light.intensity > 0.1f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Obtém o número de conexões ativas
    /// </summary>
    public int GetActiveConnectionCount()
    {
        int count = 0;
        for (int i = 1; i < isConnectedTo.Length; i++)
        {
            if (isConnectedTo[i]) count++;
        }
        return count;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Desenhar conexões alvo
        if (targetColumns != null && puzzleManager != null)
        {
            foreach (int targetColumnID in targetColumns)
            {
                ColumnController targetColumn = puzzleManager.GetColumn(targetColumnID);
                if (targetColumn != null)
                {
                    // Cor baseada no estado da conexão
                    if (Application.isPlaying && isConnectedTo != null && isConnectedTo[targetColumnID])
                    {
                        Gizmos.color = Color.green; // Conectado
                    }
                    else
                    {
                        Gizmos.color = Color.red; // Não conectado
                    }
                    
                    Gizmos.DrawLine(transform.position, targetColumn.transform.position);
                }
            }
        }
        
        // Desenhar direções das tochas
        if (torches != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform torch in torches)
            {
                if (torch != null)
                {
                    Vector3 direction = torch.forward;
                    if (Application.isPlaying)
                    {
                        // Considerar rotação atual da coluna
                        direction = Quaternion.Euler(0, currentRotation, 0) * torch.forward;
                    }
                    Gizmos.DrawRay(torch.position, direction * 5f);
                }
            }
        }
        
        // Desenhar alcance de conexão
        Gizmos.color = new Color(0, 1, 1, 0.1f);
        Gizmos.DrawWireSphere(transform.position, connectionRange);
    }
}
