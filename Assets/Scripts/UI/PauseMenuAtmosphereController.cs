using UnityEngine;
using UnityEngine.UI;

public class PauseMenuAtmosphereController : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RawImage fogOverlay;
    [SerializeField] private RawImage dustOverlay;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource buzzSource;
    [SerializeField, Range(0f, 1f)] private float ambienceVolumeScale = 0.22f;
    [SerializeField, Range(0f, 1f)] private float buzzVolumeScale = 0.08f;
    [SerializeField, Range(0f, 1f)] private float flickerStrength = 0.08f;
    [SerializeField] private Vector2 fogTiling = new Vector2(3.6f, 2.2f);
    [SerializeField] private Vector2 dustTiling = new Vector2(6.2f, 3.2f);

    private void OnEnable()
    {
        SettingsManager.EnsureInstance();
        SettingsManager.SettingsChanged += RefreshAudioState;
        RefreshAudioState();
        PlayLoop(ambienceSource);
        PlayLoop(buzzSource);
    }

    private void OnDisable()
    {
        SettingsManager.SettingsChanged -= RefreshAudioState;
        StopLoop(ambienceSource);
        StopLoop(buzzSource);
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        if (group != null)
        {
            group.alpha = 0.92f + Mathf.PerlinNoise(time * 1.1f, 4.7f) * flickerStrength;
        }

        if (fogOverlay != null)
        {
            fogOverlay.uvRect = new Rect(time * 0.01f, time * 0.004f, fogTiling.x, fogTiling.y);
        }

        if (dustOverlay != null)
        {
            dustOverlay.uvRect = new Rect(-time * 0.018f, time * 0.012f, dustTiling.x, dustTiling.y);
        }
    }

    private void RefreshAudioState()
    {
        SettingsManager settings = SettingsManager.EnsureInstance();
        bool muted = settings.MuteAll || settings.SfxVolume <= 0.001f;
        ApplyAudioState(ambienceSource, muted, settings.SfxVolume * ambienceVolumeScale);
        ApplyAudioState(buzzSource, muted, settings.SfxVolume * buzzVolumeScale);
    }

    private static void ApplyAudioState(AudioSource source, bool muted, float volume)
    {
        if (source == null) return;

        source.spatialBlend = 0f;
        source.loop = true;
        source.playOnAwake = false;
        source.mute = muted;
        source.volume = Mathf.Clamp01(volume);
    }

    private static void PlayLoop(AudioSource source)
    {
        if (source != null && source.clip != null && !source.isPlaying)
        {
            source.Play();
        }
    }

    private static void StopLoop(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }
}
