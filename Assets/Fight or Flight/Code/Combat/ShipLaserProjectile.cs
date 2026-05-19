using UnityEngine;

public class ShipLaserProjectile : MonoBehaviour
{
    public float speed = 1500f;
    public float lifeTime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't collide with the ship that fired it
        if (other.GetComponentInParent<Ship>() != null) return;

        // Check if we hit an asteroid
        Asteroid asteroid = other.GetComponentInParent<Asteroid>();
        if (asteroid != null)
        {
            asteroid.SelfDestruct();
        }

        // Destroy on impact
        Destroy(gameObject);
    }
}
