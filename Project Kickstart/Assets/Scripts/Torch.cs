using UnityEngine;

public class Torch : MonoBehaviour
{
    [Header("Configurações da Tocha")]
    [SerializeField] private Transform targetFlower;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Configurações Visuais")]
    [SerializeField] private Color lightColor = Color.yellow;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    
    [Header("Iluminação Natural")]
    [SerializeField] private Light torchLight; // Luz da tocha
    [SerializeField] private bool createTorchLight = true;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightRange = 10f;
    [SerializeField] private bool flickerEffect = true;
    
    [Header("Efeito de Chama")]
    [SerializeField] private ParticleSystem flameParticles; // Partículas da chama
    [SerializeField] private bool createFlameEffect = true;
    
    [Header("Controle de Ativação")]
    [SerializeField] private bool autoEmitOnStart = true; // Se deve emitir luz automaticamente no início
    
    [Header("Sistema de Luz Volumétrica")]
    [SerializeField] private bool useVolumetricLight = true;
    [SerializeField] private int volumeLayers = 5;
    [SerializeField] private float maxVolumeWidth = 0.5f;
    
    // Componentes criados automaticamente
    private LineRenderer[] volumeLines;
    private ParticleSystem lightParticles;
    
    // Variáveis para efeito de tremulação
    private float originalIntensity;
    private float flickerTime;

    private void Start()
    {
        ConfigureLineRenderer();
        SetupTorchLight();
        SetupFlameEffect();
        
        // Só emite luz se autoEmitOnStart estiver marcado
        if (autoEmitOnStart)
        {
            EmitLightVisualOnly(); // Emite apenas o visual, sem ativar a flor
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
            
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    
    private void CreateVolumetricLightSystem()
    {
        Debug.Log($"[TORCH VOLUMETRIC] Criando sistema de luz volumétrica para {gameObject.name}");
        
        CreateVolumeLayers();
        CreateParticleSystem();
        
        Debug.Log($"[TORCH VOLUMETRIC] Sistema criado com sucesso!");
    }
    
    private void CreateVolumeLayers()
    {
        volumeLines = new LineRenderer[volumeLayers];
        
        for (int i = 0; i < volumeLayers; i++)
        {
            GameObject layerObj = new GameObject($"TorchVolumeLayer_{i}");
            layerObj.transform.SetParent(transform);
            layerObj.transform.localPosition = Vector3.zero;
            
            LineRenderer layer = layerObj.AddComponent<LineRenderer>();
            
            if (lineMaterial == null)
            {
                layer.material = CreateGlowMaterial(i);
            }
            else
            {
                layer.material = lineMaterial;
            }
            
            float layerAlpha = 1f - (float)i / volumeLayers;
            float layerWidth = lineWidth + (maxVolumeWidth * (float)i / volumeLayers);
            
            Color layerColor = lightColor;
            layerColor.a = layerAlpha * 0.3f;
            
            layer.startColor = layerColor;
            layer.endColor = layerColor;
            layer.startWidth = layerWidth;
            layer.endWidth = layerWidth;
            layer.positionCount = 2;
            layer.useWorldSpace = true;
            layer.enabled = false;
            
            layer.receiveShadows = false;
            layer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            layer.sortingOrder = -i;
            
            volumeLines[i] = layer;
        }
        
        Debug.Log($"[TORCH VOLUMETRIC] {volumeLayers} camadas de volume criadas");
    }
    
    private Material CreateGlowMaterial(int layerIndex)
    {
        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        glowMat.name = $"TorchAutoGlow_Layer_{layerIndex}";
        
        // Usar a cor da tocha (amarelo), não roxa!
        glowMat.color = lightColor;
        glowMat.SetFloat("_Mode", 2);
        glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        glowMat.SetInt("_ZWrite", 0);
        glowMat.DisableKeyword("_ALPHATEST_ON");
        glowMat.EnableKeyword("_ALPHABLEND_ON");
        glowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        glowMat.renderQueue = 3000;
        
        return glowMat;
    }
    
    private void CreateParticleSystem()
    {
        GameObject particleObj = new GameObject("TorchLightParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        lightParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = lightParticles.main;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.startSize = 0.05f;
        main.startColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.7f); // Usar cor da tocha
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = lightParticles.emission;
        emission.enabled = false;
        emission.rateOverTime = 15f;
        
        var shape = lightParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.1f, 0.1f, 1f);
        
        var velocity = lightParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = 0.2f;
        
        var colorOverLifetime = lightParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(lightColor, 0.0f),  // Usar cor da tocha 
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
        
        Debug.Log($"[TORCH VOLUMETRIC] Sistema de partículas criado com cor {lightColor}");
    }

    private void EmitLight()
    {
        if (targetFlower == null)
        {
            Debug.LogWarning("Torch: targetFlower não configurado!");
            return;
        }

        if (useVolumetricLight)
        {
            EmitVolumetricLight();
        }
        else
        {
            // Sistema simples original
            if (lineRenderer == null)
            {
                Debug.LogWarning("Torch: LineRenderer não configurado!");
                return;
            }
            
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetFlower.position);
        }

        // Ativa a primeira flor
        Flower flower = targetFlower.GetComponent<Flower>();
        if (flower != null)
        {
            flower.ReceiveLight();
        }
        else
        {
            Debug.LogWarning("Torch: O targetFlower não possui o componente Flower!");
        }
    }
    
    private void EmitVolumetricLight()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetFlower.position;
        float distance = Vector3.Distance(startPos, endPos);
        
        Debug.Log($"[TORCH VOLUMETRIC] Emitindo luz volumétrica para {targetFlower.name}, cor: {lightColor}");
        
        // Configurar todas as camadas de volume
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
        
        // Configurar sistema de partículas
        if (lightParticles != null)
        {
            Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
            lightParticles.transform.position = midPoint;
            lightParticles.transform.LookAt(endPos);
            
            var shape = lightParticles.shape;
            shape.scale = new Vector3(0.2f, 0.2f, distance);
            
            var emission = lightParticles.emission;
            emission.enabled = true;
        }
    }

    private void Update()
    {
        // Atualizar sistema de luz
        if (targetFlower != null)
        {
            if (useVolumetricLight)
            {
                UpdateVolumetricLight();
            }
            else if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, targetFlower.position);
            }
        }
        
        // Efeito de tremulação da luz
        if (flickerEffect && torchLight != null)
        {
            FlickerLight();
        }
    }
    
    private void UpdateVolumetricLight()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetFlower.position;
        float distance = Vector3.Distance(startPos, endPos);
        
        // Atualizar camadas de volume
        if (volumeLines != null)
        {
            for (int i = 0; i < volumeLines.Length; i++)
            {
                if (volumeLines[i] != null && volumeLines[i].enabled)
                {
                    volumeLines[i].SetPosition(0, startPos);
                    volumeLines[i].SetPosition(1, endPos);
                    
                    // Animação de pulsação
                    float pulse = Mathf.Sin(Time.time * 1.5f + i * 0.2f) * 0.1f + 1f;
                    Color currentColor = volumeLines[i].startColor;
                    Color baseColor = lightColor;
                    baseColor.a = currentColor.a;
                    volumeLines[i].startColor = baseColor * pulse;
                    volumeLines[i].endColor = baseColor * pulse;
                }
            }
        }
        
        // Atualizar partículas
        if (lightParticles != null && lightParticles.emission.enabled)
        {
            Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
            lightParticles.transform.position = midPoint;
            lightParticles.transform.LookAt(endPos);
            
            var shape = lightParticles.shape;
            shape.scale = new Vector3(0.2f, 0.2f, distance);
        }
    }

    // Método para configurar a flor alvo via script
    public void SetTargetFlower(Transform newTarget)
    {
        targetFlower = newTarget;
        EmitLight();
    }
    
    // Método para ativar a tocha manualmente
    public void ActivateTorch()
    {
        EmitLight();
    }
    
    // Método para emitir apenas o visual da luz (sem ativar a flor)
    public void EmitLightVisualOnly()
    {
        if (targetFlower == null)
        {
            Debug.LogWarning("Torch: targetFlower não configurado!");
            return;
        }

        Debug.Log($"[TORCH] Emitindo luz visual inicial para {targetFlower.name} (sem ativar)");

        if (useVolumetricLight)
        {
            EmitVolumetricLight();
        }
        else
        {
            // Sistema simples original
            if (lineRenderer == null)
            {
                Debug.LogWarning("Torch: LineRenderer não configurado!");
                return;
            }
            
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetFlower.position);
        }
        
        // NÃO ativa a flor - apenas mostra o visual da luz
    }
    
    // Propriedade para acessar o alvo atual
    public Transform CurrentTarget => targetFlower;

    private void SetupTorchLight()
    {
        if (createTorchLight && torchLight == null)
        {
            // Cria automaticamente um Light component
            GameObject lightObject = new GameObject("TorchLight");
            lightObject.transform.SetParent(transform);
            lightObject.transform.localPosition = Vector3.up * 0.5f; // Um pouco acima da tocha
            
            torchLight = lightObject.AddComponent<Light>();
        }
        
        if (torchLight != null)
        {
            torchLight.type = LightType.Point;
            torchLight.color = lightColor;
            torchLight.intensity = lightIntensity;
            torchLight.range = lightRange;
            torchLight.shadows = LightShadows.Soft; // Sombras suaves
            
            originalIntensity = lightIntensity;
        }
    }
    
    private void SetupFlameEffect()
    {
        if (createFlameEffect && flameParticles == null)
        {
            // Cria um sistema de partículas básico para a chama
            GameObject flameObject = new GameObject("FlameEffect");
            flameObject.transform.SetParent(transform);
            flameObject.transform.localPosition = Vector3.up * 0.3f;
            
            flameParticles = flameObject.AddComponent<ParticleSystem>();
            ConfigureFlameParticles();
        }
    }
    
    private void ConfigureFlameParticles()
    {
        if (flameParticles == null) return;
        
        var main = flameParticles.main;
        main.startLifetime = 1f;
        main.startSpeed = 2f;
        main.startSize = 0.3f;
        main.startColor = new Color(1f, 0.5f, 0f, 0.8f); // Laranja/amarelo
        main.maxParticles = 30;
        
        var emission = flameParticles.emission;
        emission.rateOverTime = 20;
        
        var shape = flameParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.1f;
        
        var velocityOverLifetime = flameParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.y = 3f;
        
        var colorOverLifetime = flameParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.yellow, 0.0f), 
                new GradientColorKey(Color.red, 0.5f), 
                new GradientColorKey(Color.black, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.5f, 0.5f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
    }
    
    private void FlickerLight()
    {
        flickerTime += Time.deltaTime;
        
        // Cria um efeito de tremulação usando Perlin noise
        float flicker = Mathf.PerlinNoise(flickerTime * 5f, 0f);
        flicker = Mathf.Clamp01(flicker);
        
        // Varia a intensidade entre 70% e 100% do valor original
        torchLight.intensity = originalIntensity * (0.7f + flicker * 0.3f);
        
        // Varia levemente a cor para dar mais realismo
        float colorVariation = Mathf.PerlinNoise(flickerTime * 3f, 100f) * 0.1f;
        Color baseColor = lightColor;
        torchLight.color = new Color(
            baseColor.r + colorVariation,
            baseColor.g + colorVariation * 0.5f,
            baseColor.b,
            baseColor.a
        );
    }

    private void OnValidate()
    {
        // Atualiza as configurações no editor
        if (lineRenderer != null)
        {
            ConfigureLineRenderer();
        }
        
        if (torchLight != null)
        {
            torchLight.color = lightColor;
            torchLight.intensity = lightIntensity;
            torchLight.range = lightRange;
            originalIntensity = lightIntensity;
        }
    }
}
