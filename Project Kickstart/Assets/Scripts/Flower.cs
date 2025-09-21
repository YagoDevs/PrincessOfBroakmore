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
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private GameObject lightEffect; // Efeito visual quando ativada
    
    [Header("Sistema de Luz Volumétrica")]
    [SerializeField] private bool useVolumetricLight = true;
    [SerializeField] private int volumeLayers = 5; // Quantas camadas de volume
    [SerializeField] private float maxVolumeWidth = 0.5f; // Largura máxima do volume
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float particleDensity = 50f;
    
    // Componentes criados automaticamente
    private LineRenderer[] volumeLines;
    private ParticleSystem lightParticles;
    private Light originLight;
    private Light destinationLight;
    private LensFlare originFlare;
    private LensFlare destinationFlare;
    
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
        if (useVolumetricLight)
        {
            CreateVolumetricLightSystem();
        }
        else
        {
            // Sistema simples original
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
            lineRenderer.enabled = false;
            
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    
    private void CreateVolumetricLightSystem()
    {
        Debug.Log($"[VOLUMETRIC] Criando sistema de luz volumétrica para {gameObject.name}");
        
        // 1. Criar múltiplas linhas para volume
        CreateVolumeLayers();
        
        // 2. Criar sistema de partículas
        CreateParticleSystem();
        
        // 3. Criar luzes pontuais
        CreatePointLights();
        
        // 4. Criar lens flares
        CreateLensFlares();
        
        Debug.Log($"[VOLUMETRIC] Sistema criado com sucesso!");
    }
    
    private void CreateVolumeLayers()
    {
        volumeLines = new LineRenderer[volumeLayers];
        
        for (int i = 0; i < volumeLayers; i++)
        {
            // Criar GameObject filho para cada linha
            GameObject layerObj = new GameObject($"VolumeLayer_{i}");
            layerObj.transform.SetParent(transform);
            layerObj.transform.localPosition = Vector3.zero;
            
            // Configurar LineRenderer
            LineRenderer layer = layerObj.AddComponent<LineRenderer>();
            
            // Criar material automaticamente se não existir
            if (lineMaterial == null)
            {
                layer.material = CreateGlowMaterial(i);
            }
            else
            {
                layer.material = lineMaterial;
            }
            
            // Configurar propriedades baseadas na camada
            float layerAlpha = 1f - (float)i / volumeLayers; // Camadas externas mais transparentes
            float layerWidth = lineWidth + (maxVolumeWidth * (float)i / volumeLayers);
            
            Color layerColor = lightColor;
            layerColor.a = layerAlpha * 0.3f; // Bem transparente para efeito de volume
            
            layer.startColor = layerColor;
            layer.endColor = layerColor;
            layer.startWidth = layerWidth;
            layer.endWidth = layerWidth;
            layer.positionCount = 2;
            layer.useWorldSpace = true;
            layer.enabled = false;
            
            // Configurações de renderização
            layer.receiveShadows = false;
            layer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            layer.sortingOrder = -i; // Camadas mais espessas atrás
            
            volumeLines[i] = layer;
        }
        
        Debug.Log($"[VOLUMETRIC] {volumeLayers} camadas de volume criadas");
    }
    
    private Material CreateGlowMaterial(int layerIndex)
    {
        // Criar material com shader padrão e configurações de glow
        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        glowMat.name = $"AutoGlow_Layer_{layerIndex}";
        
        // Configurar para glow
        glowMat.color = lightColor;
        glowMat.SetFloat("_Mode", 2); // Transparent
        glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive blending
        glowMat.SetInt("_ZWrite", 0);
        glowMat.DisableKeyword("_ALPHATEST_ON");
        glowMat.EnableKeyword("_ALPHABLEND_ON");
        glowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        glowMat.renderQueue = 3000;
        
        return glowMat;
    }
    
    private void CreateParticleSystem()
    {
        // Criar GameObject para partículas
        GameObject particleObj = new GameObject("LightParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        // Adicionar sistema de partículas
        lightParticles = particleObj.AddComponent<ParticleSystem>();
        
        // Configurar partículas
        var main = lightParticles.main;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.startSize = 0.05f;
        main.startColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.7f);
        main.maxParticles = (int)particleDensity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Configurar emissão
        var emission = lightParticles.emission;
        emission.enabled = false; // Só ativa quando a luz estiver ativa
        emission.rateOverTime = particleDensity / 2f;
        
        // Configurar forma (linha)
        var shape = lightParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.1f, 0.1f, 1f); // Será ajustado dinamicamente
        
        // Configurar velocidade
        var velocity = lightParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = 0.2f;
        
        // Configurar cor ao longo da vida
        var colorOverLifetime = lightParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(lightColor, 0.0f), 
                new GradientColorKey(lightColor, 0.5f), 
                new GradientColorKey(Color.white, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.8f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.3f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // Configurar tamanho ao longo da vida
        var sizeOverLifetime = lightParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        Debug.Log($"[VOLUMETRIC] Sistema de partículas criado");
    }
    
    private void CreatePointLights()
    {
        // Luz na origem (flor)
        GameObject originLightObj = new GameObject("OriginLight");
        originLightObj.transform.SetParent(transform);
        originLightObj.transform.localPosition = Vector3.zero;
        
        originLight = originLightObj.AddComponent<Light>();
        originLight.type = LightType.Point;
        originLight.color = lightColor;
        originLight.intensity = lightIntensity;
        originLight.range = 3f;
        originLight.shadows = LightShadows.Soft;
        originLight.enabled = false;
        
        // A luz de destino será criada dinamicamente quando necessário
        Debug.Log($"[VOLUMETRIC] Luzes pontuais criadas");
    }
    
    private void CreateLensFlares()
    {
        // Lens flare na origem
        originFlare = originLight.gameObject.AddComponent<LensFlare>();
        
        // Configurar flare automaticamente
        originFlare.brightness = 0.5f;
        originFlare.fadeSpeed = 3f;
        originFlare.color = lightColor;
        
        // Criar flare texture simples se não existir
        if (originFlare.flare == null)
        {
            // Unity tem flares padrão que podemos tentar usar
            var defaultFlare = Resources.Load<Flare>("Default-Flare");
            if (defaultFlare != null)
            {
                originFlare.flare = defaultFlare;
            }
        }
        
        Debug.Log($"[VOLUMETRIC] Lens flares criados");
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
        if (currentTarget == null)
        {
            Debug.Log($"Flor {gameObject.name}: Sem alvo para emitir luz.");
            return;
        }

        if (useVolumetricLight)
        {
            // Sistema volumétrico
            EmitVolumetricLight();
        }
        else
        {
            // Sistema simples original
            if (lineRenderer == null)
            {
                Debug.Log($"Flor {gameObject.name}: LineRenderer não configurado.");
                return;
            }
            
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentTarget.position);
        }

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
    
    private void EmitVolumetricLight()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = currentTarget.position;
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);
        
        Debug.Log($"[VOLUMETRIC] Emitindo luz volumétrica para {currentTarget.name}, distância: {distance:F2}");
        
        // 1. Configurar todas as camadas de volume
        if (volumeLines != null)
        {
            for (int i = 0; i < volumeLines.Length; i++)
            {
                if (volumeLines[i] != null)
                {
                    volumeLines[i].enabled = true;
                    volumeLines[i].SetPosition(0, startPos);
                    volumeLines[i].SetPosition(1, endPos);
                }
            }
        }
        
        // 2. Configurar sistema de partículas
        if (lightParticles != null)
        {
            // Posicionar o sistema no meio do caminho
            Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
            lightParticles.transform.position = midPoint;
            lightParticles.transform.LookAt(endPos);
            
            // Ajustar forma das partículas para seguir a linha
            var shape = lightParticles.shape;
            shape.scale = new Vector3(0.2f, 0.2f, distance);
            
            // Ativar emissão
            var emission = lightParticles.emission;
            emission.enabled = true;
        }
        
        // 3. Ativar luzes pontuais
        if (originLight != null)
        {
            originLight.enabled = true;
        }
        
        // 4. Criar luz no destino se necessário
        CreateDestinationLight(endPos);
        
        // 5. Ativar lens flares
        if (originFlare != null)
        {
            originFlare.enabled = true;
        }
    }
    
    private void CreateDestinationLight(Vector3 position)
    {
        // Verificar se já existe uma luz no alvo
        if (currentTarget.GetComponent<Light>() == null)
        {
            // Criar luz temporária no destino
            GameObject destLightObj = new GameObject("DestinationLight_Temp");
            destLightObj.transform.position = position;
            
            destinationLight = destLightObj.AddComponent<Light>();
            destinationLight.type = LightType.Point;
            destinationLight.color = lightColor;
            destinationLight.intensity = lightIntensity * 0.7f;
            destinationLight.range = 2f;
            destinationLight.shadows = LightShadows.Soft;
            
            // Adicionar lens flare
            destinationFlare = destLightObj.AddComponent<LensFlare>();
            destinationFlare.brightness = 0.3f;
            destinationFlare.fadeSpeed = 3f;
            destinationFlare.color = lightColor;
            
            // Auto-destruir após um tempo
            Destroy(destLightObj, 10f);
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
        
        if (useVolumetricLight)
        {
            DeactivateVolumetricLight();
        }
        else
        {
            // Sistema simples
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
        
        if (lightEffect != null)
        {
            lightEffect.SetActive(false);
        }
        
        Debug.Log($"Flor {gameObject.name} foi desativada!");
    }
    
    private void DeactivateVolumetricLight()
    {
        Debug.Log($"[VOLUMETRIC] Desativando sistema volumétrico de {gameObject.name}");
        
        // 1. Desativar todas as camadas de volume
        if (volumeLines != null)
        {
            for (int i = 0; i < volumeLines.Length; i++)
            {
                if (volumeLines[i] != null)
                {
                    volumeLines[i].enabled = false;
                }
            }
        }
        
        // 2. Desativar sistema de partículas
        if (lightParticles != null)
        {
            var emission = lightParticles.emission;
            emission.enabled = false;
        }
        
        // 3. Desativar luzes pontuais
        if (originLight != null)
        {
            originLight.enabled = false;
        }
        
        if (destinationLight != null)
        {
            destinationLight.enabled = false;
        }
        
        // 4. Desativar lens flares
        if (originFlare != null)
        {
            originFlare.enabled = false;
        }
        
        if (destinationFlare != null)
        {
            destinationFlare.enabled = false;
        }
    }

    private void Update()
    {
        if (!isActivated || currentTarget == null) return;
        
        if (useVolumetricLight)
        {
            UpdateVolumetricLight();
        }
        else
        {
            // Sistema simples original
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, currentTarget.position);
            }
        }
    }
    
    private void UpdateVolumetricLight()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = currentTarget.position;
        float distance = Vector3.Distance(startPos, endPos);
        
        // 1. Atualizar todas as camadas de volume
        if (volumeLines != null)
        {
            for (int i = 0; i < volumeLines.Length; i++)
            {
                if (volumeLines[i] != null && volumeLines[i].enabled)
                {
                    volumeLines[i].SetPosition(0, startPos);
                    volumeLines[i].SetPosition(1, endPos);
                    
                    // Animação de pulsação
                    float pulse = Mathf.Sin(Time.time * 2f + i * 0.3f) * 0.1f + 1f;
                    Color currentColor = volumeLines[i].startColor;
                    currentColor.a = (currentColor.a * pulse);
                    volumeLines[i].startColor = currentColor;
                    volumeLines[i].endColor = currentColor;
                }
            }
        }
        
        // 2. Atualizar sistema de partículas
        if (lightParticles != null && lightParticles.emission.enabled)
        {
            Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
            lightParticles.transform.position = midPoint;
            lightParticles.transform.LookAt(endPos);
            
            var shape = lightParticles.shape;
            shape.scale = new Vector3(0.2f, 0.2f, distance);
        }
        
        // 3. Animação das luzes pontuais (pulsação)
        if (originLight != null && originLight.enabled)
        {
            float lightPulse = Mathf.Sin(Time.time * 3f) * 0.2f + 1f;
            originLight.intensity = lightIntensity * lightPulse;
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
