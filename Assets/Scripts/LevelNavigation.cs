using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Button handlers for the end screens and the pause menu. Replaces GameOverUI.cs.
/// Hook these up in each Button's OnClick list.
/// </summary>
public class LevelNavigation : MonoBehaviour
{
    [Tooltip("Exact name of your main menu scene (it must be in File > Build Profiles / Build Settings).")]
    public string mainMenuSceneName = "MainMenu";

    public void Restart()
    {
        Load(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;

        if (next < SceneManager.sceneCountInBuildSettings)
            Load(next);
        else
            MainMenu(); // last level finished
    }

    public void MainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogWarning($"LevelNavigation: scene '{mainMenuSceneName}' is not in the build settings.");
            return;
        }

        ResetState();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Load(int buildIndex)
    {
        ResetState();
        SceneManager.LoadScene(buildIndex);
    }

    // Always unfreeze before leaving a scene.
    static void ResetState()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}