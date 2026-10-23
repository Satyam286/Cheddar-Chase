using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Minigame");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit"); // works only in build
    }
}
