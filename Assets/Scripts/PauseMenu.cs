using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject PauseMenue;
    public bool GamePaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!GamePaused)
            {
                PauseGame();
            }
            else
            {
                UnpauseGame();
            }
        }
    }

    public void PauseGame()
    {
        PauseMenue.SetActive(true);
        Time.timeScale = 0f;
        GamePaused = true;

        // Show cursor and unlock it so player can click UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Pause all sounds
        AudioListener.pause = true;
    }

    public void UnpauseGame()
    {
        PauseMenue.SetActive(false);
        Time.timeScale = 1f;
        GamePaused = false;

        // Hide cursor and lock it again for first-person control
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Resume all sounds
        AudioListener.pause = false;
    }
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
