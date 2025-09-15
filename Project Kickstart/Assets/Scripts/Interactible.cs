using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public interface IInteractible
{
    virtual void DoInteraction() { }
    virtual void CheckInteraction() { }
}

public class    Interactible : MonoBehaviour, IInteractible
{
    public GameObject interactionObject = null;
    public bool needObject;

    virtual public void DoInteraction()
    {
        return;
    }

    public void CheckInteraction()
    {
        if (interactionObject == null && needObject)
        {
            print("FUCK YOU");
        }
        else
        {
            print("you may interact");
            DoInteraction();
        }
    }
}