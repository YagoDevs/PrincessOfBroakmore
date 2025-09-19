using UnityEngine;

public class DoorInteraction : Interactible
{
    override public void DoInteraction()
    {
        print("yo it works");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CheckInteraction();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
