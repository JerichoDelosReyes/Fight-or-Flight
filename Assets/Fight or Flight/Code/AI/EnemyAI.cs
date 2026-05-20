using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private Transform player;
    private ShipPhysics physics;
    
    [Header("Combat")]
    public GameObject laserPrefab;
    public float fireRate = 0.8f;
    public float attackRange = 8000f; // Significantly increased range
    public float stopDistance = 2000f; // Larger stop distance to avoid collision
    public float laserSpawnOffset = 2000f; // Spawn laser well in front of huge model
    
    [Header("Behavior")]
    public float wanderRadius = 10000f;
    public float turnSpeedFactor = 1.0f; // Faster turns
    public float throttleFactor = 0.5f; // Faster movement
    public float detectionRange = 15000f;
    public float separationDistance = 1500f; // Larger separation for larger ships
    
    [Header("Obstacle Avoidance")]
    public float avoidanceOffset = 400f; // Wider offset for huge ships
    public float avoidanceRange = 4000f; // Longer avoidance range
    public float avoidanceStrength = 20f;

    [Header("Visuals")]
    public TrailRenderer trail;
    public Light glow;

    private Vector3 targetPosition;
    private float nextFireTime;

    private static System.Collections.Generic.List<EnemyAI> allEnemies = new System.Collections.Generic.List<EnemyAI>();

    private void Awake()
    {
        physics = GetComponent<ShipPhysics>();
    }

    private void OnEnable()
    {
        GameEventManager.OnPlayerDestroyed += OnPlayerDestroyed;
        allEnemies.Add(this);
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerDestroyed -= OnPlayerDestroyed;
        allEnemies.Remove(this);
    }

    private void OnPlayerDestroyed()
    {
        if (Camera.main != null)
        {
            player = Camera.main.transform;
            targetPosition = player.position;
        }
    }

    private void Start()
    {
        if (Ship.PlayerShip != null)
            player = Ship.PlayerShip.transform;

        SetupVisuals();
    }

    private void SetupVisuals()
    {
        if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
        if (glow == null) glow = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        if (player == null)
        {
            if (Ship.PlayerShip != null) player = Ship.PlayerShip.transform;
            else return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Pathfinding & Movement
        PathfindingAndNavigate();

        // Shooting
        if (distanceToPlayer < attackRange)
        {
            if (TargetInfront() && HaveRaycastLineOfSight())
            {
                if (Time.time >= nextFireTime)
                {
                    FireLasers();
                    nextFireTime = Time.time + fireRate;
                }
            }
        }
    }

    private void PathfindingAndNavigate()
    {
        Vector3 directionToTarget = (player.position - transform.position).normalized;
        
        // Separation (Formation-like behavior)
        Vector3 separationVector = Vector3.zero;
        foreach (var enemy in allEnemies)
        {
            if (enemy == this || enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < separationDistance)
            {
                separationVector += (transform.position - enemy.transform.position).normalized * (separationDistance - dist);
            }
        }

        // Combine direction to player and separation
        Vector3 finalDirection = (directionToTarget + separationVector * 0.5f).normalized;

        // Raycast Obstacle Avoidance (Improved with more rays)
        RaycastHit hit;
        Vector3 avoidanceOffsetVector = Vector3.zero;

        // Front rays in a cross pattern
        Vector3[] rayOffsets = {
            transform.right * avoidanceOffset,
            -transform.right * avoidanceOffset,
            transform.up * avoidanceOffset,
            -transform.up * avoidanceOffset,
            (transform.right + transform.up).normalized * avoidanceOffset,
            (-transform.right + transform.up).normalized * avoidanceOffset,
            (transform.right - transform.up).normalized * avoidanceOffset,
            (-transform.right - transform.up).normalized * avoidanceOffset
        };

        foreach (var offset in rayOffsets)
        {
            // Start rays forward to avoid own ship's collider
            if (Physics.Raycast(transform.position + offset + transform.forward * laserSpawnOffset, transform.forward, out hit, avoidanceRange))
            {
                avoidanceOffsetVector -= offset.normalized;
            }
        }

        // Additional center ray for direct obstacles
        if (Physics.Raycast(transform.position + transform.forward * laserSpawnOffset, transform.forward, out hit, avoidanceRange * 1.5f))
        {
            avoidanceOffsetVector += transform.up; // Default to up-steer if directly in front
        }

        if (avoidanceOffsetVector != Vector3.zero)
        {
            // Steer away from obstacles
            transform.Rotate(avoidanceOffsetVector * avoidanceStrength * Time.deltaTime * 5f);
        }
        else
        {
            // Smoothly rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeedFactor * Time.deltaTime * 2f);
        }

        // Apply constant forward movement
        float currentThrottle = (Vector3.Distance(transform.position, player.position) > stopDistance) ? throttleFactor : 0f;
        physics.SetPhysicsInput(new Vector3(0, 0, currentThrottle), Vector3.zero);
    }

    private bool TargetInfront()
    {
        if (player == null) return false;
        Vector3 directionToTarget = player.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle < 60f; // Wider cone for better engagement
    }

    private bool HaveRaycastLineOfSight()
    {
        if (player == null) return false;
        Vector3 startPoint = transform.position + transform.forward * laserSpawnOffset;
        Vector3 directionToTarget = player.position - startPoint;
        if (Physics.Raycast(startPoint, directionToTarget, out RaycastHit hit, attackRange))
        {
            return hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player") || hit.transform.GetComponentInParent<Ship>()?.isPlayer == true;
        }
        // If it hit nothing, it might be the player is too far for the raycast but we still want to shoot?
        // Actually, raycast is good for line of sight.
        return false;
    }

    private void FireLasers()
    {
        if (laserPrefab == null) return;

        GameObject laser = Instantiate(laserPrefab, transform.position + transform.forward * laserSpawnOffset, transform.rotation);
        var script = laser.GetComponent<ShipLaserProjectile>();
        if (script != null)
        {
            script.targetTag = "Player";
            if (physics != null && physics.Rigidbody != null)
            {
                script.Initialize(physics.Rigidbody.linearVelocity);
            }
        }
    }
    }
