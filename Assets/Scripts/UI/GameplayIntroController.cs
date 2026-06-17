using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayIntroController : MonoBehaviour
{
    private static readonly string[] IntroKeys =
    {
        "intro.gameplay.1",
        "intro.gameplay.2",
        "intro.gameplay.3",
        "intro.gameplay.4",
        "intro.gameplay.5"
    };

    public static GameplayIntroController Instance { get; private set; }

    private bool introPlaying;

    public static GameplayIntroController EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameplayIntroController existing = FindFirstObjectByType<GameplayIntroController>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        return new GameObject("GameplayIntroController").AddComponent<GameplayIntroController>();
    }

    public static bool ShouldShowIntroForScene(string sceneName)
    {
        return sceneName == SceneIds.Exterior
            && WorldState.Instance != null
            && !WorldState.Instance.introShown;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        TryStartIntro(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryStartIntro(scene);
    }

    private void TryStartIntro(Scene scene)
    {
        if (introPlaying || !ShouldShowIntroForScene(scene.name)) return;

        StartCoroutine(PlayIntroNextFrame());
    }

    private IEnumerator PlayIntroNextFrame()
    {
        introPlaying = true;
        yield return null;

        if (!ShouldShowIntroForScene(SceneManager.GetActiveScene().name))
        {
            introPlaying = false;
            yield break;
        }

        WorldState.Instance.introShown = true;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        List<string> pages = new List<string>(IntroKeys.Length);
        foreach (string key in IntroKeys)
        {
            pages.Add(localizer.Get(key));
        }

        DialogueController.EnsureInstance().ShowDialoguePages(
            localizer.Get("speaker.system", "SYSTEM"),
            pages,
            () => introPlaying = false);
    }
}
