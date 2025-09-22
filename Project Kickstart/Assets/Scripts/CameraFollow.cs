using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;   // arraste o Player aqui no inspector
    public Vector3 offset;     // ajuste a distância da câmera pro player
    public float smoothSpeed = 0.125f; // suavização do movimento

    void LateUpdate()
    {
        Vector3 desiredPosition = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Se quiser que a câmera olhe para o player (3D), descomente:
        // transform.LookAt(player);
    }
}
