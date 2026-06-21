using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpeningCutsceneController : MonoBehaviour
{
    private static readonly string[] IntroKeys =
    {
        "intro.gameplay.1",
        "intro.gameplay.2",
        "intro.gameplay.3",
        "intro.gameplay.4",
        "intro.gameplay.5"
    };

    private const float NormalFadeSeconds = 0.35f;
    private const float NormalCharacterSeconds = 0.018f;
    private const float PageSettleSeconds = 0.12f;

    public static OpeningCutsceneController Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private GameObject rootObject;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI pageText;
    private TextMeshProUGUI continueButtonText;
    private TextMeshProUGUI skipButtonText;
    private Button continueButton;
    private Button skipButton;

    private readonly List<string> pages = new List<string>();
    private Coroutine playRoutine;
    private Action completeAction;
    private bool advanceRequested;
    private bool skipRequested;
    private bool completing;

    public static OpeningCutsceneController EnsureInstance()
    {
        if (Instance != null) return Instance;

        OpeningCutsceneController existing = FindFirstObjectByType<OpeningCutsceneController>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        return new GameObject("OpeningCutsceneController").AddComponent<OpeningCutsceneController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Play(Action onComplete)
    {
        if (!EnsureUi())
        {
            onComplete?.Invoke();
            return false;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        completeAction = onComplete;
        completing = false;
        playRoutine = StartCoroutine(PlayRoutine());
        return true;
    }

    private IEnumerator PlayRoutine()
    {
        BuildPages();
        if (pages.Count == 0)
        {
            Complete();
            yield break;
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        bool reduceMotion = SettingsManager.Instance != null && SettingsManager.Instance.ReduceMotion;
        float fadeSeconds = reduceMotion ? 0f : NormalFadeSeconds;
        rootObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 0f;

        if (titleText != null) titleText.text = LocalizationManager.EnsureInstance().Get("speaker.system", "SYSTEM");
        yield return FadeTo(1f, fadeSeconds);

        for (int i = 0; i < pages.Count; i++)
        {
            advanceRequested = false;
            skipRequested = false;
            RefreshLabels(i);
            yield return TypePage(pages[i], reduceMotion);

            while (!advanceRequested && !skipRequested)
            {
                yield return null;
            }

            if (skipRequested) break;
        }

        yield return FadeTo(0f, fadeSeconds);
        Complete();
    }

    private void BuildPages()
    {
        pages.Clear();
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        foreach (string key in IntroKeys)
        {
            string page = localizer.Get(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(page)) pages.Add(page);
        }
    }

    private IEnumerator TypePage(string page, bool reduceMotion)
    {
        if (bodyText == null) yield break;

        bodyText.text = string.Empty;

        if (reduceMotion)
        {
            bodyText.text = page;
            yield return new WaitForSecondsRealtime(PageSettleSeconds);
            yield break;
        }

        for (int i = 0; i < page.Length; i++)
        {
            if (advanceRequested || skipRequested)
            {
                bodyText.text = page;
                advanceRequested = false;
                yield return new WaitForSecondsRealtime(PageSettleSeconds);
                yield break;
            }

            bodyText.text = page.Substring(0, i + 1);
            yield return new WaitForSecondsRealtime(NormalCharacterSeconds);
        }

        yield return new WaitForSecondsRealtime(PageSettleSeconds);
    }

    private IEnumerator FadeTo(float targetAlpha, float seconds)
    {
        if (canvasGroup == null) yield break;

        if (seconds <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / seconds));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void RefreshLabels(int pageIndex)
    {
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (continueButtonText != null)
        {
            bool isLastPage = pageIndex >= pages.Count - 1;
            continueButtonText.text = isLastPage
                ? localizer.Get("dialogue.continue", "Continue")
                : localizer.Format("dialogue.next", pageIndex + 1, pages.Count);
        }

        if (skipButtonText != null)
        {
            skipButtonText.text = localizer.Get("dialogue.skip", "Skip");
        }

        if (pageText != null)
        {
            pageText.text = $"{pageIndex + 1}/{pages.Count}";
        }
    }

    private void RequestAdvance()
    {
        advanceRequested = true;
    }

    private void RequestSkip()
    {
        skipRequested = true;
        advanceRequested = true;
    }

    private void Complete()
    {
        if (completing) return;

        completing = true;
        playRoutine = null;
        advanceRequested = false;
        skipRequested = false;

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }

        Action action = completeAction;
        completeAction = null;
        action?.Invoke();
    }

    private bool EnsureUi()
    {
        if (rootObject != null) return true;

        GameObject canvasObject = new GameObject("OpeningCutsceneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        rootObject = canvasObject;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();

        GameObject background = CreateRect("Background", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = Color.black;

        titleText = CreateText("Title", canvasObject.transform, "SYSTEM", 28f, new Color32(255, 58, 58, 255));
        ConfigureRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(980f, 48f));
        titleText.alignment = TextAlignmentOptions.Center;

        bodyText = CreateText("Body", canvasObject.transform, string.Empty, 30f, Color.white);
        ConfigureRect(bodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(900f, 300f));
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Overflow;

        pageText = CreateText("Page", canvasObject.transform, string.Empty, 16f, new Color32(170, 178, 190, 255));
        ConfigureRect(pageText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 122f), new Vector2(180f, 28f));
        pageText.alignment = TextAlignmentOptions.Center;

        continueButton = CreateButton("ContinueButton", canvasObject.transform, new Vector2(152f, 64f));
        ConfigureRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 48f), new Vector2(220f, 52f));
        continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        continueButton.onClick.AddListener(RequestAdvance);

        skipButton = CreateButton("SkipButton", canvasObject.transform, new Vector2(152f, 64f));
        ConfigureRect(skipButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(16f, 48f), new Vector2(160f, 52f));
        skipButtonText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
        skipButton.onClick.AddListener(RequestSkip);

        rootObject.SetActive(false);
        return canvasGroup != null && bodyText != null && continueButton != null && skipButton != null;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return obj;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.outlineColor = Color.black;
        label.outlineWidth = 0.12f;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 padding)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.9f, 0.92f, 0.94f, 1f);

        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateText("Label", obj.transform, string.Empty, 19f, Color.black);
        label.outlineWidth = 0f;
        label.alignment = TextAlignmentOptions.Center;
        ConfigureRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        label.rectTransform.offsetMin = new Vector2(padding.x * 0.12f, padding.y * 0.08f);
        label.rectTransform.offsetMax = new Vector2(-padding.x * 0.12f, -padding.y * 0.08f);

        return button;
    }
}
