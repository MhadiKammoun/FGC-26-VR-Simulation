using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    // Call this from the Start button
    public void LoadGame()
    {
        SceneManager.LoadScene("Hi");
    }

    // Call this from the Exit button
    public void UnLoadGame()
    {
        Application.Quit();

        // This line makes the Exit button also work inside the Unity Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}