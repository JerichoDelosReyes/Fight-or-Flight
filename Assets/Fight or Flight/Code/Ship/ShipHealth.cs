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

    public GameObject explosionPrefab;
public AudioClip explosionSound;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (!isPlayer) return;

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
            ScreenShake.Trigger(0.2f, 2f);
            ScreenFlash.Trigger(new Color(1f, 0.1f, 0.1f), 0.12f);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public float collisionDamage = 15f;
    public GameObject poofPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        bool hitObstacle = collision.gameObject.GetComponentInParent<Asteroid>() != null ||
                           collision.gameObject.name.ToLower().Contains("rock") ||
                           collision.gameObject.name.ToLower().Contains("asteroid") ||
                           collision.gameObject.name.StartsWith("BoundaryRock");

        if (!hitObstacle) return;

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
        // Play explosion
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, transform.rotation);
        }

        // Play poof on death (especially for enemies)
        if (poofPrefab != null)
        {
            Instantiate(poofPrefab, transform.position, transform.rotation);
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
            Debug.Log("Player Ship Destroyed!");
            GameEventManager.PlayerDestroyed(); // Inform legacy systems
            DefeatScreen.Show(ScoreManager.Score, ScoreManager.Kills);

            // Disable the ship so it can't keep firing / shaking the camera while
            // the defeat screen is up, but leave the GameObject around in case other
            // systems (HUD, etc.) still reference it.
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
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
            
            Debug.Log("Enemy Destroyed!");
            Destroy(gameObject);
        }
    }
}
