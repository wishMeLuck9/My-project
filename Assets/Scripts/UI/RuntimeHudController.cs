using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RuntimeHudController : MonoBehaviour
{
    private const string ExteriorScene = "LOCATION_01_EXTERIOR_DAY";
    private const string NightScene = "LOCATION_02_PROTECTED_ALLEYS_NIGHT";
    private const float ExteriorHintDelay = 45f;

    public static RuntimeHudController Instance { get; private set; }

    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI controlsText;
    private TextMeshProUGUI interactionText;
    private TextMeshProUGUI stabilityText;
    private TextMeshProUGUI systemText;
    private TextMeshProUGUI purgatoryText;
    private GameObject controlsPanel;
    private GameObject interactionPanel;
    private GameObject stabilityPanel;
    private GameObject systemPanel;
    private GameObject pausePanel;
    private GameObject purgatoryPanel;
    private GameObject minimapPanel;
    private RectTransform minimapExitMarker;
    private RawImage minimapImage;
    private Camera minimapCamera;
    private RenderTexture minimapTexture;
    private PlayerController3D player;
    private PlayerInputReader inputReader;
    private InteractionController interaction;
    private ExteriorHuntController hunt;
    private Transform minimapExit;
    private bool paused;
    private bool startupShown;
    private bool nightUnlockShown;
    private bool nightMapHintShown;
    private bool exteriorHintShown;
    private float exteriorElapsed;
    private float nextReferenceResolveAt;
    private string currentScene;
    private string lastControlsText;
    private string lastObjectiveText;
    private string lastStabilityText;
    private Coroutine clearSystemMessageRoutine;

    public bool IsPaused => paused;

    public static RuntimeHudController EnsureInstance()
    {
        if (Instance != null) return Instance;

        RuntimeHudController existing = FindFirstObjectByType<RuntimeHudController>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        return new GameObject("RuntimeHudCanvas").AddComponent<RuntimeHudController>();
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
        BuildRuntimeUi();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        BindInputReader(null);
        Time.timeScale = 1f;
        ReleaseMinimap();
        Instance = null;
    }

    private void Update()
    {
        ResolveSceneReferences();
        UpdateCursor();
        UpdateControls();
        UpdateObjective();
        UpdateInteractionPrompt();
        UpdateStability();
        UpdateExteriorHint();
    }

    private void LateUpdate()
    {
        UpdateMinimap();
    }

    public void NotifyNightUnlocked()
    {
        if (nightUnlockShown) return;

        nightUnlockShown = true;
        ShowSystemMessage("\u041d\u043e\u0447\u044c \u043f\u0440\u0438\u0437\u043d\u0430\u043b\u0430 \u0432 \u0442\u0435\u0431\u0435 \u0441\u0438\u043b\u0443. \u0423\u0434\u0430\u0440 \u0434\u043e\u0441\u0442\u0443\u043f\u0435\u043d.", 4f);
    }

    public void ShowSystemMessage(string message, float duration = 3.5f)
    {
        if (clearSystemMessageRoutine != null) StopCoroutine(clearSystemMessageRoutine);

        SetSystemMessage(message);
        clearSystemMessageRoutine = StartCoroutine(ClearSystemMessageAfterDelay(duration));
    }

    public void ShowPurgatoryTransition(string message)
    {
        purgatoryText.text = message;
        purgatoryPanel.SetActive(true);
    }

    public void HidePurgatoryTransition()
    {
        purgatoryPanel.SetActive(false);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene);
    }

    private void ApplyScene(Scene scene)
    {
        currentScene = scene.name;
        player = null;
        interaction = null;
        hunt = null;
        minimapExit = null;
        nextReferenceResolveAt = 0f;
        lastControlsText = null;
        lastObjectiveText = null;
        lastStabilityText = null;
        BindInputReader(null);

        if (currentScene == ExteriorScene && !startupShown)
        {
            startupShown = true;
            StartCoroutine(PlayStartupSequence());
        }

        ConfigureMinimap(currentScene == NightScene);
        if (currentScene == NightScene && !nightMapHintShown)
        {
            nightMapHintShown = true;
            ShowSystemMessage("SYSTEM // \u041a\u0430\u0440\u0442\u0430 \u043a\u0432\u0430\u0434\u0440\u0430\u0442\u0430 \u0430\u043a\u0442\u0438\u0432\u043d\u0430. \u0412\u044b\u0445\u043e\u0434 \u043e\u0442\u043c\u0435\u0447\u0435\u043d \u0433\u043e\u043b\u0443\u0431\u044b\u043c.", 4f);
        }
    }

    private void ResolveSceneReferences()
    {
        if (Time.unscaledTime < nextReferenceResolveAt) return;
        nextReferenceResolveAt = Time.unscaledTime + 0.5f;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController3D>();
            BindInputReader(player != null ? player.GetComponent<PlayerInputReader>() : null);
        }

        if (interaction == null && player != null) interaction = player.GetComponent<InteractionController>();
        if (hunt == null) hunt = FindFirstObjectByType<ExteriorHuntController>();
        if (minimapExit == null && currentScene == NightScene)
        {
            GameObject exit = GameObject.Find("EXIT_To_FinalGate_Exit");
            if (exit != null) minimapExit = exit.transform;
        }
    }

    private void BindInputReader(PlayerInputReader newInputReader)
    {
        if (inputReader == newInputReader) return;
        if (inputReader != null) inputReader.PausePressed -= HandlePausePressed;
        inputReader = newInputReader;
        if (inputReader != null) inputReader.PausePressed += HandlePausePressed;
    }

    private void HandlePausePressed()
    {
        SetPaused(!paused);
    }

    private void SetPaused(bool state)
    {
        paused = state;
        Time.timeScale = paused ? 0f : 1f;
        pausePanel.SetActive(paused);
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        bool dialogueOpen = DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen;
        bool shouldRelease = paused || dialogueOpen || purgatoryPanel.activeSelf;
        Cursor.lockState = shouldRelease ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldRelease;
    }

    private void UpdateControls()
    {
        string attackHint = currentScene == NightScene ? "\n[LMB] \u0410\u0442\u0430\u043a\u0430" : string.Empty;
        string text = "[WASD] \u0411\u0435\u0433\n[SHIFT] \u0425\u043e\u0434\u044c\u0431\u0430\n[SPACE] \u041f\u0440\u044b\u0436\u043e\u043a\n[E] \u0412\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0435"
            + attackHint
            + "\n[ESC] \u041f\u0430\u0443\u0437\u0430";
        if (lastControlsText == text) return;

        lastControlsText = text;
        controlsText.text = text;
        controlsPanel.SetActive(true);
    }

    private void UpdateObjective()
    {
        WorldState state = WorldState.Instance;
        string text;
        if (currentScene == ExteriorScene)
        {
            text = state != null && state.hasExteriorFragment
                ? "SYSTEM // \u0426\u0415\u041b\u042c: \u0434\u043e\u0431\u0435\u0440\u0438\u0441\u044c \u0434\u043e \u0434\u0432\u0435\u0440\u0438 \u043a\u0432\u0430\u0434\u0440\u0430\u0442\u0430"
                : "SYSTEM // \u0426\u0415\u041b\u042c: \u043d\u0430\u0439\u0434\u0438 \u0444\u0440\u0430\u0433\u043c\u0435\u043d\u0442 \u0441\u0432\u0435\u0442\u0430";
        }
        else if (currentScene == NightScene)
        {
            text = state != null && state.hasInnerNightFragment
                ? "SYSTEM // \u0426\u0415\u041b\u042c: \u0434\u043e\u0431\u0435\u0440\u0438\u0441\u044c \u0434\u043e \u0432\u044b\u0445\u043e\u0434\u0430"
                : "SYSTEM // \u0426\u0415\u041b\u042c: \u043d\u0430\u0431\u043b\u044e\u0434\u0430\u0439 \u0438\u043b\u0438 \u0434\u0435\u0439\u0441\u0442\u0432\u0443\u0439";
        }
        else
        {
            text = "SYSTEM // \u0426\u0415\u041b\u042c: \u0434\u043e\u0439\u0434\u0438 \u0434\u043e \u0432\u0440\u0430\u0442";
        }

        if (lastObjectiveText == text) return;
        lastObjectiveText = text;
        objectiveText.text = text;
    }

    private void UpdateInteractionPrompt()
    {
        interactionPanel.SetActive(interaction != null && interaction.HasNearbyInteractable && !paused);
    }

    private void UpdateStability()
    {
        bool visible = hunt != null && hunt.IsHunting && WorldState.Instance != null;
        stabilityPanel.SetActive(visible);
        if (!visible) return;

        int remaining = Mathf.Clamp(5 - WorldState.Instance.exteriorCaptureCount, 0, 5);
        string text = "\u0423\u0421\u0422\u041e\u0419\u0427\u0418\u0412\u041e\u0421\u0422\u042c  " + new string('\u25a0', remaining) + new string('\u25a1', 5 - remaining);
        if (lastStabilityText == text) return;

        lastStabilityText = text;
        stabilityText.text = text;
    }

    private void UpdateExteriorHint()
    {
        if (currentScene != ExteriorScene || exteriorHintShown || WorldState.Instance == null) return;
        if (WorldState.Instance.hasExteriorFragment) return;

        exteriorElapsed += Time.deltaTime;
        if (exteriorElapsed < ExteriorHintDelay) return;

        exteriorHintShown = true;
        ShowSystemMessage("\u0415\u0441\u043b\u0438 \u0442\u044b \u043d\u0435 \u0432\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u0443\u0435\u0448\u044c, \u0442\u044b \u0438\u0441\u0447\u0435\u0437\u0430\u0435\u0448\u044c.", 5f);
    }

    private IEnumerator PlayStartupSequence()
    {
        yield return ShowStartupLine("\u041f\u0440\u043e\u0442\u043e\u043a\u043e\u043b \u043a\u0432\u0430\u0434\u0440\u0430\u0442\u0430 \u0430\u043a\u0442\u0438\u0432\u0435\u043d.");
        yield return ShowStartupLine("\u0424\u0440\u0430\u0433\u043c\u0435\u043d\u0442 \u043e\u0431\u043d\u0430\u0440\u0443\u0436\u0435\u043d.");
        yield return ShowStartupLine("\u0422\u044b \u043d\u0430\u0443\u0447\u0438\u043b\u0441\u044f \u0438\u0434\u0442\u0438. \u042d\u0442\u043e\u0433\u043e \u0434\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e, \u0447\u0442\u043e\u0431\u044b \u043d\u0430\u0447\u0430\u0442\u044c.\n\u0412\u044b\u0436\u0438\u0432\u0435\u0448\u044c \u043b\u0438 \u0442\u044b - \u0437\u0430\u0432\u0438\u0441\u0438\u0442 \u043e\u0442 \u0442\u0435\u0431\u044f.");
    }

    private IEnumerator ShowStartupLine(string line)
    {
        SetSystemMessage(line);
        yield return new WaitForSecondsRealtime(2.4f);
        SetSystemMessage(string.Empty);
        yield return new WaitForSecondsRealtime(0.25f);
    }

    private IEnumerator ClearSystemMessageAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        SetSystemMessage(string.Empty);
        clearSystemMessageRoutine = null;
    }

    private void SetSystemMessage(string message)
    {
        systemText.text = message;
        systemPanel.SetActive(!string.IsNullOrWhiteSpace(message));
    }

    private void ConfigureMinimap(bool enabled)
    {
        minimapPanel.SetActive(enabled);
        if (!enabled)
        {
            ReleaseMinimap();
            return;
        }

        if (minimapTexture == null)
        {
            minimapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
            {
                name = "RuntimeNightMinimap"
            };
            minimapTexture.Create();
        }

        if (minimapCamera == null)
        {
            GameObject cameraObject = new GameObject("RuntimeNightMinimapCamera");
            DontDestroyOnLoad(cameraObject);
            minimapCamera = cameraObject.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 18f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.015f, 0.015f, 0.035f, 1f);
            minimapCamera.depth = -10f;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        minimapCamera.targetTexture = minimapTexture;
        minimapImage.texture = minimapTexture;
    }

    private void UpdateMinimap()
    {
        if (minimapCamera == null || player == null || currentScene != NightScene) return;

        Vector3 playerPosition = player.transform.position;
        minimapCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y + 32f, playerPosition.z);
        if (minimapExitMarker == null || minimapExit == null) return;

        Vector3 delta = minimapExit.position - playerPosition;
        float halfSize = 82f;
        float scale = halfSize / minimapCamera.orthographicSize;
        minimapExitMarker.anchoredPosition = new Vector2(
            Mathf.Clamp(delta.x * scale, -halfSize, halfSize),
            Mathf.Clamp(delta.z * scale, -halfSize, halfSize));
    }

    private void ReleaseMinimap()
    {
        if (minimapCamera != null) Destroy(minimapCamera.gameObject);
        minimapCamera = null;
        if (minimapTexture != null)
        {
            minimapTexture.Release();
            Destroy(minimapTexture);
        }

        minimapTexture = null;
    }

    private void BuildRuntimeUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        gameObject.AddComponent<GraphicRaycaster>();

        controlsPanel = CreatePanel("ControlsPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(18f, -18f), new Vector2(380f, 220f), new Vector2(0f, 1f));
        objectiveText = CreateText("Objective", controlsPanel.transform, string.Empty, 18f, new Vector2(12f, -10f), new Vector2(356f, 52f), new Vector2(0f, 1f));
        controlsText = CreateText("Controls", controlsPanel.transform, string.Empty, 18f, new Vector2(12f, -68f), new Vector2(356f, 144f), new Vector2(0f, 1f));

        interactionPanel = CreatePanel("InteractionPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(0f, 18f), new Vector2(420f, 54f), new Vector2(0.5f, 0f));
        interactionText = CreateText("InteractionPrompt", interactionPanel.transform, "[E] \u0412\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u043e\u0432\u0430\u0442\u044c", 22f, Vector2.zero, new Vector2(400f, 44f), new Vector2(0.5f, 0.5f));
        interactionText.alignment = TextAlignmentOptions.Center;
        interactionPanel.SetActive(false);

        stabilityPanel = CreatePanel("StabilityPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(0f, -18f), new Vector2(500f, 54f), new Vector2(0.5f, 1f));
        stabilityText = CreateText("Stability", stabilityPanel.transform, string.Empty, 24f, Vector2.zero, new Vector2(480f, 44f), new Vector2(0.5f, 0.5f));
        stabilityText.alignment = TextAlignmentOptions.Center;
        stabilityPanel.SetActive(false);

        systemPanel = CreatePanel("SystemPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(18f, -252f), new Vector2(680f, 112f), new Vector2(0f, 1f));
        systemText = CreateText("SystemMessage", systemPanel.transform, string.Empty, 22f, new Vector2(12f, -10f), new Vector2(656f, 92f), new Vector2(0f, 1f));
        systemPanel.SetActive(false);

        minimapPanel = CreatePanel("MinimapPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.95f), new Vector2(-18f, 18f), new Vector2(190f, 190f), new Vector2(1f, 0f));
        GameObject minimapImageObject = CreateRect("MinimapImage", minimapPanel.transform, Vector2.zero, new Vector2(174f, 174f), new Vector2(0.5f, 0.5f));
        minimapImage = minimapImageObject.AddComponent<RawImage>();
        CreateImage("PlayerMarker", minimapPanel.transform, new Color(0.95f, 0.9f, 0.35f, 1f), Vector2.zero, new Vector2(8f, 8f), new Vector2(0.5f, 0.5f)).SetAsLastSibling();
        minimapExitMarker = CreateImage("ExitMarker", minimapPanel.transform, new Color(0.35f, 0.85f, 1f, 1f), Vector2.zero, new Vector2(10f, 10f), new Vector2(0.5f, 0.5f));
        minimapExitMarker.SetAsLastSibling();

        pausePanel = CreatePanel("PausePanel", transform, new Color(0f, 0f, 0f, 0.92f), Vector2.zero, new Vector2(420f, 310f), new Vector2(0.5f, 0.5f));
        TextMeshProUGUI pauseTitle = CreateText("PauseTitle", pausePanel.transform, "\u041f\u0410\u0423\u0417\u0410", 32f, new Vector2(0f, -28f), new Vector2(380f, 48f), new Vector2(0.5f, 1f));
        pauseTitle.alignment = TextAlignmentOptions.Center;
        CreateButton("ResumeButton", pausePanel.transform, "\u041f\u0440\u043e\u0434\u043e\u043b\u0436\u0438\u0442\u044c", new Vector2(0f, 38f), () => SetPaused(false));
        CreateButton("ControlsButton", pausePanel.transform, "\u0423\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u0438\u0435", new Vector2(0f, -22f), () => controlsPanel.SetActive(true));
        CreateButton("ExitButton", pausePanel.transform, "\u0412\u044b\u0439\u0442\u0438 \u0438\u0437 \u0438\u0433\u0440\u044b", new Vector2(0f, -82f), QuitGame);
        pausePanel.SetActive(false);

        purgatoryPanel = CreatePanel("PurgatoryPanel", transform, Color.black, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), true);
        purgatoryText = CreateText("PurgatoryText", purgatoryPanel.transform, string.Empty, 28f, Vector2.zero, new Vector2(900f, 180f), new Vector2(0.5f, 0.5f));
        purgatoryText.alignment = TextAlignmentOptions.Center;
        purgatoryPanel.SetActive(false);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 position, Vector2 size, Vector2 anchor, bool stretch = false)
    {
        GameObject panel = CreateRect(name, parent, position, size, anchor);
        panel.AddComponent<Image>().color = color;
        if (stretch)
        {
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return panel;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Vector2 position, Vector2 rectSize, Vector2 anchor)
    {
        GameObject obj = CreateRect(name, parent, position, rectSize, anchor);
        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.outlineColor = Color.black;
        label.outlineWidth = 0.14f;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject obj = CreateRect(name, parent, position, size, anchor);
        obj.AddComponent<Image>().color = color;
        return obj.GetComponent<RectTransform>();
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private static void CreateButton(string name, Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = CreateRect(name, parent, position, new Vector2(260f, 46f), new Vector2(0.5f, 0.5f));
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.84f, 0.88f, 0.92f, 1f);
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        TextMeshProUGUI label = CreateText("Label", obj.transform, text, 20f, Vector2.zero, new Vector2(240f, 38f), new Vector2(0.5f, 0.5f));
        label.color = Color.black;
        label.outlineWidth = 0f;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
