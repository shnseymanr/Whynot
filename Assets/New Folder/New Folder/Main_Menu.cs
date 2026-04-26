using UnityEngine;
using UnityEngine.SceneManagement;

public class Main_Menu : MonoBehaviour
{
    [SerializeField] private string villageSceneName = "VillageScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string settingsSceneName = "Settings Menu";

    public void playGame()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
            return;
        }

        SceneManager.LoadScene(villageSceneName);
    }

    public void mainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void settingsMenu()
    {
        SceneManager.LoadScene(settingsSceneName);
    }

    public void quitGame()
    {
        QuitGame();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void PlayGame()
    {
        StartGame();
    }

    public void MainMenu()
    {
        mainMenu();
    }

    public void SettingsMenu()
    {
        settingsMenu();
    }
}
