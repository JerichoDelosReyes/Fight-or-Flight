using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string startSceneName = "MainScene";

    public void StartGame()
    {
        SceneManager.LoadScene(startSceneName);
    }

    public void OpenInstructions()
    {
        Debug.Log("Instructions clicked");
        // Add logic to show instructions panel
    }

    public void OpenSettings()
    {
        Debug.Log("Settings clicked");
        // Add logic to show settings panel
    }

    public void QuitGame()
    {
        Debug.Log("Quit clicked");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
