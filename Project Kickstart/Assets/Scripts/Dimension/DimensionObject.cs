using UnityEngine;

/// <summary>
/// Script para objetos que só existem em uma dimensão específica.
/// Exemplo: uma porta que só existe na Dimensão A, uma plataforma que só existe na Dimensão B.
/// </summary>
public class DimensionObject : MonoBehaviour
{
    [Header("Configuração de Dimensão")]
    [SerializeField] private DimensionType activeDimension = DimensionType.DimensionA;
    [SerializeField] private bool startActive = true;
    
    [Header("Modo de Ocultação")]
    [SerializeField] private HidingMode hidingMode = HidingMode.SetActive;
    [SerializeField] private bool disableColliders = true;
    [SerializeField] private bool disableRenderers = true;
    
    [Header("Animação de Transição")]
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public enum HidingMode
    {
        SetActive,      // Ativa/desativa o GameObject completamente
        SetVisible,     // Apenas controla visibilidade (renderers e colliders)
        SetTransparent  // Usa transparência para esconder/mostrar
    }

    // Componentes para controle
    private Collider2D[] colliders2D;
    private Collider[] colliders3D;
    private Renderer[] renderers;
    private SpriteRenderer[] spriteRenderers;
    
    // Estado e animação
    private bool isCurrentlyActive = false;
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Vector3 originalScale;
    private Color[] originalColors;
    
    // Valores para transição
    private bool targetActiveState;

    private void Awake()
    {
        // Cache dos componentes
        colliders2D = GetComponentsInChildren<Collider2D>();
        colliders3D = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        // Salva escala e cores originais
        originalScale = transform.localScale;
        CacheOriginalColors();
    }

    private void Start()
    {
        // Define estado inicial
        isCurrentlyActive = startActive;
        
        // Se o DimensionManager já existe, aplica a dimensão atual imediatamente
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension, false);
        }
        else
        {
            // Aplica estado inicial se não há manager ainda
            SetObjectState(isCurrentlyActive, false);
        }
        
        // Inscreve-se no evento de mudança de dimensão
        DimensionManager.OnDimensionChanged += OnDimensionChanged;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: DimensionObject iniciado para dimensão {activeDimension}");
        }
    }

    private void Update()
    {
        // Processa animação de transição se estiver ativa
        if (isTransitioning && useTransitionAnimation)
        {
            ProcessTransition();
        }
    }

    /// <summary>
    /// Armazena as cores originais dos sprites
    /// </summary>
    private void CacheOriginalColors()
    {
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    /// <summary>
    /// Chamado quando a dimensão muda
    /// </summary>
    /// <param name="newDimension">Nova dimensão ativa</param>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        ApplyDimensionState(newDimension, useTransitionAnimation);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Dimensão mudou para {newDimension}. Objeto deve estar {(newDimension == activeDimension ? "ativo" : "inativo")}");
        }
    }

    /// <summary>
    /// Aplica o estado correspondente à dimensão
    /// </summary>
    /// <param name="dimension">Dimensão atual</param>
    /// <param name="animated">Se deve usar animação</param>
    private void ApplyDimensionState(DimensionType dimension, bool animated = true)
    {
        bool shouldBeActive = (dimension == activeDimension);
        
        if (isCurrentlyActive == shouldBeActive && !isTransitioning)
            return;

        SetObjectState(shouldBeActive, animated);
    }

    /// <summary>
    /// Define o estado ativo/inativo do objeto
    /// </summary>
    /// <param name="active">Se o objeto deve estar ativo</param>
    /// <param name="animated">Se deve usar animação</param>
    private void SetObjectState(bool active, bool animated = true)
    {
        if (animated && useTransitionAnimation && Application.isPlaying)
        {
            StartTransition(active);
        }
        else
        {
            // Aplicação imediata
            ApplyStateImmediate(active);
        }
        
        isCurrentlyActive = active;
    }

    /// <summary>
    /// Aplica o estado imediatamente sem animação
    /// </summary>
    /// <param name="active">Estado ativo</param>
    private void ApplyStateImmediate(bool active)
    {
        switch (hidingMode)
        {
            case HidingMode.SetActive:
                gameObject.SetActive(active);
                break;
                
            case HidingMode.SetVisible:
                SetColliders(active && disableColliders);
                SetRenderers(active && disableRenderers);
                break;
                
            case HidingMode.SetTransparent:
                SetColliders(active && disableColliders);
                SetTransparency(active ? 1f : 0f);
                break;
        }
        
        // Restaura escala original se necessário
        if (active)
        {
            transform.localScale = originalScale;
        }
    }

    /// <summary>
    /// Inicia uma transição animada
    /// </summary>
    /// <param name="targetActive">Estado alvo</param>
    private void StartTransition(bool targetActive)
    {
        if (isTransitioning)
        {
            CompleteTransition();
        }

        targetActiveState = targetActive;
        transitionTimer = 0f;
        isTransitioning = true;
        
        // Se está aparecendo, garante que o objeto esteja visível para a animação
        if (targetActive)
        {
            switch (hidingMode)
            {
                case HidingMode.SetActive:
                    if (!gameObject.activeInHierarchy)
                        gameObject.SetActive(true);
                    break;
                    
                case HidingMode.SetVisible:
                    SetRenderers(true);
                    break;
                    
                case HidingMode.SetTransparent:
                    // Transparency será controlada na animação
                    break;
            }
        }
    }

    /// <summary>
    /// Processa a animação de transição
    /// </summary>
    private void ProcessTransition()
    {
        transitionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(transitionTimer / transitionDuration);
        
        if (progress >= 1f)
        {
            CompleteTransition();
            return;
        }

        // Inverte o progresso se está desaparecendo
        float animationProgress = targetActiveState ? progress : (1f - progress);
        
        // Aplica curvas de animação
        float scaleValue = scaleCurve.Evaluate(animationProgress);
        float alphaValue = alphaCurve.Evaluate(animationProgress);
        
        // Animação de escala
        transform.localScale = originalScale * scaleValue;
        
        // Animação de transparência para modo transparente
        if (hidingMode == HidingMode.SetTransparent)
        {
            SetTransparency(alphaValue);
        }
        else if (hidingMode == HidingMode.SetVisible)
        {
            // Para modo visível, usa apenas transparência como animação
            SetTransparency(alphaValue);
        }
    }

    /// <summary>
    /// Completa a transição
    /// </summary>
    private void CompleteTransition()
    {
        isTransitioning = false;
        ApplyStateImmediate(targetActiveState);
    }

    /// <summary>
    /// Controla a transparência dos sprites
    /// </summary>
    /// <param name="alpha">Valor de alpha (0-1)</param>
    private void SetTransparency(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color color = originalColors[i];
                color.a = alpha;
                spriteRenderers[i].color = color;
            }
        }
    }

    /// <summary>
    /// Ativa/desativa colliders
    /// </summary>
    /// <param name="enabled">Estado dos colliders</param>
    private void SetColliders(bool enabled)
    {
        foreach (var collider in colliders2D)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
        
        foreach (var collider in colliders3D)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    /// <summary>
    /// Ativa/desativa renderers
    /// </summary>
    /// <param name="enabled">Estado dos renderers</param>
    private void SetRenderers(bool enabled)
    {
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }

    /// <summary>
    /// Força a aplicação do estado para a dimensão atual
    /// </summary>
    [ContextMenu("Aplicar Estado Atual")]
    public void ForceApplyCurrentState()
    {
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension, false);
        }
    }

    /// <summary>
    /// Alterna a dimensão ativa deste objeto
    /// </summary>
    [ContextMenu("Alternar Dimensão Ativa")]
    public void ToggleActiveDimension()
    {
        activeDimension = activeDimension == DimensionType.DimensionA ? 
            DimensionType.DimensionB : DimensionType.DimensionA;
            
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionState(DimensionManager.Instance.CurrentDimension, false);
        }
    }

    private void OnDestroy()
    {
        // Remove inscrição do evento ao destruir
        DimensionManager.OnDimensionChanged -= OnDimensionChanged;
    }

    private void OnDrawGizmosSelected()
    {
        // Desenha indicador visual da dimensão ativa
        Gizmos.color = activeDimension == DimensionType.DimensionA ? Color.red : Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
