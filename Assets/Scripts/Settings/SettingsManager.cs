using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    private const string Prefix = "virus9.settings.";

    public static SettingsManager Instance { get; private set; }
    public static event Action SettingsChanged;

    public float MasterVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public float VoiceVolume { get; private set; }
    public bool MuteAll { get; private set; }
    public int ResolutionIndex { get; private set; }
    public bool Fullscreen { get; private set; }
    public float Brightness { get; private set; }
    public int QualityIndex { get; private set; }
    public bool VSync { get; private set; }
    public float MouseSensitivity { get; private set; }
    public bool InvertY { get; private set; }
    public bool Subtitles { get; private set; }
    public int SubtitleSize { get; private set; }
    public float UiScale { get; private set; }
    public bool HighContrast { get; private set; }
    public bool ColorblindFriendly { get; private set; }
    public bool ReduceScreenShake { get; private set; }
    public bool ReduceMotion { get; private set; }
    public bool TutorialHints { get; private set; }
    public bool HoldInsteadOfRepeat { get; private set; }
    public bool ToggleRun { get; private set; }
    public bool SimplePrompts { get; private set; }

    public IReadOnlyList<Resolution> AvailableResolutions { get; private set; }

    private Image brightnessOverlay;

    public static SettingsManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        SettingsManager existing = FindFirstObjectByType<SettingsManager>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        return new GameObject("SettingsManager").AddComponent<SettingsManager>();
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
        AvailableResolutions = Screen.resolutions
            .GroupBy(resolution => new { resolution.width, resolution.height })
            .Select(group => group.Last())
            .ToArray();
        if (AvailableResolutions.Count == 0)
        {
            AvailableResolutions = new[] { Screen.currentResolution };
        }

        Load();
        ApplyAll();
    }

    public void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(Prefix + "masterVolume", 1f);
        MusicVolume = PlayerPrefs.GetFloat(Prefix + "musicVolume", 0.8f);
        SfxVolume = PlayerPrefs.GetFloat(Prefix + "sfxVolume", 0.8f);
        VoiceVolume = PlayerPrefs.GetFloat(Prefix + "voiceVolume", 0.8f);
        MuteAll = ReadBool("muteAll", false);
        ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "resolution", FindCurrentResolution()), 0, AvailableResolutions.Count - 1);
        Fullscreen = ReadBool("fullscreen", Screen.fullScreen);
        Brightness = PlayerPrefs.GetFloat(Prefix + "brightness", 1f);
        QualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "quality", QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        VSync = ReadBool("vsync", QualitySettings.vSyncCount > 0);
        MouseSensitivity = PlayerPrefs.GetFloat(Prefix + "mouseSensitivity", 1f);
        InvertY = ReadBool("invertY", false);
        Subtitles = ReadBool("subtitles", true);
        SubtitleSize = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "subtitleSize", 1), 0, 2);
        UiScale = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "uiScale", 1f), 0.8f, 1.4f);
        HighContrast = ReadBool("highContrast", false);
        ColorblindFriendly = ReadBool("colorblindFriendly", false);
        ReduceScreenShake = ReadBool("reduceScreenShake", false);
        ReduceMotion = ReadBool("reduceMotion", false);
        TutorialHints = ReadBool("tutorialHints", true);
        HoldInsteadOfRepeat = ReadBool("holdInsteadOfRepeat", false);
        ToggleRun = ReadBool("toggleRun", false);
        SimplePrompts = ReadBool("simplePrompts", true);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(Prefix + "masterVolume", MasterVolume);
        PlayerPrefs.SetFloat(Prefix + "musicVolume", MusicVolume);
        PlayerPrefs.SetFloat(Prefix + "sfxVolume", SfxVolume);
        PlayerPrefs.SetFloat(Prefix + "voiceVolume", VoiceVolume);
        WriteBool("muteAll", MuteAll);
        PlayerPrefs.SetInt(Prefix + "resolution", ResolutionIndex);
        WriteBool("fullscreen", Fullscreen);
        PlayerPrefs.SetFloat(Prefix + "brightness", Brightness);
        PlayerPrefs.SetInt(Prefix + "quality", QualityIndex);
        WriteBool("vsync", VSync);
        PlayerPrefs.SetFloat(Prefix + "mouseSensitivity", MouseSensitivity);
        WriteBool("invertY", InvertY);
        WriteBool("subtitles", Subtitles);
        PlayerPrefs.SetInt(Prefix + "subtitleSize", SubtitleSize);
        PlayerPrefs.SetFloat(Prefix + "uiScale", UiScale);
        WriteBool("highContrast", HighContrast);
        WriteBool("colorblindFriendly", ColorblindFriendly);
        WriteBool("reduceScreenShake", ReduceScreenShake);
        WriteBool("reduceMotion", ReduceMotion);
        WriteBool("tutorialHints", TutorialHints);
        WriteBool("holdInsteadOfRepeat", HoldInsteadOfRepeat);
        WriteBool("toggleRun", ToggleRun);
        WriteBool("simplePrompts", SimplePrompts);
        PlayerPrefs.Save();
    }

    public void ApplyAll()
    {
        float targetVolume = MuteAll ? 0f : MasterVolume;
        if (!Mathf.Approximately(AudioListener.volume, targetVolume))
        {
            AudioListener.volume = targetVolume;
        }

        Resolution resolution = AvailableResolutions[Mathf.Clamp(ResolutionIndex, 0, AvailableResolutions.Count - 1)];
        if (Screen.width != resolution.width || Screen.height != resolution.height || Screen.fullScreen != Fullscreen)
        {
            Screen.SetResolution(resolution.width, resolution.height, Fullscreen);
        }

        int qualityIndex = Mathf.Clamp(QualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        if (QualitySettings.names.Length > 0 && QualitySettings.GetQualityLevel() != qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }

        int targetVSync = VSync ? 1 : 0;
        if (QualitySettings.vSyncCount != targetVSync)
        {
            QualitySettings.vSyncCount = targetVSync;
        }

        EnsureBrightnessOverlay();
        brightnessOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - Brightness) * 0.55f);
        ApplyCanvasScale();
        SettingsChanged?.Invoke();
    }

    public void ApplyFromUi(
        float masterVolume,
        float musicVolume,
        float sfxVolume,
        float voiceVolume,
        bool muteAll,
        int resolutionIndex,
        bool fullscreen,
        float brightness,
        int qualityIndex,
        bool vSync,
        float mouseSensitivity,
        bool invertY,
        bool subtitles,
        int subtitleSize,
        float uiScale,
        bool highContrast,
        bool colorblindFriendly,
        bool reduceScreenShake,
        bool reduceMotion,
        bool tutorialHints,
        bool holdInsteadOfRepeat,
        bool toggleRun,
        bool simplePrompts)
    {
        MasterVolume = masterVolume;
        MusicVolume = musicVolume;
        SfxVolume = sfxVolume;
        VoiceVolume = voiceVolume;
        MuteAll = muteAll;
        ResolutionIndex = Mathf.Clamp(resolutionIndex, 0, AvailableResolutions.Count - 1);
        Fullscreen = fullscreen;
        Brightness = brightness;
        QualityIndex = Mathf.Clamp(qualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        VSync = vSync;
        MouseSensitivity = mouseSensitivity;
        InvertY = invertY;
        Subtitles = subtitles;
        SubtitleSize = subtitleSize;
        UiScale = uiScale;
        HighContrast = highContrast;
        ColorblindFriendly = colorblindFriendly;
        ReduceScreenShake = reduceScreenShake;
        ReduceMotion = reduceMotion;
        TutorialHints = tutorialHints;
        HoldInsteadOfRepeat = holdInsteadOfRepeat;
        ToggleRun = toggleRun;
        SimplePrompts = simplePrompts;
        Save();
        ApplyAll();
    }

    public void ApplyCanvasScale()
    {
        foreach (CanvasScaler scaler in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (scaler == null || scaler.gameObject.name == "SettingsBrightnessOverlay") continue;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f) / Mathf.Max(0.1f, UiScale);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private int FindCurrentResolution()
    {
        for (int i = 0; i < AvailableResolutions.Count; i++)
        {
            Resolution resolution = AvailableResolutions[i];
            if (resolution.width == Screen.width && resolution.height == Screen.height) return i;
        }

        return AvailableResolutions.Count - 1;
    }

    private void EnsureBrightnessOverlay()
    {
        if (brightnessOverlay != null) return;

        GameObject canvasObject = new GameObject("SettingsBrightnessOverlay");
        DontDestroyOnLoad(canvasObject);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        canvasObject.AddComponent<CanvasScaler>();

        GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        brightnessOverlay = overlayObject.GetComponent<Image>();
        brightnessOverlay.raycastTarget = false;
    }

    private static bool ReadBool(string key, bool fallback)
    {
        return PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0;
    }

    private static void WriteBool(string key, bool value)
    {
        PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
    }
}
