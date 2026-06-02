using System;
using System.Collections.Generic;
using UnityEngine;

public class LightFragmentPickup : Interactable
{
    public static event Action<FragmentKind> FragmentCollected;

    public enum FragmentKind
    {
        Exterior,
        InnerNight
    }

    [SerializeField] private FragmentKind fragmentKind = FragmentKind.Exterior;

    public FragmentKind Kind => fragmentKind;

    public void Configure(FragmentKind kind)
    {
        fragmentKind = kind;
    }

    public override void Interact()
    {
        if (DialogueController.Instance == null || WorldState.Instance == null) return;

        WorldState state = WorldState.Instance;
        if (state.HasFragment(fragmentKind))
        {
            gameObject.SetActive(false);
            return;
        }

        string text = fragmentKind == FragmentKind.Exterior
            ? "Фрагмент лежит там, где на тебя еще смотрели без погони. Поднять его?"
            : "Фрагмент остался на месте чужого выбора. Взять его с собой?";

        DialogueController.Instance.ShowChoices(
            "FRAGMENT",
            text,
            new List<DialogueChoice>
            {
                new DialogueChoice("Поднять", Collect),
                new DialogueChoice("Оставить", null)
            });
    }

    public void RestoreForRetry()
    {
        if (fragmentKind == FragmentKind.Exterior) gameObject.SetActive(true);
    }

    private void Collect()
    {
        if (WorldState.Instance == null || WorldState.Instance.HasFragment(fragmentKind)) return;

        WorldState.Instance.AcquireFragment(fragmentKind);
        FragmentCollected?.Invoke(fragmentKind);
        gameObject.SetActive(false);

        if (fragmentKind == FragmentKind.Exterior)
        {
            ExteriorHuntController hunt = FindFirstObjectByType<ExteriorHuntController>();
            if (hunt != null) hunt.BeginHunt();
            DialogueController.Instance?.ShowDialogue("SYSTEM", "Фрагмент принят. Квадрат узнал тебя. Беги к зданию на окраине.");
        }
        else
        {
            DialogueController.Instance?.ShowDialogue("SYSTEM", "Второй фрагмент принят. Теперь врата смогут назвать цену.");
        }
    }
}
