using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip sceneClip;
    [SerializeField] private AudioClip exteriorCalmClip;
    [SerializeField] private AudioClip exteriorEscapeClip;
    [SerializeField] private AudioClip finalPhaseTwoClip;
    [SerializeField] private AudioClip finalPhaseThreeClip;
    [SerializeField, Range(0f, 1f)] private float finalPhaseThreeHealthRatio = 0.25f;
    [SerializeField, Range(0f, 1f)] private float volumeScale = 0.62f;
    [SerializeField] private float crossfadeSeconds = 1.15f;

    private AudioSource audioSource;
    private AudioClip desiredClip;
    private float currentVolume;
    private FinalBossDirector finalBossDirector;

    public void Configure(AudioClip clip, AudioClip calmClip, AudioClip escapeClip, AudioClip phaseTwoClip = null, AudioClip phaseThreeClip = null)
    {
        sceneClip = clip;
        exteriorCalmClip = calmClip;
        exteriorEscapeClip = escapeClip;
        finalPhaseTwoClip = phaseTwoClip;
        finalPhaseThreeClip = phaseThreeClip;
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

        if (sceneName == SceneIds.Final)
        {
            FinalBossDirector bossDirector = ResolveFinalBossDirector();
            if (bossDirector != null && bossDirector.IsFightActive && bossDirector.IsPhaseTwo)
            {
                if (finalPhaseThreeClip != null && bossDirector.NormalizedBossHealth <= finalPhaseThreeHealthRatio)
                {
                    return finalPhaseThreeClip;
                }

                return finalPhaseTwoClip != null ? finalPhaseTwoClip : sceneClip;
            }
        }

        return sceneClip;
    }

    private FinalBossDirector ResolveFinalBossDirector()
    {
        if (finalBossDirector == null)
        {
            finalBossDirector = FindFirstObjectByType<FinalBossDirector>();
        }

        return finalBossDirector;
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
