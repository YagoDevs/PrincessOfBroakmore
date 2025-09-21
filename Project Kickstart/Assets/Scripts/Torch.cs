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
    
    // Variáveis para efeito de tremulação
    private float originalIntensity;
    private float flickerTime;

    private void Start()
    {
        ConfigureLineRenderer();
        SetupTorchLight();
        SetupFlameEffect();
        EmitLight();
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
        
        // Configurar para não ser afetado por iluminação
        lineRenderer.receiveShadows = false;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void EmitLight()
    {
        if (targetFlower == null || lineRenderer == null)
        {
            Debug.LogWarning("Torch: targetFlower ou LineRenderer não configurado!");
            return;
        }

        // Define as posições da linha de luz
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, targetFlower.position);

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

    private void Update()
    {
        // Atualiza a posição da linha caso os objetos se movam
        if (targetFlower != null && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetFlower.position);
        }
        
        // Efeito de tremulação da luz
        if (flickerEffect && torchLight != null)
        {
            FlickerLight();
        }
    }

    // Método para configurar a flor alvo via script
    public void SetTargetFlower(Transform newTarget)
    {
        targetFlower = newTarget;
        EmitLight();
    }

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
