using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main_Menu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game Scene";
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private string settingsSceneName = "Settings Menu";

    public void playGame()
    {
        Debug.Log("Play butonuna basıldı -> Oyun sahnesi yükleniyor");
        SceneManager.LoadScene(gameSceneName);
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
        Debug.Log("Quit butonuna basıldı");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}