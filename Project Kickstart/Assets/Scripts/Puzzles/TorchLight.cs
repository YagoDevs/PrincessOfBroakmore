using UnityEngine;

public class TorchLight : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light torchLight;
    [SerializeField] private ParticleSystem torchParticles;
    [SerializeField] private Renderer torchRenderer;
    
    [Header("Light Properties")]
    [SerializeField] private Color connectedColor = Color.yellow;
    [SerializeField] private Color disconnectedColor = Color.red;
    [SerializeField] private float connectedIntensity = 2f;
    [SerializeField] private float disconnectedIntensity = 0.5f;
    [SerializeField] private float lightRange = 10f;
    
    [Header("Material Settings")]
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private string emissionProperty = "_EmissionColor";
    
    [Header("Animation")]
    [SerializeField] private bool animateLight = true;
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float flickerIntensity = 0.1f;
    [SerializeField] private AnimationCurve flickerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Effects")]
    [SerializeField] private float activationEffectDuration = 1f;
    [SerializeField] private AnimationCurve activationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Estado
    private bool isConnected = false;
    private bool isTransitioning = false;
    private float baseIntensity;
    private Color baseColor;
    private float transitionProgress = 0f;
    private Material materialInstance;
    
    // Componentes
    private ColumnController parentColumn;
    
    private void Start()
    {
        // Encontrar coluna pai
        parentColumn = GetComponentInParent<ColumnController>();
        
        // Validar componentes
        ValidateComponents();
        
        // Configurar material instância
        SetupMaterialInstance();
        
        // Configurar estado inicial
        SetConnectionState(false, true);
        
        if (showDebugInfo)
        {
            Debug.Log($"TorchLight initialized on column {(parentColumn ? parentColumn.ColumnID.ToString() : "Unknown")}");
        }
    }
    
    private void Update()
    {
        // Animar luz se ativo
        if (animateLight && torchLight != null && torchLight.enabled)
        {
            AnimateTorchLight();
        }
        
        // Processar transição se ativa
        if (isTransitioning)
        {
            UpdateTransition();
        }
    }
    
    /// <summary>
    /// Valida componentes necessários
    /// </summary>
    private void ValidateComponents()
    {
        if (torchLight == null)
        {
            torchLight = GetComponent<Light>();
            if (torchLight == null)
            {
                Debug.LogError($"TorchLight on {gameObject.name}: No Light component found!");
            }
        }
        
        if (torchRenderer == null)
        {
            torchRenderer = GetComponent<Renderer>();
        }
        
        if (torchParticles == null)
        {
            torchParticles = GetComponentInChildren<ParticleSystem>();
        }
    }
    
    /// <summary>
    /// Configura instância do material
    /// </summary>
    private void SetupMaterialInstance()
    {
        if (torchRenderer != null)
        {
            materialInstance = torchRenderer.material; // Cria automaticamente uma instância
        }
    }
    
    /// <summary>
    /// Define o estado de conexão da tocha
    /// </summary>
    /// <param name="connected">Se está conectada</param>
    /// <param name="immediate">Se deve mudar imediatamente ou com transição</param>
    public void SetConnectionState(bool connected, bool immediate = false)
    {
        if (isConnected == connected && !immediate) return;
        
        isConnected = connected;
        
        if (immediate)
        {
            ApplyConnectionState();
        }
        else
        {
            StartTransition();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"TorchLight connection state changed to: {connected} (immediate: {immediate})");
        }
    }
    
    /// <summary>
    /// Inicia transição suave entre estados
    /// </summary>
    private void StartTransition()
    {
        isTransitioning = true;
        transitionProgress = 0f;
        
        // Tocar som de conexão/desconexão
        PlayConnectionSound();
    }
    
    /// <summary>
    /// Atualiza transição entre estados
    /// </summary>
    private void UpdateTransition()
    {
        transitionProgress += Time.deltaTime / activationEffectDuration;
        
        if (transitionProgress >= 1f)
        {
            transitionProgress = 1f;
            isTransitioning = false;
        }
        
        // Aplicar estado interpolado
        ApplyTransitionState();
    }
    
    /// <summary>
    /// Aplica estado final sem transição
    /// </summary>
    private void ApplyConnectionState()
    {
        if (torchLight != null)
        {
            torchLight.enabled = isConnected;
            torchLight.color = isConnected ? connectedColor : disconnectedColor;
            torchLight.intensity = isConnected ? connectedIntensity : disconnectedIntensity;
            torchLight.range = lightRange;
            
            baseIntensity = torchLight.intensity;
            baseColor = torchLight.color;
        }
        
        // Atualizar partículas
        UpdateParticleSystem();
        
        // Atualizar material
        UpdateMaterial();
    }
    
    /// <summary>
    /// Aplica estado durante transição
    /// </summary>
    private void ApplyTransitionState()
    {
        if (torchLight == null) return;
        
        float curveValue = activationCurve.Evaluate(transitionProgress);
        
        if (isConnected)
        {
            // Transição para conectado
            torchLight.enabled = true;
            torchLight.color = Color.Lerp(disconnectedColor, connectedColor, curveValue);
            torchLight.intensity = Mathf.Lerp(disconnectedIntensity, connectedIntensity, curveValue);
        }
        else
        {
            // Transição para desconectado
            torchLight.color = Color.Lerp(connectedColor, disconnectedColor, curveValue);
            torchLight.intensity = Mathf.Lerp(connectedIntensity, disconnectedIntensity, curveValue);
            
            if (transitionProgress >= 1f)
            {
                torchLight.enabled = false;
            }
        }
        
        baseIntensity = torchLight.intensity;
        baseColor = torchLight.color;
        
        // Atualizar outros componentes
        UpdateParticleSystem();
        UpdateMaterial();
    }
    
    /// <summary>
    /// Anima a luz da tocha com efeito de flicker
    /// </summary>
    private void AnimateTorchLight()
    {
        if (torchLight == null || !torchLight.enabled) return;
        
        float time = Time.time * flickerSpeed;
        float flicker = flickerCurve.Evaluate(Mathf.PingPong(time, 1f));
        float intensity = baseIntensity + (flicker * flickerIntensity);
        
        torchLight.intensity = intensity;
    }
    
    /// <summary>
    /// Atualiza sistema de partículas
    /// </summary>
    private void UpdateParticleSystem()
    {
        if (torchParticles == null) return;
        
        if (isConnected && !torchParticles.isPlaying)
        {
            torchParticles.Play();
        }
        else if (!isConnected && torchParticles.isPlaying)
        {
            torchParticles.Stop();
        }
        
        // Atualizar cor das partículas
        var main = torchParticles.main;
        main.startColor = isConnected ? connectedColor : disconnectedColor;
    }
    
    /// <summary>
    /// Atualiza material da tocha
    /// </summary>
    private void UpdateMaterial()
    {
        if (materialInstance == null) return;
        
        // Usar material ativo/inativo se disponível
        Material targetMaterial = isConnected ? activeMaterial : inactiveMaterial;
        if (targetMaterial != null && torchRenderer != null)
        {
            torchRenderer.material = targetMaterial;
            materialInstance = torchRenderer.material;
        }
        
        // Atualizar propriedade de emissão se suportada
        if (materialInstance.HasProperty(emissionProperty))
        {
            Color emissionColor = isConnected ? connectedColor : Color.black;
            materialInstance.SetColor(emissionProperty, emissionColor);
        }
    }
    
    /// <summary>
    /// Reproduz som de conexão
    /// </summary>
    private void PlayConnectionSound()
    {
        if (AudioManager.Instance == null) return;
        
        if (isConnected)
        {
            // Som de conexão (similar ao som da flor quando recebe luz)
            AudioManager.Instance.PlayFlowerLightReceptionSound(
                transform.position,
                $"Torch_{(parentColumn ? parentColumn.ColumnID.ToString() : "Unknown")}"
            );
        }
        else
        {
            // Som de desconexão mais sutil
            AudioManager.Instance.PlayFlowerEmissionToObjectSound(
                transform.position,
                "TorchDisconnect"
            );
        }
    }
    
    /// <summary>
    /// Força atualização do estado (para debug)
    /// </summary>
    [ContextMenu("Force Update State")]
    public void ForceUpdateState()
    {
        ApplyConnectionState();
    }
    
    /// <summary>
    /// Testa conexão (para debug)
    /// </summary>
    [ContextMenu("Test Connection")]
    public void TestConnection()
    {
        SetConnectionState(!isConnected);
    }
    
    /// <summary>
    /// Obtém estado atual da conexão
    /// </summary>
    public bool IsConnected => isConnected;
    
    /// <summary>
    /// Verifica se a tocha está acesa (alias para IsConnected)
    /// </summary>
    public bool IsLit => isConnected;
    
    /// <summary>
    /// Obtém referência da luz
    /// </summary>
    public Light GetLight() => torchLight;
    
    private void OnDestroy()
    {
        // Limpar material instância
        if (materialInstance != null && torchRenderer != null)
        {
            DestroyImmediate(materialInstance);
        }
    }
    
    /// <summary>
    /// Gizmos para debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (torchLight != null)
        {
            // Desenhar alcance da luz
            Gizmos.color = isConnected ? connectedColor : disconnectedColor;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, lightRange);
            
            // Desenhar direção da luz
            Gizmos.color = isConnected ? Color.yellow : Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
        }
        
        // Label com estado
#if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 2f;
        UnityEditor.Handles.Label(labelPos, $"Torch: {(isConnected ? "Connected" : "Disconnected")}");
#endif
    }
}

