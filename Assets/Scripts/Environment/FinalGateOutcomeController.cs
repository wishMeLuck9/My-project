using System.Collections.Generic;
using UnityEngine;

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
    private bool started;
    private bool resolved;
    private float nextArenaResetTime;

    private void Start()
    {
        ResolveReferences();
        if (attack != null) attack.SetSceneAttackEnabled(false);
    }

    public void BeginResolution()
    {
        if (started || resolved || WorldState.Instance == null) return;
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

        if (state.enemyShadowsDefeated > 0)
        {
            StartArena(localizer);
        }
        else
        {
            OfferSacrifice(localizer);
        }
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
        if (resolved || guardians == null || guardians.Length == 0) return;

        foreach (GuardianController guardian in guardians)
        {
            if (guardian == null || !guardian.IsDefeated) return;
        }

        WorldState.Instance?.CompleteForceEnding();
        ResolveEnding("raw.ending.force.title", "raw.ending.force.text");
    }

    private void StartArena(LocalizationManager localizer)
    {
        if (attack != null) attack.SetSceneAttackEnabled(true);
        foreach (GuardianController guardian in guardians)
        {
            guardian?.BeginBattle(this, player != null ? player.transform : null);
        }

        bossDirector?.BeginFight();
        DialogueController.Instance?.ShowDialogue(GateSpeaker(localizer), localizer.Get("raw.gate.violent"));
    }

    private void OfferSacrifice(LocalizationManager localizer)
    {
        if (DialogueController.Instance == null) return;

        DialogueController.Instance.ShowChoices(
            GateSpeaker(localizer),
            localizer.Get("raw.gate.peaceful.prompt"),
            new List<DialogueChoice>
            {
                new DialogueChoice(localizer.Get("raw.gate.pay"), () =>
                {
                    WorldState.Instance.CompleteSacrificeEnding();
                    ResolveEnding("raw.ending.paid.title", "raw.ending.paid.text");
                }),
                new DialogueChoice(localizer.Get("raw.gate.restart.choice"), () => started = false)
            });
    }

    private void ResolveEnding(string titleKey, string textKey)
    {
        resolved = true;
        bossDirector?.EndFight();
        if (attack != null) attack.SetSceneAttackEnabled(false);
        OpenGate();
        LocalizationManager localizer = LocalizationManager.EnsureInstance();

        DialogueController.Instance?.ShowChoices(
            localizer.Get(titleKey),
            localizer.Get(textKey) + "\n\n" + localizer.Get("raw.ending.placeholder"),
            new List<DialogueChoice>
            {
                new DialogueChoice(localizer.Get("ending.restart"), RestartGame)
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

    private static string GateSpeaker(LocalizationManager localizer)
    {
        return localizer.Get("speaker.gate", "ВРАТА");
    }
}
