using UnityEngine;

public class NightPhaseController : MonoBehaviour
{
    public static bool IntroComplete { get; private set; }

    private void Start()
    {
        IntroComplete = false;
        if (GameFlowController.Instance != null) GameFlowController.Instance.SetNight(true);
        PlayerAttackController attack = FindFirstObjectByType<PlayerAttackController>();
        if (attack != null) attack.SetSceneAttackEnabled(true);

        RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.08f);
        RenderSettings.fogDensity = 0.045f;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController dialogue = DialogueController.Instance;
        if (dialogue == null)
        {
            MarkIntroComplete();
            return;
        }

        dialogue.ShowDialoguePages(
            "SYSTEM",
            new[]
            {
                localizer.Get("raw.night.intro.square"),
                localizer.Get("raw.night.intro.attack"),
                localizer.Get("raw.night.intro.shadows"),
                localizer.Get("raw.night.intro.hunted")
            },
            MarkIntroComplete);
    }

    private static void MarkIntroComplete()
    {
        IntroComplete = true;
        RuntimeHudController.Instance?.NotifyNightUnlocked();
    }
}
