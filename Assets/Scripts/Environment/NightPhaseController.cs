using UnityEngine;

public class NightPhaseController : MonoBehaviour
{
    private void Start()
    {
        if (GameFlowController.Instance != null) GameFlowController.Instance.SetNight(true);

        if (DialogueController.Instance != null)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", "Ночь началась без запроса. Это не ошибка. Это свойство входа.");
        }

        RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.08f);
        RenderSettings.fogDensity = 0.045f;
    }
}
