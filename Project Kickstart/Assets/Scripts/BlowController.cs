using System;
using UnityEngine;

public class BlowController : MonoBehaviour
{
    private Rigidbody ObjectRb;
    public float pushForce;
    public Camera Camera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(Camera.GetComponent<CameraShake>().Shake(0.5f, 0.1f));
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Pushable") && (Input.GetKeyDown(KeyCode.F)))
        {
            Debug.Log("It works");
            StartCoroutine(Camera.GetComponent<CameraShake>().Shake(0.5f, 0.1f));
            ObjectRb = other.GetComponent<Rigidbody>();
            Vector3 pushDirection = transform.forward;
            ObjectRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        }
    }
}
