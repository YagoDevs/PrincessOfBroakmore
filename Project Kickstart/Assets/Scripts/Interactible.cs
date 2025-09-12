using UnityEngine;

public interface IInteractible
{
    virtual void Interaction() { }
    virtual void CheckInteraction() { }
}

public class Damageable : IInteractible
{
    public GameObject interactionObject;

    public void CheckInteraction()
    {
        
    }
}

public class Interactible : MonoBehaviour
{

    public GameObject interactionObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
