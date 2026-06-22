using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBootstrap
{
    private const float DefaultMoveSpeed = 5f;
    private const float DefaultRotationSpeed = 10f;
    private const float DefaultMaxTurnDegreesPerSecond = 240f;
    private const float LateSceneMoveSpeed = 5.75f;
    private const float LateSceneRotationSpeed = 12.5f;
    private const float LateSceneMaxTurnDegreesPerSecond = 300f;

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
        LocalizationManager.EnsureInstance();
        SettingsManager.EnsureInstance();
        SaveGameManager.EnsureInstance();
        EnsureAudioListener();

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

        if (!SceneIds.IsGameplay(SceneManager.GetActiveScene())) return;

        DialogueController.EnsureInstance();
        RuntimeHudController.EnsureInstance();
        PauseMenuController.EnsureInstance();
        GameplayIntroController.EnsureInstance();
        ExteriorBoundaryController.EnsureForCurrentScene();
        ConfigureGameplayPlayer(SceneManager.GetActiveScene().name);
    }

    private static void ConfigureGameplayPlayer(string sceneName)
    {
        PlayerController3D player = Object.FindFirstObjectByType<PlayerController3D>();
        if (player == null) return;

        player.ConfigureTraversal(true);
        if (player.GetComponent<PlayerClimbController>() == null)
        {
            player.gameObject.AddComponent<PlayerClimbController>();
        }

        if (sceneName == SceneIds.Night || sceneName == SceneIds.Final)
        {
            player.ConfigureLocomotion(LateSceneMoveSpeed, LateSceneRotationSpeed, LateSceneMaxTurnDegreesPerSecond);
            return;
        }

        player.ConfigureLocomotion(DefaultMoveSpeed, DefaultRotationSpeed, DefaultMaxTurnDegreesPerSecond);
    }

    private static void EnsureAudioListener()
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (listeners.Length > 0) return;

        Camera targetCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (targetCamera != null)
        {
            targetCamera.gameObject.AddComponent<AudioListener>();
            return;
        }

        GameObject listenerObject = new GameObject("RuntimeAudioListener");
        listenerObject.AddComponent<AudioListener>();
    }
}
