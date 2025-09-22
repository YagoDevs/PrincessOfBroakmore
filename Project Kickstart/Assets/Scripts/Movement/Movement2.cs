
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class Movement2: MonoBehaviour
{
    [Header("Adjustables")]
    public float moveForce = 10f;
    public float jumpForce = 7f;
    public float groundDistance = 0.2f;
    public float maxSpeed = 6f;
    public float acceleration = 20f;
    public float deceleration = 40f;
    public float noInputDamping = 20f;

    [Header("Variables")]
    private bool isGrounded;
    private float currentMoveForce;

    [Header("References")]
    private string SceneName;
    private Rigidbody rb;
    public LayerMask groundMask;
    public Transform groundCheck;
    [SerializeField] Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("Missing components in player");
        rb.freezeRotation = true;
        currentMoveForce = moveForce;
        SceneName = SceneManager.GetActiveScene().name;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        //if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        //{
        //    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //}

        if(!isGrounded)
        {
            moveForce = moveForce - (moveForce / 2);
        }
        if (isGrounded)
        {
            moveForce = currentMoveForce;
        }
    }

    void FixedUpdate()
    {
        Vector3 direction = Vector3.zero;
        if(SceneName == "dugeon") 
        {
            if (Input.GetKey(KeyCode.W)) direction += Vector3.right;   
            if (Input.GetKey(KeyCode.S)) direction += Vector3.left;     
            if (Input.GetKey(KeyCode.A)) direction += Vector3.forward;      
            if (Input.GetKey(KeyCode.D)) direction += Vector3.back;
        }
        else
        {
        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;   // W = Para frente
        if (Input.GetKey(KeyCode.S)) direction += Vector3.back;      // S = Para trás  
        if (Input.GetKey(KeyCode.A)) direction += Vector3.left;      // A = Para esquerda
        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;     // D = Para direita
        }
       
        
        // Debug: Mostrar direção quando pressionar teclas
        if (direction != Vector3.zero)
        {
            Debug.Log($"[MOVEMENT] Direção: {direction} - W:{Input.GetKey(KeyCode.W)} S:{Input.GetKey(KeyCode.S)} A:{Input.GetKey(KeyCode.A)} D:{Input.GetKey(KeyCode.D)}");
        }

        if (direction != Vector3.zero)
        {
            direction.Normalize();

            
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            
            float rotationSpeed = 360f; 
            Quaternion smoothRotation = Quaternion.RotateTowards(
                rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime
            );

            
            rb.MoveRotation(smoothRotation);

            float angle = Quaternion.Angle(rb.rotation, targetRotation);
            if (angle < 30f) 
            {
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                float dt = Time.fixedDeltaTime;

                // If moving opposite to input, brake to a stop before accelerating
                float alignment = horizontalVelocity.sqrMagnitude > 0.0001f
                    ? Vector3.Dot(horizontalVelocity.normalized, direction)
                    : 1f;

                Vector3 newHorizontalVelocity = horizontalVelocity;
                if (alignment <= 0f)
                {
                    newHorizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, deceleration * dt);
                }
                else
                {
                    Vector3 target = direction * maxSpeed;
                    newHorizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * dt);
                }

                rb.linearVelocity = new Vector3(newHorizontalVelocity.x, rb.linearVelocity.y, newHorizontalVelocity.z);
            }
        }
        else
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 damped = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, noInputDamping * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(damped.x, rb.linearVelocity.y, damped.z);
        }

        // Calculate if moving to alternate between idle and running
        Vector3 animatorVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = animatorVelocity.magnitude;
        
        //Threshold mais alto para evitar animação tremulante
        float speedThreshold = 0.5f;
        bool isRunning = currentSpeed > speedThreshold;
        
        // Send state to Animator
        animator.SetBool("IsRunning", isRunning);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}