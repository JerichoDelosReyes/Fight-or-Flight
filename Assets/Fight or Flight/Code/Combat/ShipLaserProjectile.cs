using UnityEngine;

public class ShipLaserProjectile : MonoBehaviour
{
    public float speed = 200000f; 
    public float lifeTime = 5f;
    public float damage = 8f;
    public string targetTag = "Enemy";

    public AudioClip shotSound;
    private AudioSource audioSource;
    private Vector3 movementDirection = Vector3.zero;

    // Initialize with explicit firing direction (world space). shipVelocity is ignored.
    public void Initialize(Vector3 initialDirection)
    {
        if (initialDirection != Vector3.zero)
            movementDirection = initialDirection.normalized;
        // Align visual rotation to movement direction immediately
        if (movementDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up);
        }
    }

    private void Start()
    {
        // Ensure it's not parented to the ship so it doesn't follow its movement
        transform.SetParent(null, true);

        Destroy(gameObject, lifeTime);

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
            // Clear velocity BEFORE setting isKinematic to avoid console warnings.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void Update()
    {
        // Straight movement in world space using cached direction
        if (movementDirection != Vector3.zero)
        {
            transform.position += (movementDirection * speed) * Time.deltaTime;
        }
        else
        {
            // Fallback if forward was somehow zero
            transform.position += (transform.forward * speed) * Time.deltaTime;
        }
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