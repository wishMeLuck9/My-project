using System;

[Serializable]
public class WorldStateSnapshot
{
    public int recognition;
    public int pursuitLevel;
    public int apathyTimer;
    public int lightLevel;
    public int cycleCount;
    public int nonStepBias;
    public bool hasHeart;
    public bool hasShadow;
    public bool helpedShadow;
    public bool ignoredShadow;
    public bool resistedSystem;
    public bool paidEntryFragment;
    public bool shadowsFearedPlayer;
    public bool purgatoryMarked;
    public bool paidMemory;
    public bool paidName;
    public bool paidJoy;
    public int aggressionChoice;
    public int mercyChoice;
    public int nightDebt;
    public int foundTraceCount;
    public int shadowViolence;
    public int enemyShadowsDefeated;
    public int playerDeaths;
    public bool hasExteriorFragment;
    public bool hasInnerNightFragment;
    public int exteriorCaptureCount;
    public bool nightViolenceAttempted;
    public WorldState.NightFragmentRoute nightFragmentRoute;
    public WorldState.EndingOutcome endingOutcome;

    public static WorldStateSnapshot Capture(WorldState state)
    {
        if (state == null) return new WorldStateSnapshot();

        return new WorldStateSnapshot
        {
            recognition = state.recognition,
            pursuitLevel = state.pursuitLevel,
            apathyTimer = state.apathyTimer,
            lightLevel = state.lightLevel,
            cycleCount = state.cycleCount,
            nonStepBias = state.nonStepBias,
            hasHeart = state.hasHeart,
            hasShadow = state.hasShadow,
            helpedShadow = state.helpedShadow,
            ignoredShadow = state.ignoredShadow,
            resistedSystem = state.resistedSystem,
            paidEntryFragment = state.paidEntryFragment,
            shadowsFearedPlayer = state.shadowsFearedPlayer,
            purgatoryMarked = state.purgatoryMarked,
            paidMemory = state.paidMemory,
            paidName = state.paidName,
            paidJoy = state.paidJoy,
            aggressionChoice = state.aggressionChoice,
            mercyChoice = state.mercyChoice,
            nightDebt = state.nightDebt,
            foundTraceCount = state.foundTraceCount,
            shadowViolence = state.shadowViolence,
            enemyShadowsDefeated = state.enemyShadowsDefeated,
            playerDeaths = state.playerDeaths,
            hasExteriorFragment = state.hasExteriorFragment,
            hasInnerNightFragment = state.hasInnerNightFragment,
            exteriorCaptureCount = state.exteriorCaptureCount,
            nightViolenceAttempted = state.nightViolenceAttempted,
            nightFragmentRoute = state.nightFragmentRoute,
            endingOutcome = state.endingOutcome
        };
    }

    public void Apply(WorldState state)
    {
        if (state == null) return;

        state.recognition = recognition;
        state.pursuitLevel = pursuitLevel;
        state.apathyTimer = apathyTimer;
        state.lightLevel = lightLevel;
        state.cycleCount = cycleCount;
        state.nonStepBias = nonStepBias;
        state.hasHeart = hasHeart;
        state.hasShadow = hasShadow;
        state.helpedShadow = helpedShadow;
        state.ignoredShadow = ignoredShadow;
        state.resistedSystem = resistedSystem;
        state.paidEntryFragment = paidEntryFragment;
        state.shadowsFearedPlayer = shadowsFearedPlayer;
        state.purgatoryMarked = purgatoryMarked;
        state.paidMemory = paidMemory;
        state.paidName = paidName;
        state.paidJoy = paidJoy;
        state.aggressionChoice = aggressionChoice;
        state.mercyChoice = mercyChoice;
        state.nightDebt = nightDebt;
        state.foundTraceCount = foundTraceCount;
        state.shadowViolence = shadowViolence;
        state.enemyShadowsDefeated = enemyShadowsDefeated;
        state.playerDeaths = playerDeaths;
        state.hasExteriorFragment = hasExteriorFragment;
        state.hasInnerNightFragment = hasInnerNightFragment;
        state.exteriorCaptureCount = exteriorCaptureCount;
        state.nightViolenceAttempted = nightViolenceAttempted;
        state.nightFragmentRoute = nightFragmentRoute;
        state.endingOutcome = endingOutcome;
    }
}
