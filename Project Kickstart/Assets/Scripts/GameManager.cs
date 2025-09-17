using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    GameObject endScreen;

    [SerializeField] SceneLights lights;
    [SerializeField] DimensionManager dimManager;

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
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
        dimManager.SwitchDimension();
        //SwitchDimension();
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