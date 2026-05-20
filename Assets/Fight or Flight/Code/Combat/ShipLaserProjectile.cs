using UnityEngine;

public class ShipLaserProjectile : MonoBehaviour
{
    public float speed = 7000f; 
    public float lifeTime = 3f;
    public float damage = 8f;
    public string targetTag = "Enemy";

    public AudioClip shotSound;
    private AudioSource audioSource;
    private Vector3 movementDirection;

    public void Initialize(Vector3 shipVelocity)
    {
        // Ignore velocity inheritance as requested
    }

    private void Start()
    {
        // Cache direction immediately on spawn
        movementDirection = transform.forward;
        Destroy(gameObject, lifeTime);
        
        transform.SetParent(null);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        if (shotSound != null)
        {
            audioSource.PlayOneShot(shotSound, 0.5f);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void Update()
    {
        // Straight movement in world space using cached direction
        transform.position += (movementDirection * speed) * Time.deltaTime;
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