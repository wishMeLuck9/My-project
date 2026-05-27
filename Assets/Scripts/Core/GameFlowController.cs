using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public bool IsNight { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TransitionToLocation(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

#if UNITY_EDITOR
        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        if (System.IO.File.Exists(scenePath))
        {
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        SceneManager.LoadScene(sceneName);
    }

    public void SetNight(bool state)
    {
        IsNight = state;
    }
}
