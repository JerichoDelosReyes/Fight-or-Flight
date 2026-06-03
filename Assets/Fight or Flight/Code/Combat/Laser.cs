using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
[RequireComponent(typeof(LineRenderer))]
public class Laser : MonoBehaviour
{
    [Tooltip("Time in seconds")] [SerializeField] private float _laserDuration = 0.15f;
    [SerializeField] private float _laserDistance = 5000f;
    [SerializeField] private float _fireDelay = 1.5f;

    private LineRenderer _laserBeam;
    private Light _laserLight;
    private bool _canFire = true;

    private void Awake()
    {
        _laserBeam = GetComponent<LineRenderer>();
        _laserLight = GetComponent<Light>();
    }

    private void Start()
    {
        if (_laserBeam != null) _laserBeam.enabled = false;
        if (_laserLight != null) _laserLight.enabled = false;
        _canFire = true;
    }

    private void Update()
    {
        Debug.DrawRay(transform.position, transform.forward * _laserDistance, Color.yellow);
    }

    private Vector3 CastRay()
    {
        Vector3 laserDirection = transform.forward * _laserDistance;
        if (Physics.Raycast(transform.position, laserDirection, out RaycastHit hit, _laserDistance))
        {
            Debug.Log("Raycast Hit: " + hit.transform.name);

            ShipHealth health = hit.transform.GetComponentInParent<ShipHealth>();
            if (health != null)
            {
                health.TakeDamage(10f);
            }

            if (hit.transform.CompareTag("Pickup"))
            {
                var pickup = hit.transform.GetComponent<Pickup>();
                if (pickup != null) pickup.Collect();
            }

            return hit.point;
        }
        else
        {
            return transform.position + (transform.forward * _laserDistance);
        }
    }

    public void FireLaser()
    {
        FireLaser(CastRay(), null);
    }

    public void FireLaser(Vector3 targetPosition, Transform target = null)
    {
        if (_canFire)
        {
            _canFire = false;

            if (_laserBeam != null)
            {
                _laserBeam.SetPosition(0, transform.position);
                _laserBeam.SetPosition(1, targetPosition);
                _laserBeam.enabled = true;
            }

            if (_laserLight != null) _laserLight.enabled = true;

            Invoke("TurnOffLaser", _laserDuration);
            Invoke("ResetFire", _fireDelay);
        }
    }

    private void TurnOffLaser()
    {
        if (_laserBeam != null) _laserBeam.enabled = false;
        if (_laserLight != null) _laserLight.enabled = false;
    }

    private void ResetFire()
    {
        _canFire = true;
    }

    public float Distance => _laserDistance;
}
