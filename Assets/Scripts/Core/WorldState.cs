using UnityEngine;

public class WorldState : MonoBehaviour
{
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
}
