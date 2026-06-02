using System.Collections;
using UnityEngine;

public class ExteriorHuntController : MonoBehaviour
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

    public bool IsHunting => hunting;

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

        if (DialogueController.Instance == null || !DialogueController.Instance.IsDialogueOpen)
        {
            SetHunting(true);
            return;
        }

        StartCoroutine(BeginAfterDialogue());
    }

    public void CapturePlayer()
    {
        if (!hunting || captureLocked || WorldState.Instance == null || player == null) return;

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

        SetHunting(true);
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
