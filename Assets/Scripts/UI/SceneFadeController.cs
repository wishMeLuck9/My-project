using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFadeController : MonoBehaviour
{
    private const float ReferenceWidth = 1280f;
    private const float ReferenceHeight = 720f;

    public static SceneFadeController Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;
    private bool waitingForGameplayScene;
    private float pendingDelaySeconds;
    private float pendingFadeSeconds;

    public static SceneFadeController EnsureInstance()
    {
        if (Instance != null) return Instance;

        SceneFadeController existing = FindFirstObjectByType<SceneFadeController>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        return new GameObject("SceneFadeController").AddComponent<SceneFadeController>();
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
        EnsureUi();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    public void HoldBlack()
    {
        EnsureUi();
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    public void FadeOutAfterNextGameplayScene(float delaySeconds, float fadeSeconds)
    {
        pendingDelaySeconds = Mathf.Max(0f, delaySeconds);
        pendingFadeSeconds = Mathf.Max(0f, fadeSeconds);

        if (!waitingForGameplayScene)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            waitingForGameplayScene = true;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!SceneIds.IsGameplay(scene.name)) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        waitingForGameplayScene = false;

        EnsureUi();
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeFromBlackRoutine(pendingDelaySeconds, pendingFadeSeconds));
    }

    private IEnumerator FadeFromBlackRoutine(float delaySeconds, float fadeSeconds)
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        SetGameplayControl(false);

        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

        if (fadeSeconds <= 0f)
        {
            canvasGroup.alpha = 0f;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.gameObject.SetActive(false);
        SetGameplayControl(true);
        fadeRoutine = null;
    }

    private void EnsureUi()
    {
        if (canvasGroup != null) return;

        GameObject canvasObject = new GameObject("SceneFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject backgroundObject = new GameObject("Blackout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = backgroundObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        canvasObject.SetActive(false);
    }

    private static void SetGameplayControl(bool state)
    {
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) player.SetCanMove(state);

        PlayerAttackController attack = FindFirstObjectByType<PlayerAttackController>();
        if (attack != null) attack.SetCanAttack(state);
    }
}
