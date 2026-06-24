using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CombatantHealth))]
public class PlayerHealthController : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invulnerabilitySeconds = 1.1f;
    [SerializeField] private float respawnDelay = 0.75f;
    [SerializeField] private Transform checkpoint;

    private CombatantHealth health;
    private PlayerController3D movement;
    private PlayerAttackController attack;
    private float invulnerableUntil;
    private bool respawning;
    private static readonly List<IPlayerDeathHandler> deathHandlers = new List<IPlayerDeathHandler>();

    public int CurrentHealth => Health.CurrentHealth;
    public int MaxHealth => Health.MaxHealth;
    public bool IsDead => Health.IsDead;
    public bool IsInvulnerable => Time.time < invulnerableUntil || respawning;
    public CombatantHealth Health => health != null ? health : health = GetComponent<CombatantHealth>();

    public event Action<PlayerHealthController> HealthChanged;
    public event Action<PlayerHealthController> PlayerDied;
    public event Action<PlayerHealthController> PlayerRespawned;

    public static void RegisterDeathHandler(IPlayerDeathHandler handler)
    {
        if (handler == null || deathHandlers.Contains(handler)) return;
        deathHandlers.Add(handler);
    }

    public static void UnregisterDeathHandler(IPlayerDeathHandler handler)
    {
        if (handler == null) return;
        deathHandlers.Remove(handler);
    }

    private void Awake()
    {
        health = GetComponent<CombatantHealth>();
        movement = GetComponent<PlayerController3D>();
        attack = GetComponent<PlayerAttackController>();
        health.Configure(maxHealth);
    }

    private void OnEnable()
    {
        Health.HealthChanged += HandleHealthChanged;
        Health.Died += HandleDied;
    }

    private void OnDisable()
    {
        Health.HealthChanged -= HandleHealthChanged;
        Health.Died -= HandleDied;
    }

    public void ConfigureCheckpoint(Transform newCheckpoint)
    {
        checkpoint = newCheckpoint;
    }

    public bool ApplyDamage(int amount, GameObject source = null)
    {
        if (amount <= 0 || IsInvulnerable || Health.IsDead) return false;

        bool damaged = Health.ApplyDamage(amount, source);
        if (damaged)
        {
            invulnerableUntil = Time.time + invulnerabilitySeconds;
        }

        return damaged;
    }

    public void ResetToFullHealth()
    {
        respawning = false;
        Health.ResetHealth();
        invulnerableUntil = Time.time + invulnerabilitySeconds;
        SetControlState(true);
    }

    public void GrantTemporaryInvulnerability(float seconds)
    {
        if (seconds <= 0f) return;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    public void SetControlEnabled(bool state)
    {
        SetControlState(state);
    }

    public void RespawnAtCheckpoint()
    {
        if (respawning) return;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        respawning = true;
        SetControlState(false);
        PlayerDied?.Invoke(this);

        yield return new WaitForSeconds(respawnDelay);

        if (checkpoint != null && movement != null)
        {
            movement.Teleport(checkpoint.position, checkpoint.rotation);
        }

        Health.ResetHealth();
        invulnerableUntil = Time.time + invulnerabilitySeconds;
        SetControlState(true);
        respawning = false;
        PlayerRespawned?.Invoke(this);
    }

    private void HandleHealthChanged(CombatantHealth changed)
    {
        HealthChanged?.Invoke(this);
    }

    private void HandleDied(CombatantHealth changed, GameObject source)
    {
        if (respawning) return;
        if (TryHandleSceneDeath()) return;

        RespawnAtCheckpoint();
    }

    private bool TryHandleSceneDeath()
    {
        for (int i = deathHandlers.Count - 1; i >= 0; i--)
        {
            IPlayerDeathHandler handler = deathHandlers[i];
            if (handler == null || handler is UnityEngine.Object unityObject && unityObject == null)
            {
                deathHandlers.RemoveAt(i);
                continue;
            }

            if (handler.HandlePlayerDeath(this))
            {
                return true;
            }
        }

        return false;
    }

    private void SetControlState(bool state)
    {
        if (movement != null) movement.SetCanMove(state);
        if (attack != null) attack.SetCanAttack(state);
    }
}

public interface IPlayerDeathHandler
{
    bool HandlePlayerDeath(PlayerHealthController health);
}
