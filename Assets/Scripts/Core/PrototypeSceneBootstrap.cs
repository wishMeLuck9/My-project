using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureCoreManagers();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCoreManagers();
    }

    private static void EnsureCoreManagers()
    {
        DialogueController.EnsureInstance();
        RuntimeHudController.EnsureInstance();

        GameObject managers = GameObject.Find("Managers") ?? GameObject.Find("Managers_Runtime");
        if (managers == null) managers = new GameObject("Managers_Runtime");

        if (WorldState.Instance == null && Object.FindFirstObjectByType<WorldState>() == null)
        {
            managers.AddComponent<WorldState>();
        }

        if (GameFlowController.Instance == null && Object.FindFirstObjectByType<GameFlowController>() == null)
        {
            managers.AddComponent<GameFlowController>();
        }
    }
}
