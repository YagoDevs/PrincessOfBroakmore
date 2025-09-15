using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script para objetos móveis que mantêm posições independentes em cada dimensão.
/// Cada caixa salva suas coordenadas separadamente para A e B.
/// </summary>
public class DimensionBox : MonoBehaviour
{
    [Header("Configurações da Caixa")]
    [SerializeField] private bool canBeMoved = true;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool usePhysics = true;
    
    [Header("Posições por Dimensão")]
    [SerializeField] private Vector3 positionInDimensionA;
    [SerializeField] private Vector3 positionInDimensionB;
    [SerializeField] private bool useRotation = false;
    [SerializeField] private Vector3 rotationInDimensionA;
    [SerializeField] private Vector3 rotationInDimensionB;
    
    [Header("Transição entre Dimensões")]
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Detecção de Movimento")]
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float savePositionDelay = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showPositionGizmos = true;

    // Estado da caixa
    private DimensionType currentDimension;
    private bool isTransitioning = false;
    private bool isBeingMoved = false;
    private Vector3 lastSavedPosition;
    private float timeSinceLastMovement = 0f;
    
    // Componentes
    private Rigidbody2D rb2D;
    private Rigidbody rb3D;
    private bool hasPhysics = false;
    
    // Transição
    private float transitionTimer = 0f;
    private Vector3 transitionStartPosition;
    private Vector3 transitionTargetPosition;
    private Vector3 transitionStartRotation;
    private Vector3 transitionTargetRotation;
    
    // Sistema de salvamento automático
    private Coroutine savePositionCoroutine;

    private void Awake()
    {
        // Cache dos componentes de física
        rb2D = GetComponent<Rigidbody2D>();
        rb3D = GetComponent<Rigidbody>();
        hasPhysics = (rb2D != null || rb3D != null) && usePhysics;
        
        // Inicializa posições se não foram definidas
        if (positionInDimensionA == Vector3.zero)
            positionInDimensionA = transform.position;
        if (positionInDimensionB == Vector3.zero)
            positionInDimensionB = transform.position;
            
        if (useRotation)
        {
            if (rotationInDimensionA == Vector3.zero)
                rotationInDimensionA = transform.eulerAngles;
            if (rotationInDimensionB == Vector3.zero)
                rotationInDimensionB = transform.eulerAngles;
        }
    }

    private void Start()
    {
        // Define dimensão atual
        currentDimension = DimensionManager.Instance != null ? 
            DimensionManager.Instance.CurrentDimension : DimensionType.DimensionA;
        
        // Aplica posição inicial
        ApplyDimensionPosition(currentDimension, false);
        lastSavedPosition = transform.position;
        
        // Inscreve-se nos eventos de mudança de dimensão
        DimensionManager.OnDimensionSwitched += OnDimensionSwitched;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: DimensionBox iniciada na dimensão {currentDimension}");
        }
    }

    private void Update()
    {
        // Processa animação de transição
        if (isTransitioning)
        {
            ProcessTransition();
        }
        
        // Detecta movimento da caixa (agora sempre, mesmo em transição)
        if (canBeMoved)
        {
            DetectMovement();
        }
    }

    /// <summary>
    /// Detecta se a caixa foi movida e inicia timer para salvamento
    /// </summary>
    private void DetectMovement()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastSavedPosition);
        
        if (distanceMoved > movementThreshold)
        {
            if (!isBeingMoved)
            {
                isBeingMoved = true;
                if (showDebugInfo)
                {
                    Debug.Log($"{gameObject.name}: Movimento detectado na dimensão {currentDimension}");
                }
            }
            
            timeSinceLastMovement = 0f;
        }
        else if (isBeingMoved)
        {
            timeSinceLastMovement += Time.deltaTime;
            
            if (timeSinceLastMovement >= savePositionDelay)
            {
                SaveCurrentPosition();
                isBeingMoved = false;
                timeSinceLastMovement = 0f;
            }
        }
    }

    /// <summary>
    /// Salva a posição atual na dimensão ativa
    /// </summary>
    private void SaveCurrentPosition()
    {
        Vector3 currentPos = transform.position;
        Vector3 currentRot = transform.eulerAngles;
        
        if (currentDimension == DimensionType.DimensionA)
        {
            positionInDimensionA = currentPos;
            if (useRotation) rotationInDimensionA = currentRot;
        }
        else
        {
            positionInDimensionB = currentPos;
            if (useRotation) rotationInDimensionB = currentRot;
        }
        
        lastSavedPosition = currentPos;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Posição salva na dimensão {currentDimension}: {currentPos}");
        }
    }

    /// <summary>
    /// Chamado quando a dimensão muda
    /// </summary>
    /// <param name="fromDimension">Dimensão anterior</param>
    /// <param name="toDimension">Nova dimensão</param>
    private void OnDimensionSwitched(DimensionType fromDimension, DimensionType toDimension)
    {
        // Salva posição atual antes de trocar
        if (isBeingMoved)
        {
            SaveCurrentPosition();
            isBeingMoved = false;
        }
        
        currentDimension = toDimension;
        ApplyDimensionPosition(toDimension, useTransitionAnimation);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Dimensão mudou de {fromDimension} para {toDimension}");
        }
    }

    /// <summary>
    /// Aplica a posição correspondente à dimensão
    /// </summary>
    /// <param name="dimension">Dimensão alvo</param>
    /// <param name="animated">Se deve usar animação</param>
    private void ApplyDimensionPosition(DimensionType dimension, bool animated = true)
    {
        Vector3 targetPosition = dimension == DimensionType.DimensionA ? 
            positionInDimensionA : positionInDimensionB;
        Vector3 targetRotation = useRotation ? 
            (dimension == DimensionType.DimensionA ? rotationInDimensionA : rotationInDimensionB) :
            transform.eulerAngles;

        if (animated && useTransitionAnimation && Application.isPlaying)
        {
            StartTransition(targetPosition, targetRotation);
        }
        else
        {
            // Aplicação imediata
            SetPositionImmediate(targetPosition, targetRotation);
        }
    }

    /// <summary>
    /// Define posição e rotação imediatamente
    /// </summary>
    /// <param name="position">Nova posição</param>
    /// <param name="rotation">Nova rotação</param>
    private void SetPositionImmediate(Vector3 position, Vector3 rotation)
    {
        if (hasPhysics)
        {
            // Move via física para evitar problemas de colisão
            if (rb2D != null)
            {
                rb2D.MovePosition(position);
                rb2D.MoveRotation(rotation.z);
            }
            else if (rb3D != null)
            {
                rb3D.MovePosition(position);
                rb3D.MoveRotation(Quaternion.Euler(rotation));
            }
        }
        else
        {
            // Move diretamente via transform
            transform.position = position;
            if (useRotation)
                transform.eulerAngles = rotation;
        }
        
        lastSavedPosition = position;
    }

    /// <summary>
    /// Inicia transição animada para nova posição
    /// </summary>
    /// <param name="targetPosition">Posição alvo</param>
    /// <param name="targetRotation">Rotação alvo</param>
    private void StartTransition(Vector3 targetPosition, Vector3 targetRotation)
    {
        transitionStartPosition = transform.position;
        transitionTargetPosition = targetPosition;
        transitionStartRotation = transform.eulerAngles;
        transitionTargetRotation = targetRotation;
        
        transitionTimer = 0f;
        isTransitioning = true;
        
        // NÃO pausa física - permite empurrão durante transição!
        // Comentado para permitir AddForce funcionar:
        // if (hasPhysics)
        // {
        //     if (rb2D != null) rb2D.isKinematic = true;
        //     if (rb3D != null) rb3D.isKinematic = true;
        // }
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
        
        // Interpola posição - mas só se não foi empurrada!
        Vector3 currentPosition = Vector3.Lerp(transitionStartPosition, transitionTargetPosition, curveValue);
        
        // Verifica se a caixa foi movida por força externa (empurrão)
        float distanceFromExpected = Vector3.Distance(transform.position, currentPosition);
        if (distanceFromExpected > movementThreshold * 2f)
        {
            // Se foi empurrada, cancela a transição suave e mantém posição atual
            Debug.Log($"{gameObject.name}: Empurrão detectado durante transição! Cancelando transição.");
            CompleteTransition();
            return;
        }
        
        // Interpola rotação se necessário
        Vector3 currentRotation = useRotation ? 
            Vector3.Lerp(transitionStartRotation, transitionTargetRotation, curveValue) :
            transform.eulerAngles;
        
        // Aplica transformações apenas se não foi empurrada
        transform.position = currentPosition;
        if (useRotation)
            transform.eulerAngles = currentRotation;
    }

    /// <summary>
    /// Completa a transição
    /// </summary>
    private void CompleteTransition()
    {
        isTransitioning = false;
        SetPositionImmediate(transitionTargetPosition, transitionTargetRotation);
        
        // Como não pausamos física, não precisamos restaurar
        // Comentado porque não alteramos isKinematic:
        // if (hasPhysics)
        // {
        //     if (rb2D != null) rb2D.isKinematic = false;
        //     if (rb3D != null) rb3D.isKinematic = false;
        // }
    }

    /// <summary>
    /// Força o salvamento da posição atual
    /// </summary>
    [ContextMenu("Salvar Posição Atual")]
    public void ForceSaveCurrentPosition()
    {
        SaveCurrentPosition();
    }

    /// <summary>
    /// Reseta posições para a posição atual
    /// </summary>
    [ContextMenu("Reset Posições")]
    public void ResetPositions()
    {
        Vector3 currentPos = transform.position;
        Vector3 currentRot = transform.eulerAngles;
        
        positionInDimensionA = currentPos;
        positionInDimensionB = currentPos;
        
        if (useRotation)
        {
            rotationInDimensionA = currentRot;
            rotationInDimensionB = currentRot;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Posições resetadas para {currentPos}");
        }
    }

    /// <summary>
    /// Define posição específica para uma dimensão
    /// </summary>
    /// <param name="dimension">Dimensão alvo</param>
    /// <param name="position">Nova posição</param>
    public void SetPositionForDimension(DimensionType dimension, Vector3 position)
    {
        if (dimension == DimensionType.DimensionA)
        {
            positionInDimensionA = position;
        }
        else
        {
            positionInDimensionB = position;
        }
        
        // Se é a dimensão atual, aplica imediatamente
        if (dimension == currentDimension)
        {
            ApplyDimensionPosition(dimension, false);
        }
    }

    private void OnDestroy()
    {
        // Remove inscrição dos eventos
        DimensionManager.OnDimensionSwitched -= OnDimensionSwitched;
    }

    private void OnDrawGizmos()
    {
        if (!showPositionGizmos)
            return;
            
        // Desenha posições salvas para cada dimensão
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(positionInDimensionA, Vector3.one * 0.5f);
        Gizmos.DrawIcon(positionInDimensionA + Vector3.up * 0.8f, "d_winbtn_mac_max", true);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(positionInDimensionB, Vector3.one * 0.5f);
        Gizmos.DrawIcon(positionInDimensionB + Vector3.up * 0.8f, "d_winbtn_mac_max", true);
        
        // Liga as posições com uma linha
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(positionInDimensionA, positionInDimensionB);
        
        // Destaca a posição atual
        if (Application.isPlaying)
        {
            Gizmos.color = currentDimension == DimensionType.DimensionA ? Color.red : Color.blue;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}
