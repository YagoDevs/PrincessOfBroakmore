using System;
using System.Collections.Generic;
using UnityEngine;

public class BlowController : MonoBehaviour
{
    private Rigidbody ObjectRb;
    public float pushForce;
    public Camera Camera;
    
    // Lista de caixas que estão no trigger
    private List<Rigidbody> pushableObjects = new List<Rigidbody>();
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Verifica F no Update (mais confiável)
        if(Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("F detectado no Update!");
            StartCoroutine(Camera.GetComponent<CameraShake>().Shake(0.5f, 0.1f));
            
            // Empurra todas as caixas próximas
            PushNearbyObjects();
        }
    }
    
    /// <summary>
    /// Empurra todos os objetos que estão no trigger
    /// </summary>
    private void PushNearbyObjects()
    {
        if (pushableObjects.Count == 0)
        {
            Debug.Log("Nenhuma caixa próxima para empurrar!");
            return;
        }
        
        foreach (Rigidbody rb in pushableObjects)
        {
            if (rb != null)
            {
                Debug.Log($"Empurrando: {rb.name}");
                Debug.Log($"isKinematic: {rb.isKinematic}");
                Debug.Log($"Mass: {rb.mass}");
                
                Vector3 pushDirection = transform.forward;
                Debug.Log($"Direção: {pushDirection}");
                Debug.Log($"Força: {pushDirection * pushForce}");
                
                // Tenta AddForce primeiro
                rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                Debug.Log("AddForce aplicado!");
                
                // Backup: aplica velocity diretamente
                rb.velocity += pushDirection * (pushForce / rb.mass);
                Debug.Log($"Velocity final: {rb.velocity}");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Apenas detecta - não faz nada aqui
        if (other.CompareTag("Pushable"))
        {
            Debug.Log($"Caixa detectada: {other.name} na posição {other.transform.position}");
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
                Debug.Log($"Caixa adicionada à lista: {other.name} (Total: {pushableObjects.Count})");
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
                Debug.Log($"Caixa removida da lista: {other.name} (Total: {pushableObjects.Count})");
            }
        }
    }
}
