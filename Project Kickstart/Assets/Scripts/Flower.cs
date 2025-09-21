using UnityEngine;

public class Flower : MonoBehaviour
{
    [Header("Configurações da Flor")]
    [SerializeField] private Transform currentTarget;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Múltiplas Direções")]
    [SerializeField] private Transform nextFlowerTarget; // Direção para próxima flor
    [SerializeField] private Transform objectTarget1; // Direção para objeto 1
    [SerializeField] private Transform objectTarget2; // Direção para objeto 2
    [SerializeField] private int currentDirectionIndex = 0; // Índice da direção atual (0=flor, 1=obj1, 2=obj2)
    
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
        
        // Configura o alvo inicial baseado na direção atual
        UpdateCurrentTarget();
        
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

    // Método para atualizar o alvo atual baseado na direção
    private void UpdateCurrentTarget()
    {
        Transform newTarget = null;
        
        switch (currentDirectionIndex)
        {
            case 0:
                newTarget = nextFlowerTarget;
                Debug.Log($"Flor {gameObject.name}: Direção mudada para PRÓXIMA FLOR ({(nextFlowerTarget != null ? nextFlowerTarget.name : "null")})");
                break;
            case 1:
                newTarget = objectTarget1;
                Debug.Log($"Flor {gameObject.name}: Direção mudada para OBJETO 1 ({(objectTarget1 != null ? objectTarget1.name : "null")})");
                break;
            case 2:
                newTarget = objectTarget2;
                Debug.Log($"Flor {gameObject.name}: Direção mudada para OBJETO 2 ({(objectTarget2 != null ? objectTarget2.name : "null")})");
                break;
        }
        
        currentTarget = newTarget;
        
        // Se a flor já está ativa, atualiza a linha de luz
        if (isActivated)
        {
            EmitLightToTarget();
        }
    }
    
    // Método para ciclar para a próxima direção
    public void CycleToNextDirection()
    {
        currentDirectionIndex = (currentDirectionIndex + 1) % 3; // Cicla entre 0, 1, 2
        UpdateCurrentTarget();
        Debug.Log($"Flor {gameObject.name}: Ciclou para direção {currentDirectionIndex}");
    }
    
    // Método para definir uma direção específica
    public void SetDirection(int directionIndex)
    {
        if (directionIndex >= 0 && directionIndex <= 2)
        {
            currentDirectionIndex = directionIndex;
            UpdateCurrentTarget();
            Debug.Log($"Flor {gameObject.name}: Direção definida para {currentDirectionIndex}");
        }
        else
        {
            Debug.LogWarning($"Flor {gameObject.name}: Índice de direção inválido: {directionIndex}");
        }
    }
    
    // Propriedades para acesso externo
    public bool IsActivated => isActivated;
    public Transform CurrentTarget => currentTarget;
    public int CurrentDirectionIndex => currentDirectionIndex;
    public string CurrentDirectionName
    {
        get
        {
            switch (currentDirectionIndex)
            {
                case 0: return "Próxima Flor";
                case 1: return "Objeto 1";
                case 2: return "Objeto 2";
                default: return "Desconhecido";
            }
        }
    }
}
