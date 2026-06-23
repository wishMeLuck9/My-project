using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalGateOutcomeController : MonoBehaviour
{
    [SerializeField] private GuardianController[] guardians;
    [SerializeField] private Transform arenaRespawnPoint;
    [SerializeField] private Transform leftGateDoor;
    [SerializeField] private Transform rightGateDoor;
    [SerializeField] private Collider finalEntryTrigger;

    private PlayerController3D player;
    private PlayerAttackController attack;
    private PlayerHealthController playerHealth;
    private FinalBossDirector bossDirector;
    private bool introStarted;
    private bool introComplete;
    private bool started;
    private bool resolved;
    private bool forceResolutionQueued;
    private bool routeRecoveryQueued;
    private float nextArenaResetTime;

    private void Start()
    {
        ResolveReferences();
        if (attack != null) attack.SetSceneAttackEnabled(false);
        if (TryStartRouteRecovery()) return;

        Invoke(nameof(StartFinalIntro), 0.85f);
    }

    public void BeginResolution()
    {
        if (routeRecoveryQueued) return;
        if (started || resolved || WorldState.Instance == null) return;
        if (!introComplete)
        {
            StartFinalIntro();
            return;
        }

        started = true;
        ResolveReferences();

        WorldState state = WorldState.Instance;
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        if (!state.hasExteriorFragment || !state.hasInnerNightFragment)
        {
            DialogueController.Instance?.ShowDialogue(GateSpeaker(localizer), localizer.Get("raw.gate.incomplete"));
            started = false;
            return;
        }

        if (HasViolentRoute(state))
        {
            StartArena(localizer);
        }
        else
        {
            OfferFinalChoices(localizer);
        }
    }

    public void StartForcedBattleFromIntro()
    {
        if (routeRecoveryQueued) return;
        if (started || resolved) return;

        started = true;
        StartArena(LocalizationManager.EnsureInstance());
    }

    public void ResetArenaAfterPlayerHit()
    {
        if (resolved || Time.time < nextArenaResetTime || player == null) return;

        nextArenaResetTime = Time.time + 0.75f;
        if (WorldState.Instance != null) WorldState.Instance.playerDeaths += 1;

        Vector3 respawnPosition = arenaRespawnPoint != null ? arenaRespawnPoint.position : player.transform.position;
        Quaternion respawnRotation = arenaRespawnPoint != null ? arenaRespawnPoint.rotation : player.transform.rotation;
        player.Teleport(respawnPosition, respawnRotation);
        playerHealth?.ConfigureCheckpoint(arenaRespawnPoint);

        foreach (GuardianController guardian in guardians)
        {
            guardian?.ResetBattleState();
        }

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController.Instance?.ShowDialogue(GateSpeaker(localizer), localizer.Get("raw.gate.restart"));
    }

    public void NotifyPlayerDamaged(PlayerHealthController health)
    {
        if (resolved || health == null) return;

        if (health.IsDead)
        {
            ResetArenaAfterPlayerHit();
            return;
        }

        RuntimeHudController.Instance?.ShowSystemMessage(
            LocalizationManager.EnsureInstance().Format("hud.damage", health.CurrentHealth, health.MaxHealth),
            1.4f);
    }

    public void NotifyGuardianDefeated()
    {
        if (resolved || forceResolutionQueued || guardians == null || guardians.Length == 0) return;

        foreach (GuardianController guardian in guardians)
        {
            if (guardian == null || !guardian.IsDefeated) return;
        }

        forceResolutionQueued = true;
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController.Instance?.ShowDialoguePages(
            GateSpeaker(localizer),
            new[]
            {
                localizer.Get("raw.final.bound.1"),
                localizer.Get("raw.final.bound.2")
            },
            () =>
            {
                WorldState.Instance?.CompleteForceEnding();
                ResolveEnding("raw.ending.force.title", "raw.ending.force.text");
            });
    }

    private void StartFinalIntro()
    {
        if (routeRecoveryQueued) return;
        if (introStarted || resolved || DialogueController.Instance == null) return;
        introStarted = true;
        ResolveReferences();

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        WorldState state = WorldState.Instance;
        bool violent = state != null && HasViolentRoute(state);
        List<string> pages = new List<string>
        {
            localizer.Get("raw.final.intro.guardians"),
            localizer.Get("raw.final.intro.boundaries")
        };

        if (violent)
        {
            pages.Add(localizer.Get("raw.final.intro.violent"));
            pages.Add(localizer.Get("raw.final.intro.attack"));
        }
        else
        {
            pages.Add(localizer.Get("raw.final.intro.peaceful"));
            pages.Add(localizer.Get("raw.final.intro.queue"));
            pages.Add(localizer.Get("raw.final.intro.offer"));
        }

        DialogueController.Instance.ShowDialoguePages(
            GateSpeaker(localizer),
            pages,
            () =>
            {
                introComplete = true;
                if (violent) StartForcedBattleFromIntro();
            });
    }

    private void StartArena(LocalizationManager localizer)
    {
        ResolveReferences();
        if (attack != null) attack.SetSceneAttackEnabled(true);
        foreach (GuardianController guardian in guardians)
        {
            guardian?.BeginBattle(this, player != null ? player.transform : null);
        }

        bossDirector?.BeginFight();
        DialogueController.Instance?.ShowDialogue(GateSpeaker(localizer), localizer.Get("raw.gate.violent"));
    }

    private void OfferFinalChoices(LocalizationManager localizer)
    {
        if (DialogueController.Instance == null) return;

        DialogueController.Instance.ShowChoices(
            GateSpeaker(localizer),
            localizer.Get("raw.gate.peaceful.prompt"),
            new List<DialogueChoice>
            {
                new DialogueChoice(localizer.Get("raw.gate.accept.chains"), ResolveKeeperPath),
                new DialogueChoice(localizer.Get("raw.gate.fight"), () => StartArena(localizer)),
                new DialogueChoice(localizer.Get("raw.gate.destroy"), ResolveFragmentDestroyedEnding)
            });
    }

    private void ResolveKeeperPath()
    {
        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController.Instance?.ShowDialoguePages(
            GateSpeaker(localizer),
            new[]
            {
                localizer.Get("raw.gate.accept.1"),
                localizer.Get("raw.gate.accept.2")
            },
            () =>
            {
                WorldState.Instance?.CompleteSacrificeEnding();
                ResolveEnding("raw.ending.paid.title", "raw.ending.paid.text");
            });
    }

    private void ResolveFragmentDestroyedEnding()
    {
        WorldState.Instance?.RecordFragmentDestroyedEnding();
        ResolveEnding("raw.ending.destroy.title", "raw.ending.destroy.text", false);
    }

    private void ResolveEnding(string titleKey, string textKey, bool openGate = true)
    {
        resolved = true;
        bossDirector?.EndFight();
        if (attack != null) attack.SetSceneAttackEnabled(false);
        if (openGate) OpenGate();
        LocalizationManager localizer = LocalizationManager.EnsureInstance();

        string title = localizer.Get(titleKey);
        DialogueController.Instance?.ShowDialogue(title, localizer.Get(textKey), () =>
        {
            DialogueController.Instance?.ShowChoices(
                title,
                localizer.Get("raw.ending.restart.prompt"),
                new List<DialogueChoice>
                {
                    new DialogueChoice(localizer.Get("ending.restart"), RestartGame)
                });
        });
    }

    private void OpenGate()
    {
        ResolveDoorReferences();
        if (leftGateDoor != null) leftGateDoor.localPosition += Vector3.left * 2.2f;
        if (rightGateDoor != null) rightGateDoor.localPosition += Vector3.right * 2.2f;
        if (finalEntryTrigger != null) finalEntryTrigger.enabled = false;
    }

    private void RestartGame()
    {
        WorldState.Instance?.ResetRun();
        GameFlowController.Instance?.TransitionToLocation(SceneIds.Exterior);
    }

    private bool TryStartRouteRecovery()
    {
        WorldState state = WorldState.Instance;
        if (state == null || (state.hasExteriorFragment && state.hasInnerNightFragment)) return false;

        bool hasAnyFragment = state.hasExteriorFragment || state.hasInnerNightFragment;
        string targetScene = hasAnyFragment ? SceneIds.Night : SceneIds.Exterior;
        string messageKey = hasAnyFragment
            ? "raw.gate.recovery.night"
            : "raw.gate.recovery.exterior";
        string fallback = hasAnyFragment
            ? "The second trace is unfinished. The gate folds you back to the protected alleys."
            : "The route is not recorded. Your body cannot hold the third square, so the gate returns you to the beginning.";

        routeRecoveryQueued = true;
        StartCoroutine(RouteRecoveryRoutine(targetScene, messageKey, fallback));
        return true;
    }

    private IEnumerator RouteRecoveryRoutine(string targetScene, string messageKey, string fallback)
    {
        Time.timeScale = 1f;
        yield return null;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        DialogueController dialogue = DialogueController.EnsureInstance();
        if (dialogue != null)
        {
            bool completed = false;
            dialogue.ShowDialogue(GateSpeaker(localizer), localizer.Get(messageKey, fallback), () => completed = true);
            while (!completed && dialogue.IsDialogueOpen)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.75f);
        }

        GameFlowController flow = GameFlowController.Instance ?? FindFirstObjectByType<GameFlowController>();
        if (flow != null)
        {
            flow.TransitionToLocation(targetScene);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    private void ResolveReferences()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController3D>();
        if (attack == null && player != null) attack = player.GetComponent<PlayerAttackController>();
        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealthController>();
            if (playerHealth != null) playerHealth.ConfigureCheckpoint(arenaRespawnPoint);
        }
        if (guardians == null || guardians.Length == 0) guardians = FindObjectsByType<GuardianController>(FindObjectsSortMode.None);
        if (bossDirector == null) bossDirector = GetComponent<FinalBossDirector>() ?? FindFirstObjectByType<FinalBossDirector>();
        if (bossDirector != null) bossDirector.Configure(this, player, guardians);
        if (finalEntryTrigger == null)
        {
            FinalGateEntryTrigger entry = FindFirstObjectByType<FinalGateEntryTrigger>();
            if (entry != null) finalEntryTrigger = entry.GetComponent<Collider>();
        }
        ResolveDoorReferences();
    }

    private void ResolveDoorReferences()
    {
        if (leftGateDoor == null || rightGateDoor == null)
            Debug.LogWarning("Final gate doors are not assigned on FinalGateOutcomeController.", this);
    }

    private static bool HasViolentRoute(WorldState state)
    {
        return state.enemyShadowsDefeated > 0 ||
               state.nightViolenceAttempted ||
               state.nightFragmentRoute == WorldState.NightFragmentRoute.Violence;
    }

    private static string GateSpeaker(LocalizationManager localizer)
    {
        return localizer.Get("speaker.gate", "GATE");
    }
}
