using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Laser Projectile Settings")]
    public GameObject laserPrefab;
    public Transform[] firePoints;
    public float fireRate = 0.8f; // Changed to 0.8s as requested
    public float bulletSpeed = 2500f; // Reduced speed

    private Transform _target;
    private float _nextFireTime;
    private Rigidbody _targetRb;

    private void Update()
    {
        if (!TargetPlayer()) return;

        // Wider firing arc (40 degrees) to compensate for movement
        if (Time.time >= _nextFireTime && TargetInfront() && HaveLineOfSight())
        {
            FireLaser();
            _nextFireTime = Time.time + fireRate;
        }
    }

    private bool TargetInfront()
    {
        if (_target == null) return false;
        
        Vector3 directionToTarget = (_target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        
        // Widened arc to 90 degrees (45 each side) for more aggressive initial fire
        return angle < 45f;
    }

    private bool HaveLineOfSight()
    {
        if (_target == null) return false;
        
        Vector3 directionToTarget = (_target.position - transform.position).normalized;
        Vector3 rayStart = transform.position + transform.forward * 15f; 
        
        // Use a layer mask or just be more lenient. For now, let's keep it but ensure it's working.
        if (Physics.Raycast(rayStart, directionToTarget, out RaycastHit hit, 15000f))
        {
            // If we hit something, check if it's NOT an obstacle (Asteroid/Rock/Boundary)
            bool hitObstacle = hit.transform.CompareTag("Untagged") && 
                (hit.transform.name.Contains("Asteroid") || hit.transform.name.Contains("Rock") || hit.transform.name.Contains("Boundary"));
            
            if (!hitObstacle)
            {
                return true;
            }
        }
        else
        {
            // Clear space, safe to fire
            return true;
        }
        return false;
    }

    private void FireLaser()
    {
        if (laserPrefab == null) return;

        // Lead Targeting Logic
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
            
            // Calculate direction from the specific fire point to the target
            Vector3 direction = (targetPos - point.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            
            GameObject laser = Instantiate(laserPrefab, point.position, rotation);
            ShipLaserProjectile script = laser.GetComponent<ShipLaserProjectile>();
            if (script != null)
            {
                script.targetTag = "Player";
                script.Initialize(direction);
                script.speed = bulletSpeed;
            }
        }
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
                _targetRb = _target.GetComponent<Rigidbody>();
            }
            else if (Ship.PlayerShip != null)
            {
                _target = Ship.PlayerShip.transform;
                _targetRb = _target.GetComponent<Rigidbody>();
            }
        }
        return (_target != null);
    }
}
