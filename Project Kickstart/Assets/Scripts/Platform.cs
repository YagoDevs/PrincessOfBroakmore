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
    
    private Renderer platformRenderer;
    private Transform originalFlowerTarget; // Armazena o alvo original da flor

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
        
        // Configura estado inicial
        UpdateVisualState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isActivated)
        {
            ActivatePlatform();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && isActivated && canBeDeactivated)
        {
            DeactivatePlatform();
        }
    }

    private void ActivatePlatform()
    {
        if (flower == null)
        {
            Debug.LogWarning($"Platform {gameObject.name}: Flor não configurada!");
            return;
        }

        isActivated = true;
        
        // Muda o alvo da flor
        flower.ChangeTarget(newTarget);
        
        // Atualiza visual
        UpdateVisualState();
        
        Debug.Log($"Plataforma {gameObject.name} ativada! Flor redirecionada para {(newTarget != null ? newTarget.name : "null")}");
    }

    private void DeactivatePlatform()
    {
        if (flower == null) return;

        isActivated = false;
        
        // Restaura o alvo original da flor
        flower.ChangeTarget(originalFlowerTarget);
        
        // Atualiza visual
        UpdateVisualState();
        
        Debug.Log($"Plataforma {gameObject.name} desativada! Flor restaurada para alvo original.");
    }

    private void UpdateVisualState()
    {
        // Ativa/desativa efeito visual
        if (activatedEffect != null)
        {
            activatedEffect.SetActive(isActivated);
        }
        
        // Muda material se configurado
        if (platformRenderer != null)
        {
            if (isActivated && activatedMaterial != null)
            {
                platformRenderer.material = activatedMaterial;
            }
            else if (!isActivated && deactivatedMaterial != null)
            {
                platformRenderer.material = deactivatedMaterial;
            }
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
