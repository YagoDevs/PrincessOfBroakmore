using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ColumnPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [SerializeField] private ColumnController[] columns = new ColumnController[4]; // Índices 0-3 para colunas 1-4
    [SerializeField] private DimensionType requiredDimension = DimensionType.DimensionB;
    
    [Header("Reward Settings")]
    [SerializeField] private bool useExistingBoxInScene = true; // Usar uma box já existente na cena
    [SerializeField] private GameObject existingBox; // Referência à box já posicionada na cena
    [SerializeField] private GameObject boxPrefab; // Alternativa: prefab de box para spawnar
    [SerializeField] private Transform boxSpawnPoint; // Onde a box aparece (se usar prefab)
    [SerializeField] private bool destroyBoxOnReset = true; // Apenas para prefab spawnado
    
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem[] completionEffects; // Efeitos quando puzzle é resolvido
    [SerializeField] private AudioClip puzzleCompletionSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool autoCheckConnections = true;
    [SerializeField] private float connectionCheckInterval = 0.5f;
    
    // Estado
    private bool isPuzzleSolved = false;
    private GameObject spawnedBox;
    private float lastConnectionCheck = 0f;
    private DimensionObject boxDimensionObject; // Referência ao DimensionObject da box
    
    // Eventos
    public System.Action OnPuzzleSolved;
    public System.Action OnPuzzleReset;
    
    private void Start()
    {
        // Validar configuração
        ValidateSetup();
        
        // Inicializar referências das colunas
        InitializeColumns();
        
        // Garantir que a box está DESATIVADA inicialmente
        if (useExistingBoxInScene && existingBox != null)
        {
            // Verificar se a box tem um DimensionObject
            boxDimensionObject = existingBox.GetComponent<DimensionObject>();
            
            // Se tem DimensionObject, desabilitar para termos controle total
            if (boxDimensionObject != null)
            {
                boxDimensionObject.enabled = false;
                if (showDebugInfo)
                {
                    Debug.Log("Box DimensionObject disabled - puzzle manager takes control");
                }
            }
            
            existingBox.SetActive(false);
            if (showDebugInfo)
            {
                Debug.Log("Box set to INACTIVE initially (overriding dimension control)");
            }
        }
        
        // Inscrever-se para mudanças de dimensão
        if (DimensionManager.Instance != null)
        {
            DimensionManager.OnDimensionChanged += OnDimensionChanged;
        }
        
        // DEPOIS verificar estado inicial (pode ativar a box se condições já estiverem atendidas)
        CheckPuzzleState();
        
        if (showDebugInfo)
        {
            Debug.Log("ColumnPuzzleManager initialized");
            LogPuzzleConfiguration();
        }
    }
    
    private void Update()
    {
        // Verificar conexões automaticamente
        if (autoCheckConnections && Time.time - lastConnectionCheck >= connectionCheckInterval)
        {
            CheckPuzzleState();
            lastConnectionCheck = Time.time;
        }
    }
    
    /// <summary>
    /// Chamado quando a dimensão muda
    /// </summary>
    /// <param name="newDimension">Nova dimensão ativa</param>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Dimension changed to {newDimension}, checking puzzle state...");
        }
        
        // Forçar verificação do estado do puzzle quando a dimensão muda
        CheckPuzzleState();
    }
    
    /// <summary>
    /// Valida a configuração do puzzle
    /// </summary>
    private void ValidateSetup()
    {
        if (columns == null || columns.Length != 4)
        {
            Debug.LogError("ColumnPuzzleManager: Must have exactly 4 columns assigned!");
            return;
        }
        
        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i] == null)
            {
                Debug.LogError($"ColumnPuzzleManager: Column {i + 1} is not assigned!");
            }
        }
        
        // Avisos de configuração da recompensa
        if (useExistingBoxInScene)
        {
            if (existingBox == null)
            {
                Debug.LogWarning("ColumnPuzzleManager: Using existing box in scene, but 'existingBox' is not assigned.");
            }
        }
        else
        {
            if (boxPrefab == null)
            {
                Debug.LogError("ColumnPuzzleManager: Box prefab not assigned!");
            }
            
            if (boxSpawnPoint == null)
            {
                Debug.LogWarning("ColumnPuzzleManager: Box spawn point not assigned, will use manager position");
                boxSpawnPoint = transform;
            }
        }
    }
    
    /// <summary>
    /// Inicializa as referências das colunas
    /// </summary>
    private void InitializeColumns()
    {
        // Se colunas não foram atribuídas manualmente, tentar encontrar automaticamente
        if (columns.Any(c => c == null))
        {
            ColumnController[] foundColumns = FindObjectsOfType<ColumnController>();
            
            foreach (ColumnController column in foundColumns)
            {
                int index = column.ColumnID - 1; // Converter ID 1-4 para índice 0-3
                if (index >= 0 && index < 4)
                {
                    columns[index] = column;
                }
            }
        }
        
        if (showDebugInfo)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i] != null)
                {
                    Debug.Log($"Column {i + 1} assigned: {columns[i].name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Obtém uma coluna específica pelo ID
    /// </summary>
    /// <param name="columnID">ID da coluna (1-4)</param>
    /// <returns>ColumnController ou null se não encontrado</returns>
    public ColumnController GetColumn(int columnID)
    {
        int index = columnID - 1; // Converter ID 1-4 para índice 0-3
        if (index >= 0 && index < columns.Length)
        {
            return columns[index];
        }
        return null;
    }
    
    /// <summary>
    /// Chamado quando as conexões de uma coluna mudaram
    /// </summary>
    public void OnColumnConnectionsChanged()
    {
        // Verificar se precisa atualizar o estado do puzzle
        CheckPuzzleState();
    }
    
    /// <summary>
    /// Verifica o estado atual do puzzle
    /// </summary>
    public void CheckPuzzleState()
    {
        // Verificar se está na dimensão correta
        if (DimensionManager.Instance != null)
        {
            DimensionType currentDimension = DimensionManager.Instance.CurrentDimension;
            if (currentDimension != requiredDimension)
            {
                if (isPuzzleSolved)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"Wrong dimension ({currentDimension}), hiding box and resetting puzzle");
                    }
                    ResetPuzzle();
                }
                
                // Forçar box a ficar inativa se não estamos na dimensão correta
                if (useExistingBoxInScene && existingBox != null && existingBox.activeSelf)
                {
                    existingBox.SetActive(false);
                    if (showDebugInfo)
                    {
                        Debug.Log("Box forced INACTIVE - wrong dimension");
                    }
                }
                
                return;
            }
        }
        
        // Definir rotações corretas para cada coluna (Z-axis)
        Dictionary<int, float> correctRotations = new Dictionary<int, float>
        {
            { 1, 0f },    // Coluna 1: Z = 0°
            { 2, 90f },   // Coluna 2: Z = 90°
            { 3, 90f },   // Coluna 3: Z = 90°
            { 4, 180f }   // Coluna 4: Z = 180°
        };
        
        bool allConditionsMet = true;
        
        // Verificar se todas as colunas estão na rotação correta E com tochas acesas
        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i] == null)
            {
                allConditionsMet = false;
                if (showDebugInfo)
                {
                    Debug.Log($"Column {i + 1} is null!");
                }
                continue;
            }
            
            int columnID = columns[i].ColumnID;
            if (!correctRotations.ContainsKey(columnID))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Column {columnID} not found in solution!");
                }
                allConditionsMet = false;
                continue;
            }
            
            float requiredRotation = correctRotations[columnID];
            float currentRotation = columns[i].CurrentRotation;
            
            // Normalizar ângulos para comparação (0-360°)
            float normalizedCurrent = ((currentRotation % 360f) + 360f) % 360f;
            float normalizedRequired = ((requiredRotation % 360f) + 360f) % 360f;
            
            // Verificar se está na rotação correta (tolerância de 5°)
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(normalizedCurrent, normalizedRequired));
            
            if (angleDifference > 5f)
            {
                allConditionsMet = false;
                if (showDebugInfo)
                {
                    Debug.Log($"Column {columnID}: Current={normalizedCurrent:F1}°, Required={normalizedRequired}°, Difference={angleDifference:F1}°");
                }
                continue;
            }
            
            // Verificar se pelo menos uma tocha está acesa
            if (!columns[i].HasAnyTorchLit())
            {
                allConditionsMet = false;
                if (showDebugInfo)
                {
                    Debug.Log($"Column {columnID}: Correct rotation ({normalizedCurrent:F1}°) but no torch lit!");
                }
                continue;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"✅ Column {columnID}: Rotation={normalizedCurrent:F1}° (required {normalizedRequired}°) + torch lit!");
            }
        }
        
        // Atualizar estado do puzzle
        if (allConditionsMet && !isPuzzleSolved)
        {
            if (showDebugInfo)
            {
                Debug.Log("🎉 ALL CONDITIONS MET! Solving puzzle and activating box...");
            }
            SolvePuzzle();
        }
        else if (!allConditionsMet && isPuzzleSolved)
        {
            if (showDebugInfo)
            {
                Debug.Log("❌ Conditions no longer met. Resetting puzzle and hiding box...");
            }
            ResetPuzzle();
        }
        else if (showDebugInfo)
        {
            if (allConditionsMet && isPuzzleSolved)
            {
                Debug.Log("✅ Puzzle already solved and conditions still met");
            }
            else if (!allConditionsMet && !isPuzzleSolved)
            {
                Debug.Log("⏳ Puzzle not solved, conditions not met (normal state)");
            }
        }
    }
    
    /// <summary>
    /// Resolve o puzzle
    /// </summary>
    private void SolvePuzzle()
    {
        isPuzzleSolved = true;
        
        if (showDebugInfo)
        {
            Debug.Log("🎉 Column Puzzle SOLVED!");
        }
        
        // Disponibilizar a box
        EnableRewardBox();
        
        // Efeitos visuais e sonoros
        PlayCompletionEffects();
        
        // Disparar evento
        OnPuzzleSolved?.Invoke();
    }
    
    /// <summary>
    /// Reseta o puzzle
    /// </summary>
    private void ResetPuzzle()
    {
        isPuzzleSolved = false;
        
        if (showDebugInfo)
        {
            Debug.Log("Column Puzzle reset");
        }
        
        // Recolher/Desativar box conforme o modo
        DisableRewardBox();
        
        // Disparar evento
        OnPuzzleReset?.Invoke();
    }
    
    /// <summary>
    /// Ativa a box de recompensa (usar existente ou spawnar)
    /// </summary>
    private void EnableRewardBox()
    {
        if (useExistingBoxInScene)
        {
            if (existingBox != null)
            {
                existingBox.SetActive(true);
                Debug.Log($"📦 PUZZLE SOLVED! Reward box (existing) enabled: {existingBox.name}");
            }
            else
            {
                Debug.LogWarning("Reward box set to use existing, but 'existingBox' is not assigned.");
            }
        }
        else
        {
            if (boxPrefab == null || boxSpawnPoint == null) return;

            if (spawnedBox != null)
            {
                Destroy(spawnedBox);
            }

            spawnedBox = Instantiate(boxPrefab, boxSpawnPoint.position, boxSpawnPoint.rotation);
            Debug.Log($"📦 PUZZLE SOLVED! Reward box spawned at {boxSpawnPoint.position}");
        }
    }

    /// <summary>
    /// Desativa/Remove a box de recompensa conforme o modo configurado
    /// </summary>
    private void DisableRewardBox()
    {
        if (useExistingBoxInScene)
        {
            if (existingBox != null)
            {
                existingBox.SetActive(false);
                Debug.Log($"📦 PUZZLE RESET! Reward box (existing) disabled: {existingBox.name}");
            }
        }
        else
        {
            if (spawnedBox != null && destroyBoxOnReset)
            {
                Destroy(spawnedBox);
                spawnedBox = null;
                Debug.Log("📦 PUZZLE RESET! Spawned reward box destroyed");
            }
        }
    }
    
    /// <summary>
    /// Reproduz efeitos de conclusão
    /// </summary>
    private void PlayCompletionEffects()
    {
        // Efeitos de partículas
        if (completionEffects != null)
        {
            foreach (ParticleSystem effect in completionEffects)
            {
                if (effect != null)
                {
                    effect.Play();
                }
            }
        }
        
        // Som de conclusão
        if (puzzleCompletionSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySoundAtPosition(
                puzzleCompletionSound,
                transform.position,
                1f,
                1f
            );
        }
        
        // Shake da câmera
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeAllActiveCameras(0.8f, 0.2f);
        }
        
        // Tocar som especial de vitória para todas as tochas
        foreach (ColumnController column in columns)
        {
            if (column != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFlowerEmissionToFlowerSound(
                    column.transform.position,
                    $"Column{column.ColumnID}_Victory"
                );
            }
        }
    }
    
    /// <summary>
    /// Força verificação manual do puzzle (para debug)
    /// </summary>
    [ContextMenu("Force Check Puzzle State")]
    public void ForceCheckPuzzleState()
    {
        CheckPuzzleState();
    }
    
    /// <summary>
    /// Força reset do puzzle (para debug)
    /// </summary>
    [ContextMenu("Force Reset Puzzle")]
    public void ForceResetPuzzle()
    {
        ResetPuzzle();
    }
    
    /// <summary>
    /// Força resolução do puzzle (para debug)
    /// </summary>
    [ContextMenu("Force Solve Puzzle")]
    public void ForceSolvePuzzle()
    {
        SolvePuzzle();
    }
    
    /// <summary>
    /// Log da configuração do puzzle
    /// </summary>
    private void LogPuzzleConfiguration()
    {
        Debug.Log("=== Column Puzzle Configuration ===");
        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i] != null)
            {
                Debug.Log($"Column {i + 1}: {columns[i].name} (ID: {columns[i].ColumnID})");
            }
            else
            {
                Debug.Log($"Column {i + 1}: NOT ASSIGNED");
            }
        }
        Debug.Log($"Required Dimension: {requiredDimension}");
        Debug.Log($"Use Existing Box: {useExistingBoxInScene}");
        Debug.Log($"Existing Box: {(existingBox ? existingBox.name : "NOT ASSIGNED")}\n");
        Debug.Log($"Box Prefab: {(boxPrefab ? boxPrefab.name : "NOT ASSIGNED")}\n");
        Debug.Log($"Spawn Point: {(boxSpawnPoint ? boxSpawnPoint.name : "NOT ASSIGNED")}\n");
        Debug.Log("=====================================");
    }
    
    /// <summary>
    /// Obtém status detalhado do puzzle para UI/debug
    /// </summary>
    public string GetPuzzleStatus()
    {
        string status = $"Puzzle Solved: {isPuzzleSolved}\n";
        status += $"Current Dimension: {(DimensionManager.Instance ? DimensionManager.Instance.CurrentDimension.ToString() : "Unknown")}\n";
        status += $"Required Dimension: {requiredDimension}\n\n";
        
        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i] != null)
            {
                status += $"Column {i + 1}: {columns[i].GetActiveConnectionCount()}/2 connections\n";
            }
            else
            {
                status += $"Column {i + 1}: NOT ASSIGNED\n";
            }
        }
        
        return status;
    }
    
    private void OnDestroy()
    {
        // Desinscrever-se dos eventos
        if (DimensionManager.Instance != null)
        {
            DimensionManager.OnDimensionChanged -= OnDimensionChanged;
        }
        
        // Reabilitar DimensionObject da box se existir
        if (boxDimensionObject != null)
        {
            boxDimensionObject.enabled = true;
        }
    }
    
    /// <summary>
    /// Gizmos para debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Desenhar conexões entre colunas
        if (columns != null)
        {
            // Desenhar layout em retângulo
            Gizmos.color = Color.cyan;
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i] != null)
                {
                    // Desenhar ID da coluna
                    Vector3 labelPos = columns[i].transform.position + Vector3.up * 3f;
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(labelPos, $"Column {i + 1}");
#endif
                }
            }
        }
        
        // Desenhar ponto de spawn da box
        if (boxSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxSpawnPoint.position, Vector3.one);
            Gizmos.DrawWireSphere(boxSpawnPoint.position + Vector3.up, 0.5f);
        }
        
        // Desenhar status do puzzle
        if (Application.isPlaying)
        {
            Gizmos.color = isPuzzleSolved ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}

