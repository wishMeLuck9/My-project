using System.Collections.Generic;
using UnityEngine;

public class FinalGateOutcomeController : MonoBehaviour
{
    [SerializeField] private GuardianController[] guardians;
    [SerializeField] private Transform arenaRespawnPoint;
    [SerializeField] private Transform leftGateDoor;
    [SerializeField] private Transform rightGateDoor;

    private PlayerController3D player;
    private PlayerAttackController attack;
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
        if (!state.hasExteriorFragment || !state.hasInnerNightFragment)
        {
            DialogueController.Instance?.ShowDialogue("GATE", "Врата не признают неполный след. Вернись с двумя фрагментами.");
            started = false;
            return;
        }

        if (state.enemyShadowsDefeated > 0)
        {
            StartArena();
        }
        else
        {
            OfferSacrifice();
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

        foreach (GuardianController guardian in guardians)
        {
            guardian?.ResetBattleState();
        }

        DialogueController.Instance?.ShowDialogue("GATE", "Страж коснулся тебя. Суд начинается заново.");
    }

    public void NotifyGuardianDefeated()
    {
        if (resolved || guardians == null || guardians.Length == 0) return;

        foreach (GuardianController guardian in guardians)
        {
            if (guardian == null || !guardian.IsDefeated) return;
        }

        WorldState.Instance?.CompleteForceEnding();
        ResolveEnding("ПРОХОД ВЗЯТ СИЛОЙ", "Стражи рассеяны. Врата открыты, но ночь прошла вместе с тобой.");
    }

    private void StartArena()
    {
        if (attack != null) attack.SetSceneAttackEnabled(true);
        foreach (GuardianController guardian in guardians)
        {
            guardian?.BeginBattle(this, player != null ? player.transform : null);
        }

        DialogueController.Instance?.ShowDialogue("GATE", "Ты принес убийство. Защитники не примут цену. Выживи и открой врата силой.");
    }

    private void OfferSacrifice()
    {
        if (DialogueController.Instance == null) return;

        DialogueController.Instance.ShowChoices(
            "GATE",
            "Ты дошел без убийства. Врата требуют оба фрагмента, память и жизнь. Отдать все ради прохода?",
            new List<DialogueChoice>
            {
                new DialogueChoice("Отдать все и войти", () =>
                {
                    WorldState.Instance.CompleteSacrificeEnding();
                    ResolveEnding("ПРОХОД ОПЛАЧЕН", "Фрагменты погасли. Память оставлена у порога. Врата открыты.");
                }),
                new DialogueChoice("Отойти", () => started = false)
            });
    }

    private void ResolveEnding(string title, string text)
    {
        resolved = true;
        if (attack != null) attack.SetSceneAttackEnabled(false);
        OpenGate();

        DialogueController.Instance?.ShowChoices(
            title,
            text + "\n\nФинальный ролик будет подключен к этому исходу.",
            new List<DialogueChoice>
            {
                new DialogueChoice("Начать заново", RestartGame)
            });
    }

    private void OpenGate()
    {
        ResolveDoorReferences();
        if (leftGateDoor != null) leftGateDoor.localPosition += Vector3.left * 2.2f;
        if (rightGateDoor != null) rightGateDoor.localPosition += Vector3.right * 2.2f;

        GameObject entry = GameObject.Find("GATE_FinalEntryTrigger");
        Collider trigger = entry != null ? entry.GetComponent<Collider>() : null;
        if (trigger != null) trigger.enabled = false;
    }

    private void RestartGame()
    {
        WorldState.Instance?.ResetRun();
        GameFlowController.Instance?.TransitionToLocation("LOCATION_01_EXTERIOR_DAY");
    }

    private void ResolveReferences()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController3D>();
        if (attack == null && player != null) attack = player.GetComponent<PlayerAttackController>();
        if (guardians == null || guardians.Length == 0) guardians = FindObjectsByType<GuardianController>(FindObjectsSortMode.None);
        ResolveDoorReferences();
    }

    private void ResolveDoorReferences()
    {
        if (leftGateDoor == null)
        {
            GameObject left = GameObject.Find("Atmos_Door_Left");
            if (left != null) leftGateDoor = left.transform;
        }

        if (rightGateDoor == null)
        {
            GameObject right = GameObject.Find("Atmos_Door_Right");
            if (right != null) rightGateDoor = right.transform;
        }
    }
}
