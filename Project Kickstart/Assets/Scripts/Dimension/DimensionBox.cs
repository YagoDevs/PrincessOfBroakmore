using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script for movable objects that maintain independent positions in each dimension.
/// Each box saves its coordinates separately for A and B.
/// </summary>
public class DimensionBox : MonoBehaviour
{
    [Header("Box Settings")]
    [SerializeField] private bool canBeMoved = true;
    [SerializeField] private bool usePhysics = true;
    
    [Header("Positions per Dimension")]
    [SerializeField] private Vector3 positionInDimensionA;
    [SerializeField] private Vector3 positionInDimensionB;
    [SerializeField] private bool useRotation = false;
    [SerializeField] private Vector3 rotationInDimensionA;
    [SerializeField] private Vector3 rotationInDimensionB;
    
    [Header("Transition between Dimensions")]
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Movement Detection")]
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float savePositionDelay = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private float minForceForSound = 0.1f;  // Mais sensível
    [SerializeField] private float maxSoundCooldown = 0.5f;  // Mais tempo entre sons
    
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
    
    // Transition
    private float transitionTimer = 0f;
    private Vector3 transitionStartPosition;
    private Vector3 transitionTargetPosition;
    private Vector3 transitionStartRotation;
    private Vector3 transitionTargetRotation;
    
    // Automatic save system
    private Coroutine savePositionCoroutine;
    
    // Audio system
    private float lastSoundTime = 0f;
    private Vector3 lastVelocity = Vector3.zero;
    private bool isDimensionSwitching = false;

    private void Awake()
    {
        // Cache physics components
        rb2D = GetComponent<Rigidbody2D>();
        rb3D = GetComponent<Rigidbody>();
        hasPhysics = (rb2D != null || rb3D != null) && usePhysics;
        
        // Initialize positions if not defined
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
        // Set current dimension
        currentDimension = DimensionManager.Instance != null ? 
            DimensionManager.Instance.CurrentDimension : DimensionType.DimensionA;
        
        // Apply initial position
        ApplyDimensionPosition(currentDimension, false);
        lastSavedPosition = transform.position;
        
        // Subscribe to dimension change events
        DimensionManager.OnDimensionSwitched += OnDimensionSwitched;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: DimensionBox started in dimension {currentDimension}");
        }
    }

    private void Update()
    {
        // Process transition animation
        if (isTransitioning)
        {
            ProcessTransition();
        }
        
        // Detect box movement (now always, even during transition)
        if (canBeMoved)
        {
            DetectMovement();
            DetectPushForAudio();
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
                    Debug.Log($"{gameObject.name}: Movement detected in dimension {currentDimension}");
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
    /// Detecta empurrões para tocar som baseado na velocidade
    /// </summary>
    private void DetectPushForAudio()
    {
        if (!enableAudio || isDimensionSwitching)
        {
            if (showDebugInfo && Time.frameCount % 120 == 0) // Log a cada 2 segundos
                Debug.Log($"{gameObject.name}: Audio disabled or dimension switching");
            return;
        }

        if (!hasPhysics)
        {
            if (showDebugInfo && Time.frameCount % 120 == 0)
                Debug.Log($"{gameObject.name}: No physics components");
            return;
        }

        if (AudioManager.Instance == null)
        {
            if (showDebugInfo && Time.frameCount % 120 == 0)
                Debug.Log($"{gameObject.name}: AudioManager not found");
            return;
        }

        Vector3 currentVelocity = Vector3.zero;
        
        // Get current velocity from physics
        if (rb3D != null)
        {
            currentVelocity = rb3D.linearVelocity;
        }
        else if (rb2D != null)
        {
            currentVelocity = rb2D.linearVelocity;
        }

        // Calculate force (change in velocity)
        Vector3 velocityChange = currentVelocity - lastVelocity;
        float force = velocityChange.magnitude;
        
        // Debug info (occasional)
        if (showDebugInfo && force > 0.01f) // Log only when there's some movement
        {
            Debug.Log($"{gameObject.name}: Velocity={currentVelocity.magnitude:F2}, Force={force:F2}, MinForce={minForceForSound}, TimeSince={Time.time - lastSoundTime:F2}");
        }

        // Check if force is significant and enough time has passed
        if (force > minForceForSound && Time.time - lastSoundTime > maxSoundCooldown)
        {
            // Play push sound
            AudioManager.Instance.PlayBoxPushSound(transform.position, force);
            lastSoundTime = Time.time;
            
            if (showDebugInfo)
            {
                Debug.Log($"🔊 {gameObject.name}: Push sound triggered with force {force:F2}");
            }
        }

        lastVelocity = currentVelocity;
    }

    /// <summary>
    /// Save current position in active dimension
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
            Debug.Log($"{gameObject.name}: Position saved in dimension {currentDimension}: {currentPos}");
        }
    }

    /// <summary>
    /// Called when dimension changes
    /// </summary>
    /// <param name="fromDimension">Previous dimension</param>
    /// <param name="toDimension">New dimension</param>
    private void OnDimensionSwitched(DimensionType fromDimension, DimensionType toDimension)
    {
        // Save current position before switching
        if (isBeingMoved)
        {
            SaveCurrentPosition();
            isBeingMoved = false;
        }
        
        // Disable audio detection during dimension switch
        isDimensionSwitching = true;
        
        currentDimension = toDimension;
        ApplyDimensionPosition(toDimension, useTransitionAnimation);
        
        // Re-enable audio after transition completes
        float audioDisableTime = useTransitionAnimation ? transitionDuration + 0.2f : 0.3f;
        StartCoroutine(ReEnableAudioAfterDelay(audioDisableTime));
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Dimension changed from {fromDimension} to {toDimension}");
        }
    }

    /// <summary>
    /// Re-enable audio detection after dimension switch delay
    /// </summary>
    private System.Collections.IEnumerator ReEnableAudioAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isDimensionSwitching = false;
        
        // Reset velocity tracking to avoid false positives
        if (rb3D != null)
            lastVelocity = rb3D.linearVelocity;
        else if (rb2D != null)
            lastVelocity = rb2D.linearVelocity;
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name}: Audio detection re-enabled after dimension switch");
        }
    }

    /// <summary>
    /// Apply position corresponding to dimension
    /// </summary>
    /// <param name="dimension">Target dimension</param>
    /// <param name="animated">Whether to use animation</param>
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
            // Immediate application
            SetPositionImmediate(targetPosition, targetRotation);
        }
    }

    /// <summary>
    /// Set position and rotation immediately
    /// </summary>
    /// <param name="position">New position</param>
    /// <param name="rotation">New rotation</param>
    private void SetPositionImmediate(Vector3 position, Vector3 rotation)
    {
        if (hasPhysics)
        {
            // Move via physics to avoid collision problems
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
    /// Start animated transition to new position
    /// </summary>
    /// <param name="targetPosition">Target position</param>
    /// <param name="targetRotation">Target rotation</param>
    private void StartTransition(Vector3 targetPosition, Vector3 targetRotation)
    {
        transitionStartPosition = transform.position;
        transitionTargetPosition = targetPosition;
        transitionStartRotation = transform.eulerAngles;
        transitionTargetRotation = targetRotation;
        
        transitionTimer = 0f;
        isTransitioning = true;
        
        // DON'T pause physics - allows pushing during transition!
        // Commented to allow AddForce to work:
        // if (hasPhysics)
        // {
        //     if (rb2D != null) rb2D.isKinematic = true;
        //     if (rb3D != null) rb3D.isKinematic = true;
        // }
    }

    /// <summary>
    /// Process transition animation
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

        // Apply animation curve
        float curveValue = transitionCurve.Evaluate(progress);
        
        // Interpolate position - but only if not pushed!
        Vector3 currentPosition = Vector3.Lerp(transitionStartPosition, transitionTargetPosition, curveValue);
        
        // Check if box was moved by external force (push)
        float distanceFromExpected = Vector3.Distance(transform.position, currentPosition);
        if (distanceFromExpected > movementThreshold * 2f)
        {
            // If pushed, cancel smooth transition and keep current position
            Debug.Log($"{gameObject.name}: Push detected during transition! Canceling transition.");
            CompleteTransition();
            return;
        }
        
        // Interpolate rotation if necessary
        Vector3 currentRotation = useRotation ? 
            Vector3.Lerp(transitionStartRotation, transitionTargetRotation, curveValue) :
            transform.eulerAngles;
        
        // Apply transformations only if not pushed
        transform.position = currentPosition;
        if (useRotation)
            transform.eulerAngles = currentRotation;
    }

    /// <summary>
    /// Complete transition
    /// </summary>
    private void CompleteTransition()
    {
        isTransitioning = false;
        SetPositionImmediate(transitionTargetPosition, transitionTargetRotation);
        
        // Since we don't pause physics, we don't need to restore
        // Commented because we don't change isKinematic:
        // if (hasPhysics)
        // {
        //     if (rb2D != null) rb2D.isKinematic = false;
        //     if (rb3D != null) rb3D.isKinematic = false;
        // }
    }

    /// <summary>
    /// Force save current position
    /// </summary>
    [ContextMenu("Salvar Posição Atual")]
    public void ForceSaveCurrentPosition()
    {
        SaveCurrentPosition();
    }

    /// <summary>
    /// Reset positions to current position
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
            Debug.Log($"{gameObject.name}: Positions reset to {currentPos}");
        }
    }

    /// <summary>
    /// Set specific position for a dimension
    /// </summary>
    /// <param name="dimension">Target dimension</param>
    /// <param name="position">New position</param>
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
        
        // If it's current dimension, apply immediately
        if (dimension == currentDimension)
        {
            ApplyDimensionPosition(dimension, false);
        }
    }

    private void OnDestroy()
    {
        // Remove event subscriptions
        DimensionManager.OnDimensionSwitched -= OnDimensionSwitched;
    }

    private void OnDrawGizmos()
    {
        if (!showPositionGizmos)
            return;
            
        // Draw saved positions for each dimension
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(positionInDimensionA, Vector3.one * 0.5f);
        Gizmos.DrawIcon(positionInDimensionA + Vector3.up * 0.8f, "d_winbtn_mac_max", true);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(positionInDimensionB, Vector3.one * 0.5f);
        Gizmos.DrawIcon(positionInDimensionB + Vector3.up * 0.8f, "d_winbtn_mac_max", true);
        
        // Connect positions with a line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(positionInDimensionA, positionInDimensionB);
        
        // Highlight current position
        if (Application.isPlaying)
        {
            Gizmos.color = currentDimension == DimensionType.DimensionA ? Color.red : Color.blue;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}
