using UnityEngine;

public class NightPhaseController : MonoBehaviour
{
    private void Start()
    {
        if (GameFlowController.Instance != null) GameFlowController.Instance.SetNight(true);
        PlayerAttackController attack = FindFirstObjectByType<PlayerAttackController>();
        if (attack != null) attack.SetSceneAttackEnabled(true);
        RuntimeHudController.Instance?.NotifyNightUnlocked();

        RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.08f);
        RenderSettings.fogDensity = 0.045f;
    }
}
