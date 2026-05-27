using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LocationTransition : Interactable
{
    [SerializeField] private string targetScene;
    [SerializeField] private bool triggerNightOnTransition;
    [SerializeField] private bool requiresLightPayment;

    public void Configure(string newTargetScene, bool triggerNight, bool requireLightPayment)
    {
        targetScene = newTargetScene;
        triggerNightOnTransition = triggerNight;
        requiresLightPayment = requireLightPayment;
    }

    public override void Interact()
    {
        if (DialogueController.Instance == null || GameFlowController.Instance == null || WorldState.Instance == null) return;

        string resolvedTarget = ResolveTargetScene();
        if (string.IsNullOrWhiteSpace(resolvedTarget)) return;

        if (RequiresEntryPayment(resolvedTarget))
        {
            ShowPaidEntranceChoices(resolvedTarget);
            return;
        }

        ShowSimpleTransitionChoices(resolvedTarget);
    }

    private void ShowPaidEntranceChoices(string resolvedTarget)
    {
        WorldState state = WorldState.Instance;
        if (state.lightLevel <= 0)
        {
            DialogueController.Instance.ShowChoices(
                "SYSTEM",
                "Вход распознан. Ночной квадрат не принимает пустых. Найди фрагмент света и вернись к зданию.",
                new List<DialogueChoice>
                {
                    new DialogueChoice("Отойти", null)
                });
            return;
        }

        DialogueController.Instance.ShowChoices(
            "BUILDING_SQUARE",
            "Здание дышит за стеной. Чтобы войти, нужно отдать фрагмент света. После входа ночь начнется сама.",
            new List<DialogueChoice>
            {
                new DialogueChoice("Отдать фрагмент и войти", () =>
                {
                    if (!state.SpendLight(1)) return;

                    state.paidEntryFragment = true;
                    state.nightDebt += 1;
                    state.recognition += 8;
                    Transition(resolvedTarget);
                }),
                new DialogueChoice("Отойти", null)
            });
    }

    private void ShowSimpleTransitionChoices(string resolvedTarget)
    {
        DialogueController.Instance.ShowChoices(
            "SYSTEM",
            "Выход распознан. Назад можно будет вернуться не тем же, кто вошел. Продолжить?",
            new List<DialogueChoice>
            {
                new DialogueChoice("Войти", () => Transition(resolvedTarget)),
                new DialogueChoice("Отойти", null)
            });
    }

    private void Transition(string resolvedTarget)
    {
        if (ShouldTriggerNight(resolvedTarget))
        {
            WorldState.Instance.cycleCount++;
            GameFlowController.Instance.SetNight(true);
        }

        GameFlowController.Instance.TransitionToLocation(resolvedTarget);
    }

    private string ResolveTargetScene()
    {
        if (!string.IsNullOrWhiteSpace(targetScene)) return targetScene;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "LOCATION_01_EXTERIOR_DAY") return "LOcate2";
        if (currentScene == "LOcate2" || currentScene == "LOCATION_02_INNER_NIGHT_SQUARE") return "LOCATION_03_GATE_FINAL";

        return string.Empty;
    }

    private bool RequiresEntryPayment(string resolvedTarget)
    {
        if (requiresLightPayment) return true;
        return SceneManager.GetActiveScene().name == "LOCATION_01_EXTERIOR_DAY"
            && (resolvedTarget == "LOcate2" || resolvedTarget == "LOCATION_02_INNER_NIGHT_SQUARE");
    }

    private bool ShouldTriggerNight(string resolvedTarget)
    {
        return triggerNightOnTransition
            || resolvedTarget == "LOcate2"
            || resolvedTarget == "LOCATION_02_INNER_NIGHT_SQUARE";
    }
}
