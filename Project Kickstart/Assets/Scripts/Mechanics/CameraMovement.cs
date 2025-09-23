using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    GameObject player;

    private void Start()
    {
        player = FindFirstObjectByType<Movement2>().gameObject;
    }
    void Update()
    {
        
    }
}
