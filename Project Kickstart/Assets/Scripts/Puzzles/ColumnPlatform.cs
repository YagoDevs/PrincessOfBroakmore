using UnityEngine;

public class ColumnPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    [SerializeField] private ColumnController associatedColumn;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private DimensionType requiredDimension = DimensionType.DimensionB;
    [SerializeField] private bool hideInWrongDimension = true; // Esconde a plataforma quando não está na dimensão requerida
    
    [Header("Visual Feedback")]
    [SerializeField] private float activationDepth = 0.1f; // Quanto a plataforma afunda
    [SerializeField] private float activationSpeed = 5f;
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private GameObject activatedModel; // Modelo quando ativada
    [SerializeField] private GameObject deactivatedModel; // Modelo quando desativada
    
    [Header("Cooldown")]
    [SerializeField] private float activationCooldown = 1f; // Tempo entre ativações
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Estado
    private bool isPlayerOnPlatform = false;
    private bool isActivated = false;
    private bool isOnCooldown = false;
    private bool hasPlayerLeftSinceLastActivation = true; // Exige sair e entrar novamente para reativar
    private Vector3 originalPosition;
    private Vector3 activatedPosition;
    
    private void Start()
    {
        // Salvar posição original
        originalPosition = transform.localPosition;
        activatedPosition = originalPosition - Vector3.up * activationDepth;
        
        // Validar configuração
        ValidateSetup();
        
        // Configurar material inicial
        UpdateMaterial();
        UpdateVisibilityByDimension();
        
        if (showDebugInfo)
        {
            Debug.Log($"ColumnPlatform initialized for Column {(associatedColumn ? associatedColumn.ColumnID.ToString() : "NULL")}");
        }
    }
    
    private void ValidateSetup()
    {
        if (associatedColumn == null)
        {
            // Tentar resolver automaticamente
            associatedColumn = GetComponentInParent<ColumnController>();
            if (associatedColumn == null)
            {
                Debug.LogError($"ColumnPlatform on {gameObject.name}: No associated column assigned!");
            }
            else if (showDebugInfo)
            {
                Debug.Log($"ColumnPlatform on {gameObject.name}: Auto-bound to Column {associatedColumn.ColumnID}");
            }
        }
        
        if (platformRenderer == null)
        {
            platformRenderer = GetComponent<Renderer>();
            if (platformRenderer == null)
            {
                Debug.LogWarning($"ColumnPlatform on {gameObject.name}: No renderer found for material changes");
            }
        }
    }
    
    private void Update()
    {
        // Verificar se pode ser ativada
        bool canActivate = CanActivate();
        if (showDebugInfo)
        {
            var dim = DimensionManager.Instance ? DimensionManager.Instance.CurrentDimension.ToString() : "NoDM";
            Debug.Log($"[ColumnPlatform] canActivate={canActivate}, isActivated={isActivated}, isOnCooldown={isOnCooldown}, playerOn={isPlayerOnPlatform}, leftSinceLast={hasPlayerLeftSinceLastActivation}, dim={dim}, required={requiredDimension}");
        }
        
        // Atualizar visual baseado no estado
        UpdateVisuals(canActivate);
        
        // Processar ativação (qualquer ordem, sem dependência)
        if (isPlayerOnPlatform && canActivate && !isActivated && !isOnCooldown && hasPlayerLeftSinceLastActivation)
        {
            ActivatePlatform();
        }
    }
    
    /// <summary>
    /// Verifica se a plataforma pode ser ativada
    /// </summary>
    private bool CanActivate()
    {
        // Verificar dimensão
        if (DimensionManager.Instance != null)
        {
            DimensionType currentDimension = DimensionManager.Instance.CurrentDimension;
            if (currentDimension != requiredDimension)
            {
                return false;
            }
        }
        
        // Verificar se a coluna não está rotacionando
        if (associatedColumn != null && associatedColumn.IsRotating)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Ativa a plataforma e rotaciona a coluna
    /// </summary>
    private void ActivatePlatform()
    {
        if (isOnCooldown || associatedColumn == null) return;
        
        isActivated = true;
        isOnCooldown = true;
        hasPlayerLeftSinceLastActivation = false; // bloquear novas ativações até sair
        
        if (showDebugInfo)
        {
            Debug.Log($"ColumnPlatform activated for Column {associatedColumn.ColumnID}");
        }
        
        // Rotacionar a coluna associada
        associatedColumn.RotateColumn();
        
        // Feedback visual/sonoro
        PlayActivationFeedback();
        
        // Iniciar cooldown
        StartCoroutine(CooldownCoroutine());
    }
    
    /// <summary>
    /// Reproduz feedback de ativação
    /// </summary>
    private void PlayActivationFeedback()
    {
        // Som de ativação da plataforma
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlatformMovementSound(transform.position, true);
        }
        
        // Shake da câmera (pequeno)
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeAllActiveCameras(0.2f, 0.05f);
        }
    }
    
    /// <summary>
    /// Atualiza visuais da plataforma
    /// </summary>
    private void UpdateVisuals(bool canActivate)
    {
        // Atualizar posição baseado no estado
        Vector3 targetPosition = isActivated ? activatedPosition : originalPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, activationSpeed * Time.deltaTime);
        
        // Atualizar material
        UpdateMaterial(canActivate);

        // Atualizar modelos ativo/inativo (se configurados)
        if (activatedModel != null && deactivatedModel != null)
        {
            if (activatedModel.activeSelf != isActivated)
            {
                activatedModel.SetActive(isActivated);
            }
            if (deactivatedModel.activeSelf == isActivated)
            {
                deactivatedModel.SetActive(!isActivated);
            }
        }

        // Garantir visibilidade correta por dimensão
        UpdateVisibilityByDimension();
    }

    /// <summary>
    /// Esconde/mostra a plataforma baseada na dimensão requerida
    /// </summary>
    private void UpdateVisibilityByDimension()
    {
        if (!hideInWrongDimension || DimensionManager.Instance == null) return;
        bool inCorrectDimension = DimensionManager.Instance.CurrentDimension == requiredDimension;

        // Controlar visual (renderer)
        if (platformRenderer != null)
        {
            platformRenderer.enabled = inCorrectDimension;
        }

        // Controlar modelos (se configurados)
        if (activatedModel != null)
        {
            activatedModel.SetActive(inCorrectDimension && isActivated);
        }
        if (deactivatedModel != null)
        {
            deactivatedModel.SetActive(inCorrectDimension && !isActivated);
        }

        // Controlar interação (collider)
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = inCorrectDimension;
        }
    }
    
    /// <summary>
    /// Atualiza material da plataforma
    /// </summary>
    private void UpdateMaterial(bool canActivate = true)
    {
        if (platformRenderer == null) return;
        
        Material targetMaterial = null;
        
        if (!canActivate)
        {
            targetMaterial = inactiveMaterial; // Dimensão errada ou coluna rotacionando
        }
        else if (isPlayerOnPlatform)
        {
            targetMaterial = activeMaterial; // Player em cima e pode ativar
        }
        else
        {
            targetMaterial = inactiveMaterial; // Estado normal
        }
        
        if (targetMaterial != null && platformRenderer.material != targetMaterial)
        {
            platformRenderer.material = targetMaterial;
        }
    }
    
    /// <summary>
    /// Corrotina de cooldown
    /// </summary>
    private System.Collections.IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(activationCooldown);
        
        isOnCooldown = false;
        isActivated = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"ColumnPlatform cooldown finished for Column {(associatedColumn ? associatedColumn.ColumnID.ToString() : "NULL")}");
        }
    }
    
    /// <summary>
    /// Detecta entrada do player
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerOnPlatform = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"Player entered ColumnPlatform for Column {(associatedColumn ? associatedColumn.ColumnID.ToString() : "NULL")}");
            }
        }
    }
    
    /// <summary>
    /// Detecta saída do player
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerOnPlatform = false;
            hasPlayerLeftSinceLastActivation = true; // liberar próxima ativação ao reentrar
            
            if (showDebugInfo)
            {
                Debug.Log($"Player exited ColumnPlatform for Column {(associatedColumn ? associatedColumn.ColumnID.ToString() : "NULL")}");
            }
        }
    }
    
    /// <summary>
    /// Força reset da plataforma (para debug ou casos especiais)
    /// </summary>
    [ContextMenu("Reset Platform")]
    public void ResetPlatform()
    {
        isActivated = false;
        isOnCooldown = false;
        transform.localPosition = originalPosition;
        UpdateMaterial();
        
        if (showDebugInfo)
        {
            Debug.Log($"ColumnPlatform reset for Column {(associatedColumn ? associatedColumn.ColumnID.ToString() : "NULL")}");
        }
    }
    
    /// <summary>
    /// Informações de debug no Gizmos
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Desenhar conexão com a coluna associada
        if (associatedColumn != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, associatedColumn.transform.position);
            
            // Desenhar ID da coluna
            Gizmos.color = Color.white;
            Vector3 labelPos = transform.position + Vector3.up * 2f;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"Platform for Column {associatedColumn.ColumnID}");
#endif
        }
        
        // Desenhar área de ativação
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        
        // Desenhar movimento da plataforma
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(activatedPosition, Vector3.one * 0.1f);
        }
    }
}
