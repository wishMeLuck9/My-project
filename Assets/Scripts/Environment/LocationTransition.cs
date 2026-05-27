using System.Collections.Generic;
using UnityEngine;

public class LocationTransition : Interactable
{
    [SerializeField] private string targetScene;
    [SerializeField] private bool triggerNightOnTransition;
    [SerializeField] private bool requiresExteriorFragment;
    [SerializeField] private bool requiresInnerNightFragment;

    public void Configure(string newTargetScene, bool triggerNight, bool requireExterior, bool requireInnerNight)
    {
        targetScene = newTargetScene;
        triggerNightOnTransition = triggerNight;
        requiresExteriorFragment = requireExterior;
        requiresInnerNightFragment = requireInnerNight;
    }

    public override void Interact()
    {
        if (DialogueController.Instance == null || GameFlowController.Instance == null || WorldState.Instance == null) return;
        if (string.IsNullOrWhiteSpace(targetScene)) return;

        WorldState state = WorldState.Instance;
        if (requiresExteriorFragment && !state.hasExteriorFragment)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", "Выход не признает тебя без первого фрагмента.");
            return;
        }

        if (requiresInnerNightFragment && !state.hasInnerNightFragment)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", "До врат нельзя дойти без фрагмента, оставшегося в ночном квадрате.");
            return;
        }

        DialogueController.Instance.ShowChoices(
            "SYSTEM",
            "Порог найден. Продолжить путь?",
            new List<DialogueChoice>
            {
                new DialogueChoice("Войти", Transition),
                new DialogueChoice("Отойти", null)
            });
    }

    private void Transition()
    {
        if (triggerNightOnTransition)
        {
            WorldState.Instance.cycleCount++;
            GameFlowController.Instance.SetNight(true);
        }

        GameFlowController.Instance.TransitionToLocation(targetScene);
    }
}
