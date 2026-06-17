using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class FinalBossDirector : MonoBehaviour
{
    [SerializeField] private FinalGateOutcomeController outcome;
    [SerializeField] private GuardianController[] guardians;
    [SerializeField] private PlayerController3D player;
    [SerializeField] private float introLockSeconds = 2.5f;
    [SerializeField] private float phaseTwoHealthRatio = 0.5f;

    private PlayerAttackController playerAttack;
    private bool fightActive;
    private bool phaseTwoStarted;
    private int maxBossHealth;
    private int currentBossHealth;

    public bool IsFightActive => fightActive;
    public bool IsPhaseTwo => phaseTwoStarted;
    public int CurrentBossHealth => currentBossHealth;
    public int MaxBossHealth => maxBossHealth;
    public float NormalizedBossHealth => maxBossHealth <= 0 ? 0f : Mathf.Clamp01((float)currentBossHealth / maxBossHealth);

    public event Action<FinalBossDirector> BossHealthChanged;
    public event Action<FinalBossDirector> PhaseTwoStarted;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SubscribeGuardianHealth();
    }

    private void OnDisable()
    {
        UnsubscribeGuardianHealth();
    }

    public void Configure(FinalGateOutcomeController newOutcome, PlayerController3D newPlayer, params GuardianController[] newGuardians)
    {
        UnsubscribeGuardianHealth();
        outcome = newOutcome;
        player = newPlayer;
        guardians = newGuardians;
        ResolveReferences();
        SubscribeGuardianHealth();
        RecalculateBossHealth();
    }

    public void BeginFight()
    {
        ResolveReferences();
        if (fightActive) return;

        fightActive = true;
        phaseTwoStarted = false;
        RecalculateBossHealth();
        StartCoroutine(IntroRoutine());
    }

    public void EndFight()
    {
        fightActive = false;
        SetPlayerControl(true);
    }

    public void RecalculateBossHealth()
    {
        maxBossHealth = 0;
        currentBossHealth = 0;

        if (guardians != null)
        {
            foreach (GuardianController guardian in guardians)
            {
                if (guardian == null) continue;
                maxBossHealth += guardian.MaxHealth;
                currentBossHealth += Mathf.Max(0, guardian.CurrentHealth);
            }
        }

        BossHealthChanged?.Invoke(this);
        TryStartPhaseTwo();
    }

    private IEnumerator IntroRoutine()
    {
        SetPlayerControl(false);
        RuntimeHudController.Instance?.ShowSystemMessage(LocalizationManager.EnsureInstance().Get("boss.intro"), introLockSeconds);
        yield return new WaitForSeconds(introLockSeconds);
        SetPlayerControl(true);
    }

    private void HandleGuardianHealthChanged(CombatantHealth health)
    {
        RecalculateBossHealth();
    }

    private void TryStartPhaseTwo()
    {
        if (!fightActive || phaseTwoStarted || maxBossHealth <= 0) return;
        if (NormalizedBossHealth > phaseTwoHealthRatio) return;

        phaseTwoStarted = true;
        foreach (GuardianAttackController attack in FindObjectsByType<GuardianAttackController>(FindObjectsSortMode.None))
        {
            attack.SetPhaseTwo(true);
        }

        RuntimeHudController.Instance?.ShowSystemMessage(LocalizationManager.EnsureInstance().Get("boss.phase2"), 3.5f);
        PhaseTwoStarted?.Invoke(this);
    }

    private void ResolveReferences()
    {
        if (outcome == null) outcome = GetComponent<FinalGateOutcomeController>() ?? FindFirstObjectByType<FinalGateOutcomeController>();
        if (player == null) player = FindFirstObjectByType<PlayerController3D>();
        if (playerAttack == null && player != null) playerAttack = player.GetComponent<PlayerAttackController>();
        if (guardians == null || guardians.Length == 0)
        {
            guardians = FindObjectsByType<GuardianController>(FindObjectsSortMode.None);
        }
    }

    private void SubscribeGuardianHealth()
    {
        ResolveReferences();
        if (guardians == null) return;

        foreach (CombatantHealth health in guardians.Where(g => g != null).Select(g => g.Health).Where(h => h != null))
        {
            health.HealthChanged -= HandleGuardianHealthChanged;
            health.HealthChanged += HandleGuardianHealthChanged;
        }
    }

    private void UnsubscribeGuardianHealth()
    {
        if (guardians == null) return;

        foreach (CombatantHealth health in guardians.Where(g => g != null).Select(g => g.Health).Where(h => h != null))
        {
            health.HealthChanged -= HandleGuardianHealthChanged;
        }
    }

    private void SetPlayerControl(bool state)
    {
        if (player != null) player.SetCanMove(state);
        if (playerAttack != null) playerAttack.SetCanAttack(state);
    }
}
