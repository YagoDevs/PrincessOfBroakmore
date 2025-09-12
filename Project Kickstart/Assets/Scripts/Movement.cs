
//using System;
//using Unity.VisualScripting;
//using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
//public class CharacterMovement : MonoBehaviour
//{
//    public float moveForce = 10f;
//    public float pushForce = 0f;
//    private Rigidbody rb;
//    private Rigidbody ObjectRb;


//    void Start()
//    {
//        rb = GetComponent<Rigidbody>();
//        rb.freezeRotation = true;
//    }


//    void FixedUpdate()
//    {
//        Vector3 direction = Vector3.zero;

//        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
//        if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
//        if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
//        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;

//        if (direction != Vector3.zero)
//        {
//            direction.Normalize();
//            Quaternion rotation = Quaternion.LookRotation(direction);
//            rb.MoveRotation(rotation);
//            rb.AddForce(direction * moveForce, ForceMode.Force);
//        }
//    }


//}


using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    public float moveForce = 10f;
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
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;

        if (direction != Vector3.zero)
        {
            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(rotation);
            rb.AddForce(direction * moveForce, ForceMode.Force);
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