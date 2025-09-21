using UnityEngine;

public class Flower : MonoBehaviour
{
    [Header("Configurações da Flor")]
    [SerializeField] private Transform currentTarget;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Configurações Visuais")]
    [SerializeField] private Color lightColor = Color.cyan;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private GameObject lightEffect; // Efeito visual quando ativada
    
    [Header("Estado da Flor")]
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool autoActivateOnStart = false; // Se deve ativar automaticamente no início
    [SerializeField] private bool autoChainActivation = false; // Se deve ativar automaticamente a próxima flor

    private void Start()
    {
        ConfigureLineRenderer();
        
        // Desativa o efeito de luz inicialmente
        if (lightEffect != null)
        {
            lightEffect.SetActive(isActivated);
        }
        
        // Se autoActivateOnStart estiver marcado, ativa a flor
        if (autoActivateOnStart && !isActivated)
        {
            ReceiveLight();
        }
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = lightColor;
        lineRenderer.endColor = lightColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false; // Inicialmente desabilitado
        
        // Configurar para não ser afetado por iluminação
        lineRenderer.receiveShadows = false;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void ReceiveLight()
    {
        if (isActivated) return; // Evita ativação múltipla
        
        isActivated = true;
        
        // Ativa efeito visual
        if (lightEffect != null)
        {
            lightEffect.SetActive(true);
        }
        
        // Emite luz para o próximo alvo se existir
        EmitLightToTarget();
        
        Debug.Log($"Flor {gameObject.name} foi ativada!");
    }

    private void EmitLightToTarget()
    {
        if (currentTarget == null || lineRenderer == null)
        {
            Debug.Log($"Flor {gameObject.name}: Sem alvo para emitir luz ou LineRenderer não configurado.");
            return;
        }

        // Ativa e configura a linha de luz
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, currentTarget.position);

        // Se o alvo for outra flor E autoChainActivation estiver ativo, ativa ela
        Flower nextFlower = currentTarget.GetComponent<Flower>();
        if (nextFlower != null && autoChainActivation)
        {
            nextFlower.ReceiveLight();
            Debug.Log($"Flor {gameObject.name}: Ativou automaticamente a próxima flor {nextFlower.name}");
        }
        else if (nextFlower != null && !autoChainActivation)
        {
            Debug.Log($"Flor {gameObject.name}: Emitindo luz para {nextFlower.name}, mas não ativando automaticamente");
        }
        else
        {
            Debug.Log($"Flor {gameObject.name}: Alvo {currentTarget.name} não é uma flor.");
        }
    }

    public void ChangeTarget(Transform newTarget)
    {
        // Remove a luz anterior se estava ativa
        if (isActivated && lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        currentTarget = newTarget;
        
        Debug.Log($"Flor {gameObject.name}: Alvo mudado para {(newTarget != null ? newTarget.name : "null")}");

        // Se a flor já estava ativada, emite luz para o novo alvo
        if (isActivated)
        {
            EmitLightToTarget();
        }
    }

    public void DeactivateFlower()
    {
        isActivated = false;
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        if (lightEffect != null)
        {
            lightEffect.SetActive(false);
        }
        
        Debug.Log($"Flor {gameObject.name} foi desativada!");
    }

    private void Update()
    {
        // Atualiza a posição da linha caso os objetos se movam
        if (isActivated && currentTarget != null && lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentTarget.position);
        }
    }

    private void OnValidate()
    {
        // Atualiza as configurações no editor
        if (lineRenderer != null)
        {
            ConfigureLineRenderer();
        }
    }

    // Propriedades para acesso externo
    public bool IsActivated => isActivated;
    public Transform CurrentTarget => currentTarget;
}
