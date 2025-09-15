using UnityEngine;

public class Mirror : MonoBehaviour
{
    Movement2 Movement2;
    GameObject player;

    private void Start()
    {
        player = FindFirstObjectByType<Movement2>().gameObject;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Player") return;
        GameStats.isManic = !GameStats.isManic;
    }
}
