using UnityEngine;

[DisallowMultipleComponent]
public class Explosion : MonoBehaviour
{
    [SerializeField] private GameObject _explosion;
    [SerializeField] private GameObject _blowUp;
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private float _laserHitForce = 10f;
    [SerializeField] private float _explosionDuration = 6f;

    private void OnCollisionEnter(Collision collision)
    {
        foreach (var contactPoint in collision.contacts)
        {
            SpawnExplosion(contactPoint.point);
        }
    }

    private void SpawnExplosion(Vector3 explosionPosition)
    {
        if (_explosion == null) return;
        GameObject spawnedExplosion = Instantiate(_explosion, explosionPosition, Quaternion.identity, transform);
        Destroy(spawnedExplosion, _explosionDuration);
    }

    public void AddForce(Vector3 hitPosition, Transform hitSource)
    {
        SpawnExplosion(hitPosition);
        if (_rigidBody == null) return;
        Vector3 forceDirection = (hitSource.position - transform.position).normalized;
        _rigidBody.AddForceAtPosition(forceDirection * _laserHitForce, hitPosition, ForceMode.Impulse);
    }

    public void BlowUp()
    {
        if (_blowUp != null)
        {
            var spawnedExplosion = Instantiate(_blowUp, transform.position, Quaternion.identity);
            Destroy(spawnedExplosion, _explosionDuration);
        }

        ShipHealth health = GetComponent<ShipHealth>();
        if (health != null)
        {
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
