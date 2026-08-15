using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public void Exit()
    {
        Application.Quit();
    }

    public void LoadMainLevel()
    {
        SceneManager.LoadScene(3);
    }

    public void LoadSettingsScene()
    {
        SceneManager.LoadScene(2);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
