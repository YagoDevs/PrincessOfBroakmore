using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraShake : MonoBehaviour
{
    // Singleton para acesso global
    public static CameraShake Instance { get; private set; }
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Lista de câmeras que estão fazendo shake atualmente
    private List<Camera> shakingCameras = new List<Camera>();
    private Dictionary<Camera, Vector3> originalPositions = new Dictionary<Camera, Vector3>();
    
    private void Awake()
    {
        // Implementação singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Faz shake em todas as câmeras ativas na cena
    /// </summary>
    /// <param name="duration">Duração do shake em segundos</param>
    /// <param name="magnitude">Intensidade do shake</param>
    public void ShakeAllActiveCameras(float duration, float magnitude)
    {
        Camera[] allCameras = FindObjectsOfType<Camera>();
        List<Camera> activeCameras = new List<Camera>();
        
        foreach (Camera cam in allCameras)
        {
            if (cam.gameObject.activeInHierarchy && cam.enabled)
            {
                activeCameras.Add(cam);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CameraShake: Found {activeCameras.Count} active cameras to shake");
            foreach (Camera cam in activeCameras)
            {
                Debug.Log($"- Shaking camera: {cam.name}");
            }
        }
        
        StartCoroutine(ShakeMultipleCameras(activeCameras, duration, magnitude));
    }
    
    /// <summary>
    /// Faz shake em uma câmera específica (compatibilidade com código antigo)
    /// </summary>
    /// <param name="duration">Duração do shake em segundos</param>
    /// <param name="magnitude">Intensidade do shake</param>
    public IEnumerator Shake(float duration, float magnitude)
    {
        // Usa a câmera deste objeto se existir
        Camera thisCamera = GetComponent<Camera>();
        if (thisCamera != null && thisCamera.gameObject.activeInHierarchy && thisCamera.enabled)
        {
            yield return StartCoroutine(ShakeSingleCamera(thisCamera, duration, magnitude));
        }
        else
        {
            // Se não há câmera neste objeto, faz shake em todas as ativas
            ShakeAllActiveCameras(duration, magnitude);
        }
    }
    
    /// <summary>
    /// Corrotina para fazer shake em múltiplas câmeras simultaneamente
    /// </summary>
    private IEnumerator ShakeMultipleCameras(List<Camera> cameras, float duration, float magnitude)
    {
        // Salvar posições originais
        foreach (Camera cam in cameras)
        {
            if (!originalPositions.ContainsKey(cam))
            {
                originalPositions[cam] = cam.transform.localPosition;
            }
            if (!shakingCameras.Contains(cam))
            {
                shakingCameras.Add(cam);
            }
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            foreach (Camera cam in cameras)
            {
                if (cam != null && shakingCameras.Contains(cam))
                {
                    Vector3 originalPos = originalPositions[cam];
                    float x = Random.Range(-1f, 1f) * magnitude;
                    float y = Random.Range(-1f, 1f) * magnitude;
                    
                    cam.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restaurar posições originais
        foreach (Camera cam in cameras)
        {
            if (cam != null && originalPositions.ContainsKey(cam))
            {
                cam.transform.localPosition = originalPositions[cam];
                shakingCameras.Remove(cam);
                originalPositions.Remove(cam);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log("CameraShake: Shake completed for all cameras");
        }
    }
    
    /// <summary>
    /// Corrotina para fazer shake em uma única câmera
    /// </summary>
    private IEnumerator ShakeSingleCamera(Camera camera, float duration, float magnitude)
    {
        Vector3 originalPos = camera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            camera.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camera.transform.localPosition = originalPos;
    }
    
    /// <summary>
    /// Para todos os shakes ativos (para casos de emergência)
    /// </summary>
    public void StopAllShakes()
    {
        StopAllCoroutines();
        
        // Restaurar todas as posições
        foreach (var pair in originalPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.transform.localPosition = pair.Value;
            }
        }
        
        shakingCameras.Clear();
        originalPositions.Clear();
        
        if (showDebugInfo)
        {
            Debug.Log("CameraShake: All shakes stopped and positions restored");
        }
    }
}