using UnityEngine;

public class WorldState : MonoBehaviour
{
    public enum NightFragmentRoute
    {
        None,
        Mercy,
        Violence
    }

    public enum EndingOutcome
    {
        None,
        Sacrifice,
        Force,
        FragmentDestroyed
    }

    public static WorldState Instance { get; private set; }

    [Header("Core Variables")]
    public int recognition = 0;
    public int pursuitLevel = 0;
    public int apathyTimer = 0;
    public int lightLevel = 0;
    public int cycleCount = 0;
    public int nonStepBias = 0;

    [Header("Boolean States")]
    public bool hasHeart = false;
    public bool hasShadow = false;
    public bool helpedShadow = false;
    public bool ignoredShadow = false;
    public bool resistedSystem = false;
    public bool paidEntryFragment = false;
    public bool shadowsFearedPlayer = false;
    public bool purgatoryMarked = false;

    [Header("Personality Payments")]
    public bool paidMemory = false;
    public bool paidName = false;
    public bool paidJoy = false;

    [Header("Choices")]
    public int aggressionChoice = 0;
    public int mercyChoice = 0;
    public int nightDebt = 0;
    public int foundTraceCount = 0;
    public int shadowViolence = 0;
    public int enemyShadowsDefeated = 0;
    public int playerDeaths = 0;

    [Header("Story Route")]
    public bool introShown = false;
    public bool hasExteriorFragment = false;
    public bool hasInnerNightFragment = false;
    public int exteriorCaptureCount = 0;
    public bool nightViolenceAttempted = false;
    public bool nightGuardianChainStarted = false;
    public int nightGuardianChainDefeatedCount = 0;
    public NightFragmentRoute nightFragmentRoute = NightFragmentRoute.None;
    public EndingOutcome endingOutcome = EndingOutcome.None;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddLight(int amount)
    {
        lightLevel += amount;
        recognition += 5;
    }

    public bool SpendLight(int amount)
    {
        if (amount <= 0) return true;
        if (lightLevel < amount) return false;

        lightLevel -= amount;
        return true;
    }

    public void AddApathy(int amount)
    {
        apathyTimer += amount;
    }

    public void AddPursuit(int amount)
    {
        pursuitLevel += amount;
    }

    public void RecordShadowAttack(bool defeated)
    {
        nightViolenceAttempted = true;
        shadowViolence += 1;
        aggressionChoice += 1;
        pursuitLevel += 8;
        shadowsFearedPlayer = true;

        if (defeated)
        {
            enemyShadowsDefeated += 1;
            nightDebt += 1;
        }
    }

    public bool HasFragment(LightFragmentPickup.FragmentKind fragmentKind)
    {
        return fragmentKind == LightFragmentPickup.FragmentKind.Exterior
            ? hasExteriorFragment
            : hasInnerNightFragment;
    }

    public void AcquireFragment(LightFragmentPickup.FragmentKind fragmentKind)
    {
        if (fragmentKind == LightFragmentPickup.FragmentKind.Exterior)
        {
            hasExteriorFragment = true;
        }
        else
        {
            hasInnerNightFragment = true;
        }

        AddLight(1);
        foundTraceCount += 1;
    }

    public void RegisterExteriorCapture()
    {
        exteriorCaptureCount += 1;
    }

    public void ResetExteriorAttempt()
    {
        hasExteriorFragment = false;
        exteriorCaptureCount = 0;
        lightLevel = Mathf.Max(0, lightLevel - 1);
    }

    public void MarkPurgatoryDeath()
    {
        playerDeaths += 1;
        purgatoryMarked = true;
    }

    public void GrantNightFragmentRoute(NightFragmentRoute route)
    {
        nightFragmentRoute = route;
    }

    public void BeginNightGuardianChain()
    {
        nightGuardianChainStarted = true;
    }

    public void SetNightGuardianChainDefeatedCount(int defeatedCount)
    {
        nightGuardianChainDefeatedCount = Mathf.Max(nightGuardianChainDefeatedCount, defeatedCount);
    }

    public void CompleteSacrificeEnding()
    {
        hasExteriorFragment = false;
        hasInnerNightFragment = false;
        lightLevel = 0;
        paidMemory = true;
        paidName = true;
        paidJoy = true;
        endingOutcome = EndingOutcome.Sacrifice;
    }

    public void CompleteForceEnding()
    {
        endingOutcome = EndingOutcome.Force;
    }

    public void RecordFragmentDestroyedEnding()
    {
        hasExteriorFragment = false;
        hasInnerNightFragment = false;
        lightLevel = 0;
        endingOutcome = EndingOutcome.FragmentDestroyed;
    }

    public void ResetRun()
    {
        recognition = 0;
        pursuitLevel = 0;
        apathyTimer = 0;
        lightLevel = 0;
        cycleCount = 0;
        nonStepBias = 0;
        hasHeart = false;
        hasShadow = false;
        helpedShadow = false;
        ignoredShadow = false;
        resistedSystem = false;
        paidEntryFragment = false;
        shadowsFearedPlayer = false;
        purgatoryMarked = false;
        paidMemory = false;
        paidName = false;
        paidJoy = false;
        aggressionChoice = 0;
        mercyChoice = 0;
        nightDebt = 0;
        foundTraceCount = 0;
        shadowViolence = 0;
        enemyShadowsDefeated = 0;
        playerDeaths = 0;
        introShown = false;
        hasExteriorFragment = false;
        hasInnerNightFragment = false;
        exteriorCaptureCount = 0;
        nightViolenceAttempted = false;
        nightGuardianChainStarted = false;
        nightGuardianChainDefeatedCount = 0;
        nightFragmentRoute = NightFragmentRoute.None;
        endingOutcome = EndingOutcome.None;
    }
}
