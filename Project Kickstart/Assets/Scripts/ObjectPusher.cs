using UnityEngine;

public class ObjectPusher : MonoBehaviour
{
    [Header("Configurações de Empurrar")]
    [SerializeField] private float pushForce = 500f; // Força para empurrar objetos
    [SerializeField] private LayerMask pushableLayer = 1; // Layer dos objetos empurráveis
    [SerializeField] private float maxPushDistance = 1.5f; // Distância máxima para empurrar
    
    [Header("Tags de Objetos Empurráveis")]
    [SerializeField] private string[] pushableTags = { "Pushable" }; // Apenas tags que existem
    
    private Rigidbody playerRb;
    
    private void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        if (playerRb == null)
        {
            Debug.LogWarning("ObjectPusher: Jogador precisa de Rigidbody para empurrar objetos!");
        }
    }
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Para CharacterController
        PushObject(hit.rigidbody, hit.point, hit.normal);
    }
    
    private void OnCollisionStay(Collision collision)
    {
        // Para Rigidbody
        if (collision.rigidbody != null)
        {
            Vector3 pushDirection = collision.transform.position - transform.position;
            pushDirection.y = 0; // Só empurrar horizontalmente
            pushDirection.Normalize();
            
            PushObject(collision.rigidbody, collision.contacts[0].point, pushDirection);
        }
    }
    
    private void PushObject(Rigidbody objectRb, Vector3 contactPoint, Vector3 pushDirection)
    {
        if (objectRb == null) return;
        
        // Verificar se o objeto é empurrável
        if (!IsPushable(objectRb.gameObject)) return;
        
        // Verificar distância
        float distance = Vector3.Distance(transform.position, objectRb.transform.position);
        if (distance > maxPushDistance) return;
        
        // Verificar se o jogador está se movendo
        if (playerRb != null && playerRb.linearVelocity.magnitude < 0.1f) return;
        
        // Calcular direção de empurrar
        Vector3 pushDir = (objectRb.transform.position - transform.position).normalized;
        pushDir.y = 0; // Manter no plano horizontal
        
        // Aplicar força
        float currentPushForce = pushForce * Time.fixedDeltaTime;
        objectRb.AddForce(pushDir * currentPushForce, ForceMode.Force);
        
        Debug.Log($"[PUSHER] Empurrando {objectRb.name} com força {currentPushForce}");
    }
    
    private bool IsPushable(GameObject obj)
    {
        // Verificar por tag (com verificação se a tag existe)
        foreach (string tag in pushableTags)
        {
            try
            {
                if (obj.CompareTag(tag))
                {
                    return true;
                }
            }
            catch (UnityEngine.UnityException)
            {
                // Tag não existe, ignorar
                Debug.LogWarning($"[ObjectPusher] Tag '{tag}' não existe. Criando ou removendo da lista.");
                continue;
            }
        }
        
        // Verificar por layer
        return ((1 << obj.layer) & pushableLayer) != 0;
    }
    
    // Método para detectar objetos empurráveis próximos usando raycast
    private void Update()
    {
        // Detectar objetos na frente do jogador
        Vector3 forward = transform.forward;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, forward, out hit, maxPushDistance))
        {
            if (hit.rigidbody != null && IsPushable(hit.rigidbody.gameObject))
            {
                // Verificar se está tentando se mover
                bool isMoving = false;
                
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || 
                    Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
                {
                    isMoving = true;
                }
                
                if (isMoving)
                {
                    // Empurrar objeto à distância
                    Vector3 pushDirection = hit.point - transform.position;
                    pushDirection.y = 0;
                    pushDirection.Normalize();
                    
                    float force = pushForce * 0.5f * Time.deltaTime; // Força menor para raycast
                    hit.rigidbody.AddForce(pushDirection * force, ForceMode.Force);
                }
            }
        }
    }
    
    // Visualização no editor
    private void OnDrawGizmosSelected()
    {
        // Desenhar alcance de empurrar
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxPushDistance);
        
        // Desenhar raio de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * maxPushDistance);
    }
}
