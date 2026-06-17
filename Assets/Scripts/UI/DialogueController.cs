using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }

    private const float MinPanelHeight = 150f;
    private const float MaxPanelScreenRatio = 0.52f;
    private const float MaxChoiceColumnWidth = 270f;
    private const float MinChoiceColumnWidth = 220f;
    private const float Padding = 16f;
    private const float MaxChoiceButtonHeight = 52f;
    private const float MinChoiceButtonHeight = 38f;
    private const float ChoiceSpacing = 8f;
    private const float BaseDialogueFontSize = 22f;
    private const float HeldSubmitDelay = 0.65f;
    private const float HeldSubmitInterval = 0.28f;

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    private readonly List<Button> activeButtons = new List<Button>();
    private Action onDialogueComplete;
    private float currentPanelHeight = MinPanelHeight;
    private float currentChoiceColumnWidth = MaxChoiceColumnWidth;
    private float currentChoiceButtonHeight = MaxChoiceButtonHeight;
    private int selectedButtonIndex = -1;
    private int ignoreSubmitUntilFrame;

    // Pagination
    private string fullDialogueText;
    private int currentPage;
    private List<string> pages = new List<string>();
    private string currentSpeaker;
    private float nextHeldSubmitAt;

    public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;

    public static DialogueController EnsureInstance()
    {
        if (Instance != null) return Instance;

        DialogueController existing = FindFirstObjectByType<DialogueController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject canvasObject = new GameObject("RuntimeDialogueCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        DialogueController controller = canvasObject.AddComponent<DialogueController>();
        controller.BuildRuntimeUi(canvasObject.transform);
        controller.ConfigureDialogueLayout(1);
        controller.dialoguePanel.SetActive(false);
        return controller;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureEventSystem();
        SettingsManager.SettingsChanged += RefreshSettings;
        ConfigureCanvasScaler();
        ConfigureDialogueLayout(1);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsDialogueOpen || activeButtons.Count == 0) return;

        HandleChoiceNavigation();

        if (Time.frameCount <= ignoreSubmitUntilFrame) return;

        if (WasSubmitRequested() && selectedButtonIndex >= 0 && selectedButtonIndex < activeButtons.Count)
        {
            activeButtons[selectedButtonIndex].onClick.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SettingsManager.SettingsChanged -= RefreshSettings;
        Instance = null;
    }

    public void ShowDialogue(string speaker, string text, Action onComplete = null)
    {
        if (!HasRequiredReferences()) return;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        speaker = localizer.TranslateRaw(speaker);
        text = localizer.TranslateRaw(text);
        onDialogueComplete = onComplete;
        currentSpeaker = speaker;
        fullDialogueText = text;

        ConfigureDialogueLayout(1);
        dialoguePanel.SetActive(true);
        speakerText.text = speaker;

        // Build pages after layout is ready
        BuildPages(text);
        currentPage = 0;
        ShowCurrentPage();

        SetPlayerControl(false);
    }

    public void ShowDialoguePages(string speaker, IReadOnlyList<string> pageTexts, Action onComplete = null)
    {
        if (!HasRequiredReferences())
        {
            onComplete?.Invoke();
            return;
        }

        if (pageTexts == null || pageTexts.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        speaker = localizer.TranslateRaw(speaker);
        onDialogueComplete = onComplete;
        currentSpeaker = speaker;
        fullDialogueText = string.Join("\n\n", pageTexts);

        ConfigureDialogueLayout(3);
        dialoguePanel.SetActive(true);
        speakerText.text = speaker;

        pages.Clear();
        for (int i = 0; i < pageTexts.Count; i++)
        {
            string page = localizer.TranslateRaw(pageTexts[i]);
            if (!string.IsNullOrWhiteSpace(page)) pages.Add(page);
        }

        if (pages.Count == 0)
        {
            CompleteDialogue();
            return;
        }

        currentPage = 0;
        ShowCurrentPage();

        SetPlayerControl(false);
    }

    public void ShowChoices(string speaker, string text, List<DialogueChoice> choices)
    {
        if (!HasRequiredReferences()) return;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        speaker = localizer.TranslateRaw(speaker);
        text = localizer.TranslateRaw(text);
        int choiceCount = choices != null ? choices.Count : 0;
        ConfigureDialogueLayout(choiceCount);
        dialoguePanel.SetActive(true);
        speakerText.text = speaker;
        dialogueText.text = text;
        onDialogueComplete = null;

        ClearChoices();

        if (choices != null)
        {
            foreach (DialogueChoice choice in choices)
            {
                DialogueChoice capturedChoice = choice;
                Button button = Instantiate(choiceButtonPrefab, choiceContainer.transform);
                ConfigureChoiceButton(button, localizer.TranslateRaw(capturedChoice.text));
                button.onClick.AddListener(() =>
                {
                    CompleteDialogue();
                    capturedChoice.action?.Invoke();
                });
                activeButtons.Add(button);
            }
        }

        if (activeButtons.Count > 0)
        {
            SelectButton(0);
        }

        SetPlayerControl(false);
    }

    private void BuildPages(string text)
    {
        pages.Clear();

        if (dialogueText == null) { pages.Add(text); return; }

        // Calculate how much text fits in the available area
        RectTransform textRect = dialogueText.rectTransform;
        float availableWidth = textRect.rect.width;
        float availableHeight = textRect.rect.height;

        if (availableWidth <= 0 || availableHeight <= 0)
        {
            // Layout not ready yet, force rebuild
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialoguePanel.GetComponent<RectTransform>());
            availableWidth = textRect.rect.width;
            availableHeight = textRect.rect.height;
        }

        if (availableWidth <= 0 || availableHeight <= 0)
        {
            pages.Add(text);
            return;
        }

        // Use TMP to calculate text overflow and split into pages
        dialogueText.text = text;
        dialogueText.ForceMeshUpdate();

        TMP_TextInfo textInfo = dialogueText.textInfo;

        if (textInfo.characterCount >= text.Length || !dialogueText.isTextOverflowing)
        {
            // All text fits on one page
            pages.Add(text);
            return;
        }

        // Text overflows - split into pages
        string remaining = text;
        int safetyCounter = 0;
        while (remaining.Length > 0 && safetyCounter < 50)
        {
            safetyCounter++;
            dialogueText.text = remaining;
            dialogueText.ForceMeshUpdate();

            if (!dialogueText.isTextOverflowing || dialogueText.textInfo.characterCount <= 0)
            {
                pages.Add(remaining);
                break;
            }

            int visibleChars = dialogueText.textInfo.characterCount;

            // Find a good break point (space, period, comma) before the cutoff
            int breakPoint = visibleChars;
            for (int i = visibleChars - 1; i > visibleChars - 60 && i >= 0; i--)
            {
                char c = remaining[i];
                if (c == ' ' || c == '.' || c == ',' || c == '!' || c == '?' || c == '\n')
                {
                    breakPoint = i + 1;
                    break;
                }
            }

            if (breakPoint <= 0) breakPoint = visibleChars;

            string page = remaining.Substring(0, breakPoint).TrimEnd();
            pages.Add(page);
            remaining = remaining.Substring(breakPoint).TrimStart();
        }

        if (pages.Count == 0)
        {
            pages.Add(text);
        }
    }

    private void ShowCurrentPage()
    {
        if (currentPage < 0 || currentPage >= pages.Count) return;

        dialogueText.text = pages[currentPage];

        ClearChoices();

        bool isLastPage = currentPage >= pages.Count - 1;
        Button button = Instantiate(choiceButtonPrefab, choiceContainer.transform);

        if (isLastPage)
        {
            ConfigureChoiceButton(button, LocalizationManager.EnsureInstance().Get("dialogue.continue", "Продолжить"));
            button.onClick.AddListener(CompleteDialogue);
        }
        else
        {
            string label = LocalizationManager.EnsureInstance().Format("dialogue.next", currentPage + 1, pages.Count);
            ConfigureChoiceButton(button, label);
            button.onClick.AddListener(NextPage);
        }

        activeButtons.Add(button);
        SelectButton(0);
    }

    private void NextPage()
    {
        currentPage++;
        ignoreSubmitUntilFrame = Time.frameCount + 1;
        ShowCurrentPage();
    }

    private void ClearChoices()
    {
        if (choiceContainer == null) return;

        activeButtons.Clear();
        selectedButtonIndex = -1;
        foreach (Transform child in choiceContainer.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void ConfigureDialogueLayout(int choiceCount)
    {
        ConfigureCanvasScaler();
        CalculateResponsiveLayout(choiceCount);
        ConfigurePanelRect();
        ConfigureSpeakerText();
        ConfigureDialogueText();
        ConfigureChoiceContainer();

        if (dialoguePanel != null)
        {
            dialoguePanel.transform.SetAsLastSibling();
        }

        if (choiceContainer != null)
        {
            choiceContainer.transform.SetAsLastSibling();
        }

        ignoreSubmitUntilFrame = Time.frameCount + 1;
    }

    private void CalculateResponsiveLayout(int choiceCount)
    {
        int safeChoiceCount = Mathf.Max(1, choiceCount);
        Vector2 canvasSize = GetCanvasSize();
        float maxPanelHeight = Mathf.Max(MinPanelHeight, canvasSize.y * MaxPanelScreenRatio);
        float wantedPanelHeight = 104f + safeChoiceCount * MinChoiceButtonHeight + Mathf.Max(0, safeChoiceCount - 1) * ChoiceSpacing;

        currentPanelHeight = Mathf.Clamp(wantedPanelHeight, MinPanelHeight, maxPanelHeight);
        currentChoiceColumnWidth = Mathf.Clamp(canvasSize.x * 0.3f, MinChoiceColumnWidth, MaxChoiceColumnWidth);

        float availableButtonHeight = currentPanelHeight - Padding * 2f - Mathf.Max(0, safeChoiceCount - 1) * ChoiceSpacing;
        currentChoiceButtonHeight = Mathf.Clamp(availableButtonHeight / safeChoiceCount, MinChoiceButtonHeight, MaxChoiceButtonHeight);
    }

    private Vector2 GetCanvasSize()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect != null && canvasRect.rect.width > 0f && canvasRect.rect.height > 0f)
        {
            return canvasRect.rect.size;
        }

        return new Vector2(
            Screen.width > 0 ? Screen.width : 1280f,
            Screen.height > 0 ? Screen.height : 720f);
    }

    private void ConfigureCanvasScaler()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        float scale = SettingsManager.Instance != null ? SettingsManager.Instance.UiScale : 1f;
        scaler.referenceResolution = new Vector2(1280f, 720f) / Mathf.Max(0.1f, scale);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigurePanelRect()
    {
        if (dialoguePanel == null) return;

        RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = new Vector2(0f, currentPanelHeight);
        }

        Graphic panelGraphic = dialoguePanel.GetComponent<Graphic>();
        if (panelGraphic != null)
        {
            panelGraphic.raycastTarget = true;
        }
    }

    private void ConfigureSpeakerText()
    {
        if (speakerText == null) return;

        RectTransform speakerRect = speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.offsetMin = new Vector2(Padding, -44f);
        speakerRect.offsetMax = new Vector2(-(currentChoiceColumnWidth + Padding * 2f), -8f);

        speakerText.raycastTarget = false;
        speakerText.alignment = TextAlignmentOptions.Left;
        speakerText.textWrappingMode = TextWrappingModes.NoWrap;
        speakerText.overflowMode = TextOverflowModes.Ellipsis;
        speakerText.enableAutoSizing = true;
        speakerText.fontSizeMin = 16f;
        speakerText.fontSizeMax = 24f;
    }

    private void ConfigureDialogueText()
    {
        if (dialogueText == null) return;

        RectTransform textRect = dialogueText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.offsetMin = new Vector2(Padding, Padding);
        textRect.offsetMax = new Vector2(-(currentChoiceColumnWidth + Padding * 2f), -48f);

        dialogueText.raycastTarget = false;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.overflowMode = TextOverflowModes.Truncate;
        dialogueText.enableAutoSizing = false;
        dialogueText.fontSize = ResolveDialogueFontSize();
    }

    private void ConfigureChoiceContainer()
    {
        if (choiceContainer == null) return;

        RectTransform containerRect = choiceContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.anchorMin = new Vector2(1f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 0.5f);
            containerRect.offsetMin = new Vector2(-(currentChoiceColumnWidth + Padding), Padding);
            containerRect.offsetMax = new Vector2(-Padding, -Padding);
        }

        VerticalLayoutGroup layout = choiceContainer.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = choiceContainer.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = ChoiceSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
    }

    private void ConfigureChoiceButton(Button button, string label)
    {
        if (button == null) return;

        button.gameObject.SetActive(true);
        button.interactable = true;

        Graphic buttonGraphic = button.targetGraphic;
        if (buttonGraphic != null)
        {
            buttonGraphic.raycastTarget = true;
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.preferredWidth = currentChoiceColumnWidth;
        layout.minHeight = currentChoiceButtonHeight;
        layout.preferredHeight = currentChoiceButtonHeight;

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.raycastTarget = false;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.textWrappingMode = TextWrappingModes.Normal;
            buttonText.overflowMode = TextOverflowModes.Ellipsis;
            buttonText.enableAutoSizing = true;
            buttonText.fontSizeMin = 13f;
            buttonText.fontSizeMax = 21f;
        }
    }

    private void BuildRuntimeUi(Transform root)
    {
        dialoguePanel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialoguePanel.transform.SetParent(root, false);
        Image panelImage = dialoguePanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.94f);

        speakerText = CreateText("SpeakerText", dialoguePanel.transform, "SYSTEM", new Color32(255, 56, 56, 255), 22f);
        dialogueText = CreateText("DialogueText", dialoguePanel.transform, string.Empty, Color.white, ResolveDialogueFontSize());

        choiceContainer = new GameObject("ChoiceContainer", typeof(RectTransform));
        choiceContainer.transform.SetParent(dialoguePanel.transform, false);

        choiceButtonPrefab = CreateChoiceButtonTemplate(root);
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string text, Color color, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.color = color;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.outlineColor = Color.black;
        textComponent.outlineWidth = 0.14f;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static Button CreateChoiceButtonTemplate(Transform root)
    {
        GameObject buttonObject = new GameObject("ChoiceButton_RuntimeTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(root, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.93f, 0.93f, 0.9f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Choice";
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        buttonObject.SetActive(false);
        return button;
    }

    private void SelectButton(int index)
    {
        if (index < 0 || index >= activeButtons.Count) return;

        selectedButtonIndex = index;

        Button button = activeButtons[index];
        if (button == null) return;

        EnsureEventSystem();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private void HandleChoiceNavigation()
    {
        if (activeButtons.Count <= 1 || Keyboard.current == null) return;

        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            SelectButton((selectedButtonIndex + 1 + activeButtons.Count) % activeButtons.Count);
        }
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
        {
            SelectButton((selectedButtonIndex - 1 + activeButtons.Count) % activeButtons.Count);
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private bool WasSubmitRequested()
    {
        if (WasSubmitPressedThisFrame())
        {
            nextHeldSubmitAt = Time.unscaledTime + HeldSubmitDelay;
            return true;
        }

        SettingsManager settings = SettingsManager.Instance;
        if (settings == null || !settings.HoldInsteadOfRepeat || !IsSubmitHeld())
        {
            nextHeldSubmitAt = 0f;
            return false;
        }

        if (nextHeldSubmitAt <= 0f)
        {
            nextHeldSubmitAt = Time.unscaledTime + HeldSubmitDelay;
            return false;
        }

        if (Time.unscaledTime < nextHeldSubmitAt) return false;

        nextHeldSubmitAt = Time.unscaledTime + HeldSubmitInterval;
        return true;
    }

    private static bool WasSubmitPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null
            && (keyboard.eKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private static bool IsSubmitHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null
            && (keyboard.eKey.isPressed
                || keyboard.spaceKey.isPressed
                || keyboard.enterKey.isPressed
                || keyboard.numpadEnterKey.isPressed);
#else
        return Input.GetKey(KeyCode.E)
            || Input.GetKey(KeyCode.Space)
            || Input.GetKey(KeyCode.Return)
            || Input.GetKey(KeyCode.KeypadEnter);
#endif
    }

    private void RefreshSettings()
    {
        ConfigureCanvasScaler();
        if (dialogueText != null) dialogueText.fontSize = ResolveDialogueFontSize();
    }

    private static float ResolveDialogueFontSize()
    {
        SettingsManager settings = SettingsManager.Instance;
        if (settings == null) return BaseDialogueFontSize;

        return settings.SubtitleSize switch
        {
            0 => BaseDialogueFontSize - 3f,
            2 => BaseDialogueFontSize + 4f,
            _ => BaseDialogueFontSize
        };
    }

    private bool HasRequiredReferences()
    {
        return dialoguePanel != null
            && speakerText != null
            && dialogueText != null
            && choiceContainer != null
            && choiceButtonPrefab != null;
    }

    private void CompleteDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        ClearChoices();
        SetPlayerControl(true);
        Action complete = onDialogueComplete;
        onDialogueComplete = null;
        complete?.Invoke();
    }

    private void SetPlayerControl(bool state)
    {
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) player.SetCanMove(state);

        PlayerAttackController attack = FindFirstObjectByType<PlayerAttackController>();
        if (attack != null) attack.SetCanAttack(state);
    }
}

public class DialogueChoice
{
    public string text;
    public Action action;

    public DialogueChoice(string text, Action action)
    {
        this.text = text;
        this.action = action;
    }
}
