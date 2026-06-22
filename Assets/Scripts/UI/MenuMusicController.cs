using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicController : MonoBehaviour
{
    private const string MutedKey = "virus9.menu.musicMuted";

    [SerializeField] private AudioClip musicClip;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image toggleIcon;
    [SerializeField] private TMP_Text toggleLabel;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField, Range(0f, 1f)] private float menuVolumeScale = 0.65f;

    private AudioSource audioSource;
    private bool mutedByToggle;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        mutedByToggle = PlayerPrefs.GetInt(MutedKey, 0) != 0;
        toggleButton?.onClick.AddListener(ToggleMusic);

        SettingsManager.EnsureInstance();
        SettingsManager.SettingsChanged += RefreshAudioState;
        RefreshAudioState();
    }

    private void OnDestroy()
    {
        SettingsManager.SettingsChanged -= RefreshAudioState;
    }

    private void ToggleMusic()
    {
        mutedByToggle = !mutedByToggle;
        PlayerPrefs.SetInt(MutedKey, mutedByToggle ? 1 : 0);
        PlayerPrefs.Save();
        RefreshAudioState();
    }

    private void RefreshAudioState()
    {
        SettingsManager settings = SettingsManager.EnsureInstance();
        bool mutedBySettings = settings.MuteAll || settings.MusicVolume <= 0.001f;
        bool shouldMute = mutedByToggle || mutedBySettings;

        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(settings.MusicVolume * menuVolumeScale);
            audioSource.mute = shouldMute;
            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        if (toggleIcon != null)
        {
            toggleIcon.sprite = shouldMute ? musicOffSprite : musicOnSprite;
            toggleIcon.preserveAspect = true;
            toggleIcon.enabled = toggleIcon.sprite != null;
        }

        if (toggleLabel != null)
        {
            toggleLabel.text = shouldMute ? "MUSIC OFF" : "MUSIC ON";
        }
    }
}
