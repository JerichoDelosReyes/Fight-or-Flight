using UnityEngine;

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

    private bool _hasDied;

    private void Awake()
    {
        currentHealth = maxHealth;

        // Sync isPlayer from the Ship component if it's set there. The prefab
        // sometimes has Ship.isPlayer=true but ShipHealth.isPlayer=false, which
        // sends the player through the enemy-death branch and skips the
        // defeat screen.
        var ship = GetComponent<Ship>();
        if (ship != null && ship.isPlayer) isPlayer = true;
    }

    private void Update()
    {
        if (!isPlayer) return;

        // Safety net: if health hit 0 by any path that bypassed TakeDamage,
        // make sure Die() still fires so the defeat screen appears.
        if (currentHealth <= 0f && !_hasDied)
        {
            currentHealth = 0f;
            Die();
            return;
        }

        // Health regen
        if (currentHealth < maxHealth && Time.time >= lastDamageTime + regenDelay)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + regenRate * Time.deltaTime);
        }

        // Shield regen
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
        // Shield absorbs damage first
        if (isPlayer && currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, amount);
            currentShield -= absorbed;
            amount        -= absorbed;
            _shieldRegenTimer = shieldRegenDelay;
        }

        if (amount <= 0f) return; // fully absorbed by shield

        currentHealth -= amount;
        lastDamageTime = Time.time;

        if (isPlayer)
        {
            ScreenShake.Trigger(0.35f, 5f); // Increased from 0.2f, 2f
            ScreenFlash.Trigger(new Color(1f, 0.1f, 0.1f, 0.3f), 0.15f);
            ScreenVignette.Trigger(0.6f, 0.7f); // New vignette effect
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public float collisionDamage = 5f; // Reduced from 15f
    public GameObject poofPrefab;

    private float _lastCollisionDamageTime;
    private const float CollisionCooldown = 1.0f; // Seconds between collision damage

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

        // Enemies don't die on rocks or other enemies anymore.
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

        // Play explosion — scaled up for drama.
        if (explosionPrefab != null)
        {
            var ex = Instantiate(explosionPrefab, transform.position, transform.rotation);
            ex.transform.localScale *= isPlayer ? 2.4f : 1.8f;
        }

        // Play poof on death (especially for enemies)
        if (poofPrefab != null)
        {
            var p = Instantiate(poofPrefab, transform.position, transform.rotation);
            p.transform.localScale *= 1.5f;
        }

        // Sound - Use spatialized audio source if available, otherwise fallback
        if (explosionSound != null)
        {
            // Instead of PlayClipAtPoint which leaks, we try to use a persistent audio source
            // But since this object is being destroyed, we use a static method or similar
            // For now, let's use a simpler approach: play it at camera position or similar
            // Actually, I'll use a simple pooler for audio if possible, but let's just use 
            // AudioSource.PlayClipAtPoint sparingly or ensure it's not called 100 times.
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
        }

        if (isPlayer)
        {
            ScreenFlash.Clear(); // remove any lingering flash before the defeat screen freezes time
            Debug.Log("Player Ship Destroyed!");
            GameEventManager.PlayerDestroyed();
            DefeatScreen.Show(ScoreManager.Score, ScoreManager.Kills);

            // Disable the ship so it can't keep firing / shaking the camera while
            // the defeat screen is up, but leave the GameObject around in case other
            // systems (HUD, etc.) still reference it.
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            var input = GetComponent<ShipInput>(); if (input != null) input.enabled = false;
var combat = GetComponent<ShipCombat>(); if (combat != null) combat.enabled = false;
        }
        else
        {
            // Enemy destroyed
            EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner != null) spawner.OnEnemyDestroyed();
            GameEventManager.EnemyDestroyed();

            // AddKillScore increments both Score and the Kills counter.
            // Pickup.cs uses the legacy GameEventManager.IncrementScore path which calls
            // ScoreManager.AddScore (no kill counter) — keeping them distinct.
            ScoreManager.AddKillScore(100);
            GameEventManager.IncrementScore(100);

            // Brief white flash for game-feel impact.
            ScreenFlash.Trigger(Color.white, 0.08f);
            ScreenShake.Trigger(0.12f, 1.2f);

            Debug.Log("Enemy Destroyed!");
            Destroy(gameObject);
        }
    }
}
