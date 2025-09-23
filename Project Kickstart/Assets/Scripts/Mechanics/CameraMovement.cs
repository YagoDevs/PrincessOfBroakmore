using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform player;     // Assign your player in the inspector
    public Vector3 offset;       // The distance between the camera and the player
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (player == null) return;

        // Desired position
        Vector3 targetPosition = player.position + offset;

        // Smooth follow
        transform.localPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // Keep the same rotation (isometric angle)
        // Or if you want the camera to always look at player:
        // transform.LookAt(player.position);
    }
}
