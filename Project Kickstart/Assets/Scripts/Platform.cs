using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Platform : MonoBehaviour
{
    [Header("Configurações da Plataforma")]
    [SerializeField] private Torch torch; // Referência à tocha que ela controla
    [SerializeField] private Transform newTarget; // Novo alvo para a tocha quando ativada
    
    [Header("Conexão Direta de Flores")]
    [SerializeField] private Flower sourceFlower; // Flor que vai emitir luz
    [SerializeField] private Flower targetFlower; // Flor que vai receber luz
    [SerializeField] private bool useDirectFlowerConnection = false; // Se deve usar conexão direta ao invés da tocha
    
    [Header("Configurações de Interação")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool canBeDeactivated = true; // Se pode ser desativada ao sair
    
    [Header("Efeitos Visuais")]
    [SerializeField] private GameObject activatedEffect; // Efeito quando ativada
    [SerializeField] private Material activatedMaterial; // Material quando ativada
    [SerializeField] private Material deactivatedMaterial; // Material quando desativada
    [SerializeField] private GameObject activatedModel; // Modelo quando ativada
    [SerializeField] private GameObject deactivatedModel; // Modelo quando desativada
    
    [Header("Configurações de Movimento")]
    [SerializeField] private float depthOffset = 0.1f; // Quanto a plataforma desce
    [SerializeField] private float animationSpeed = 5f; // Velocidade da animação
    
    private Renderer platformRenderer;
    private Transform originalTorchTarget; // Armazena o alvo original da tocha
    private Transform originalSourceTarget; // Armazena o alvo original da flor fonte
    private Vector3 originalPosition; // Posição original da plataforma
    private Vector3 targetPosition; // Posição alvo da plataforma
    private bool isMoving = false; // Se está se movendo

    private void Start()
    {
        // Configura o trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"Platform {gameObject.name}: Collider não encontrado! Adicionando BoxCollider...");
            gameObject.AddComponent<BoxCollider>().isTrigger = true;
        }

        // Obtém o renderer para mudanças visuais
        platformRenderer = GetComponent<Renderer>();
        
        // Armazena o alvo original da tocha
        if (torch != null)
        {
            originalTorchTarget = torch.CurrentTarget;
        }
        
        // Armazena o alvo original da flor fonte
        if (sourceFlower != null)
        {
            originalSourceTarget = sourceFlower.CurrentTarget;
        }
        
        // Armazena a posição original
        originalPosition = transform.position;
        targetPosition = originalPosition;
        
        // Configura estado inicial
        UpdateVisualState();
    }

    private void Update()
    {
        // Anima o movimento da plataforma
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, animationSpeed * Time.deltaTime);
            
            // Para o movimento quando está próximo o suficiente
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PLATFORM DEBUG] Algo entrou no trigger: {other.name} com tag: {other.tag}");
        
        if (other.CompareTag(playerTag) && !isActivated)
        {
            Debug.Log($"[PLATFORM DEBUG] Jogador detectado! Ativando plataforma...");
            ActivatePlatform();
        }
        else if (!other.CompareTag(playerTag))
        {
            Debug.Log($"[PLATFORM DEBUG] Objeto {other.name} não tem a tag '{playerTag}'");
        }
        else if (isActivated)
        {
            Debug.Log($"[PLATFORM DEBUG] Plataforma já está ativada");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[PLATFORM DEBUG] Algo saiu do trigger: {other.name}");
        
        if (other.CompareTag(playerTag) && isActivated && canBeDeactivated)
        {
            Debug.Log($"[PLATFORM DEBUG] Jogador saiu! Desativando plataforma...");
            DeactivatePlatform();
        }
    }

    private void ActivatePlatform()
    {
        Debug.Log($"[PLATFORM DEBUG] Tentando ativar plataforma {gameObject.name}");
        
        isActivated = true;
        Debug.Log($"[PLATFORM DEBUG] Estado alterado para ativado");
        
        if (useDirectFlowerConnection)
        {
            // Modo: Conexão direta entre flores
            if (sourceFlower == null || targetFlower == null)
            {
                Debug.LogWarning($"[PLATFORM DEBUG] Platform {gameObject.name}: Flores não configuradas para conexão direta!");
                return;
            }
            
            // Muda o alvo da flor fonte para a flor destino
            sourceFlower.ChangeTarget(targetFlower.transform);
            Debug.Log($"[PLATFORM DEBUG] Flor {sourceFlower.name} redirecionada para: {targetFlower.name}");
            
            // Ativa a flor fonte se ela não estiver ativa
            if (!sourceFlower.IsActivated)
            {
                sourceFlower.ReceiveLight();
                Debug.Log($"[PLATFORM DEBUG] Flor {sourceFlower.name} ativada");
            }
        }
        else
        {
            // Modo: Controle da tocha
            if (torch == null)
            {
                Debug.LogWarning($"[PLATFORM DEBUG] Platform {gameObject.name}: Tocha não configurada!");
                return;
            }
            
            // Muda o alvo da tocha
            torch.SetTargetFlower(newTarget);
            Debug.Log($"[PLATFORM DEBUG] Tocha redirecionada para: {(newTarget != null ? newTarget.name : "null")}");
        }
        
        // Abaixa a plataforma
        targetPosition = originalPosition - Vector3.up * depthOffset;
        isMoving = true;
        Debug.Log($"[PLATFORM DEBUG] Posição original: {originalPosition}, Nova posição: {targetPosition}");
        
        // Atualiza visual
        UpdateVisualState();
        Debug.Log($"[PLATFORM DEBUG] Visual atualizado");
        
        Debug.Log($"[PLATFORM DEBUG] Plataforma {gameObject.name} ativada com sucesso!");
    }

    private void DeactivatePlatform()
    {
        isActivated = false;
        
        if (useDirectFlowerConnection)
        {
            // Modo: Conexão direta entre flores
            if (sourceFlower != null)
            {
                // Restaura o alvo original da flor fonte
                sourceFlower.ChangeTarget(originalSourceTarget);
                Debug.Log($"Plataforma {gameObject.name} desativada! Flor {sourceFlower.name} restaurada para alvo original.");
            }
        }
        else
        {
            // Modo: Controle da tocha
            if (torch != null)
            {
                // Restaura o alvo original da tocha
                torch.SetTargetFlower(originalTorchTarget);
                Debug.Log($"Plataforma {gameObject.name} desativada! Tocha restaurada para alvo original.");
            }
        }
        
        // Volta a plataforma para a posição original
        targetPosition = originalPosition;
        isMoving = true;
        
        // Atualiza visual
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        Debug.Log($"[PLATFORM DEBUG] Atualizando visual - Estado ativado: {isActivated}");
        
        // Ativa/desativa efeito visual
        if (activatedEffect != null)
        {
            activatedEffect.SetActive(isActivated);
            Debug.Log($"[PLATFORM DEBUG] Efeito ativado: {isActivated}");
        }
        
        // Muda material se configurado
        if (platformRenderer != null)
        {
            if (isActivated && activatedMaterial != null)
            {
                platformRenderer.material = activatedMaterial;
                Debug.Log($"[PLATFORM DEBUG] Material mudado para ativado");
            }
            else if (!isActivated && deactivatedMaterial != null)
            {
                platformRenderer.material = deactivatedMaterial;
                Debug.Log($"[PLATFORM DEBUG] Material mudado para desativado");
            }
        }
        
        // Muda modelo se configurado
        if (activatedModel != null && deactivatedModel != null)
        {
            activatedModel.SetActive(isActivated);
            deactivatedModel.SetActive(!isActivated);
            Debug.Log($"[PLATFORM DEBUG] Modelos atualizados - Ativado: {isActivated}, Desativado: {!isActivated}");
        }
        else
        {
            Debug.Log($"[PLATFORM DEBUG] Modelos não configurados - ActivatedModel: {(activatedModel != null ? "OK" : "NULL")}, DeactivatedModel: {(deactivatedModel != null ? "OK" : "NULL")}");
        }
    }

    // Método para configurar a tocha via script
    public void SetTorch(Torch newTorch)
    {
        torch = newTorch;
        if (torch != null)
        {
            originalTorchTarget = torch.CurrentTarget;
        }
    }

    // Método para configurar o novo alvo via script
    public void SetNewTarget(Transform target)
    {
        newTarget = target;
        
        // Se a plataforma já está ativada, atualiza imediatamente
        if (isActivated && torch != null)
        {
            torch.SetTargetFlower(newTarget);
        }
    }

    // Métodos para configurar modelos via script
    public void SetActivatedModel(GameObject model)
    {
        activatedModel = model;
        UpdateVisualState();
    }

    public void SetDeactivatedModel(GameObject model)
    {
        deactivatedModel = model;
        UpdateVisualState();
    }

    // Método para configurar o offset de profundidade
    public void SetDepthOffset(float offset)
    {
        depthOffset = offset;
    }

    // Método para configurar a velocidade de animação
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }

    // Método para forçar ativação/desativação
    public void ForceActivate()
    {
        if (!isActivated)
        {
            ActivatePlatform();
        }
    }

    public void ForceDeactivate()
    {
        if (isActivated)
        {
            DeactivatePlatform();
        }
    }

    private void OnValidate()
    {
        // Garante que o collider seja trigger no editor
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    // Propriedades para acesso externo
    public bool IsActivated => isActivated;
    public Torch AssociatedTorch => torch;
    public Transform NewTarget => newTarget;
}
