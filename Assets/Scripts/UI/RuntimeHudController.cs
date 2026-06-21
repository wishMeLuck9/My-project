using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RuntimeHudController : MonoBehaviour
{
    private const string ExteriorScene = "LOCATION_01_EXTERIOR_DAY";
    private const string NightScene = "LOCATION_02_PROTECTED_ALLEYS_NIGHT";
    private const float ExteriorHintDelay = 45f;
    private const float ControlsFullDisplaySeconds = 10f;
    private const float AmbientSystemMessageCooldown = 3.2f;
    private const float DuplicateAmbientMessageCooldown = 6f;
    private const float HudReferenceWidth = 1280f;
    private const float HudReferenceHeight = 720f;
    private const float HudMargin = 24f;
    private const float HudGap = 14f;
    private const float MinLeftPanelWidth = 320f;
    private const float MaxLeftPanelWidth = 430f;

    public static RuntimeHudController Instance { get; private set; }

    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI controlsText;
    private TextMeshProUGUI interactionText;
    private TextMeshProUGUI stabilityText;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI bossText;
    private TextMeshProUGUI systemText;
    private TextMeshProUGUI purgatoryText;
    private GameObject controlsPanel;
    private RectTransform canvasRect;
    private RectTransform controlsPanelRect;
    private GameObject interactionPanel;
    private RectTransform interactionPanelRect;
    private GameObject stabilityPanel;
    private RectTransform stabilityPanelRect;
    private GameObject healthPanel;
    private RectTransform healthPanelRect;
    private GameObject bossPanel;
    private RectTransform bossPanelRect;
    private GameObject systemPanel;
    private RectTransform systemPanelRect;
    private GameObject purgatoryPanel;
    private GameObject minimapPanel;
    private RectTransform minimapPanelRect;
    private RectTransform minimapExitMarker;
    private RawImage minimapImage;
    private Camera minimapCamera;
    private RenderTexture minimapTexture;
    private PlayerController3D player;
    private PlayerHealthController playerHealth;
    private FinalBossDirector bossDirector;
    private PlayerInputReader inputReader;
    private InteractionController interaction;
    private ExteriorHuntController hunt;
    private Transform minimapExit;
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
    private string lastHealthText;
    private string lastBossText;
    private Coroutine clearSystemMessageRoutine;
    private float sceneStartedAt;
    private float nextAmbientSystemMessageAllowedAt;
    private readonly Dictionary<string, float> recentAmbientMessages = new Dictionary<string, float>();
    private CanvasScaler canvasScaler;
    private bool lastFullControlsVisible = true;

    public bool IsPaused => PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;

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
        LocalizationManager.LanguageChanged += RefreshLocalizedText;
        SettingsManager.SettingsChanged += RefreshSettings;
        RefreshLocalizedText();
        ApplyScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        LocalizationManager.LanguageChanged -= RefreshLocalizedText;
        SettingsManager.SettingsChanged -= RefreshSettings;
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
        UpdateHealth();
        UpdateBossHealth();
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
        ShowSystemMessage(LocalizationManager.EnsureInstance().Get("hud.night_unlocked"), 4f);
    }

    public void ShowSystemMessage(string message, float duration = 3.5f)
    {
        string translatedMessage = LocalizationManager.EnsureInstance().TranslateRaw(message);
        if (string.IsNullOrWhiteSpace(translatedMessage)) return;

        if (clearSystemMessageRoutine != null) StopCoroutine(clearSystemMessageRoutine);

        SetSystemMessage(translatedMessage);
        clearSystemMessageRoutine = StartCoroutine(ClearSystemMessageAfterDelay(duration));
    }

    public void ShowAmbientMessage(string message, float duration = 1.8f)
    {
        string translatedMessage = LocalizationManager.EnsureInstance().TranslateRaw(message);
        if (string.IsNullOrWhiteSpace(translatedMessage)) return;

        if (Time.unscaledTime < nextAmbientSystemMessageAllowedAt) return;
        if (recentAmbientMessages.TryGetValue(translatedMessage, out float lastShownAt) &&
            Time.unscaledTime - lastShownAt < DuplicateAmbientMessageCooldown)
        {
            return;
        }

        recentAmbientMessages[translatedMessage] = Time.unscaledTime;
        nextAmbientSystemMessageAllowedAt = Time.unscaledTime + AmbientSystemMessageCooldown;
        ShowSystemMessage(translatedMessage, duration);
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
        sceneStartedAt = Time.unscaledTime;
        nextAmbientSystemMessageAllowedAt = 0f;
        recentAmbientMessages.Clear();
        player = null;
        playerHealth = null;
        bossDirector = null;
        interaction = null;
        hunt = null;
        minimapExit = null;
        nextReferenceResolveAt = 0f;
        lastControlsText = null;
        lastObjectiveText = null;
        lastStabilityText = null;
        lastHealthText = null;
        lastBossText = null;

        if (currentScene == ExteriorScene && !startupShown && !GameplayIntroController.ShouldShowIntroForScene(currentScene))
        {
            startupShown = true;
            StartCoroutine(PlayStartupSequence());
        }

        ConfigureMinimap(currentScene == NightScene);
        if (currentScene == NightScene && !nightMapHintShown)
        {
            nightMapHintShown = true;
            ShowSystemMessage(LocalizationManager.EnsureInstance().Get("hud.map_active"), 4f);
        }
    }

    private void ResolveSceneReferences()
    {
        if (Time.unscaledTime < nextReferenceResolveAt) return;
        nextReferenceResolveAt = Time.unscaledTime + 0.5f;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController3D>();
            inputReader = player != null ? player.GetComponent<PlayerInputReader>() : null;
            playerHealth = player != null ? player.GetComponent<PlayerHealthController>() : null;
        }

        if (playerHealth == null && player != null) playerHealth = player.GetComponent<PlayerHealthController>();
        if (bossDirector == null) bossDirector = FindFirstObjectByType<FinalBossDirector>();
        if (interaction == null && player != null) interaction = player.GetComponent<InteractionController>();
        if (hunt == null) hunt = FindFirstObjectByType<ExteriorHuntController>();
        if (minimapExit == null && currentScene == NightScene)
        {
            GameObject exit = GameObject.Find("EXIT_To_FinalGate_Exit");
            if (exit != null) minimapExit = exit.transform;
        }
    }

    private void RefreshLocalizedText()
    {
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        lastControlsText = null;
        lastObjectiveText = null;
        lastStabilityText = null;
        if (interactionText != null) interactionText.text = localizer.Get("hud.interact");
        if (systemText != null) systemText.text = localizer.TranslateRaw(systemText.text);
        if (purgatoryText != null) purgatoryText.text = localizer.TranslateRaw(purgatoryText.text);
    }

    private void RefreshSettings()
    {
        lastControlsText = null;
        lastHealthText = null;
        lastBossText = null;
        ApplySettingsPresentation();
        UpdateControls();
        UpdateInteractionPrompt();
        UpdateHealth();
        UpdateBossHealth();
    }

    private void UpdateCursor()
    {
        bool dialogueOpen = DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen;
        bool shouldRelease = IsPaused || dialogueOpen || purgatoryPanel.activeSelf;
        Cursor.lockState = shouldRelease ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldRelease;
    }

    private void UpdateControls()
    {
        bool showFullControls = Time.unscaledTime - sceneStartedAt < ControlsFullDisplaySeconds;
        ApplyResponsiveLayout(showFullControls);

        if (SettingsManager.Instance != null && !SettingsManager.Instance.TutorialHints)
        {
            controlsPanel.SetActive(false);
            return;
        }

        controlsPanel.SetActive(true);
        controlsText.gameObject.SetActive(showFullControls);

        string key = currentScene == NightScene ? "hud.controls.night" : "hud.controls.day";
        string text = showFullControls ? LocalizationManager.EnsureInstance().Get(key) : string.Empty;
        if (lastControlsText == text) return;

        lastControlsText = text;
        controlsText.text = text;
    }

    private void UpdateObjective()
    {
        WorldState state = WorldState.Instance;
        string text;
        if (currentScene == ExteriorScene)
        {
            text = LocalizationManager.EnsureInstance().Get(state != null && state.hasExteriorFragment
                ? "hud.objective.exterior.gate"
                : "hud.objective.exterior.fragment");
        }
        else if (currentScene == NightScene)
        {
            text = LocalizationManager.EnsureInstance().Get(state != null && state.hasInnerNightFragment
                ? "hud.objective.night.exit"
                : "hud.objective.night.choice");
        }
        else
        {
            text = LocalizationManager.EnsureInstance().Get("hud.objective.final");
        }

        if (lastObjectiveText == text) return;
        lastObjectiveText = text;
        objectiveText.text = text;
    }

    private void UpdateInteractionPrompt()
    {
        bool enabled = SettingsManager.Instance == null || SettingsManager.Instance.SimplePrompts;
        interactionPanel.SetActive(enabled && interaction != null && interaction.HasNearbyInteractable && !IsPaused);
    }

    private void UpdateStability()
    {
        bool visible = hunt != null && hunt.IsHunting && WorldState.Instance != null;
        stabilityPanel.SetActive(visible);
        if (!visible) return;

        int remaining = Mathf.Clamp(5 - WorldState.Instance.exteriorCaptureCount, 0, 5);
        string blocks = new string('\u25a0', remaining) + new string('\u25a1', 5 - remaining);
        string text = LocalizationManager.EnsureInstance().Format("hud.stability", blocks);
        if (lastStabilityText == text) return;

        lastStabilityText = text;
        stabilityText.text = text;
    }

    private void UpdateHealth()
    {
        bool visible = playerHealth != null;
        healthPanel.SetActive(visible);
        if (!visible) return;

        string text = LocalizationManager.EnsureInstance().Format("hud.health", playerHealth.CurrentHealth, playerHealth.MaxHealth);
        if (lastHealthText == text) return;

        lastHealthText = text;
        healthText.text = text;
    }

    private void UpdateBossHealth()
    {
        bool visible = bossDirector != null && bossDirector.IsFightActive && bossDirector.MaxBossHealth > 0;
        bossPanel.SetActive(visible);
        if (!visible) return;

        int percent = Mathf.RoundToInt(bossDirector.NormalizedBossHealth * 100f);
        string key = bossDirector.IsPhaseTwo ? "hud.boss.phase2" : "hud.boss";
        string text = LocalizationManager.EnsureInstance().Format(key, percent);
        if (lastBossText == text) return;

        lastBossText = text;
        bossText.text = text;
    }

    private void UpdateExteriorHint()
    {
        if (currentScene != ExteriorScene || exteriorHintShown || WorldState.Instance == null) return;
        if (WorldState.Instance.hasExteriorFragment) return;

        exteriorElapsed += Time.deltaTime;
        if (exteriorElapsed < ExteriorHintDelay) return;

        exteriorHintShown = true;
        ShowSystemMessage(LocalizationManager.EnsureInstance().Get("hud.idle_hint"), 5f);
    }

    private IEnumerator PlayStartupSequence()
    {
        yield return ShowStartupLine(LocalizationManager.EnsureInstance().Get("hud.start.1"));
        yield return ShowStartupLine(LocalizationManager.EnsureInstance().Get("hud.start.2"));
        yield return ShowStartupLine(LocalizationManager.EnsureInstance().Get("hud.start.3"));
    }

    private IEnumerator ShowStartupLine(string line)
    {
        SetSystemMessage(line);
        yield return new WaitForSecondsRealtime(SettingsManager.Instance != null && SettingsManager.Instance.ReduceMotion ? 1.4f : 2.4f);
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
        float halfSize = minimapPanelRect != null
            ? Mathf.Max(40f, (minimapPanelRect.rect.width - 26f) * 0.5f)
            : 82f;
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
        canvasRect = GetComponent<RectTransform>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(HudReferenceWidth, HudReferenceHeight);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        controlsPanel = CreatePanel("ControlsPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(18f, -18f), new Vector2(380f, 220f), new Vector2(0f, 1f));
        controlsPanelRect = controlsPanel.GetComponent<RectTransform>();
        objectiveText = CreateText("Objective", controlsPanel.transform, string.Empty, 18f, new Vector2(12f, -10f), new Vector2(356f, 52f), new Vector2(0f, 1f));
        controlsText = CreateText("Controls", controlsPanel.transform, string.Empty, 18f, new Vector2(12f, -68f), new Vector2(356f, 144f), new Vector2(0f, 1f));

        healthPanel = CreatePanel("HealthPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(18f, -252f), new Vector2(170f, 48f), new Vector2(0f, 1f));
        healthPanelRect = healthPanel.GetComponent<RectTransform>();
        healthText = CreateText("Health", healthPanel.transform, string.Empty, 22f, Vector2.zero, new Vector2(150f, 38f), new Vector2(0.5f, 0.5f));
        healthText.alignment = TextAlignmentOptions.Center;
        healthPanel.SetActive(false);

        bossPanel = CreatePanel("BossPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.95f), new Vector2(0f, -82f), new Vector2(520f, 48f), new Vector2(0.5f, 1f));
        bossPanelRect = bossPanel.GetComponent<RectTransform>();
        bossText = CreateText("BossHealth", bossPanel.transform, string.Empty, 22f, Vector2.zero, new Vector2(500f, 38f), new Vector2(0.5f, 0.5f));
        bossText.alignment = TextAlignmentOptions.Center;
        bossPanel.SetActive(false);

        interactionPanel = CreatePanel("InteractionPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(0f, 18f), new Vector2(420f, 54f), new Vector2(0.5f, 0f));
        interactionPanelRect = interactionPanel.GetComponent<RectTransform>();
        interactionText = CreateText("InteractionPrompt", interactionPanel.transform, "[E] \u0412\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u043e\u0432\u0430\u0442\u044c", 22f, Vector2.zero, new Vector2(400f, 44f), new Vector2(0.5f, 0.5f));
        interactionText.alignment = TextAlignmentOptions.Center;
        interactionPanel.SetActive(false);

        stabilityPanel = CreatePanel("StabilityPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(0f, -18f), new Vector2(500f, 54f), new Vector2(0.5f, 1f));
        stabilityPanelRect = stabilityPanel.GetComponent<RectTransform>();
        stabilityText = CreateText("Stability", stabilityPanel.transform, string.Empty, 24f, Vector2.zero, new Vector2(480f, 44f), new Vector2(0.5f, 0.5f));
        stabilityText.alignment = TextAlignmentOptions.Center;
        stabilityPanel.SetActive(false);

        systemPanel = CreatePanel("SystemPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.94f), new Vector2(18f, -308f), new Vector2(680f, 112f), new Vector2(0f, 1f));
        systemPanelRect = systemPanel.GetComponent<RectTransform>();
        systemText = CreateText("SystemMessage", systemPanel.transform, string.Empty, 22f, new Vector2(12f, -10f), new Vector2(656f, 92f), new Vector2(0f, 1f));
        systemPanel.SetActive(false);

        minimapPanel = CreatePanel("MinimapPanel", transform, new Color(0.01f, 0.02f, 0.04f, 0.95f), new Vector2(-18f, 18f), new Vector2(190f, 190f), new Vector2(1f, 0f));
        minimapPanelRect = minimapPanel.GetComponent<RectTransform>();
        GameObject minimapImageObject = CreateRect("MinimapImage", minimapPanel.transform, Vector2.zero, new Vector2(174f, 174f), new Vector2(0.5f, 0.5f));
        minimapImage = minimapImageObject.AddComponent<RawImage>();
        CreateImage("PlayerMarker", minimapPanel.transform, new Color(0.95f, 0.9f, 0.35f, 1f), Vector2.zero, new Vector2(8f, 8f), new Vector2(0.5f, 0.5f)).SetAsLastSibling();
        minimapExitMarker = CreateImage("ExitMarker", minimapPanel.transform, new Color(0.35f, 0.85f, 1f, 1f), Vector2.zero, new Vector2(10f, 10f), new Vector2(0.5f, 0.5f));
        minimapExitMarker.SetAsLastSibling();

        purgatoryPanel = CreatePanel("PurgatoryPanel", transform, Color.black, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), true);
        purgatoryText = CreateText("PurgatoryText", purgatoryPanel.transform, string.Empty, 28f, Vector2.zero, new Vector2(900f, 180f), new Vector2(0.5f, 0.5f));
        purgatoryText.alignment = TextAlignmentOptions.Center;
        purgatoryPanel.SetActive(false);
        ApplySettingsPresentation();
        ApplyResponsiveLayout(true);
    }

    private void ApplySettingsPresentation()
    {
        SettingsManager settings = SettingsManager.Instance;
        float uiScale = settings != null ? settings.UiScale : 1f;
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(HudReferenceWidth, HudReferenceHeight) / Mathf.Max(0.1f, uiScale);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }

        float textScale = settings == null
            ? 1f
            : settings.SubtitleSize switch
            {
                0 => 0.9f,
                2 => 1.14f,
                _ => 1f
            };

        SetFontSize(objectiveText, 18f * textScale);
        SetFontSize(controlsText, 18f * textScale);
        SetFontSize(interactionText, 22f * textScale);
        SetFontSize(stabilityText, 24f * textScale);
        SetFontSize(healthText, 22f * textScale);
        SetFontSize(bossText, 22f * textScale);
        SetFontSize(systemText, 22f * textScale);
        SetFontSize(purgatoryText, 28f * textScale);

        bool highContrast = settings != null && settings.HighContrast;
        bool colorblind = settings != null && settings.ColorblindFriendly;
        Color panelColor = highContrast ? new Color(0f, 0f, 0f, 0.98f) : new Color(0.01f, 0.02f, 0.04f, 0.94f);
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image.name.Contains("Marker")) continue;
            if (image.gameObject.name.EndsWith("Panel")) image.color = panelColor;
        }

        if (minimapExitMarker != null)
        {
            Image marker = minimapExitMarker.GetComponent<Image>();
            if (marker != null) marker.color = colorblind ? new Color(1f, 0.75f, 0.15f, 1f) : new Color(0.35f, 0.85f, 1f, 1f);
        }

        ApplyResponsiveLayout(lastFullControlsVisible);
    }

    private void ApplyResponsiveLayout(bool fullControlsVisible)
    {
        lastFullControlsVisible = fullControlsVisible;

        Rect safe = GetCanvasSafeRect();
        float safeWidth = Mathf.Max(1f, safe.width);
        float safeHeight = Mathf.Max(1f, safe.height);
        float leftInset = safe.xMin;
        float rightInset = GetCanvasWidth() - safe.xMax;
        float topInset = GetCanvasHeight() - safe.yMax;
        float bottomInset = safe.yMin;
        float availableWidth = Mathf.Max(1f, safeWidth - HudMargin * 2f);

        float leftPanelWidth = ClampToAvailable(safeWidth * 0.38f, MinLeftPanelWidth, MaxLeftPanelWidth, availableWidth);
        float controlsPanelHeight = fullControlsVisible
            ? Mathf.Clamp(safeHeight * 0.31f, 190f, 236f)
            : 88f;
        float objectiveHeight = fullControlsVisible ? Mathf.Clamp(controlsPanelHeight * 0.31f, 58f, 74f) : controlsPanelHeight - 20f;

        SetRect(controlsPanelRect, new Vector2(0f, 1f),
            new Vector2(leftInset + HudMargin, -(topInset + HudMargin)),
            new Vector2(leftPanelWidth, controlsPanelHeight));
        SetRect(objectiveText != null ? objectiveText.rectTransform : null, new Vector2(0f, 1f),
            new Vector2(12f, -10f),
            new Vector2(Mathf.Max(1f, leftPanelWidth - 24f), objectiveHeight));
        SetRect(controlsText != null ? controlsText.rectTransform : null, new Vector2(0f, 1f),
            new Vector2(12f, -(objectiveHeight + 18f)),
            new Vector2(Mathf.Max(1f, leftPanelWidth - 24f), Mathf.Max(48f, controlsPanelHeight - objectiveHeight - 28f)));

        float healthWidth = ClampToAvailable(safeWidth * 0.17f, 170f, 230f, availableWidth);
        SetRect(healthPanelRect, new Vector2(0f, 1f),
            new Vector2(leftInset + HudMargin, -(topInset + HudMargin + controlsPanelHeight + HudGap)),
            new Vector2(healthWidth, 48f));
        SetRect(healthText != null ? healthText.rectTransform : null, new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(Mathf.Max(1f, healthWidth - 20f), 38f));

        float systemWidth = ClampToAvailable(safeWidth * 0.56f, 360f, 680f, availableWidth);
        SetRect(systemPanelRect, new Vector2(0f, 1f),
            new Vector2(leftInset + HudMargin, -(topInset + HudMargin + controlsPanelHeight + HudGap + 48f + HudGap)),
            new Vector2(systemWidth, 112f));
        SetRect(systemText != null ? systemText.rectTransform : null, new Vector2(0f, 1f),
            new Vector2(12f, -10f),
            new Vector2(Mathf.Max(1f, systemWidth - 24f), 92f));

        float centerWidth = ClampToAvailable(safeWidth * 0.45f, 360f, 520f, availableWidth);
        SetRect(stabilityPanelRect, new Vector2(0.5f, 1f),
            new Vector2(0f, -(topInset + HudMargin)),
            new Vector2(centerWidth, 54f));
        SetRect(stabilityText != null ? stabilityText.rectTransform : null, new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(Mathf.Max(1f, centerWidth - 20f), 44f));

        SetRect(bossPanelRect, new Vector2(0.5f, 1f),
            new Vector2(0f, -(topInset + HudMargin + 64f)),
            new Vector2(centerWidth, 48f));
        SetRect(bossText != null ? bossText.rectTransform : null, new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(Mathf.Max(1f, centerWidth - 20f), 38f));

        float interactionWidth = ClampToAvailable(safeWidth * 0.42f, 340f, 460f, availableWidth);
        SetRect(interactionPanelRect, new Vector2(0.5f, 0f),
            new Vector2(0f, bottomInset + HudMargin),
            new Vector2(interactionWidth, 54f));
        SetRect(interactionText != null ? interactionText.rectTransform : null, new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(Mathf.Max(1f, interactionWidth - 20f), 44f));

        float minimapSize = Mathf.Clamp(safeWidth * 0.17f, 160f, 190f);
        SetRect(minimapPanelRect, new Vector2(1f, 0f),
            new Vector2(-(rightInset + HudMargin), bottomInset + HudMargin),
            new Vector2(minimapSize, minimapSize));
        SetRect(minimapImage != null ? minimapImage.rectTransform : null, new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(Mathf.Max(1f, minimapSize - 16f), Mathf.Max(1f, minimapSize - 16f)));
    }

    private Rect GetCanvasSafeRect()
    {
        float canvasWidth = GetCanvasWidth();
        float canvasHeight = GetCanvasHeight();
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return new Rect(0f, 0f, canvasWidth, canvasHeight);
        }

        Rect safeArea = Screen.safeArea;
        float scaleX = canvasWidth / Screen.width;
        float scaleY = canvasHeight / Screen.height;
        return new Rect(safeArea.x * scaleX, safeArea.y * scaleY, safeArea.width * scaleX, safeArea.height * scaleY);
    }

    private float GetCanvasWidth()
    {
        return canvasRect != null && canvasRect.rect.width > 1f ? canvasRect.rect.width : HudReferenceWidth;
    }

    private float GetCanvasHeight()
    {
        return canvasRect != null && canvasRect.rect.height > 1f ? canvasRect.rect.height : HudReferenceHeight;
    }

    private static float ClampToAvailable(float value, float min, float max, float available)
    {
        float upper = Mathf.Min(max, Mathf.Max(1f, available));
        float lower = Mathf.Min(min, upper);
        return Mathf.Clamp(value, lower, upper);
    }

    private static void SetFontSize(TMP_Text label, float size)
    {
        if (label == null) return;

        label.enableAutoSizing = true;
        label.fontSize = size;
        label.fontSizeMax = size;
        label.fontSizeMin = Mathf.Max(10f, size - 6f);
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
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject obj = CreateRect(name, parent, position, size, anchor);
        obj.AddComponent<Image>().color = color;
        return obj.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        if (rect == null) return;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
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

}
