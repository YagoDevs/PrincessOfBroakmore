using UnityEngine;

public class CameraMovement : MonoBehaviour
{
 public Transform player;     // Assign your player in the inspector
    public Vector3 offset;       // The distance between the camera and the player
    public Vector3 newOffset;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (player == null) return;

        // Desired position
        Vector3 targetPosition = player.position + offset;

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        //if(Input.GetKey(KeyCode.Tab))
        //{
        //    offset = newOffset;
        //    transform.rotation = Quaternion.Euler(37.3900261f, 125.438133f, -2.79390097e-05f);
        //}

        // Keep the same rotation (isometric angle)
        // Or if you want the camera to always look at player:
        // transform.LookAt(player.position);

        //Manic offset should be -6.4, 9.3, 12
        //rotation should be Vector3(37.3900261,125.438133,-2.79390097e-05)
    }

    public void Rotate()
    {
        offset = newOffset;
        transform.rotation = Quaternion.Euler(37.3900261f, 125.438133f, -2.79390097e-05f);
    }
}
