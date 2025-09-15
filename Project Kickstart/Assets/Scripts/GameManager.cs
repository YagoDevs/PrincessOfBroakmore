using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    GameObject endScreen;
    public UnityEvent enterManic = new UnityEvent();
    public UnityEvent exitManic = new UnityEvent();

    private void OnEnable()
    {
        GameStats.OnManicChanged += HandleManicChange;
    }

    void Start()
    {
        endScreen = FindFirstObjectByType(typeof(Canvas)) as GameObject;
    }

    void HandleManicChange(bool isManic)
    {
        if (isManic) enterManic.Invoke();
        else exitManic.Invoke();
    }

    public void EnterManic()
    {
        //change color of lights
        //change vignette?
    }

    public void ExitManic()
    {
        //opposite of above
    }

    public void OnGameEnd()
    {
        endScreen.SetActive(true);
        GameStats.isManic = false;
    }

    private void OnDisable()
    {
        GameStats.OnManicChanged -= HandleManicChange;
    }
}
