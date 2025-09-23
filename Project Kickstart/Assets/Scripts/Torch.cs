using UnityEngine;

public class Torch : MonoBehaviour
{
    [Header("Torch Settings")]
    [SerializeField] private Transform targetFlower;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Visual Settings")]
    [SerializeField] private Color lightColor = Color.yellow;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    
    [Header("Natural Lighting")]
    [SerializeField] private Light torchLight; // Torch light
    [SerializeField] private bool createTorchLight = true;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightRange = 10f;
    [SerializeField] private bool flickerEffect = true;
    
    [Header("Flame Effect")]
    [SerializeField] private ParticleSystem flameParticles; // Flame particles
    [SerializeField] private bool createFlameEffect = true;
    
    [Header("Activation Control")]
    [SerializeField] private bool autoEmitOnStart = true; // Whether to emit light automatically on start
    
    [Header("Volumetric Light System")]
    [SerializeField] private bool useVolumetricLight = true;
    [SerializeField] private int volumeLayers = 5;
    [SerializeField] private float maxVolumeWidth = 0.5f;
    
    // Components created automatically
    private LineRenderer[] volumeLines;
    private ParticleSystem lightParticles;
    
    // Variables for flicker effect
    private float originalIntensity;
    private float flickerTime;

    private void Start()
    {
        ConfigureLineRenderer();
        SetupTorchLight();
        SetupFlameEffect();
        
        // Only emit light if autoEmitOnStart is enabled
        if (autoEmitOnStart)
        {
            EmitLightVisualOnly(); // Emit visuals only, without activating the flower
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
            
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    
    private void CreateVolumetricLightSystem()
    {
        Debug.Log($"[TORCH VOLUMETRIC] Creating volumetric light system for {gameObject.name}");
        
        CreateVolumeLayers();
        CreateParticleSystem();
        
        Debug.Log($"[TORCH VOLUMETRIC] System created successfully!");
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
        
        Debug.Log($"[TORCH VOLUMETRIC] {volumeLayers} volume layers created");
    }
    
    private Material CreateGlowMaterial(int layerIndex)
    {
        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        glowMat.name = $"TorchAutoGlow_Layer_{layerIndex}";
        
        // Use the torch color (yellow), not purple!
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
        main.startColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.7f); // Use torch color
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
        
        Debug.Log($"[TORCH VOLUMETRIC] Particle system created with color {lightColor}");
    }

    private void EmitLight()
    {
        EmitLightInternal(false);
    }
    
    /// <summary>
    /// Emit light silently (for dimension changes)
    /// </summary>
    private void EmitLightSilently()
    {
        EmitLightInternal(true);
    }
    
    /// <summary>
    /// Internal method to emit light with optional sound suppression
    /// </summary>
    private void EmitLightInternal(bool suppressSound)
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
                Debug.LogWarning("Torch: LineRenderer not configured!");
                return;
            }
            
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetFlower.position);
        }

        // Activate the first flower (silently if requested)
        Flower flower = targetFlower.GetComponent<Flower>();
        if (flower != null)
        {
            if (suppressSound)
            {
                flower.ReceiveLightSilently();
            }
            else
            {
                flower.ReceiveLight();
            }
        }
        else
        {
            Debug.LogWarning("Torch: targetFlower does not have Flower component!");
        }
    }
    
    private void EmitVolumetricLight()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = targetFlower.position;
        float distance = Vector3.Distance(startPos, endPos);
        
        Debug.Log($"[TORCH VOLUMETRIC] Emitting volumetric light to {targetFlower.name}, color: {lightColor}");
        
        // Configure all volume layers
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
        
        // Configure the particle system
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
        // Update light system
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
        
        // Light flicker effect
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
        
        // Update volume layers
        if (volumeLines != null)
        {
            for (int i = 0; i < volumeLines.Length; i++)
            {
                if (volumeLines[i] != null && volumeLines[i].enabled)
                {
                    volumeLines[i].SetPosition(0, startPos);
                    volumeLines[i].SetPosition(1, endPos);
                    
                    // Pulsating animation
                    float pulse = Mathf.Sin(Time.time * 1.5f + i * 0.2f) * 0.1f + 1f;
                    Color currentColor = volumeLines[i].startColor;
                    Color baseColor = lightColor;
                    baseColor.a = currentColor.a;
                    volumeLines[i].startColor = baseColor * pulse;
                    volumeLines[i].endColor = baseColor * pulse;
                }
            }
        }
        
        // Update particles
        if (lightParticles != null && lightParticles.emission.enabled)
        {
            Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
            lightParticles.transform.position = midPoint;
            lightParticles.transform.LookAt(endPos);
            
            var shape = lightParticles.shape;
            shape.scale = new Vector3(0.2f, 0.2f, distance);
        }
    }

    // Method to configure the target flower via script
    public void SetTargetFlower(Transform newTarget)
    {
        SetTargetFlowerInternal(newTarget, false);
    }
    
    /// <summary>
    /// Set target flower without activating it (for platform deactivation)
    /// </summary>
    public void SetTargetFlowerSilently(Transform newTarget)
    {
        SetTargetFlowerInternal(newTarget, true);
    }
    
    /// <summary>
    /// Internal method to set target with optional activation suppression
    /// </summary>
    private void SetTargetFlowerInternal(Transform newTarget, bool suppressActivation)
    {
        targetFlower = newTarget;
        
        if (!suppressActivation)
        {
            EmitLight();
        }
        else
        {
            // Just emit visual light without activating the flower
            EmitLightVisualOnly();
        }
    }
    
    // Method to manually activate the torch
    public void ActivateTorch()
    {
        ActivateTorchInternal(false);
    }
    
    /// <summary>
    /// Activate torch silently (for dimension changes - no sound)
    /// </summary>
    public void ActivateTorchSilently()
    {
        ActivateTorchInternal(true);
    }
    
    /// <summary>
    /// Internal method to activate torch with optional sound suppression
    /// </summary>
    private void ActivateTorchInternal(bool suppressSound)
    {
        if (suppressSound)
        {
            EmitLightSilently();
        }
        else
        {
            EmitLight();
        }
    }
    
    // Method to emit only the light visual (without activating the flower)
    public void EmitLightVisualOnly()
    {
        if (targetFlower == null)
        {
            Debug.LogWarning("Torch: targetFlower not configured!");
            return;
        }

        Debug.Log($"[TORCH] Emitting initial visual light to {targetFlower.name} (without activating)");

        if (useVolumetricLight)
        {
            EmitVolumetricLight();
        }
        else
        {
            // Simple original system
            if (lineRenderer == null)
            {
                Debug.LogWarning("Torch: LineRenderer not configured!");
                return;
            }
            
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetFlower.position);
        }
        
        // Does NOT activate the flower - only shows the visual light
    }
    
    // Property to access the current target
    public Transform CurrentTarget => targetFlower;

    private void SetupTorchLight()
    {
        if (createTorchLight && torchLight == null)
        {
            // Automatically creates a Light component
            GameObject lightObject = new GameObject("TorchLight");
            lightObject.transform.SetParent(transform);
            lightObject.transform.localPosition = Vector3.up * 0.5f; // Slightly above the torch
            
            torchLight = lightObject.AddComponent<Light>();
        }
        
        if (torchLight != null)
        {
            torchLight.type = LightType.Point;
            torchLight.color = lightColor;
            torchLight.intensity = lightIntensity;
            torchLight.range = lightRange;
            torchLight.shadows = LightShadows.Soft; // Soft shadows
            
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
        
        // Create a flicker effect using Perlin noise
        float flicker = Mathf.PerlinNoise(flickerTime * 5f, 0f);
        flicker = Mathf.Clamp01(flicker);
        
        // Vary intensity between 70% and 100% of the original value
        torchLight.intensity = originalIntensity * (0.7f + flicker * 0.3f);
        
        // Slightly vary the color to add realism
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
        // Update settings in the editor
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
