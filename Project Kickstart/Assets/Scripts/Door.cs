using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool requiresLight = true; // Whether it needs light to open
    [SerializeField] private float openAngle = 90f; // Opening angle on the Z axis
    [SerializeField] private float animationSpeed = 50f; // Animation speed (degrees per second)
    
    [Header("Light Settings")]
    [SerializeField] private bool isReceivingLight = false;
    [SerializeField] private float lightDetectionRange = 1f; // Range to detect light
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject openEffect; // Effect when opened
    [SerializeField] private GameObject lightIndicator; // Visual indicator that it's receiving light
    [SerializeField] private Material activatedMaterial; // Material when activated
    [SerializeField] private Material deactivatedMaterial; // Material when deactivated
    
    [Header("Audio")]
    [SerializeField] private AudioSource doorAudio;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    
    // FIX: Use a simple float to control Z angle
    private float currentZAngle;
    private float targetZAngle;
    private bool isAnimating = false;
    private Renderer doorRenderer;
    private Vector3 originalEulerAngles;
    
    private void Start()
    {
        // Store original euler angles
        originalEulerAngles = transform.eulerAngles;
        currentZAngle = originalEulerAngles.z;
        targetZAngle = currentZAngle;
        
        // Configure renderer for material changes
        doorRenderer = GetComponent<Renderer>();
        
        // Configure audio if not already set
        if (doorAudio == null)
        {
            doorAudio = GetComponent<AudioSource>();
            if (doorAudio == null)
            {
                doorAudio = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Initial state
        UpdateVisualState();
        
        Debug.Log($"[DOOR] Porta {gameObject.name} inicializada - Ângulo Z inicial: {currentZAngle}°, Requer luz: {requiresLight}");
    }
    
    private void Update()
    {
        // Check if receiving light
        CheckForLight();
        
        // Animate opening/closing
        if (isAnimating)
        {
            AnimateDoor();
        }
        
        // Control opening based on light (only if not animating)
        if (requiresLight && !isAnimating)
        {
            if (isReceivingLight && !isOpen)
            {
                OpenDoor();
            }
            else if (!isReceivingLight && isOpen)
            {
                CloseDoor();
            }
        }
    }
    
    private void CheckForLight()
    {
        bool wasReceivingLight = isReceivingLight;
        isReceivingLight = false;
        
        // Check if there are flowers nearby emitting light to this door
        Flower[] allFlowers = FindObjectsOfType<Flower>();
        
        Debug.Log($"[DOOR] Checking light for door {gameObject.name} - {allFlowers.Length} flowers found");
        
        foreach (Flower flower in allFlowers)
        {
            if (flower == null) continue;
            
            float distance = Vector3.Distance(flower.transform.position, transform.position);
            string targetName = flower.CurrentTarget != null ? flower.CurrentTarget.name : "null";
            
            Debug.Log($"[DOOR] Flower {flower.name}: Activated={flower.IsActivated}, Target={targetName}, Distance={distance:F2}");
            
            if (flower.IsActivated && flower.CurrentTarget == transform)
            {
                if (distance <= lightDetectionRange || lightDetectionRange <= 0)
                {
                    isReceivingLight = true;
                    Debug.Log($"[DOOR] ✅ Door {gameObject.name} RECEIVING light from flower {flower.name}!");
                    break;
                }
                else
                {
                    Debug.Log($"[DOOR] ❌ Flower {flower.name} too far from door {gameObject.name} (distance: {distance:F2}, limit: {lightDetectionRange})");
                }
            }
        }
        
        // If light state changed, update visuals
        if (wasReceivingLight != isReceivingLight)
        {
            UpdateVisualState();
            Debug.Log($"[DOOR] 🔄 Door {gameObject.name} {(isReceivingLight ? "receiving" : "lost")} light");
        }
    }
    
    private void OpenDoor()
    {
        if (isOpen) return;
        
        isOpen = true;
        targetZAngle = originalEulerAngles.z + openAngle; // Somar ao ângulo original
        isAnimating = true;
        
        // Effects
        PlayOpenSound();
        ShowOpenEffect();
        
        Debug.Log($"[DOOR] 🚪 Opening door {gameObject.name} - Rotating Z from {currentZAngle}° to {targetZAngle}°");
    }
    
    private void CloseDoor()
    {
        if (!isOpen) return;
        
        isOpen = false;
        targetZAngle = originalEulerAngles.z; // Voltar para rotação original
        isAnimating = true;
        
        // Effects
        PlayCloseSound();
        
        Debug.Log($"[DOOR] Closing door {gameObject.name} - Returning Z to {targetZAngle}°");
    }
    
    private void AnimateDoor()
    {
        // FIX: Use MoveTowards on the float angle directly
        currentZAngle = Mathf.MoveTowards(currentZAngle, targetZAngle, animationSpeed * Time.deltaTime);
        
        // Apply rotation keeping original X and Y
        transform.eulerAngles = new Vector3(originalEulerAngles.x, originalEulerAngles.y, currentZAngle);
        
        // Check if reached destination
        if (Mathf.Approximately(currentZAngle, targetZAngle))
        {
            isAnimating = false;
            
            if (isOpen)
            {
                Debug.Log($"[DOOR] ✅ Door {gameObject.name} OPEN at Z={currentZAngle}°");
            }
            else
            {
                Debug.Log($"[DOOR] ✅ Door {gameObject.name} CLOSED at Z={currentZAngle}°");
            }
        }
    }
    
    private void UpdateVisualState()
    {
        // Update light indicator
        if (lightIndicator != null)
        {
            lightIndicator.SetActive(isReceivingLight);
        }
        
        // Update material
        if (doorRenderer != null)
        {
            if (isReceivingLight && activatedMaterial != null)
            {
                doorRenderer.material = activatedMaterial;
            }
            else if (!isReceivingLight && deactivatedMaterial != null)
            {
                doorRenderer.material = deactivatedMaterial;
            }
        }
    }
    
    private void PlayOpenSound()
    {
        if (doorAudio != null && openSound != null)
        {
            doorAudio.PlayOneShot(openSound);
        }
    }
    
    private void PlayCloseSound()
    {
        if (doorAudio != null && closeSound != null)
        {
            doorAudio.PlayOneShot(closeSound);
        }
    }
    
    private void ShowOpenEffect()
    {
        if (openEffect != null)
        {
            openEffect.SetActive(true);
            // Desativar efeito após um tempo
            Invoke(nameof(HideOpenEffect), 2f);
        }
    }
    
    private void HideOpenEffect()
    {
        if (openEffect != null)
        {
            openEffect.SetActive(false);
        }
    }
    
    // Public methods for external control
    public void ForceOpen()
    {
        requiresLight = false;
        OpenDoor();
    }
    
    public void ForceClose()
    {
        requiresLight = false;
        CloseDoor();
    }
    
    public void SetRequiresLight(bool requires)
    {
        requiresLight = requires;
    }
    
    // Public properties
    public bool IsOpen => isOpen;
    public bool IsReceivingLight => isReceivingLight;
    public bool RequiresLight => requiresLight;
    
    // Editor visualization
    private void OnDrawGizmosSelected()
    {
        // Draw light detection range
        Gizmos.color = isReceivingLight ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, lightDetectionRange);
        
        // Draw opening arc (Z rotation)
        Gizmos.color = Color.blue;
        
        // Door initial and final positions
        Vector3 startDirection = transform.right; // Initial direction
        Vector3 endDirection = Quaternion.Euler(0, 0, openAngle) * startDirection; // Final direction
        
        // Draw lines showing the opening
        Gizmos.DrawRay(transform.position, startDirection * 1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, endDirection * 1f);
        
        // Draw arc (approximation)
        Gizmos.color = Color.yellow;
        for (int i = 0; i <= 10; i++)
        {
            float angle = (openAngle / 10f) * i;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * startDirection;
            Gizmos.DrawRay(transform.position, direction * 0.8f);
        }
    }
}