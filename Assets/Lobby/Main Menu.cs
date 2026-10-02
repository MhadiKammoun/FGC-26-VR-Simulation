using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuVR : MonoBehaviour
{
    [SerializeField] private string targetScene = "Game VR";
    private bool _isLoading = false;

    public void Play()
    {
        if (_isLoading) return;
        StartCoroutine(LoadAndPurge());
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    private IEnumerator LoadAndPurge()
    {
        _isLoading = true;

        // Load new scene and fully unload current scene hierarchy
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        while (!loadOp.isDone) yield return null;

        // Purge all lingering textures, models, and audio from RAM/VRAM
        AsyncOperation purgeOp = Resources.UnloadUnusedAssets();
        while (!purgeOp.isDone) yield return null;

        // Force garbage collector to sweep all remaining memory allocations
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}