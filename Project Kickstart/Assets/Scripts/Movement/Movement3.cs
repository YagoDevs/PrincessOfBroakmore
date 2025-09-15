
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Movement3: MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f; // degrees per second
    public float jumpForce = 7f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        // Rotate left/right with A/D
        float turnInput = 0f;
        if (Input.GetKey(KeyCode.A)) turnInput -= 1f;
        if (Input.GetKey(KeyCode.D)) turnInput += 1f;

        if (Mathf.Abs(turnInput) > 0f)
        {
            Quaternion deltaRotation = Quaternion.Euler(0f, turnInput * rotationSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }

        // Move forward with W relative to current facing
        if (Input.GetKey(KeyCode.W))
        {
            Vector3 forward = rb.rotation * Vector3.forward;
            Vector3 targetPosition = rb.position + forward * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
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