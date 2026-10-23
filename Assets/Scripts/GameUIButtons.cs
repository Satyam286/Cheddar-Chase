
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIButtons : MonoBehaviour
{
    public string mainMenuScene = "MainMenu";

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void NextLevel()
    {
        Debug.Log("Next Level button clicked!");
    }
}
