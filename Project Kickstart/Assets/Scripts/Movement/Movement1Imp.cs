
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Movement1Imp : MonoBehaviour
{
    public float moveForce = 10f; // Used as horizontal speed (units/second)
    public float jumpForce = 7f;  // Used as initial jump vertical speed
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;
    public float maxFallSpeed = -50f;

    private Rigidbody rb;
    private bool isGrounded;
    private float verticalVelocity;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = true; // We move via transform, not physics forces
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            // Fallback: simple raycast from current position if groundCheck is not assigned
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDistance + 0.1f, groundMask);
        }

        if (isGrounded && verticalVelocity < 0f)
        {
            // Small downward bias to keep grounded reliably
            verticalVelocity = -2f;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    void FixedUpdate()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector3.right;
        }

        // Horizontal rotation towards movement direction
        if (direction != Vector3.zero)
        {
            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction);
            transform.rotation = rotation;
        }

        // Apply custom gravity to vertical velocity when not grounded or going up
        if (!isGrounded || verticalVelocity > 0f)
        {
            verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;
            if (verticalVelocity < maxFallSpeed)
            {
                verticalVelocity = maxFallSpeed;
            }
        }

        // Compose final motion vector (horizontal + vertical)
        Vector3 horizontal = (direction != Vector3.zero ? direction : Vector3.zero) * moveForce;
        Vector3 motion = horizontal * Time.fixedDeltaTime + Vector3.up * verticalVelocity * Time.fixedDeltaTime;

        // Prevent excessive downward motion when grounded to avoid tunneling
        if (isGrounded && motion.y < 0f)
        {
            motion.y = -0.02f;
        }

        // Move via Rigidbody for proper kinematic collision resolution
        rb.MovePosition(rb.position + motion);
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