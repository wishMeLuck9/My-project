using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RuntimeHudController : MonoBehaviour
{
    private const string ExteriorScene = "LOCATION_01_EXTERIOR_DAY";
    private const string NightScene = "LOCATION_02_PROTECTED_ALLEYS_NIGHT";
    private const float ExteriorHintDelay = 45f;

    public static RuntimeHudController Instance { get; private set; }

    private Canvas canvas;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI controlsText;
    private TextMeshProUGUI interactionText;
    private TextMeshProUGUI stabilityText;
    private TextMeshProUGUI systemText;
    private TextMeshProUGUI purgatoryText;
    private GameObject controlsPanel;
    private GameObject pausePanel;
    private GameObject purgatoryPanel;
    private GameObject minimapPanel;
    private RectTransform minimapExitMarker;
    private Camera minimapCamera;
    private RenderTexture minimapTexture;
    private PlayerController3D player;
    private InteractionController interaction;
    private ExteriorHuntController hunt;
    private Transform minimapExit;
    private bool paused;
    private bool startupShown;
    private bool nightUnlockShown;
    private bool exteriorHintShown;
    private float exteriorElapsed;
    private string currentScene;
    private Coroutine clearSystemMessageRoutine;

    public bool IsPaused => paused;

    public static RuntimeHudController EnsureInstance()
    {
        if (Instance != null) return Instance;

        RuntimeHudController existing = FindFirstObjectByType<RuntimeHudController>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        GameObject hudObject = new GameObject("RuntimeHudCanvas");
        return hudObject.AddComponent<RuntimeHudController>();
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
        Time.timeScale = 1f;
        ReleaseMinimap();
        Instance = null;
    }

    private void Update()
    {
        HandleKeyboard();
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
        ShowSystemMessage("Ночь признала в тебе силу. Удар доступен.", 4f);
    }

    public void ShowSystemMessage(string message, float duration = 3.5f)
    {
        if (clearSystemMessageRoutine != null)
        {
            StopCoroutine(clearSystemMessageRoutine);
        }

        systemText.text = message;
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
        if (currentScene == ExteriorScene && !startupShown)
        {
            startupShown = true;
            StartCoroutine(PlayStartupSequence());
        }

        ConfigureMinimap(currentScene == NightScene);
    }

    private void ResolveSceneReferences()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController3D>();
        if (interaction == null && player != null) interaction = player.GetComponent<InteractionController>();
        if (hunt == null) hunt = FindFirstObjectByType<ExteriorHuntController>();
        if (minimapExit == null && currentScene == NightScene)
        {
            GameObject exit = GameObject.Find("EXIT_To_FinalGate_Exit");
            if (exit != null) minimapExit = exit.transform;
        }
    }

    private void HandleKeyboard()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SetPaused(!paused);
        }

#endif
    }

    private void SetPaused(bool state)
    {
        paused = state;
        Time.timeScale = paused ? 0f : 1f;
        pausePanel.SetActive(paused);
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
        string attackHint = currentScene == NightScene ? "\n[LMB] Атака" : string.Empty;
        controlsText.text = "[WASD] Движение\n[SPACE] Прыжок\n[E] Взаимодействие"
            + attackHint
            + "\n[ESC] Пауза";
        controlsPanel.SetActive(true);
    }

    private void UpdateObjective()
    {
        WorldState state = WorldState.Instance;
        if (currentScene == ExteriorScene)
        {
            objectiveText.text = state != null && state.hasExteriorFragment
                ? "SYSTEM // ЦЕЛЬ: доберись до двери квадрата"
                : "SYSTEM // ЦЕЛЬ: найди фрагмент света";
            return;
        }

        if (currentScene == NightScene)
        {
            objectiveText.text = state != null && state.hasInnerNightFragment
                ? "SYSTEM // ЦЕЛЬ: доберись до выхода"
                : "SYSTEM // ЦЕЛЬ: наблюдай или действуй";
            return;
        }

        objectiveText.text = "SYSTEM // ЦЕЛЬ: дойди до врат";
    }

    private void UpdateInteractionPrompt()
    {
        interactionText.gameObject.SetActive(interaction != null && interaction.HasNearbyInteractable && !paused);
    }

    private void UpdateStability()
    {
        bool visible = hunt != null && hunt.IsHunting && WorldState.Instance != null;
        stabilityText.gameObject.SetActive(visible);
        if (!visible) return;

        int remaining = Mathf.Clamp(5 - WorldState.Instance.exteriorCaptureCount, 0, 5);
        stabilityText.text = "УСТОЙЧИВОСТЬ  " + new string('■', remaining) + new string('□', 5 - remaining);
    }

    private void UpdateExteriorHint()
    {
        if (currentScene != ExteriorScene || exteriorHintShown || WorldState.Instance == null) return;
        if (WorldState.Instance.hasExteriorFragment) return;

        exteriorElapsed += Time.deltaTime;
        if (exteriorElapsed < ExteriorHintDelay) return;

        exteriorHintShown = true;
        ShowSystemMessage("Если ты не взаимодействуешь, ты исчезаешь.", 5f);
    }

    private IEnumerator PlayStartupSequence()
    {
        yield return ShowStartupLine("Протокол квадрата активен.");
        yield return ShowStartupLine("Фрагмент обнаружен.");
        yield return ShowStartupLine("Ты научился идти. Этого достаточно, чтобы начать.\nВыживешь ли ты - зависит от тебя.");
    }

    private IEnumerator ShowStartupLine(string line)
    {
        systemText.text = line;
        yield return new WaitForSecondsRealtime(2.4f);
        systemText.text = string.Empty;
        yield return new WaitForSecondsRealtime(0.25f);
    }

    private IEnumerator ClearSystemMessageAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        systemText.text = string.Empty;
        clearSystemMessageRoutine = null;
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
            minimapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            minimapTexture.name = "RuntimeNightMinimap";
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
        minimapPanel.GetComponentInChildren<RawImage>().texture = minimapTexture;
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
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        gameObject.AddComponent<GraphicRaycaster>();

        controlsPanel = CreatePanel("ControlsPanel", transform, new Color(0.02f, 0.03f, 0.06f, 0.82f), new Vector2(18f, -18f), new Vector2(320f, 180f), new Vector2(0f, 1f));
        objectiveText = CreateText("Objective", controlsPanel.transform, string.Empty, 18f, new Vector2(12f, -10f), new Vector2(296f, 34f), new Vector2(0f, 1f));
        controlsText = CreateText("Controls", controlsPanel.transform, string.Empty, 17f, new Vector2(12f, -52f), new Vector2(296f, 116f), new Vector2(0f, 1f));
        interactionText = CreateText("InteractionPrompt", transform, "[E] Взаимодействовать", 22f, new Vector2(0f, 40f), new Vector2(380f, 42f), new Vector2(0.5f, 0f));
        interactionText.alignment = TextAlignmentOptions.Center;
        stabilityText = CreateText("Stability", transform, string.Empty, 23f, new Vector2(0f, -24f), new Vector2(440f, 42f), new Vector2(0.5f, 1f));
        stabilityText.alignment = TextAlignmentOptions.Center;
        systemText = CreateText("SystemMessage", transform, string.Empty, 22f, new Vector2(18f, -214f), new Vector2(560f, 96f), new Vector2(0f, 1f));

        minimapPanel = CreatePanel("MinimapPanel", transform, new Color(0.02f, 0.03f, 0.06f, 0.9f), new Vector2(-18f, -18f), new Vector2(180f, 180f), new Vector2(1f, 1f));
        GameObject minimapImageObject = CreateRect("MinimapImage", minimapPanel.transform, Vector2.zero, new Vector2(164f, 164f), new Vector2(0.5f, 0.5f));
        minimapImageObject.AddComponent<RawImage>();
        RectTransform playerMarker = CreateImage("PlayerMarker", minimapPanel.transform, new Color(0.95f, 0.9f, 0.35f, 1f), Vector2.zero, new Vector2(8f, 8f), new Vector2(0.5f, 0.5f));
        minimapExitMarker = CreateImage("ExitMarker", minimapPanel.transform, new Color(0.35f, 0.85f, 1f, 1f), Vector2.zero, new Vector2(10f, 10f), new Vector2(0.5f, 0.5f));
        playerMarker.SetAsLastSibling();
        minimapExitMarker.SetAsLastSibling();

        pausePanel = CreatePanel("PausePanel", transform, new Color(0f, 0f, 0f, 0.88f), Vector2.zero, new Vector2(420f, 310f), new Vector2(0.5f, 0.5f));
        TextMeshProUGUI pauseTitle = CreateText("PauseTitle", pausePanel.transform, "ПАУЗА", 32f, new Vector2(0f, -28f), new Vector2(380f, 48f), new Vector2(0.5f, 1f));
        pauseTitle.alignment = TextAlignmentOptions.Center;
        CreateButton("ResumeButton", pausePanel.transform, "Продолжить", new Vector2(0f, 38f), () => SetPaused(false));
        CreateButton("ControlsButton", pausePanel.transform, "Управление", new Vector2(0f, -22f), () => controlsPanel.SetActive(true));
        CreateButton("ExitButton", pausePanel.transform, "Выйти из игры", new Vector2(0f, -82f), QuitGame);
        pausePanel.SetActive(false);

        purgatoryPanel = CreatePanel("PurgatoryPanel", transform, Color.black, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), true);
        purgatoryText = CreateText("PurgatoryText", purgatoryPanel.transform, string.Empty, 28f, Vector2.zero, new Vector2(900f, 180f), new Vector2(0.5f, 0.5f));
        purgatoryText.alignment = TextAlignmentOptions.Center;
        purgatoryPanel.SetActive(false);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 position, Vector2 size, Vector2 anchor, bool stretch = false)
    {
        GameObject panel = CreateRect(name, parent, position, size, anchor);
        Image image = panel.AddComponent<Image>();
        image.color = color;
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
        label.color = new Color(0.82f, 0.9f, 1f, 1f);
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
