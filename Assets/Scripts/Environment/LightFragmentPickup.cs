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

    private void OnEnable()
    {
        SyncAvailability();
    }

    private void Start()
    {
        SyncAvailability();
    }

    public void Configure(FragmentKind kind)
    {
        fragmentKind = kind;
        SyncAvailability();
    }

    public override void Interact()
    {
        if (SyncAvailability()) return;
        if (DialogueController.Instance == null || WorldState.Instance == null) return;

        WorldState state = WorldState.Instance;
        if (state.HasFragment(fragmentKind))
        {
            SyncCollectedState();
            return;
        }

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        string promptKey = fragmentKind == FragmentKind.Exterior
            ? "raw.fragment.day.prompt"
            : "raw.fragment.night.prompt";

        DialogueController.Instance.ShowChoices(
            "FRAGMENT",
            localizer.Get(promptKey),
            new List<DialogueChoice>
            {
                new DialogueChoice(localizer.Get("raw.fragment.take"), Collect),
                new DialogueChoice(localizer.Get("raw.fragment.leave"), null)
            });
    }

    public void RestoreForRetry()
    {
        if (fragmentKind != FragmentKind.Exterior) return;
        if (SyncCollectedState()) return;
        gameObject.SetActive(true);
    }

    private void Collect()
    {
        if (WorldState.Instance == null) return;
        if (WorldState.Instance.HasFragment(fragmentKind))
        {
            gameObject.SetActive(false);
            return;
        }

        WorldState.Instance.AcquireFragment(fragmentKind);
        FragmentCollected?.Invoke(fragmentKind);
        gameObject.SetActive(false);

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (fragmentKind == FragmentKind.Exterior)
        {
            ExteriorHuntController hunt = FindFirstObjectByType<ExteriorHuntController>();
            if (hunt != null) hunt.BeginHunt();
            DialogueController.Instance?.ShowDialogue("SYSTEM", localizer.Get("raw.exterior.fragment.awakening"));
        }
        else
        {
            DialogueController.Instance?.ShowDialogue("SYSTEM", localizer.Get("raw.fragment.night.collected"));
        }
    }

    private bool SyncCollectedState()
    {
        if (WorldState.Instance == null || !WorldState.Instance.HasFragment(fragmentKind)) return false;

        gameObject.SetActive(false);
        return true;
    }

    private bool SyncAvailability()
    {
        if (ShouldHideUnavailableInnerNightFragment()) return true;
        return SyncCollectedState();
    }

    private bool ShouldHideUnavailableInnerNightFragment()
    {
        if (!Application.isPlaying || fragmentKind != FragmentKind.InnerNight) return false;

        WorldState state = WorldState.Instance;
        if (state == null) return false;
        if (state.hasInnerNightFragment) return false;
        if (state.nightFragmentRoute != WorldState.NightFragmentRoute.None) return false;

        gameObject.SetActive(false);
        return true;
    }
}
