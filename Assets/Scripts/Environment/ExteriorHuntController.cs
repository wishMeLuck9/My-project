using System.Collections;
using UnityEngine;

public class ExteriorHuntController : MonoBehaviour, IPlayerDeathHandler
{
    [SerializeField] private PlayerController3D player;
    [SerializeField] private PlayerAttackController playerAttack;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private LightFragmentPickup exteriorFragment;
    [SerializeField] private ExteriorPursuer[] pursuers;
    [SerializeField] private float caughtSlowMultiplier = 0.7f;
    [SerializeField] private float caughtSlowDuration = 3f;

    private bool hunting;
    private bool captureLocked;
    private bool gateSequencePaused;
    private Coroutine beginAfterDialogueRoutine;

    public bool IsHunting => hunting;
    public Transform RespawnPoint => respawnPoint;

    private void OnEnable()
    {
        PlayerHealthController.RegisterDeathHandler(this);
    }

    private void OnDisable()
    {
        PlayerHealthController.UnregisterDeathHandler(this);
    }

    private void Start()
    {
        ResolveReferences();
        if (playerAttack != null) playerAttack.SetSceneAttackEnabled(false);

        if (WorldState.Instance != null && WorldState.Instance.hasExteriorFragment)
        {
            BeginHunt();
        }
    }

    public void BeginHunt()
    {
        if (hunting) return;
        if (gateSequencePaused) return;

        if (DialogueController.Instance == null || !DialogueController.Instance.IsDialogueOpen)
        {
            SetHunting(true);
            return;
        }

        if (beginAfterDialogueRoutine == null)
        {
            beginAfterDialogueRoutine = StartCoroutine(BeginAfterDialogue());
        }
    }

    public void PauseForGateSequence(bool paused)
    {
        if (gateSequencePaused == paused) return;

        gateSequencePaused = paused;
        ResolveReferences();
        if (paused)
        {
            if (beginAfterDialogueRoutine != null)
            {
                StopCoroutine(beginAfterDialogueRoutine);
                beginAfterDialogueRoutine = null;
            }

            captureLocked = true;
            foreach (ExteriorPursuer pursuer in pursuers)
            {
                pursuer?.PauseForGateSequence(true);
            }
            return;
        }

        captureLocked = false;
        foreach (ExteriorPursuer pursuer in pursuers)
        {
            pursuer?.PauseForGateSequence(false);
            if (hunting) pursuer?.SetHunting(true, this);
        }
    }

    public void CapturePlayer()
    {
        if (!hunting || gateSequencePaused || captureLocked || WorldState.Instance == null || player == null) return;

        captureLocked = true;
        WorldState.Instance.RegisterExteriorCapture();

        if (WorldState.Instance.exteriorCaptureCount >= 5)
        {
            StartCoroutine(HandlePurgatoryFailure());
            return;
        }

        RespawnPlayer();
        DialogueController.Instance?.ShowDialogue("SHADOW", "Тебя вернули в начало. Следующий побег будет тяжелее.");
        StartCoroutine(UnlockCapture());
    }

    public bool HandlePlayerDeath(PlayerHealthController health)
    {
        if (WorldState.Instance == null || player == null || gateSequencePaused) return false;
        if (!WorldState.Instance.hasExteriorFragment && !hunting) return false;

        captureLocked = true;
        WorldState.Instance.ResetExteriorAttempt();
        exteriorFragment?.RestoreForRetry();
        SetHunting(false);
        RespawnPlayer();
        health.ResetToFullHealth();
        RuntimeHudController.Instance?.ShowSystemMessage(
            LocalizationManager.EnsureInstance().Get(
                "hud.exterior.stability_lost",
                "The fragment falls out of you. The first square returns you to the start."),
            5f);
        StartCoroutine(UnlockCapture());
        return true;
    }

    private IEnumerator HandlePurgatoryFailure()
    {
        SetHunting(false);
        player.SetCanMove(false);
        WorldState.Instance.MarkPurgatoryDeath();
        RuntimeHudController.Instance?.ShowPurgatoryTransition(
            "Тени добрались до тебя. Теперь тебя ждет чистилище.\n\nТебя не хоронят. Тебя форматируют.");

        yield return new WaitForSecondsRealtime(2.5f);

        WorldState.Instance.ResetExteriorAttempt();
        exteriorFragment?.RestoreForRetry();
        RespawnPlayer();
        player.SetCanMove(true);
        RuntimeHudController.Instance?.HidePurgatoryTransition();
        captureLocked = false;
    }

    private void RespawnPlayer()
    {
        Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;
        Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : Quaternion.identity;
        player.Teleport(position, rotation);
        player.ApplyTimedSpeedMultiplier(caughtSlowMultiplier, caughtSlowDuration);
    }

    private IEnumerator BeginAfterDialogue()
    {
        yield return null;
        while (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        beginAfterDialogueRoutine = null;
        if (!gateSequencePaused) SetHunting(true);
    }

    private IEnumerator UnlockCapture()
    {
        yield return new WaitForSeconds(0.75f);
        captureLocked = false;
    }

    private void SetHunting(bool state)
    {
        hunting = state;
        ResolveReferences();
        foreach (ExteriorPursuer pursuer in pursuers)
        {
            if (pursuer != null) pursuer.SetHunting(state, this);
        }
    }

    private void ResolveReferences()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController3D>();
        if (playerAttack == null && player != null) playerAttack = player.GetComponent<PlayerAttackController>();
        if (exteriorFragment == null) exteriorFragment = FindFirstObjectByType<LightFragmentPickup>(FindObjectsInactive.Include);
        if (pursuers == null || pursuers.Length == 0) pursuers = FindObjectsByType<ExteriorPursuer>(FindObjectsSortMode.None);
    }
}
