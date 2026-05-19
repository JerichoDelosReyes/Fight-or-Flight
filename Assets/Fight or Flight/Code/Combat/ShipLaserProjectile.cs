using UnityEngine;

public class ShipLaserProjectile : MonoBehaviour
{
    public float speed = 3500f; // Increased speed
    public float lifeTime = 3f;
    public float damage = 8f;
    public string targetTag = "Enemy";

    public AudioClip shotSound;
    private AudioSource audioSource;
    private Vector3 initialVelocity = Vector3.zero;

    public void Initialize(Vector3 shipVelocity)
    {
        initialVelocity = shipVelocity;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
        
        // Ensure it's not a child to avoid following the ship
        transform.SetParent(null);

        // Setup Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // 3D sound
        if (shotSound != null)
        {
            audioSource.PlayOneShot(shotSound, 0.5f);
        }

        // Rigidbody setup to ensure no physics interference
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void Update()
    {
        // Linear movement in world space, plus initial ship velocity
        transform.position += (transform.forward * speed + initialVelocity) * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return; 

        ShipHealth health = other.GetComponentInParent<ShipHealth>();
        if (health != null)
        {
            if ((targetTag == "Player" && health.isPlayer) || (targetTag == "Enemy" && !health.isPlayer))
            {
                health.TakeDamage(damage);
                HandleImpact(other.ClosestPoint(transform.position));
                Destroy(gameObject);
                return;
            }
        }

        Asteroid asteroid = other.GetComponentInParent<Asteroid>();
        if (asteroid != null)
        {
            asteroid.SelfDestruct();
            HandleImpact(other.ClosestPoint(transform.position));
            Destroy(gameObject);
            return;
        }
    }

    public GameObject impactParticlePrefab;
    public AudioClip impactSound;

    private void HandleImpact(Vector3 point)
    {
        if (impactParticlePrefab != null)
        {
            GameObject fx = Instantiate(impactParticlePrefab, point, Quaternion.LookRotation(transform.forward * -1f));
            Destroy(fx, 2f);
        }

        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, point, 0.7f);
        }
    }
}
