using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NightZeroStabilityCutsceneController : MonoBehaviour, IPlayerDeathHandler
{
    private const int ReferenceWidth = 1280;
    private const int ReferenceHeight = 720;

    [SerializeField] private float zeroHoldSeconds = 0.75f;
    [SerializeField] private float controlLockAfterRecoverSeconds = 0.45f;
    [SerializeField] private float empoweredInvulnerabilitySeconds = 5.5f;
    [SerializeField] private float shadowStaggerRadius = 8.5f;
    [SerializeField] private float shadowStaggerSeconds = 3.2f;
    [SerializeField] private float shadowPushDistance = 1.7f;
    [SerializeField] private float screenEffectSeconds = 1.6f;
    [SerializeField] private float cameraShakeStrength = 0.22f;
    [SerializeField] private float repeatCooldownSeconds = 4f;

    private CanvasGroup effectGroup;
    private Image blackoutImage;
    private RawImage noiseImage;
    private Texture2D noiseTexture;
    private Color32[] noisePixels;
    private Coroutine screenEffectRoutine;
    private bool cutsceneRunning;
    private float nextAllowedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (scene.name != SceneIds.Night) return;
        if (FindFirstObjectByType<NightZeroStabilityCutsceneController>(FindObjectsInactive.Include) != null) return;

        new GameObject("NightZeroStabilityCutsceneController").AddComponent<NightZeroStabilityCutsceneController>();
    }

    private void OnEnable()
    {
        PlayerHealthController.RegisterDeathHandler(this);
    }

    private void OnDisable()
    {
        PlayerHealthController.UnregisterDeathHandler(this);
    }

    private void OnDestroy()
    {
        if (noiseTexture != null)
        {
            Destroy(noiseTexture);
            noiseTexture = null;
        }
    }

    public bool HandlePlayerDeath(PlayerHealthController health)
    {
        if (SceneManager.GetActiveScene().name != SceneIds.Night) return false;
        if (cutsceneRunning) return true;
        if (Time.unscaledTime < nextAllowedAt) return false;
        if (health == null) return false;

        StartCoroutine(PlayZeroStabilityCutscene(health));
        return true;
    }

    private IEnumerator PlayZeroStabilityCutscene(PlayerHealthController health)
    {
        cutsceneRunning = true;
        nextAllowedAt = Time.unscaledTime + repeatCooldownSeconds;

        RuntimeHudController hud = RuntimeHudController.EnsureInstance();
        health.SetControlEnabled(false);
        PrototypeShadowActor.StaggerNearby(
            health.transform.position,
            shadowStaggerRadius,
            shadowStaggerSeconds,
            shadowPushDistance,
            includeGuardianProxy: true);

        NightZeroStabilityCameraShake shake = EnsureCameraShake();
        if (shake != null)
        {
            shake.Play(screenEffectSeconds, cameraShakeStrength);
        }

        PlayScreenEffect(screenEffectSeconds);
        hud.ShowSystemMessage(
            "\u0421\u0418\u0421\u0422\u0415\u041C\u0410:\n\u041E\u0431\u044A\u0435\u043A\u0442 \u043D\u0435 \u043F\u043E\u0434\u0447\u0438\u043D\u044F\u0435\u0442\u0441\u044F \u0440\u0430\u0437\u0440\u0443\u0448\u0435\u043D\u0438\u044E.\n\u0420\u0435\u043A\u043E\u043C\u0435\u043D\u0434\u0443\u0435\u0442\u0441\u044F \u043E\u0442\u0441\u0442\u0443\u043F\u043B\u0435\u043D\u0438\u0435.",
            4.4f);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, zeroHoldSeconds));

        if (health != null)
        {
            health.ResetToFullHealth();
            health.SetControlEnabled(false);
            health.GrantTemporaryInvulnerability(empoweredInvulnerabilitySeconds);
        }

        hud.ShowSystemMessage(
            "\u0423\u0421\u0422\u041E\u0419\u0427\u0418\u0412\u041E\u0421\u0422\u042C: 0\n\u041E\u0413\u0420\u0410\u041D\u0418\u0427\u0418\u0422\u0415\u041B\u042C \u0421\u041D\u042F\u0422.",
            3.6f);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, controlLockAfterRecoverSeconds));

        if (health != null)
        {
            health.SetControlEnabled(true);
            health.GrantTemporaryInvulnerability(empoweredInvulnerabilitySeconds);
        }

        cutsceneRunning = false;
    }

    private void PlayScreenEffect(float duration)
    {
        EnsureEffectUi();
        if (screenEffectRoutine != null) StopCoroutine(screenEffectRoutine);
        screenEffectRoutine = StartCoroutine(ScreenEffectRoutine(Mathf.Max(0.1f, duration)));
    }

    private IEnumerator ScreenEffectRoutine(float duration)
    {
        effectGroup.gameObject.SetActive(true);
        effectGroup.alpha = 1f;

        float elapsed = 0f;
        int frame = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(progress * Mathf.PI);
            float flicker = Mathf.Lerp(0.85f, 1.15f, Random.value);

            blackoutImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.14f, 0.36f, pulse) * flicker);
            noiseImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.16f, 0.025f, progress) * flicker);
            noiseImage.uvRect = new Rect(Random.value, Random.value, 1f, 1f);

            if (frame % 2 == 0) RefreshNoiseTexture();
            frame++;
            yield return null;
        }

        effectGroup.alpha = 0f;
        effectGroup.gameObject.SetActive(false);
        screenEffectRoutine = null;
    }

    private void EnsureEffectUi()
    {
        if (effectGroup != null) return;

        GameObject canvasObject = new GameObject("NightZeroStabilityEffectCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 925;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        effectGroup = canvasObject.GetComponent<CanvasGroup>();
        effectGroup.alpha = 0f;
        effectGroup.blocksRaycasts = false;
        effectGroup.interactable = false;

        GameObject blackoutObject = CreateFullScreenImage("ZeroStabilityBlackout", canvasObject.transform);
        blackoutImage = blackoutObject.GetComponent<Image>();
        blackoutImage.color = Color.clear;

        GameObject noiseObject = CreateFullScreenRawImage("ZeroStabilityNoise", canvasObject.transform);
        noiseImage = noiseObject.GetComponent<RawImage>();
        noiseImage.color = Color.clear;
        noiseTexture = new Texture2D(96, 54, TextureFormat.RGBA32, false)
        {
            name = "RuntimeZeroStabilityNoise",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        noisePixels = new Color32[96 * 54];
        noiseImage.texture = noiseTexture;
        RefreshNoiseTexture();

        canvasObject.SetActive(false);
    }

    private void RefreshNoiseTexture()
    {
        if (noiseTexture == null || noisePixels == null) return;

        for (int i = 0; i < noisePixels.Length; i++)
        {
            byte value = Random.value > 0.52f ? (byte)255 : (byte)0;
            noisePixels[i] = new Color32(value, value, value, 255);
        }

        noiseTexture.SetPixels32(noisePixels);
        noiseTexture.Apply(false);
    }

    private static NightZeroStabilityCameraShake EnsureCameraShake()
    {
        Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
        if (camera == null) return null;

        NightZeroStabilityCameraShake shake = camera.GetComponent<NightZeroStabilityCameraShake>();
        if (shake == null) shake = camera.gameObject.AddComponent<NightZeroStabilityCameraShake>();
        return shake;
    }

    private static GameObject CreateFullScreenImage(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        ConfigureFullScreenRect(obj.GetComponent<RectTransform>());
        obj.GetComponent<Image>().raycastTarget = false;
        return obj;
    }

    private static GameObject CreateFullScreenRawImage(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        obj.transform.SetParent(parent, false);
        ConfigureFullScreenRect(obj.GetComponent<RectTransform>());
        obj.GetComponent<RawImage>().raycastTarget = false;
        return obj;
    }

    private static void ConfigureFullScreenRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

[DefaultExecutionOrder(10000)]
internal sealed class NightZeroStabilityCameraShake : MonoBehaviour
{
    private float shakeUntil;
    private float shakeDuration;
    private float strength;
    private Vector3 lastPositionOffset;
    private Quaternion lastRotationOffset = Quaternion.identity;
    private bool hasAppliedOffset;

    public void Play(float duration, float newStrength)
    {
        RemovePreviousOffset();
        shakeDuration = Mathf.Max(0.1f, duration);
        strength = Mathf.Max(0f, newStrength);
        shakeUntil = Time.unscaledTime + shakeDuration;
    }

    private void LateUpdate()
    {
        RemovePreviousOffset();
        if (Time.unscaledTime >= shakeUntil || strength <= 0f) return;

        float remaining = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / shakeDuration);
        float amplitude = strength * remaining * remaining;
        Vector3 offset =
            transform.right * Random.Range(-amplitude, amplitude) +
            transform.up * Random.Range(-amplitude, amplitude);
        Quaternion rotationOffset = Quaternion.Euler(
            Random.Range(-amplitude, amplitude) * 2.3f,
            Random.Range(-amplitude, amplitude) * 2.3f,
            Random.Range(-amplitude, amplitude) * 3.4f);

        transform.position += offset;
        transform.rotation *= rotationOffset;
        lastPositionOffset = offset;
        lastRotationOffset = rotationOffset;
        hasAppliedOffset = true;
    }

    private void OnDisable()
    {
        RemovePreviousOffset();
    }

    private void RemovePreviousOffset()
    {
        if (!hasAppliedOffset) return;

        transform.rotation *= Quaternion.Inverse(lastRotationOffset);
        transform.position -= lastPositionOffset;
        lastRotationOffset = Quaternion.identity;
        lastPositionOffset = Vector3.zero;
        hasAppliedOffset = false;
    }
}
