using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    GameObject endScreen;

    [SerializeField] SceneLights lights;

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
        if (isManic) EnterManic();
        else ExitManic();
    }

    public void EnterManic()
    {
        print("manic");
        lights.LightsChange();
        //change color of lights
        //change vignette?
    }

    public void ExitManic()
    {
        print("not manic");
        lights.LightsChange();
        //opposite of above
    }

    public void OnGameEnd()
    {
        endScreen.SetActive(true);
    }

    private void OnDisable()
    {
        GameStats.OnManicChanged -= HandleManicChange;
    }
}