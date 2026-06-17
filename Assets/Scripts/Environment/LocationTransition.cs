using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LocationTransition : Interactable
{
    [SerializeField] private string targetScene;
    [SerializeField] private bool triggerNightOnTransition;
    [SerializeField] private bool requiresExteriorFragment;
    [SerializeField] private bool requiresInnerNightFragment;
    [SerializeField] private SquarePortalController portal;

    private Transform player;
    private ExteriorGateCutsceneController exteriorGateCutscene;
    private bool nightExitSequencePlayed;

    public SquarePortalController Portal => portal;

    private void Start()
    {
        ResolvePlayer();
        ConfigureSpecialPortalState();
    }

    public void Configure(string newTargetScene, bool triggerNight, bool requireExterior, bool requireInnerNight)
    {
        targetScene = newTargetScene;
        triggerNightOnTransition = triggerNight;
        requiresExteriorFragment = requireExterior;
        requiresInnerNightFragment = requireInnerNight;
    }

    public void ConfigurePortal(SquarePortalController newPortal)
    {
        portal = newPortal;
    }

    public override void Interact()
    {
        if (DialogueController.Instance == null || GameFlowController.Instance == null || WorldState.Instance == null) return;
        if (string.IsNullOrWhiteSpace(targetScene)) return;

        WorldState state = WorldState.Instance;
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (TryHandleExteriorGateInteraction(state, localizer)) return;
        if (TryHandleNightExitInteraction(state, localizer)) return;

        if (portal != null && !portal.IsUnlocked)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", localizer.Get("raw.transition.locked.square"));
            return;
        }

        if (requiresExteriorFragment && !state.hasExteriorFragment)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", localizer.Get("raw.transition.locked.first"));
            return;
        }

        if (requiresInnerNightFragment && !state.hasInnerNightFragment)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", localizer.Get("raw.transition.locked.second"));
            return;
        }

        DialogueController.Instance.ShowChoices(
            "SYSTEM",
            localizer.Get("raw.transition.prompt"),
            new List<DialogueChoice>
            {
                new DialogueChoice(localizer.Get("raw.transition.enter"), Transition),
                new DialogueChoice(localizer.Get("raw.transition.leave"), null)
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

    private void ConfigureSpecialPortalState()
    {
        if (portal == null) return;

        if (IsExteriorNightTransition())
        {
            portal.SetUnlockWhenFragmentCollected(false);
            portal.Lock();
            portal.SetPortalVisible(false);
            ResolveExteriorGateCutscene();
        }
        else if (IsNightFinalTransition())
        {
            portal.SetUnlockWhenFragmentCollected(false);
            portal.Lock();
        }
    }

    private bool TryHandleExteriorGateInteraction(WorldState state, LocalizationManager localizer)
    {
        if (!IsExteriorNightTransition()) return false;

        if (!state.hasExteriorFragment)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", localizer.Get("raw.transition.locked.first"));
            return true;
        }

        ResolveExteriorGateCutscene()?.StartFromInteraction();
        return true;
    }

    private bool TryHandleNightExitInteraction(WorldState state, LocalizationManager localizer)
    {
        if (!IsNightFinalTransition()) return false;

        if (!state.hasInnerNightFragment)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", localizer.Get("raw.transition.locked.second"));
            return true;
        }

        if (nightExitSequencePlayed) return false;
        nightExitSequencePlayed = true;

        DialogueController.Instance.ShowDialoguePages(
            "FRAGMENT",
            new[]
            {
                localizer.Get("raw.night.exit.cast"),
                localizer.Get("raw.night.exit.fragment"),
                localizer.Get("raw.night.exit.mercy")
            },
            () =>
            {
                CastGateProjectile();
                portal?.Unlock();
                Transition();
            });
        return true;
    }

    private void CastGateProjectile()
    {
        ResolvePlayer();
        Vector3 start = player != null
            ? player.position + Vector3.up * 1.15f + player.forward * 0.4f
            : transform.position + Vector3.up * 1.2f;
        Vector3 end = transform.position + Vector3.up * 1.5f;
        Vector3 direction = end - start;

        CorruptionSpellProjectile.Spawn(
            start,
            end,
            direction.sqrMagnitude > 0.001f ? -direction.normalized : Vector3.up,
            transform,
            16f,
            0.18f);
    }

    private bool IsExteriorNightTransition()
    {
        return triggerNightOnTransition && requiresExteriorFragment && targetScene == SceneIds.Night;
    }

    public void TransitionFromExteriorGateCutscene()
    {
        if (!IsExteriorNightTransition()) return;
        Transition();
    }

    private bool IsNightFinalTransition()
    {
        return requiresInnerNightFragment && targetScene == SceneIds.Final && SceneManager.GetActiveScene().name == SceneIds.Night;
    }

    private void ResolvePlayer()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private ExteriorGateCutsceneController ResolveExteriorGateCutscene()
    {
        if (!IsExteriorNightTransition()) return null;
        if (exteriorGateCutscene == null)
        {
            exteriorGateCutscene = GetComponent<ExteriorGateCutsceneController>();
            if (exteriorGateCutscene == null) exteriorGateCutscene = gameObject.AddComponent<ExteriorGateCutsceneController>();
        }

        exteriorGateCutscene.Configure(this, portal);
        return exteriorGateCutscene;
    }
}
