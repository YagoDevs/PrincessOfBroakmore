using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Platform : MonoBehaviour
{
    [Header("Configurações da Plataforma")]
    [SerializeField] private Flower flower; // Referência à flor que ela controla
    [SerializeField] private Transform newTarget; // Novo alvo para a flor quando ativada
    
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
    private Transform originalFlowerTarget; // Armazena o alvo original da flor
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
        
        // Armazena o alvo original da flor
        if (flower != null)
        {
            originalFlowerTarget = flower.CurrentTarget;
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
        
        if (flower == null)
        {
            Debug.LogWarning($"[PLATFORM DEBUG] Platform {gameObject.name}: Flor não configurada!");
            return;
        }

        isActivated = true;
        Debug.Log($"[PLATFORM DEBUG] Estado alterado para ativado");
        
        // Muda o alvo da flor
        flower.ChangeTarget(newTarget);
        Debug.Log($"[PLATFORM DEBUG] Flor redirecionada para: {(newTarget != null ? newTarget.name : "null")}");
        
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
        if (flower == null) return;

        isActivated = false;
        
        // Restaura o alvo original da flor
        flower.ChangeTarget(originalFlowerTarget);
        
        // Volta a plataforma para a posição original
        targetPosition = originalPosition;
        isMoving = true;
        
        // Atualiza visual
        UpdateVisualState();
        
        Debug.Log($"Plataforma {gameObject.name} desativada! Flor restaurada para alvo original.");
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

    // Método para configurar a flor via script
    public void SetFlower(Flower newFlower)
    {
        flower = newFlower;
        if (flower != null)
        {
            originalFlowerTarget = flower.CurrentTarget;
        }
    }

    // Método para configurar o novo alvo via script
    public void SetNewTarget(Transform target)
    {
        newTarget = target;
        
        // Se a plataforma já está ativada, atualiza imediatamente
        if (isActivated && flower != null)
        {
            flower.ChangeTarget(newTarget);
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
    public Flower AssociatedFlower => flower;
    public Transform NewTarget => newTarget;
}
