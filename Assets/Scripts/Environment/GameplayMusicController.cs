using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip sceneClip;
    [SerializeField] private AudioClip exteriorCalmClip;
    [SerializeField] private AudioClip exteriorEscapeClip;
    [SerializeField, Range(0f, 1f)] private float volumeScale = 0.62f;
    [SerializeField] private float crossfadeSeconds = 1.15f;

    private AudioSource audioSource;
    private AudioClip desiredClip;
    private float currentVolume;

    public void Configure(AudioClip clip, AudioClip calmClip, AudioClip escapeClip)
    {
        sceneClip = clip;
        exteriorCalmClip = calmClip;
        exteriorEscapeClip = escapeClip;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        SettingsManager.EnsureInstance();
        SettingsManager.SettingsChanged += RefreshAudioState;
        LightFragmentPickup.FragmentCollected += HandleFragmentCollected;
        RefreshDesiredClip(true);
        RefreshAudioState();
    }

    private void OnDisable()
    {
        SettingsManager.SettingsChanged -= RefreshAudioState;
        LightFragmentPickup.FragmentCollected -= HandleFragmentCollected;
    }

    private void Update()
    {
        if (audioSource == null) return;

        RefreshDesiredClip(false);
        RefreshAudioState();
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, currentVolume, Time.unscaledDeltaTime / Mathf.Max(0.05f, crossfadeSeconds));
    }

    private void HandleFragmentCollected(LightFragmentPickup.FragmentKind fragmentKind)
    {
        if (fragmentKind == LightFragmentPickup.FragmentKind.Exterior)
        {
            RefreshDesiredClip(true);
        }
    }

    private void RefreshDesiredClip(bool force)
    {
        AudioClip nextClip = ResolveSceneClip();
        if (!force && nextClip == desiredClip) return;

        desiredClip = nextClip;
        if (audioSource == null || desiredClip == null) return;

        if (audioSource.clip != desiredClip)
        {
            audioSource.clip = desiredClip;
            audioSource.volume = 0f;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private AudioClip ResolveSceneClip()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == SceneIds.Exterior)
        {
            return WorldState.Instance != null && WorldState.Instance.hasExteriorFragment
                ? exteriorEscapeClip != null ? exteriorEscapeClip : sceneClip
                : exteriorCalmClip != null ? exteriorCalmClip : sceneClip;
        }

        return sceneClip;
    }

    private void RefreshAudioState()
    {
        SettingsManager settings = SettingsManager.EnsureInstance();
        bool muted = settings.MuteAll || settings.MusicVolume <= 0.001f;
        if (audioSource != null)
        {
            audioSource.mute = muted;
        }

        currentVolume = muted ? 0f : Mathf.Clamp01(settings.MusicVolume * volumeScale);
    }
}
