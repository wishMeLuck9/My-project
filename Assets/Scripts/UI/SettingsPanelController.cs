using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 LeftCenterAnchor = new Vector2(0f, 0.5f);
    private static readonly Vector2 BottomCenterAnchor = new Vector2(0.5f, 0f);
    private static readonly Vector2 LeftTopAnchor = new Vector2(0f, 1f);
    private static readonly Color RootPanelColor = new Color(0.014f, 0.026f, 0.022f, 0.9f);
    private static readonly Color PanelColor = new Color(0.018f, 0.038f, 0.032f, 0.9f);
    private static readonly Color TabIdleColor = new Color(0.016f, 0.036f, 0.032f, 0.94f);
    private static readonly Color TabSelectedColor = new Color(0.08f, 0.18f, 0.13f, 0.96f);
    private static readonly Color ButtonHighlightColor = new Color(0.12f, 0.26f, 0.18f, 1f);
    private static readonly Color AccentColor = new Color(0.68f, 0.94f, 0.78f, 1f);
    private static readonly Color WarmAccentColor = new Color(1f, 0.58f, 0.14f, 1f);
    private static readonly Color TextColor = new Color(0.9f, 0.96f, 0.88f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.78f, 0.68f, 1f);

    private const float FallbackRootWidth = 920f;
    private const float FallbackRootHeight = 560f;
    private const float MaxRootWidth = 960f;
    private const float MaxRootHeight = 570f;
    private const float MinContentHeight = 300f;

    [Header("Navigation")]
    [SerializeField] private GameObject[] tabPanels;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Button backButton;

    [Header("Audio")]
    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider musicVolume;
    [SerializeField] private Slider sfxVolume;
    [SerializeField] private Slider voiceVolume;
    [SerializeField] private Toggle muteAll;

    [Header("Video")]
    [SerializeField] private TMP_Dropdown resolution;
    [SerializeField] private Toggle fullscreen;
    [SerializeField] private Slider brightness;
    [SerializeField] private TMP_Dropdown quality;
    [SerializeField] private Toggle vSync;

    [Header("Controls")]
    [SerializeField] private Slider mouseSensitivity;
    [SerializeField] private Toggle invertY;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown language;

    [Header("Accessibility")]
    [SerializeField] private Toggle subtitles;
    [SerializeField] private TMP_Dropdown subtitleSize;
    [SerializeField] private Slider uiScale;
    [SerializeField] private Toggle highContrast;
    [SerializeField] private Toggle colorblindFriendly;
    [SerializeField] private Toggle reduceScreenShake;
    [SerializeField] private Toggle reduceMotion;
    [SerializeField] private Toggle tutorialHints;
    [SerializeField] private Toggle holdInsteadOfRepeat;
    [SerializeField] private Toggle toggleRun;
    [SerializeField] private Toggle simplePrompts;

    private Action closeAction;
    private bool initializing;
    private bool normalizingLayout;
    private RectTransform rootRect;
    private Vector2 tabButtonPosition;
    private Vector2 contentPosition;
    private Vector2 contentSize = new Vector2(720f, 456f);
    private float tabButtonSpacing = 58f;
    private float tabButtonWidth = 220f;
    private float tabButtonHeight = 44f;
    private float labelColumnX = -215f;
    private float controlColumnX = 205f;
    private float labelWidth = 290f;
    private float controlWidth = 320f;
    private float toggleWidth = 640f;
    private float rowHeight = 30f;
    private float normalTopY = 158f;
    private float normalStep = 54f;
    private float accessibilityTopY = 170f;
    private float accessibilityStep = 38f;
    private float bodyFontSize = 16f;
    private float navFontSize = 18f;
    private float staticControlsFontSize = 18f;
    private float backButtonWidth = 260f;
    private float backButtonY = 34f;
    private int currentTabIndex;

    private void Awake()
    {
        rootRect = GetComponent<RectTransform>();
        NormalizeLayout();

        if (backButton != null) backButton.onClick.AddListener(Close);
        for (int i = 0; tabButtons != null && i < tabButtons.Length; i++)
        {
            int tabIndex = i;
            if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => ShowTab(tabIndex));
        }

        BindValueChanges();
        LocalizationManager.LanguageChanged += RefreshLocalizedOptions;
        SettingsManager.SettingsChanged += NormalizeLayout;
        RefreshFromSettings();
        NormalizeLayout();
        ShowTab(0);
    }

    private void OnDestroy()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedOptions;
        SettingsManager.SettingsChanged -= NormalizeLayout;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        NormalizeLayout();
    }

    public void Open(Action onClose)
    {
        closeAction = onClose;
        gameObject.SetActive(true);
        RefreshFromSettings();
        NormalizeLayout();
        ShowTab(0);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Action action = closeAction;
        closeAction = null;
        action?.Invoke();
    }

    private void ShowTab(int selected)
    {
        currentTabIndex = Mathf.Max(0, selected);
        for (int i = 0; tabPanels != null && i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null) tabPanels[i].SetActive(i == selected);
        }

        StyleTabButtons();
    }

    private void NormalizeLayout()
    {
        if (normalizingLayout) return;

        normalizingLayout = true;
        try
        {
            Canvas.ForceUpdateCanvases();
            FitRootToParent();
            RecalculateLayoutMetrics();
            LayoutTabButtons();
            LayoutContentPanels();
            LayoutBackButton();
            LayoutControls();
            StylePanelVisuals();
        }
        finally
        {
            normalizingLayout = false;
        }
    }

    private void FitRootToParent()
    {
        if (rootRect == null) rootRect = GetComponent<RectTransform>();
        if (rootRect == null) return;

        Vector2 parentSize = ResolveParentSize();
        float parentWidth = parentSize.x;
        float parentHeight = parentSize.y;
        if (parentWidth <= 1f || parentHeight <= 1f) return;

        float padding = Mathf.Clamp(Mathf.Min(parentWidth, parentHeight) * 0.035f, 12f, 24f);
        Vector2 targetSize = new Vector2(
            Mathf.Clamp(parentWidth - padding * 2f, 1f, MaxRootWidth),
            Mathf.Clamp(parentHeight - padding * 2f, 1f, MaxRootHeight));

        rootRect.anchorMin = CenterAnchor;
        rootRect.anchorMax = CenterAnchor;
        rootRect.pivot = CenterAnchor;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = targetSize;
        rootRect.localScale = Vector3.one;
    }

    private Vector2 ResolveParentSize()
    {
        RectTransform parentRect = rootRect != null ? rootRect.parent as RectTransform : null;
        if (parentRect != null)
        {
            Rect rect = parentRect.rect;
            if (rect.width > 1f && rect.height > 1f) return rect.size;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.pixelRect.width > 1f && canvas.pixelRect.height > 1f)
        {
            float scaleFactor = Mathf.Max(0.01f, canvas.scaleFactor);
            return canvas.pixelRect.size / scaleFactor;
        }

        return new Vector2(FallbackRootWidth, FallbackRootHeight);
    }

    private void RecalculateLayoutMetrics()
    {
        if (rootRect == null) rootRect = GetComponent<RectTransform>();

        Rect root = rootRect != null ? rootRect.rect : new Rect(0f, 0f, FallbackRootWidth, FallbackRootHeight);
        float rootWidth = root.width > 1f ? root.width : FallbackRootWidth;
        float rootHeight = root.height > 1f ? root.height : FallbackRootHeight;
        float margin = Mathf.Clamp(rootWidth * 0.025f, 16f, 32f);
        float gap = Mathf.Clamp(rootWidth * 0.032f, 18f, 48f);
        float topClearance = Mathf.Clamp(rootHeight * 0.14f, 72f, 94f);
        float bottomClearance = Mathf.Clamp(rootHeight * 0.12f, 58f, 78f);

        tabButtonWidth = Mathf.Clamp(rootWidth * 0.19f, 138f, 205f);
        tabButtonHeight = Mathf.Clamp(rootHeight * 0.07f, 38f, 44f);
        tabButtonSpacing = tabButtonHeight + Mathf.Clamp(rootHeight * 0.018f, 8f, 13f);
        tabButtonPosition = new Vector2(margin, -(topClearance + 4f));

        float contentLeft = margin + tabButtonWidth + gap * 0.85f;
        float contentRight = margin;
        float computedContentWidth = Mathf.Max(1f, rootWidth - contentLeft - contentRight);
        float computedContentHeight = Mathf.Max(MinContentHeight, rootHeight - topClearance - bottomClearance);
        contentSize = new Vector2(computedContentWidth, computedContentHeight);
        contentPosition = new Vector2(contentLeft, -topClearance);

        float padX = Mathf.Clamp(contentSize.x * 0.06f, 24f, 42f);
        float innerWidth = Mathf.Max(1f, contentSize.x - padX * 2f);
        controlWidth = Mathf.Clamp(innerWidth * 0.46f, 190f, 340f);
        labelWidth = Mathf.Max(140f, innerWidth - controlWidth - 28f);
        labelColumnX = -contentSize.x * 0.5f + padX + labelWidth * 0.5f;
        controlColumnX = contentSize.x * 0.5f - padX - controlWidth * 0.5f;
        toggleWidth = innerWidth;

        rowHeight = Mathf.Clamp(contentSize.y * 0.062f, 25f, 31f);
        normalTopY = contentSize.y * 0.5f - Mathf.Clamp(contentSize.y * 0.105f, 34f, 48f);
        normalStep = Mathf.Clamp(contentSize.y * 0.115f, 40f, 52f);
        accessibilityTopY = contentSize.y * 0.5f - 28f;
        accessibilityStep = Mathf.Clamp((contentSize.y - 58f) / 10f, 30f, 38f);

        bodyFontSize = Mathf.Clamp(contentSize.x * 0.024f, 13f, 15.5f);
        staticControlsFontSize = Mathf.Clamp(contentSize.x * 0.026f, 13.5f, 17f);
        navFontSize = Mathf.Clamp(tabButtonWidth * 0.082f, 13f, 16f);
        backButtonWidth = Mathf.Clamp(rootWidth * 0.22f, 190f, 240f);
        backButtonY = Mathf.Clamp(rootHeight * 0.05f, 24f, 34f);
    }

    private void LayoutTabButtons()
    {
        if (tabButtons == null) return;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            Button button = tabButtons[i];
            if (button == null) continue;

            RectTransform rect = button.GetComponent<RectTransform>();
            SetRect(rect, LeftTopAnchor, LeftTopAnchor, LeftTopAnchor,
                new Vector2(tabButtonPosition.x, tabButtonPosition.y - i * tabButtonSpacing),
                new Vector2(tabButtonWidth, tabButtonHeight));

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            RectTransform iconBadge = button.transform.Find("HudIconBadge")?.GetComponent<RectTransform>();
            RectTransform icon = button.transform.Find("HudIcon")?.GetComponent<RectTransform>();
            bool hasIcon = icon != null;
            SetRect(iconBadge, LeftCenterAnchor, LeftCenterAnchor, LeftCenterAnchor, new Vector2(13f, 0f), new Vector2(30f, 30f));
            SetRect(icon, LeftCenterAnchor, LeftCenterAnchor, LeftCenterAnchor, new Vector2(17f, 0f), new Vector2(22f, 22f));

            if (label != null)
            {
                if (hasIcon)
                {
                    SetStretchOffsets(label.GetComponent<RectTransform>(), new Vector2(52f, 0f), new Vector2(-10f, 0f));
                    StyleText(label, navFontSize, TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap);
                }
                else
                {
                    StyleText(label, navFontSize, TextAlignmentOptions.Center, TextWrappingModes.NoWrap);
                }
            }
        }

        StyleTabButtons();
    }

    private void LayoutContentPanels()
    {
        if (tabPanels == null) return;

        foreach (GameObject panel in tabPanels)
        {
            if (panel == null) continue;

            RectTransform rect = panel.GetComponent<RectTransform>();
            SetRect(rect, LeftTopAnchor, LeftTopAnchor, LeftTopAnchor, contentPosition, contentSize);

            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = PanelColor;
                image.raycastTarget = true;
            }
        }
    }

    private void LayoutBackButton()
    {
        if (backButton == null) return;

        RectTransform rect = backButton.GetComponent<RectTransform>();
        SetRect(rect, BottomCenterAnchor, BottomCenterAnchor, BottomCenterAnchor,
            new Vector2(0f, backButtonY),
            new Vector2(backButtonWidth, 44f));

        TMP_Text label = backButton.GetComponentInChildren<TMP_Text>(true);
        StyleText(label, navFontSize, TextAlignmentOptions.Center, TextWrappingModes.NoWrap);
        StyleButton(backButton, false);
    }

    private void LayoutControls()
    {
        LayoutSlider(masterVolume, RowY(0));
        LayoutSlider(musicVolume, RowY(1));
        LayoutSlider(sfxVolume, RowY(2));
        LayoutSlider(voiceVolume, RowY(3));
        LayoutToggle(muteAll, RowY(4));

        LayoutDropdown(resolution, RowY(0));
        LayoutToggle(fullscreen, RowY(1));
        LayoutSlider(brightness, RowY(2));
        LayoutDropdown(quality, RowY(3));
        LayoutToggle(vSync, RowY(4));

        LayoutSlider(mouseSensitivity, RowY(0));
        LayoutToggle(invertY, RowY(1));
        LayoutStaticControls(mouseSensitivity);

        LayoutDropdown(language, RowY(0));

        LayoutToggle(subtitles, AccessibilityRowY(0));
        LayoutDropdown(subtitleSize, AccessibilityRowY(1));
        LayoutSlider(uiScale, AccessibilityRowY(2));
        LayoutToggle(highContrast, AccessibilityRowY(3));
        LayoutToggle(colorblindFriendly, AccessibilityRowY(4));
        LayoutToggle(reduceScreenShake, AccessibilityRowY(5));
        LayoutToggle(reduceMotion, AccessibilityRowY(6));
        LayoutToggle(tutorialHints, AccessibilityRowY(7));
        LayoutToggle(holdInsteadOfRepeat, AccessibilityRowY(8));
        LayoutToggle(toggleRun, AccessibilityRowY(9));
        LayoutToggle(simplePrompts, AccessibilityRowY(10));
    }

    private float RowY(int row)
    {
        return normalTopY - row * normalStep;
    }

    private float AccessibilityRowY(int row)
    {
        return accessibilityTopY - row * accessibilityStep;
    }

    private void LayoutSlider(Slider slider, float y)
    {
        if (slider == null) return;

        LayoutAssociatedLabel(slider.transform, y);
        SetRect(slider.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor, CenterAnchor,
            new Vector2(controlColumnX, y),
            new Vector2(controlWidth, 24f));
    }

    private void LayoutDropdown(TMP_Dropdown dropdown, float y)
    {
        if (dropdown == null) return;

        LayoutAssociatedLabel(dropdown.transform, y);
        SetRect(dropdown.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor, CenterAnchor,
            new Vector2(controlColumnX, y),
            new Vector2(controlWidth, Mathf.Max(32f, rowHeight + 4f)));

        if (dropdown.template != null)
        {
            SetRect(dropdown.template, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, -4f),
                new Vector2(0f, Mathf.Clamp(rowHeight * 4.1f, 112f, 148f)));
        }

        StyleText(dropdown.captionText, bodyFontSize, TextAlignmentOptions.Center, TextWrappingModes.NoWrap);
        StyleText(dropdown.itemText, bodyFontSize, TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap);
        StyleDropdown(dropdown);
    }

    private void LayoutToggle(Toggle toggle, float y)
    {
        if (toggle == null) return;

        RectTransform rect = toggle.GetComponent<RectTransform>();
        SetRect(rect, CenterAnchor, CenterAnchor, CenterAnchor,
            new Vector2(10f, y),
            new Vector2(toggleWidth, rowHeight));

        RectTransform box = toggle.targetGraphic != null
            ? toggle.targetGraphic.GetComponent<RectTransform>()
            : toggle.transform.Find("Background")?.GetComponent<RectTransform>();
        SetRect(box, LeftCenterAnchor, LeftCenterAnchor, LeftCenterAnchor, Vector2.zero, new Vector2(22f, 22f));

        if (toggle.graphic != null)
        {
            SetRect(toggle.graphic.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(16f, 16f));
        }

        TMP_Text label = toggle.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text != null && text.gameObject.name != "Checkmark");
        if (label != null)
        {
            SetRect(label.GetComponent<RectTransform>(), LeftCenterAnchor, LeftCenterAnchor, LeftCenterAnchor,
                new Vector2(32f, 0f),
                new Vector2(Mathf.Max(1f, toggleWidth - 46f), rowHeight));
            StyleText(label, bodyFontSize, TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap);
        }

        StyleToggle(toggle);
    }

    private void LayoutAssociatedLabel(Transform control, float y)
    {
        TMP_Text label = FindAssociatedLabel(control);
        if (label == null) return;

        SetRect(label.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor, CenterAnchor,
            new Vector2(labelColumnX, y),
            new Vector2(labelWidth, rowHeight));
        StyleText(label, bodyFontSize, TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap);
    }

    private static TMP_Text FindAssociatedLabel(Transform control)
    {
        if (control == null || control.parent == null) return null;

        string baseName = control.name;
        baseName = TrimSuffix(baseName, "_Slider");
        baseName = TrimSuffix(baseName, "_Dropdown");
        baseName = TrimSuffix(baseName, "_Toggle");

        Transform direct = control.parent.Find(baseName + "_Label");
        if (direct != null) return direct.GetComponent<TMP_Text>();

        return control.parent
            .GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text != null && text.name.StartsWith(baseName, StringComparison.Ordinal));
    }

    private void LayoutStaticControls(Component anchor)
    {
        if (anchor == null || anchor.transform.parent == null) return;

        TMP_Text staticText = anchor.transform.parent
            .GetComponentsInChildren<LocalizedText>(true)
            .FirstOrDefault(localized => localized != null && localized.Key == "settings.static_controls")
            ?.GetComponent<TMP_Text>();

        if (staticText == null)
        {
            staticText = anchor.transform.parent
                .GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text =>
                    text != null &&
                    (text.name.IndexOf("static_controls", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     text.text.IndexOf("WASD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     text.text.IndexOf("ESC", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        if (staticText == null) return;

        float staticTop = RowY(2) + rowHeight * 0.5f;
        float staticBottom = -contentSize.y * 0.5f + 32f;
        float staticHeight = Mathf.Max(110f, staticTop - staticBottom);
        float staticY = (staticTop + staticBottom) * 0.5f;
        SetRect(staticText.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor, CenterAnchor,
            new Vector2(0f, staticY),
            new Vector2(toggleWidth, staticHeight));
        StyleText(staticText, staticControlsFontSize, TextAlignmentOptions.TopLeft, TextWrappingModes.Normal);
        staticText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static string TrimSuffix(string value, string suffix)
    {
        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value.Substring(0, value.Length - suffix.Length)
            : value;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        if (rect == null) return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetStretchOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = CenterAnchor;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static void StyleText(TMP_Text text, float maxSize, TextAlignmentOptions alignment, TextWrappingModes wrapping)
    {
        if (text == null) return;

        text.enableAutoSizing = true;
        text.fontSizeMax = maxSize;
        text.fontSizeMin = Mathf.Max(10f, maxSize - 5f);
        text.alignment = alignment;
        text.textWrappingMode = wrapping;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void StylePanelVisuals()
    {
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.color = RootPanelColor;
        }

        StyleTabButtons();
        StyleButton(backButton, false);
        StyleSlider(masterVolume);
        StyleSlider(musicVolume);
        StyleSlider(sfxVolume);
        StyleSlider(voiceVolume);
        StyleSlider(brightness);
        StyleSlider(mouseSensitivity);
        StyleSlider(uiScale);
        StyleDropdown(resolution);
        StyleDropdown(quality);
        StyleDropdown(language);
        StyleDropdown(subtitleSize);
        StyleToggle(muteAll);
        StyleToggle(fullscreen);
        StyleToggle(vSync);
        StyleToggle(invertY);
        StyleToggle(subtitles);
        StyleToggle(highContrast);
        StyleToggle(colorblindFriendly);
        StyleToggle(reduceScreenShake);
        StyleToggle(reduceMotion);
        StyleToggle(tutorialHints);
        StyleToggle(holdInsteadOfRepeat);
        StyleToggle(toggleRun);
        StyleToggle(simplePrompts);

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null) continue;
            if (text.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text.color = AccentColor;
                continue;
            }

            text.color = text.fontSizeMax <= 13f ? MutedTextColor : TextColor;
        }
    }

    private void StyleTabButtons()
    {
        if (tabButtons == null) return;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            StyleButton(tabButtons[i], i == currentTabIndex);
        }
    }

    private static void StyleButton(Button button, bool selected)
    {
        if (button == null) return;

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.color = selected ? TabSelectedColor : TabIdleColor;
        }

        Image badge = button.transform.Find("HudIconBadge")?.GetComponent<Image>();
        if (badge != null)
        {
            badge.color = selected ? AccentColor : new Color(0.18f, 0.36f, 0.24f, 0.86f);
        }

        Image icon = button.transform.Find("HudIcon")?.GetComponent<Image>();
        if (icon != null)
        {
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = selected ? TabSelectedColor : TabIdleColor;
        colors.highlightedColor = ButtonHighlightColor;
        colors.pressedColor = WarmAccentColor;
        colors.selectedColor = selected ? TabSelectedColor : ButtonHighlightColor;
        colors.disabledColor = new Color(0.02f, 0.03f, 0.035f, 0.65f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void StyleSlider(Slider slider)
    {
        if (slider == null) return;

        Image background = slider.transform.Find("Background")?.GetComponent<Image>();
        if (background != null)
        {
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = new Color(0.03f, 0.078f, 0.055f, 0.96f);
        }

        Image target = slider.targetGraphic as Image;
        if (target != null)
        {
            target.sprite = null;
            target.type = Image.Type.Simple;
            target.color = WarmAccentColor;
        }

        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            fill.color = AccentColor;
        }

        Image handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null)
        {
            handle.sprite = null;
            handle.type = Image.Type.Simple;
            handle.color = WarmAccentColor;
        }
    }

    private static void StyleDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;

        Image image = dropdown.targetGraphic as Image;
        if (image != null) image.color = TabIdleColor;

        if (dropdown.captionText != null) dropdown.captionText.color = TextColor;
        if (dropdown.itemText != null) dropdown.itemText.color = TextColor;

        TMP_Text arrow = dropdown.transform.Find("Arrow")?.GetComponent<TMP_Text>();
        if (arrow != null)
        {
            arrow.text = "▼";
            arrow.color = AccentColor;
            arrow.raycastTarget = false;
        }

        if (dropdown.template != null)
        {
            Image templateImage = dropdown.template.GetComponent<Image>();
            if (templateImage != null) templateImage.color = PanelColor;

            foreach (Image childImage in dropdown.template.GetComponentsInChildren<Image>(true))
            {
                if (childImage == null || childImage == templateImage) continue;
                if (childImage.gameObject.name.IndexOf("Checkmark", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    childImage.sprite = null;
                    childImage.color = Color.clear;
                }
                else
                {
                    childImage.color = new Color(0.018f, 0.07f, 0.052f, 0.95f);
                }
            }

            foreach (Toggle itemToggle in dropdown.template.GetComponentsInChildren<Toggle>(true))
            {
                Transform checkmark = itemToggle.transform.Find("Item Checkmark");
                TMP_Text checkmarkText = EnsurePlainCheckText(checkmark);
                if (checkmarkText != null) itemToggle.graphic = checkmarkText;
            }
        }
    }

    private static void StyleToggle(Toggle toggle)
    {
        if (toggle == null) return;

        Image box = toggle.targetGraphic as Image;
        if (box != null)
        {
            box.sprite = null;
            box.type = Image.Type.Simple;
            box.color = new Color(0.035f, 0.1f, 0.065f, 1f);
        }

        Transform checkTransform = toggle.graphic != null
            ? toggle.graphic.transform
            : toggle.transform.Find("Background/Checkmark");
        TMP_Text check = EnsurePlainCheckText(checkTransform);
        if (check != null)
        {
            check.color = WarmAccentColor;
            toggle.graphic = check;
        }
    }

    private static TMP_Text EnsurePlainCheckText(Transform target)
    {
        if (target == null) return null;

        Image image = target.GetComponent<Image>();
        if (image != null) Destroy(image);

        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null) text = target.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = "✓";
        text.fontSize = 16f;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 16f;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = WarmAccentColor;
        text.raycastTarget = false;

        RectTransform rect = text.GetComponent<RectTransform>();
        SetRect(rect, CenterAnchor, CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(20f, 20f));
        return text;
    }

    private void BindValueChanges()
    {
        masterVolume?.onValueChanged.AddListener(_ => Apply());
        musicVolume?.onValueChanged.AddListener(_ => Apply());
        sfxVolume?.onValueChanged.AddListener(_ => Apply());
        voiceVolume?.onValueChanged.AddListener(_ => Apply());
        muteAll?.onValueChanged.AddListener(_ => Apply());
        resolution?.onValueChanged.AddListener(_ => Apply());
        fullscreen?.onValueChanged.AddListener(_ => Apply());
        brightness?.onValueChanged.AddListener(_ => Apply());
        quality?.onValueChanged.AddListener(_ => Apply());
        vSync?.onValueChanged.AddListener(_ => Apply());
        mouseSensitivity?.onValueChanged.AddListener(_ => Apply());
        invertY?.onValueChanged.AddListener(_ => Apply());
        subtitles?.onValueChanged.AddListener(_ => Apply());
        subtitleSize?.onValueChanged.AddListener(_ => Apply());
        uiScale?.onValueChanged.AddListener(_ => Apply());
        highContrast?.onValueChanged.AddListener(_ => Apply());
        colorblindFriendly?.onValueChanged.AddListener(_ => Apply());
        reduceScreenShake?.onValueChanged.AddListener(_ => Apply());
        reduceMotion?.onValueChanged.AddListener(_ => Apply());
        tutorialHints?.onValueChanged.AddListener(_ => Apply());
        holdInsteadOfRepeat?.onValueChanged.AddListener(_ => Apply());
        toggleRun?.onValueChanged.AddListener(_ => Apply());
        simplePrompts?.onValueChanged.AddListener(_ => Apply());
        language?.onValueChanged.AddListener(value => LocalizationManager.EnsureInstance().SetLanguage((GameLanguage)value));
    }

    private void RefreshFromSettings()
    {
        SettingsManager settings = SettingsManager.EnsureInstance();
        initializing = true;

        if (resolution != null)
        {
            resolution.ClearOptions();
            resolution.AddOptions(settings.AvailableResolutions.Select(value => $"{value.width} x {value.height}").ToList());
            resolution.SetValueWithoutNotify(settings.ResolutionIndex);
        }

        if (quality != null)
        {
            quality.ClearOptions();
            quality.AddOptions(QualitySettings.names.ToList());
            quality.SetValueWithoutNotify(settings.QualityIndex);
        }

        RefreshLocalizedOptions();

        SetValue(masterVolume, settings.MasterVolume);
        SetValue(musicVolume, settings.MusicVolume);
        SetValue(sfxVolume, settings.SfxVolume);
        SetValue(voiceVolume, settings.VoiceVolume);
        SetValue(muteAll, settings.MuteAll);
        SetValue(fullscreen, settings.Fullscreen);
        SetValue(brightness, settings.Brightness);
        SetValue(vSync, settings.VSync);
        SetValue(mouseSensitivity, settings.MouseSensitivity);
        SetValue(invertY, settings.InvertY);
        if (language != null) language.SetValueWithoutNotify((int)LocalizationManager.EnsureInstance().CurrentLanguage);
        SetValue(subtitles, settings.Subtitles);
        if (subtitleSize != null) subtitleSize.SetValueWithoutNotify(settings.SubtitleSize);
        SetValue(uiScale, settings.UiScale);
        SetValue(highContrast, settings.HighContrast);
        SetValue(colorblindFriendly, settings.ColorblindFriendly);
        SetValue(reduceScreenShake, settings.ReduceScreenShake);
        SetValue(reduceMotion, settings.ReduceMotion);
        SetValue(tutorialHints, settings.TutorialHints);
        SetValue(holdInsteadOfRepeat, settings.HoldInsteadOfRepeat);
        SetValue(toggleRun, settings.ToggleRun);
        SetValue(simplePrompts, settings.SimplePrompts);

        initializing = false;
    }

    private void RefreshLocalizedOptions()
    {
        LocalizationManager localizer = LocalizationManager.EnsureInstance();

        if (language != null)
        {
            int languageValue = language.value;
            language.ClearOptions();
            language.AddOptions(new[]
            {
                localizer.Get("language.russian", "Русский"),
                localizer.Get("language.english", "English"),
                localizer.Get("language.portuguese", "Português")
            }.ToList());
            language.SetValueWithoutNotify(Mathf.Clamp(languageValue, 0, language.options.Count - 1));
        }

        if (subtitleSize != null)
        {
            int subtitleValue = subtitleSize.value;
            subtitleSize.ClearOptions();
            subtitleSize.AddOptions(new[]
            {
                localizer.Get("settings.small", "Маленький"),
                localizer.Get("settings.medium", "Средний"),
                localizer.Get("settings.large", "Большой")
            }.ToList());
            subtitleSize.SetValueWithoutNotify(Mathf.Clamp(subtitleValue, 0, subtitleSize.options.Count - 1));
        }

        NormalizeLayout();
    }

    private void Apply()
    {
        if (initializing) return;

        SettingsManager settings = SettingsManager.EnsureInstance();
        settings.ApplyFromUi(
            ValueOr(masterVolume, settings.MasterVolume),
            ValueOr(musicVolume, settings.MusicVolume),
            ValueOr(sfxVolume, settings.SfxVolume),
            ValueOr(voiceVolume, settings.VoiceVolume),
            ValueOr(muteAll, settings.MuteAll),
            ValueOr(resolution, settings.ResolutionIndex),
            ValueOr(fullscreen, settings.Fullscreen),
            ValueOr(brightness, settings.Brightness),
            ValueOr(quality, settings.QualityIndex),
            ValueOr(vSync, settings.VSync),
            ValueOr(mouseSensitivity, settings.MouseSensitivity),
            ValueOr(invertY, settings.InvertY),
            ValueOr(subtitles, settings.Subtitles),
            ValueOr(subtitleSize, settings.SubtitleSize),
            ValueOr(uiScale, settings.UiScale),
            ValueOr(highContrast, settings.HighContrast),
            ValueOr(colorblindFriendly, settings.ColorblindFriendly),
            ValueOr(reduceScreenShake, settings.ReduceScreenShake),
            ValueOr(reduceMotion, settings.ReduceMotion),
            ValueOr(tutorialHints, settings.TutorialHints),
            ValueOr(holdInsteadOfRepeat, settings.HoldInsteadOfRepeat),
            ValueOr(toggleRun, settings.ToggleRun),
            ValueOr(simplePrompts, settings.SimplePrompts));
    }

    private static void SetValue(Slider slider, float value)
    {
        if (slider != null) slider.SetValueWithoutNotify(value);
    }

    private static void SetValue(Toggle toggle, bool value)
    {
        if (toggle != null) toggle.SetIsOnWithoutNotify(value);
    }

    private static float ValueOr(Slider slider, float fallback)
    {
        return slider != null ? slider.value : fallback;
    }

    private static bool ValueOr(Toggle toggle, bool fallback)
    {
        return toggle != null ? toggle.isOn : fallback;
    }

    private static int ValueOr(TMP_Dropdown dropdown, int fallback)
    {
        return dropdown != null ? dropdown.value : fallback;
    }
}
