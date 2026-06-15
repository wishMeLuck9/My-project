using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
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

    private void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(Close);
        for (int i = 0; tabButtons != null && i < tabButtons.Length; i++)
        {
            int tabIndex = i;
            if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => ShowTab(tabIndex));
        }

        BindValueChanges();
        LocalizationManager.LanguageChanged += RefreshLocalizedOptions;
        RefreshFromSettings();
        ShowTab(0);
    }

    private void OnDestroy()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedOptions;
    }

    public void Open(Action onClose)
    {
        closeAction = onClose;
        gameObject.SetActive(true);
        RefreshFromSettings();
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
        for (int i = 0; tabPanels != null && i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null) tabPanels[i].SetActive(i == selected);
        }
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
