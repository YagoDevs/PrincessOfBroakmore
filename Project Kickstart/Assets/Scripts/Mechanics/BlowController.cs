using System;
using System.Collections.Generic;
using UnityEngine;

public class BlowController : MonoBehaviour
{
    private Rigidbody ObjectRb;
    public float pushForce;
    public Camera Camera; // Mantido para compatibilidade, mas não será mais usado
    public Animator Animator;
    
    [Header("Camera Shake Settings")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;
    
    // List of boxes that are in the trigger
    private List<Rigidbody> pushableObjects = new List<Rigidbody>();
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Check F in Update (more reliable)
        if(Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("F detected in Update!");
            Animator.SetTrigger("Screaming");

            // Usar o novo sistema de CameraShake
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeAllActiveCameras(shakeDuration, shakeMagnitude);
                Debug.Log("CameraShake: Shaking all active cameras!");
            }
            else
            {
                Debug.LogWarning("CameraShake.Instance is null! Make sure there's a CameraShake object in the scene.");
                
                // Fallback: tentar usar o sistema antigo se a câmera específica ainda existir
                if (Camera != null && Camera.GetComponent<CameraShake>() != null)
                {
                    StartCoroutine(Camera.GetComponent<CameraShake>().Shake(shakeDuration, shakeMagnitude));
                    Debug.Log("Using fallback camera shake system");
                }
            }
            
            // Push all nearby boxes
            PushNearbyObjects();
        }
    }
    
    /// <summary>
    /// Pushes all objects that are in the trigger
    /// </summary>
    private void PushNearbyObjects()
    {
        if (pushableObjects.Count == 0)
        {
            Debug.Log("No nearby boxes to push!");
            return;
        }
        
        foreach (Rigidbody rb in pushableObjects)
        {
            if (rb != null)
            {
                Debug.Log($"Pushing: {rb.name}");
                Debug.Log($"isKinematic: {rb.isKinematic}");
                Debug.Log($"Mass: {rb.mass}");
                
                Vector3 pushDirection = transform.forward;
                Debug.Log($"Direction: {pushDirection}");
                Debug.Log($"Force: {pushDirection * pushForce}");
                
                // Try AddForce first
                rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                Debug.Log("AddForce applied!");
                
                // Backup: apply velocity directly
                rb.linearVelocity += pushDirection * (pushForce / rb.mass);
                Debug.Log($"Final velocity: {rb.linearVelocity}");
                
                // Play push sound
                if (AudioManager.Instance != null)
                {
                    Debug.Log($"🔊 BlowController: Playing push sound for {rb.name}");
                    AudioManager.Instance.PlayBoxPushSound(rb.transform.position, pushForce / 10f);
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Just detects - does nothing here
        if (other.CompareTag("Pushable"))
        {
            Debug.Log($"Box detected: {other.name} at position {other.transform.position}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pushable"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && !pushableObjects.Contains(rb))
            {
                pushableObjects.Add(rb);
                Debug.Log($"Box added to list: {other.name} (Total: {pushableObjects.Count})");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pushable"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && pushableObjects.Contains(rb))
            {
                pushableObjects.Remove(rb);
                Debug.Log($"Box removed from list: {other.name} (Total: {pushableObjects.Count})");
            }
        }
    }
}
