using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyLoader : MonoBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";
    private bool _isLoading = false;

    // Call this void from your UI Button OnClick or VR interaction
    public void GoToLobby()
    {
        if (_isLoading) return;
        StartCoroutine(LoadAndPurge());
    }

    private IEnumerator LoadAndPurge()
    {
        _isLoading = true;

        // 1. Unload the current scene and load Lobby
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(lobbySceneName, LoadSceneMode.Single);
        while (!loadOp.isDone) yield return null;

        // 2. Dump all cached models, textures, and audio from memory
        AsyncOperation purgeOp = Resources.UnloadUnusedAssets();
        while (!purgeOp.isDone) yield return null;

        // 3. Force garbage collection sweep
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}