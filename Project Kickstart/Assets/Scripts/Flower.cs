using UnityEngine;

public class Flower : MonoBehaviour
{
    [Header("Flower Settings")]
    [SerializeField] private Transform currentTarget;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Multiple Directions")]
    [SerializeField] private Transform nextFlowerTarget; // Direction to next flower
    [SerializeField] private Transform objectTarget1; // Direction to object 1
    [SerializeField] private Transform objectTarget2; // Direction to object 2
    [SerializeField] private int currentDirectionIndex = 0; // Current direction index (0=flower, 1=obj1, 2=obj2)
    
    [Header("Visual Settings")]
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private GameObject lightEffect; // Visual effect when activated
    
    [Header("Volumetric Light System")]
    [SerializeField] private bool useVolumetricLight = true;
    [SerializeField] private int volumeLayers = 5; // How many volume layers
    [SerializeField] private float maxVolumeWidth = 0.5f; // Maximum volume width
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float particleDensity = 50f;
    
    // Components created automatically
    private LineRenderer[] volumeLines;
    private ParticleSystem lightParticles;
    private Light originLight;
    private Light destinationLight;
    private LensFlare originFlare;
    private LensFlare destinationFlare;
    
    [Header("Flower State")]
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool autoActivateOnStart = false; // Whether to auto-activate at start
    [SerializeField] private bool autoChainActivation = false; // Whether to automatically activate the next flower
    
    // Audio control
    private bool suppressEmissionSound = false; // Flag to suppress emission sound during target changes

    private void Start()
    {
        ConfigureLineRenderer();
        
        // Disable light effect initially
        if (lightEffect != null)
        {
            lightEffect.SetActive(isActivated);
        }
        
        // Set initial target based on current direction
        UpdateCurrentTarget();
        
        // If autoActivateOnStart is checked, activate the flower
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
            // Simple original system
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
        Debug.Log($"[VOLUMETRIC] Creating volumetric light system for {gameObject.name}");
        
        // 1. Create multiple lines for volume
        CreateVolumeLayers();
        
        // 2. Create particle system
        CreateParticleSystem();
        
        // 3. Create point lights
        CreatePointLights();
        
        // 4. Create lens flares
        CreateLensFlares();
        
        Debug.Log($"[VOLUMETRIC] System created successfully!");
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
        
        Debug.Log($"[VOLUMETRIC] {volumeLayers} volume layers created");
    }
    
    private Material CreateGlowMaterial(int layerIndex)
    {
        // Create material with default shader and glow settings
        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        glowMat.name = $"AutoGlow_Layer_{layerIndex}";
        
        // Configure for glow
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
        // Create GameObject for particles
        GameObject particleObj = new GameObject("LightParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        // Add particle system
        lightParticles = particleObj.AddComponent<ParticleSystem>();
        
        // Configure particles
        var main = lightParticles.main;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.startSize = 0.05f;
        main.startColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.7f);
        main.maxParticles = (int)particleDensity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Configure emission
        var emission = lightParticles.emission;
        emission.enabled = false; // Só ativa quando a luz estiver ativa
        emission.rateOverTime = particleDensity / 2f;
        
        // Configure shape (line)
        var shape = lightParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.1f, 0.1f, 1f); // Será ajustado dinamicamente
        
        // Configure velocity
        var velocity = lightParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = 0.2f;
        
        // Configure color over lifetime
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
        
        // Configure size over lifetime
        var sizeOverLifetime = lightParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        Debug.Log($"[VOLUMETRIC] Particle system created");
    }
    
    private void CreatePointLights()
    {
        // Light at origin (flower)
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
        
        // Destination light will be created dynamically when needed
        Debug.Log($"[VOLUMETRIC] Point lights created");
    }
    
    private void CreateLensFlares()
    {
        // Lens flare at origin
        originFlare = originLight.gameObject.AddComponent<LensFlare>();
        
        // Configure flare automatically
        originFlare.brightness = 0.5f;
        originFlare.fadeSpeed = 3f;
        originFlare.color = lightColor;
        
        // Create simple flare texture if none exists
        if (originFlare.flare == null)
        {
            // Unity has default flares we can try to use
            var defaultFlare = Resources.Load<Flare>("Default-Flare");
            if (defaultFlare != null)
            {
                originFlare.flare = defaultFlare;
            }
        }
        
        Debug.Log($"[VOLUMETRIC] Lens flares created");
    }

    public void ReceiveLight()
    {
        ReceiveLightInternal(false);
    }
    
    /// <summary>
    /// Receive light silently (for dimension changes - no sound)
    /// </summary>
    public void ReceiveLightSilently()
    {
        ReceiveLightInternal(true);
    }
    
    /// <summary>
    /// Internal method to receive light with optional sound suppression
    /// </summary>
    private void ReceiveLightInternal(bool suppressSound)
    {
        if (isActivated) return; // Evita ativação múltipla
        
        isActivated = true;
        
        // Play reception sound (indicates correct path in puzzle) - only if not suppressed
        if (AudioManager.Instance != null && !suppressSound)
        {
            AudioManager.Instance.PlayFlowerLightReceptionSound(transform.position, gameObject.name);
        }
        
        // Activate visual effect
        if (lightEffect != null)
        {
            lightEffect.SetActive(true);
        }
        
        // Temporarily suppress emission sound for dimension changes
        bool originalSuppression = suppressEmissionSound;
        suppressEmissionSound = suppressSound;
        
        // Emit light to the next target if it exists
        EmitLightToTarget();
        
        // Restore original suppression state
        suppressEmissionSound = originalSuppression;
        
        Debug.Log($"Flower {gameObject.name} was activated{(suppressSound ? " (silently)" : "")}!");
    }

    private void EmitLightToTarget()
    {
        if (currentTarget == null)
        {
            Debug.Log($"Flor {gameObject.name}: Sem alvo para emitir luz.");
            return;
        }

        // Play emission sound based on target type (only if not suppressed)
        if (AudioManager.Instance != null && !suppressEmissionSound)
        {
            // Check if target is another flower
            Flower targetFlower = currentTarget.GetComponent<Flower>();
            if (targetFlower != null)
            {
                // Target is a flower - correct puzzle path!
                AudioManager.Instance.PlayFlowerEmissionToFlowerSound(transform.position, currentTarget.name);
            }
            else
            {
                // Target is an object (door, box, etc.)
                AudioManager.Instance.PlayFlowerEmissionToObjectSound(transform.position, currentTarget.name);
            }
        }

        if (useVolumetricLight)
        {
            // Volumetric system
            EmitVolumetricLight();
        }
        else
        {
            // Simple original system
            if (lineRenderer == null)
            {
                Debug.Log($"Flor {gameObject.name}: LineRenderer não configurado.");
                return;
            }
            
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentTarget.position);
        }

        // If the target is another flower AND autoChainActivation is active, activate it
        Flower nextFlower = currentTarget.GetComponent<Flower>();
        if (nextFlower != null && autoChainActivation)
        {
            nextFlower.ReceiveLight();
            Debug.Log($"Flower {gameObject.name}: Automatically activated next flower {nextFlower.name}");
        }
        else if (nextFlower != null && !autoChainActivation)
        {
            Debug.Log($"Flower {gameObject.name}: Emitting light to {nextFlower.name}, but not auto-activating");
        }
        else
        {
            Debug.Log($"Flower {gameObject.name}: Target {currentTarget.name} is not a flower.");
        }
    }
    
    private void EmitVolumetricLight()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = currentTarget.position;
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);
        
        Debug.Log($"[VOLUMETRIC] Emitting volumetric light to {currentTarget.name}, distance: {distance:F2}");
        
        // 1. Configure all volume layers
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
        
        // 2. Configure particle system
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
        
        // 3. Activate point lights
        if (originLight != null)
        {
            originLight.enabled = true;
        }
        
        // 4. Create destination light if necessary
        CreateDestinationLight(endPos);
        
        // 5. Activate lens flares
        if (originFlare != null)
        {
            originFlare.enabled = true;
        }
    }
    
    private void CreateDestinationLight(Vector3 position)
    {
        // Check if there is already a light on the target
        if (currentTarget.GetComponent<Light>() == null)
        {
            // Create temporary light at destination
            GameObject destLightObj = new GameObject("DestinationLight_Temp");
            destLightObj.transform.position = position;
            
            destinationLight = destLightObj.AddComponent<Light>();
            destinationLight.type = LightType.Point;
            destinationLight.color = lightColor;
            destinationLight.intensity = lightIntensity * 0.7f;
            destinationLight.range = 2f;
            destinationLight.shadows = LightShadows.Soft;
            
            // Add lens flare
            destinationFlare = destLightObj.AddComponent<LensFlare>();
            destinationFlare.brightness = 0.3f;
            destinationFlare.fadeSpeed = 3f;
            destinationFlare.color = lightColor;
            
            // Auto-destroy after some time
            Destroy(destLightObj, 10f);
        }
    }

    public void ChangeTarget(Transform newTarget)
    {
        ChangeTargetInternal(newTarget, false);
    }
    
    /// <summary>
    /// Change target without playing emission sound (for platform deactivation)
    /// </summary>
    public void ChangeTargetSilently(Transform newTarget)
    {
        ChangeTargetInternal(newTarget, true);
    }
    
    /// <summary>
    /// Internal method to change target with optional sound suppression
    /// </summary>
    private void ChangeTargetInternal(Transform newTarget, bool suppressSound)
    {
        // Remove previous light if it was active
        if (isActivated && lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        currentTarget = newTarget;
        
        Debug.Log($"Flor {gameObject.name}: Alvo mudado para {(newTarget != null ? newTarget.name : "null")}{(suppressSound ? " (silently)" : "")}");

        // If the flower was already activated, emit light to the new target
        if (isActivated)
        {
            // Temporarily suppress sound if requested
            bool originalSuppression = suppressEmissionSound;
            suppressEmissionSound = suppressSound;
            
            EmitLightToTarget();
            
            // Restore original suppression state
            suppressEmissionSound = originalSuppression;
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
            // Simple system
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
        
        if (lightEffect != null)
        {
            lightEffect.SetActive(false);
        }
        
        Debug.Log($"Flower {gameObject.name} was deactivated!");
    }
    
    private void DeactivateVolumetricLight()
    {
        Debug.Log($"[VOLUMETRIC] Desativando sistema volumétrico de {gameObject.name}");
        
        // 1. Deactivate all volume layers
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
        
        // 2. Deactivate particle system
        if (lightParticles != null)
        {
            var emission = lightParticles.emission;
            emission.enabled = false;
        }
        
        // 3. Deactivate point lights
        // 3. Deactivate point lights
        if (originLight != null)
        {
            originLight.enabled = false;
        }
        
        // if (destinationLight != null)
        // {
        //     destinationLight.enabled = false;
        // }
        
        // 4. Desativar lens flares
        // if (originFlare != null)
        // {
        //     originFlare.enabled = false;
        // }
        
        // if (destinationFlare != null)
        // {
        //     destinationFlare.enabled = false;
        // }
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
        // Update settings in the editor
        if (lineRenderer != null)
        {
            ConfigureLineRenderer();
        }
    }

    // Method to update current target based on direction
    private void UpdateCurrentTarget()
    {
        Transform newTarget = null;
        
        switch (currentDirectionIndex)
        {
            case 0:
                newTarget = nextFlowerTarget;
                Debug.Log($"Flower {gameObject.name}: Direction changed to NEXT FLOWER ({(nextFlowerTarget != null ? nextFlowerTarget.name : "null")})");
                break;
            case 1:
                newTarget = objectTarget1;
                Debug.Log($"Flower {gameObject.name}: Direction changed to OBJECT 1 ({(objectTarget1 != null ? objectTarget1.name : "null")})");
                break;
            case 2:
                newTarget = objectTarget2;
                Debug.Log($"Flower {gameObject.name}: Direction changed to OBJECT 2 ({(objectTarget2 != null ? objectTarget2.name : "null")})");
                break;
        }
        
        currentTarget = newTarget;
        
        // If the flower is already active, update the light line
        if (isActivated)
        {
            EmitLightToTarget();
        }
    }
    
    // Method to cycle to the next direction
    public void CycleToNextDirection()
    {
        currentDirectionIndex = (currentDirectionIndex + 1) % 3; // Cycle between 0, 1, 2
        UpdateCurrentTarget();
        Debug.Log($"Flower {gameObject.name}: Cycled to direction {currentDirectionIndex}");
    }
    
    // Method to set a specific direction
    public void SetDirection(int directionIndex)
    {
        if (directionIndex >= 0 && directionIndex <= 2)
        {
            currentDirectionIndex = directionIndex;
            UpdateCurrentTarget();
            Debug.Log($"Flower {gameObject.name}: Direction set to {currentDirectionIndex}");
        }
        else
        {
            Debug.LogWarning($"Flower {gameObject.name}: Invalid direction index: {directionIndex}");
        }
    }
    
    // Properties for external access
    public bool IsActivated => isActivated;
    public Transform CurrentTarget => currentTarget;
    public int CurrentDirectionIndex => currentDirectionIndex;
    public string CurrentDirectionName
    {
        get
        {
            switch (currentDirectionIndex)
            {
                case 0: return "Next Flower";
                case 1: return "Object 1";
                case 2: return "Object 2";
                default: return "Unknown";
            }
        }
    }
}
