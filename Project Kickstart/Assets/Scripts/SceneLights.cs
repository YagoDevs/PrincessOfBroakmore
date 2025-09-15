using UnityEngine;

public class SceneLights : MonoBehaviour
{
    Light lights;

    Color manicColor = new Color(108f / 255f, 0f, 255f / 255f);
    Color nonManicColor = new Color(253f / 216f, 0f, 149f / 225f);

    //public bool change;
    void Start()
    {
        lights = GetComponentInChildren<Light>();
    }

    //private void Update()
    //{
    //    if (change) LightsChange();
    //}

    public void LightsChange()
    {
        print("made it here");
        if (GameStats.isManic) lights.color = manicColor;
        else lights.color = nonManicColor;
    }
}
