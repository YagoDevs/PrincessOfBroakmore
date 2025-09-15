using System;
using UnityEngine;

/// <summary>
/// Enumeração para identificar as dimensões disponíveis
/// </summary>
public enum DimensionType
{
    DimensionA,
    DimensionB
}

/// <summary>
/// Gerenciador principal das dimensões do jogo.
/// Controla o estado global da dimensão atual e notifica todos os objetos relevantes quando há mudança.
/// </summary>
public class DimensionManager : MonoBehaviour
{
    [Header("Configurações de Dimensão")]
    [SerializeField] private DimensionType currentDimension = DimensionType.DimensionA;
    [SerializeField] private KeyCode switchDimensionKey = KeyCode.Tab;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Singleton pattern para acesso global
    public static DimensionManager Instance { get; private set; }
    
    // Propriedade pública para acessar a dimensão atual
    public DimensionType CurrentDimension => currentDimension;
    
    // Evento que é disparado quando a dimensão muda
    public static event Action<DimensionType> OnDimensionChanged;
    
    // Evento específico para objetos que precisam saber sobre mudanças
    public static event Action<DimensionType, DimensionType> OnDimensionSwitched;

    private void Awake()
    {
        // Implementação do Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Notifica todos os objetos sobre a dimensão inicial
        NotifyDimensionChange();
        
        if (showDebugInfo)
        {
            Debug.Log($"DimensionManager iniciado. Dimensão atual: {currentDimension}");
        }
    }

    private void Update()
    {
        // Verifica se o jogador pressionou a tecla para trocar de dimensão
        if (Input.GetKeyDown(switchDimensionKey))
        {
            SwitchDimension();
        }
    }

    /// <summary>
    /// Troca para a outra dimensão
    /// </summary>
    public void SwitchDimension()
    {
        DimensionType previousDimension = currentDimension;
        
        // Alterna entre as dimensões
        currentDimension = currentDimension == DimensionType.DimensionA 
            ? DimensionType.DimensionB 
            : DimensionType.DimensionA;
        
        if (showDebugInfo)
        {
            Debug.Log($"Dimensão alterada de {previousDimension} para {currentDimension}");
        }
        
        // Notifica todos os objetos sobre a mudança
        NotifyDimensionChange();
        NotifyDimensionSwitch(previousDimension, currentDimension);
    }

    /// <summary>
    /// Define uma dimensão específica
    /// </summary>
    /// <param name="newDimension">Nova dimensão a ser definida</param>
    public void SetDimension(DimensionType newDimension)
    {
        if (currentDimension == newDimension)
            return;
            
        DimensionType previousDimension = currentDimension;
        currentDimension = newDimension;
        
        if (showDebugInfo)
        {
            Debug.Log($"Dimensão definida de {previousDimension} para {currentDimension}");
        }
        
        NotifyDimensionChange();
        NotifyDimensionSwitch(previousDimension, currentDimension);
    }

    /// <summary>
    /// Notifica todos os ouvintes sobre a mudança de dimensão
    /// </summary>
    private void NotifyDimensionChange()
    {
        OnDimensionChanged?.Invoke(currentDimension);
    }

    /// <summary>
    /// Notifica sobre a troca específica de dimensão (com dimensão anterior e nova)
    /// </summary>
    private void NotifyDimensionSwitch(DimensionType from, DimensionType to)
    {
        OnDimensionSwitched?.Invoke(from, to);
    }

    /// <summary>
    /// Verifica se a dimensão atual é a especificada
    /// </summary>
    /// <param name="dimension">Dimensão para verificar</param>
    /// <returns>True se for a dimensão atual</returns>
    public bool IsCurrentDimension(DimensionType dimension)
    {
        return currentDimension == dimension;
    }

    private void OnDestroy()
    {
        // Limpa eventos ao destruir o objeto
        OnDimensionChanged = null;
        OnDimensionSwitched = null;
    }

    private void OnGUI()
    {
        if (!showDebugInfo)
            return;
            
        // Interface de debug simples
        GUI.Box(new Rect(10, 10, 200, 60), "Dimension Manager");
        GUI.Label(new Rect(20, 30, 180, 20), $"Dimensão Atual: {currentDimension}");
        GUI.Label(new Rect(20, 50, 180, 20), $"Tecla: {switchDimensionKey}");
    }
}
