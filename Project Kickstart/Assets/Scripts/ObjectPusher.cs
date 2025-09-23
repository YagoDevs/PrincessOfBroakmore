using UnityEngine;

public class ObjectPusher : MonoBehaviour
{
    [Header("Push Settings")]
    [SerializeField] private float pushForce = 500f; // Force to push objects
    [SerializeField] private LayerMask pushableLayer = 1; // Layer of pushable objects
    [SerializeField] private float maxPushDistance = 1.5f; // Maximum distance to push
    
    [Header("Pushable Object Tags")]
    [SerializeField] private string[] pushableTags = { "Pushable" }; // Only tags that exist
    
    private Rigidbody playerRb;
    
    private void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        if (playerRb == null)
        {
            Debug.LogWarning("ObjectPusher: Player needs a Rigidbody to push objects!");
        }
    }
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // For CharacterController
        PushObject(hit.rigidbody, hit.point, hit.normal);
    }
    
    private void OnCollisionStay(Collision collision)
    {
        // For Rigidbody
        if (collision.rigidbody != null)
        {
            Vector3 pushDirection = collision.transform.position - transform.position;
            pushDirection.y = 0; // Push horizontally only
            pushDirection.Normalize();
            
            PushObject(collision.rigidbody, collision.contacts[0].point, pushDirection);
        }
    }
    
    private void PushObject(Rigidbody objectRb, Vector3 contactPoint, Vector3 pushDirection)
    {
        if (objectRb == null) return;
        
        // Check if the object is pushable
        if (!IsPushable(objectRb.gameObject)) return;
        
        // Check distance
        float distance = Vector3.Distance(transform.position, objectRb.transform.position);
        if (distance > maxPushDistance) return;
        
        // Check if the player is moving
        if (playerRb != null && playerRb.linearVelocity.magnitude < 0.1f) return;
        
        // Calculate push direction
        Vector3 pushDir = (objectRb.transform.position - transform.position).normalized;
        pushDir.y = 0; // Keep in the horizontal plane
        
        // Apply force
        float currentPushForce = pushForce * Time.fixedDeltaTime;
        objectRb.AddForce(pushDir * currentPushForce, ForceMode.Force);
        
        Debug.Log($"[PUSHER] Pushing {objectRb.name} with force {currentPushForce}");
    }
    
    private bool IsPushable(GameObject obj)
    {
        // Check by tag (with validation if tag exists)
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
                // Tag does not exist, ignore
                Debug.LogWarning($"[ObjectPusher] Tag '{tag}' does not exist. Create it or remove from list.");
                continue;
            }
        }
        
        // Verificar por layer
        return ((1 << obj.layer) & pushableLayer) != 0;
    }
    
    // Method to detect nearby pushable objects using raycast
    private void Update()
    {
        // Detect objects in front of the player
        Vector3 forward = transform.forward;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, forward, out hit, maxPushDistance))
        {
            if (hit.rigidbody != null && IsPushable(hit.rigidbody.gameObject))
            {
                // Check if trying to move
                bool isMoving = false;
                
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || 
                    Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
                {
                    isMoving = true;
                }
                
                if (isMoving)
                {
                    // Push object at a distance
                    Vector3 pushDirection = hit.point - transform.position;
                    pushDirection.y = 0;
                    pushDirection.Normalize();
                    
                    float force = pushForce * 0.5f * Time.deltaTime; // Lower force for raycast
                    hit.rigidbody.AddForce(pushDirection * force, ForceMode.Force);
                }
            }
        }
    }
    
    // Editor visualization
    private void OnDrawGizmosSelected()
    {
        // Draw push range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxPushDistance);
        
        // Draw detection ray
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * maxPushDistance);
    }
}
