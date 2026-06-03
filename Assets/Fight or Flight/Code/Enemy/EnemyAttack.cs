using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Laser Projectile Settings")]
    public GameObject laserPrefab;
    public Transform[] firePoints;
    public float fireRate    = 0.8f;
    public float bulletSpeed = 3200f;
    public float laserDamage = 8f;
    public float fireArc     = 45f;

    private Transform _target;
    private float _nextFireTime;
    private Rigidbody _targetRb;

    private void Update()
    {
        if (!TargetPlayer()) return;

        if (Time.time >= _nextFireTime && TargetInfront() && HaveLineOfSight())
        {
            FireLaser();
            _nextFireTime = Time.time + fireRate;
        }
    }

    private bool TargetInfront()
    {
        if (_target == null) return false;

        Vector3 toTarget = _target.position - transform.position;
        if (toTarget.sqrMagnitude < 0.001f) return true;

        float dotThreshold = Mathf.Cos(fireArc * Mathf.Deg2Rad);
        float dot = Vector3.Dot(transform.forward, toTarget.normalized);

        return dot > dotThreshold;
    }

    private bool HaveLineOfSight()
    {
        if (_target == null) return false;

        Vector3 directionToTarget = (_target.position - transform.position).normalized;
        Vector3 rayStart = transform.position + transform.forward * 15f;

        if (Physics.Raycast(rayStart, directionToTarget, out RaycastHit hit, 15000f))
        {
            bool hitObstacle = hit.transform.CompareTag("Untagged") &&
                (hit.transform.name.Contains("Asteroid") || hit.transform.name.Contains("Rock") || hit.transform.name.Contains("Boundary"));

            if (!hitObstacle)
            {
                return true;
            }
        }
        else
        {
            return true;
        }
        return false;
    }

    private void FireLaser()
    {
        if (laserPrefab == null) return;

        Vector3 targetPos = _target.position;
        if (_targetRb != null && _targetRb.linearVelocity.magnitude > 0.5f)
        {
            float dist = Vector3.Distance(transform.position, _target.position);
            float travelTime = dist / bulletSpeed;
            targetPos = _target.position + (_targetRb.linearVelocity * travelTime);
        }

        foreach (var point in firePoints)
        {
            if (point == null) continue;

            Vector3 direction = (targetPos - point.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);

            GameObject laser = Instantiate(laserPrefab, point.position, rotation);
            ShipLaserProjectile script = laser.GetComponent<ShipLaserProjectile>();
            if (script != null)
            {
                script.targetTag = "Player";
                script.Initialize(direction);
                script.speed  = bulletSpeed;
                script.damage = laserDamage;
            }
        }
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            if (Ship.PlayerShip != null)
            {
                _target = Ship.PlayerShip.transform;
                _targetRb = _target.GetComponent<Rigidbody>();
            }
            else
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                    _targetRb = player.GetComponent<Rigidbody>();
                }
            }
        }
        return (_target != null);
    }
}
