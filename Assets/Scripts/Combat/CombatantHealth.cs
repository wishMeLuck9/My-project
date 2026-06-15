using System;
using UnityEngine;

public class CombatantHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool resetOnEnable = true;

    private int currentHealth;
    private bool initialized;
    private bool invulnerable;

    public int MaxHealth => Mathf.Max(1, maxHealth);
    public int CurrentHealth => currentHealth;
    public bool IsDead => initialized && currentHealth <= 0;
    public float NormalizedHealth => MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / MaxHealth);

    public event Action<CombatantHealth, GameObject> Damaged;
    public event Action<CombatantHealth, GameObject> Died;
    public event Action<CombatantHealth> HealthChanged;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        if (resetOnEnable) ResetHealth();
        else EnsureInitialized();
    }

    public void Configure(int newMaxHealth, bool shouldReset = true)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        if (shouldReset) ResetHealth();
        else EnsureInitialized();
    }

    public void SetInvulnerable(bool state)
    {
        invulnerable = state;
    }

    public bool ApplyDamage(int amount, GameObject source = null)
    {
        EnsureInitialized();
        if (invulnerable || IsDead || amount <= 0) return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        HealthChanged?.Invoke(this);
        Damaged?.Invoke(this, source);

        if (currentHealth <= 0)
        {
            Died?.Invoke(this, source);
        }

        return true;
    }

    public void Heal(int amount)
    {
        EnsureInitialized();
        if (amount <= 0 || IsDead) return;

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        HealthChanged?.Invoke(this);
    }

    public void ResetHealth()
    {
        currentHealth = MaxHealth;
        initialized = true;
        invulnerable = false;
        HealthChanged?.Invoke(this);
    }

    private void EnsureInitialized()
    {
        if (initialized) return;

        currentHealth = MaxHealth;
        initialized = true;
    }
}
