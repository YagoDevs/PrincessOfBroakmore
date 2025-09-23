using UnityEngine;
using UnityEngine.SceneManagement;

public class Menus : MonoBehaviour
{
    public Scene DungeonScene;
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
}
