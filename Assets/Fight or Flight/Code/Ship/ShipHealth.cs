using UnityEngine;
using System;

public class ShipHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public bool isPlayer = false;

    [Header("Shield")]
    public float maxShield    = 50f;
    public float currentShield = 50f;
    public float shieldRegenRate  = 5f;
    public float shieldRegenDelay = 3f;
    private float _shieldRegenTimer;

    [Header("Regeneration")]
    public float regenRate = 2f;
    public float regenDelay = 5f;
    private float lastDamageTime;

    public bool IsRegenerating => currentHealth < maxHealth && Time.time >= lastDamageTime + regenDelay;

    public GameObject explosionPrefab;
public AudioClip explosionSound;

    public event Action OnDamaged;

    private bool _hasDied;

    private void Awake()
    {
        currentHealth = maxHealth;

        var ship = GetComponent<Ship>();
        if (ship != null && ship.isPlayer) isPlayer = true;
    }

    private void Update()
    {
        if (!isPlayer) return;

        if (currentHealth <= 0f && !_hasDied)
        {
            currentHealth = 0f;
            Die();
            return;
        }

        if (currentHealth < maxHealth && Time.time >= lastDamageTime + regenDelay)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + regenRate * Time.deltaTime);
        }

        if (currentShield < maxShield)
        {
            if (_shieldRegenTimer > 0f)
                _shieldRegenTimer -= Time.deltaTime;
            else
                currentShield = Mathf.Min(maxShield, currentShield + shieldRegenRate * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isPlayer && currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, amount);
            currentShield -= absorbed;
            amount        -= absorbed;
            _shieldRegenTimer = shieldRegenDelay;
        }

        if (amount <= 0f) return;

        currentHealth -= amount;
        lastDamageTime = Time.time;
        OnDamaged?.Invoke();

        if (isPlayer)
        {
            ScreenShake.Trigger(0.35f, 5f);
            ScreenFlash.Trigger(new Color(1f, 0.1f, 0.1f, 0.3f), 0.15f);
            ScreenVignette.Trigger(0.6f, 0.7f);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public float collisionDamage = 5f;
    public GameObject poofPrefab;

    private float _lastCollisionDamageTime;
    private const float CollisionCooldown = 1.0f;

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time < _lastCollisionDamageTime + CollisionCooldown) return;

        bool hitObstacle = collision.gameObject.GetComponentInParent<Asteroid>() != null ||
                           collision.gameObject.name.ToLower().Contains("rock") ||
                           collision.gameObject.name.ToLower().Contains("asteroid") ||
                           collision.gameObject.name.StartsWith("BoundaryRock");

        bool hitEnemy = collision.gameObject.CompareTag("Enemy") ||
                        collision.gameObject.GetComponentInParent<EnemyMovement>() != null;

        if (!hitObstacle && !hitEnemy) return;

        if (!isPlayer) return;

        _lastCollisionDamageTime = Time.time;
        TakeDamage(collisionDamage);

        if (isPlayer)
        {
            ScreenShake.Trigger(0.5f, 5f);
            ScreenFlash.Trigger(new Color(1f, 0.4f, 0.1f), 0.2f);

            if (poofPrefab != null && collision.contacts.Length > 0)
                Instantiate(poofPrefab, collision.contacts[0].point,
                            Quaternion.LookRotation(collision.contacts[0].normal));
        }
    }

    private void Die()
    {
        if (_hasDied) return;
        _hasDied = true;

        if (explosionPrefab != null)
        {
            var ex = Instantiate(explosionPrefab, transform.position, transform.rotation);
            ex.transform.localScale *= isPlayer ? 2.4f : 1.8f;
        }

        if (poofPrefab != null)
        {
            var p = Instantiate(poofPrefab, transform.position, transform.rotation);
            p.transform.localScale *= 1.5f;
        }

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
        }

        if (isPlayer)
        {
            ScreenFlash.Clear();
            Debug.Log("Player Ship Destroyed!");
            GameEventManager.PlayerDestroyed();
            DefeatScreen.Show(
                ScoreManager.Score,
                ScoreManager.Kills,
                WaveManager.MatchElapsedTime,
                WaveManager.CurrentWave,
                WaveManager.MaxWave,
                GameModeManager.Selected == GameModeManager.Mode.Survival);

            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            var input = GetComponent<ShipInput>(); if (input != null) input.enabled = false;
var combat = GetComponent<ShipCombat>(); if (combat != null) combat.enabled = false;
        }
        else
        {
            EnemySpawner spawner = UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner != null) spawner.OnEnemyDestroyed();
            GameEventManager.EnemyDestroyed();

            ScoreManager.AddKillScore(100);
            GameEventManager.IncrementScore(100);

            ScreenFlash.Trigger(Color.white, 0.08f);
            ScreenShake.Trigger(0.12f, 1.2f);

            Debug.Log("Enemy Destroyed!");
            Destroy(gameObject);
        }
    }
}
