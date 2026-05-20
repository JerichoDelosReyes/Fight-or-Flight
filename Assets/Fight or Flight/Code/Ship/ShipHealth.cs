using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public bool isPlayer = false;

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
        if (isPlayer && currentHealth < maxHealth)
        {
            if (Time.time >= lastDamageTime + regenDelay)
            {
                currentHealth += regenRate * Time.deltaTime;
                if (currentHealth > maxHealth) currentHealth = maxHealth;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        lastDamageTime = Time.time;
        
        if (isPlayer)
{
            // Screen shake on hit
            ScreenShake.Trigger(0.2f, 2f);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public float collisionDamage = 20f;
    public GameObject poofPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        if (isPlayer)
        {
            // Check if we hit an asteroid or rock
            bool hitObstacle = collision.gameObject.GetComponentInParent<Asteroid>() != null || 
                              collision.gameObject.name.ToLower().Contains("rock") ||
                              collision.gameObject.name.ToLower().Contains("asteroid");

            if (hitObstacle)
            {
                ScreenShake.Trigger(0.5f, 5f); // Larger shake for collisions
                TakeDamage(collisionDamage);
                
                if (poofPrefab != null)
                {
                    Instantiate(poofPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
                }
                
                Debug.Log("Player ship hit an obstacle! Damage taken: " + collisionDamage);
            }
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
        }
        else
        {
            // Enemy destroyed
            EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner != null) spawner.OnEnemyDestroyed();
            
            // Update Score - use both systems for compatibility
            ScoreManager.AddScore(100);
            GameEventManager.IncrementScore(100);
            
            Debug.Log("Enemy Destroyed!");
            Destroy(gameObject);
        }
    }
}
