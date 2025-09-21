using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Configurações da Porta")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool requiresLight = true; // Se precisa de luz para abrir
    [SerializeField] private float openAngle = 90f; // Ângulo de abertura no eixo Z
    [SerializeField] private float animationSpeed = 50f; // Velocidade da animação (graus por segundo)
    
    [Header("Configurações de Luz")]
    [SerializeField] private bool isReceivingLight = false;
    [SerializeField] private float lightDetectionRange = 1f; // Range para detectar luz
    
    [Header("Efeitos Visuais")]
    [SerializeField] private GameObject openEffect; // Efeito quando abre
    [SerializeField] private GameObject lightIndicator; // Indicador visual de que está recebendo luz
    [SerializeField] private Material activatedMaterial; // Material quando ativada
    [SerializeField] private Material deactivatedMaterial; // Material quando desativada
    
    [Header("Áudio")]
    [SerializeField] private AudioSource doorAudio;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    
    // CORREÇÃO: Usar float simples para controlar o ângulo Z
    private float currentZAngle;
    private float targetZAngle;
    private bool isAnimating = false;
    private Renderer doorRenderer;
    private Vector3 originalEulerAngles;
    
    private void Start()
    {
        // Armazenar os ângulos originais
        originalEulerAngles = transform.eulerAngles;
        currentZAngle = originalEulerAngles.z;
        targetZAngle = currentZAngle;
        
        // Configurar renderer para mudança de material
        doorRenderer = GetComponent<Renderer>();
        
        // Configurar áudio se não estiver configurado
        if (doorAudio == null)
        {
            doorAudio = GetComponent<AudioSource>();
            if (doorAudio == null)
            {
                doorAudio = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Estado inicial
        UpdateVisualState();
        
        Debug.Log($"[DOOR] Porta {gameObject.name} inicializada - Ângulo Z inicial: {currentZAngle}°, Requer luz: {requiresLight}");
    }
    
    private void Update()
    {
        // Verificar se está recebendo luz
        CheckForLight();
        
        // Animar abertura/fechamento
        if (isAnimating)
        {
            AnimateDoor();
        }
        
        // Controlar abertura baseado na luz (só se não estiver animando)
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
        
        // Verificar se há flores próximas emitindo luz para esta porta
        Flower[] allFlowers = FindObjectsOfType<Flower>();
        
        Debug.Log($"[DOOR] Verificando luz para porta {gameObject.name} - {allFlowers.Length} flores encontradas");
        
        foreach (Flower flower in allFlowers)
        {
            if (flower == null) continue;
            
            float distance = Vector3.Distance(flower.transform.position, transform.position);
            string targetName = flower.CurrentTarget != null ? flower.CurrentTarget.name : "null";
            
            Debug.Log($"[DOOR] Flor {flower.name}: Ativada={flower.IsActivated}, Alvo={targetName}, Distância={distance:F2}");
            
            if (flower.IsActivated && flower.CurrentTarget == transform)
            {
                if (distance <= lightDetectionRange || lightDetectionRange <= 0)
                {
                    isReceivingLight = true;
                    Debug.Log($"[DOOR] ✅ Porta {gameObject.name} RECEBENDO luz da flor {flower.name}!");
                    break;
                }
                else
                {
                    Debug.Log($"[DOOR] ❌ Flor {flower.name} muito longe da porta {gameObject.name} (distância: {distance:F2}, limite: {lightDetectionRange})");
                }
            }
        }
        
        // Se mudou o estado da luz, atualizar visual
        if (wasReceivingLight != isReceivingLight)
        {
            UpdateVisualState();
            Debug.Log($"[DOOR] 🔄 Porta {gameObject.name} {(isReceivingLight ? "recebendo" : "perdeu")} luz");
        }
    }
    
    private void OpenDoor()
    {
        if (isOpen) return;
        
        isOpen = true;
        targetZAngle = originalEulerAngles.z + openAngle; // Somar ao ângulo original
        isAnimating = true;
        
        // Efeitos
        PlayOpenSound();
        ShowOpenEffect();
        
        Debug.Log($"[DOOR] 🚪 Abrindo porta {gameObject.name} - Rotacionando Z de {currentZAngle}° para {targetZAngle}°");
    }
    
    private void CloseDoor()
    {
        if (!isOpen) return;
        
        isOpen = false;
        targetZAngle = originalEulerAngles.z; // Voltar para rotação original
        isAnimating = true;
        
        // Efeitos
        PlayCloseSound();
        
        Debug.Log($"[DOOR] Fechando porta {gameObject.name} - Voltando Z para {targetZAngle}°");
    }
    
    private void AnimateDoor()
    {
        // CORREÇÃO: Usar MoveTowards no ângulo float diretamente
        currentZAngle = Mathf.MoveTowards(currentZAngle, targetZAngle, animationSpeed * Time.deltaTime);
        
        // Aplicar a rotação mantendo X e Y originais
        transform.eulerAngles = new Vector3(originalEulerAngles.x, originalEulerAngles.y, currentZAngle);
        
        // Verificar se chegou ao destino
        if (Mathf.Approximately(currentZAngle, targetZAngle))
        {
            isAnimating = false;
            
            if (isOpen)
            {
                Debug.Log($"[DOOR] ✅ Porta {gameObject.name} ABERTA em Z={currentZAngle}°");
            }
            else
            {
                Debug.Log($"[DOOR] ✅ Porta {gameObject.name} FECHADA em Z={currentZAngle}°");
            }
        }
    }
    
    private void UpdateVisualState()
    {
        // Atualizar indicador de luz
        if (lightIndicator != null)
        {
            lightIndicator.SetActive(isReceivingLight);
        }
        
        // Atualizar material
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
    
    // Métodos públicos para controle externo
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
    
    // Propriedades públicas
    public bool IsOpen => isOpen;
    public bool IsReceivingLight => isReceivingLight;
    public bool RequiresLight => requiresLight;
    
    // Visualização no editor
    private void OnDrawGizmosSelected()
    {
        // Desenhar range de detecção de luz
        Gizmos.color = isReceivingLight ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, lightDetectionRange);
        
        // Desenhar arco de abertura (rotação Z)
        Gizmos.color = Color.blue;
        
        // Posição inicial e final da porta
        Vector3 startDirection = transform.right; // Direção inicial
        Vector3 endDirection = Quaternion.Euler(0, 0, openAngle) * startDirection; // Direção final
        
        // Desenhar linhas mostrando a abertura
        Gizmos.DrawRay(transform.position, startDirection * 1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, endDirection * 1f);
        
        // Desenhar arco (aproximação)
        Gizmos.color = Color.yellow;
        for (int i = 0; i <= 10; i++)
        {
            float angle = (openAngle / 10f) * i;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * startDirection;
            Gizmos.DrawRay(transform.position, direction * 0.8f);
        }
    }
}