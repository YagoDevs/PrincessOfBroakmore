using UnityEngine;

/// <summary>
/// Script para objetos que mudam de sprite dependendo da dimensão atual.
/// Exemplo: uma árvore que tem aparência diferente em cada dimensão.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DimensionSprite : MonoBehaviour
{
    [Header("Sprites por Dimensão")]
    [SerializeField] private Sprite spriteForDimensionA;
    [SerializeField] private Sprite spriteForDimensionB;
    
    [Header("Configurações Opcionais")]
    [SerializeField] private bool changeColor = false;
    [SerializeField] private Color colorForDimensionA = Color.white;
    [SerializeField] private Color colorForDimensionB = Color.white;
    
    [Header("Animação de Transição")]
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private SpriteRenderer spriteRenderer;
    private bool isTransitioning = false;
    
    // Variáveis para animação
    private float transitionTimer = 0f;
    private Sprite targetSprite;
    private Color targetColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Se o DimensionManager já existe, aplica a dimensão atual imediatamente
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionSprite(DimensionManager.Instance.CurrentDimension, false);
        }
        
        // Inscreve-se no evento de mudança de dimensão
        DimensionManager.OnDimensionChanged += OnDimensionChanged;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: DimensionSprite iniciado");
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
    /// Chamado quando a dimensão muda
    /// </summary>
    /// <param name="newDimension">Nova dimensão ativa</param>
    private void OnDimensionChanged(DimensionType newDimension)
    {
        ApplyDimensionSprite(newDimension, useTransitionAnimation);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Dimensão mudou para {newDimension}");
        }
    }

    /// <summary>
    /// Aplica o sprite e cor correspondentes à dimensão
    /// </summary>
    /// <param name="dimension">Dimensão para aplicar</param>
    /// <param name="animated">Se deve usar animação de transição</param>
    private void ApplyDimensionSprite(DimensionType dimension, bool animated = true)
    {
        // Determina o sprite e cor para a dimensão
        Sprite newSprite = dimension == DimensionType.DimensionA ? spriteForDimensionA : spriteForDimensionB;
        Color newColor = changeColor ? 
            (dimension == DimensionType.DimensionA ? colorForDimensionA : colorForDimensionB) : 
            spriteRenderer.color;

        // Verifica se há mudança necessária
        if (spriteRenderer.sprite == newSprite && spriteRenderer.color == newColor)
            return;

        if (animated && useTransitionAnimation && Application.isPlaying)
        {
            StartTransition(newSprite, newColor);
        }
        else
        {
            // Aplicação imediata
            spriteRenderer.sprite = newSprite;
            if (changeColor)
            {
                spriteRenderer.color = newColor;
            }
        }
    }

    /// <summary>
    /// Inicia uma transição animada para o novo sprite/cor
    /// </summary>
    /// <param name="newSprite">Novo sprite</param>
    /// <param name="newColor">Nova cor</param>
    private void StartTransition(Sprite newSprite, Color newColor)
    {
        if (isTransitioning)
        {
            // Se já está em transição, completa a atual imediatamente
            CompleteTransition();
        }

        targetSprite = newSprite;
        targetColor = newColor;
        transitionTimer = 0f;
        isTransitioning = true;
    }

    /// <summary>
    /// Processa a animação de transição
    /// </summary>
    private void ProcessTransition()
    {
        transitionTimer += Time.deltaTime;
        float progress = transitionTimer / transitionDuration;
        
        if (progress >= 1f)
        {
            CompleteTransition();
            return;
        }

        // Aplica curva de animação
        float curveValue = transitionCurve.Evaluate(progress);

        // Animação de fade para trocar sprite no meio da transição
        if (progress >= 0.5f && spriteRenderer.sprite != targetSprite)
        {
            spriteRenderer.sprite = targetSprite;
        }

        // Interpola a cor se necessário
        if (changeColor)
        {
            Color startColor = progress < 0.5f ? spriteRenderer.color : targetColor;
            Color endColor = targetColor;
            
            // Fade out e fade in
            float alpha = progress < 0.5f ? 
                Mathf.Lerp(1f, 0f, curveValue * 2f) : 
                Mathf.Lerp(0f, 1f, (curveValue - 0.5f) * 2f);
                
            Color currentColor = Color.Lerp(startColor, endColor, progress);
            currentColor.a = alpha;
            spriteRenderer.color = currentColor;
        }
        else
        {
            // Apenas fade do sprite
            float alpha = progress < 0.5f ? 
                Mathf.Lerp(1f, 0f, curveValue * 2f) : 
                Mathf.Lerp(0f, 1f, (curveValue - 0.5f) * 2f);
                
            Color currentColor = spriteRenderer.color;
            currentColor.a = alpha;
            spriteRenderer.color = currentColor;
        }
    }

    /// <summary>
    /// Completa a transição e restaura valores finais
    /// </summary>
    private void CompleteTransition()
    {
        isTransitioning = false;
        spriteRenderer.sprite = targetSprite;
        
        if (changeColor)
        {
            spriteRenderer.color = targetColor;
        }
        else
        {
            Color currentColor = spriteRenderer.color;
            currentColor.a = 1f;
            spriteRenderer.color = currentColor;
        }
    }

    /// <summary>
    /// Força a aplicação imediata da dimensão atual
    /// </summary>
    [ContextMenu("Aplicar Dimensão Atual")]
    public void ForceApplyCurrentDimension()
    {
        if (DimensionManager.Instance != null)
        {
            ApplyDimensionSprite(DimensionManager.Instance.CurrentDimension, false);
        }
    }

    /// <summary>
    /// Valida se os sprites foram configurados corretamente
    /// </summary>
    private void OnValidate()
    {
        if (spriteForDimensionA == null)
        {
            Debug.LogWarning($"{gameObject.name}: Sprite para Dimensão A não foi definido!");
        }
        
        if (spriteForDimensionB == null)
        {
            Debug.LogWarning($"{gameObject.name}: Sprite para Dimensão B não foi definido!");
        }
    }

    private void OnDestroy()
    {
        // Remove inscrição do evento ao destruir
        DimensionManager.OnDimensionChanged -= OnDimensionChanged;
    }
}
