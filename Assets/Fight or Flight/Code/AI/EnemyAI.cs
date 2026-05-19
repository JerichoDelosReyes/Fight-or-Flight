using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private Transform player;
    private ShipPhysics physics;
    
    [Header("Combat")]
    public GameObject laserPrefab;
    public float fireRate = 0.8f;
    public float attackRange = 200f; // Lowered range
    public float stopDistance = 150f;
    
    [Header("Behavior")]
    public float wanderRadius = 4000f;
    public float turnSpeedFactor = 0.1f; // Slower turns
    public float throttleFactor = 0.1f; // Slower movement
    public float detectionRange = 1000f;
    
    [Header("Obstacle Avoidance")]
    public float avoidanceOffset = 25f;
    public float avoidanceRange = 100f;
    public float avoidanceStrength = 5f;

    private Vector3 targetPosition;
    private float nextFireTime;
    private bool isPlayerTargeted;

    private void Awake()
    {
        physics = GetComponent<ShipPhysics>();
    }

    private void OnEnable()
    {
        GameEventManager.OnPlayerDestroyed += OnPlayerDestroyed;
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerDestroyed -= OnPlayerDestroyed;
    }

    private void OnPlayerDestroyed()
    {
        if (Camera.main != null)
        {
            player = Camera.main.transform;
            isPlayerTargeted = true;
            targetPosition = player.position;
        }
    }

    private void Start()
    {
        if (Ship.PlayerShip != null)
            player = Ship.PlayerShip.transform;
    }

    private void Update()
    {
        if (player == null)
        {
            if (Ship.PlayerShip != null) player = Ship.PlayerShip.transform;
            else return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Pathfinding & Movement (Matched to Reference Logic)
        PathfindingAndNavigate();

        // Shooting (TargetInfront & LineOfSight)
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
        
        // Raycast Obstacle Avoidance
        RaycastHit hit;
        Vector3 avoidanceOffsetVector = Vector3.zero;

        Vector3 left = transform.position - transform.right * avoidanceOffset;
        Vector3 right = transform.position + transform.right * avoidanceOffset;
        Vector3 up = transform.position + transform.up * avoidanceOffset;
        Vector3 down = transform.position - transform.up * avoidanceOffset;

        if (Physics.Raycast(left, transform.forward, out hit, avoidanceRange)) avoidanceOffsetVector += transform.right;
        else if (Physics.Raycast(right, transform.forward, out hit, avoidanceRange)) avoidanceOffsetVector -= transform.right;

        if (Physics.Raycast(up, transform.forward, out hit, avoidanceRange)) avoidanceOffsetVector -= transform.up;
        else if (Physics.Raycast(down, transform.forward, out hit, avoidanceRange)) avoidanceOffsetVector += transform.up;

        if (avoidanceOffsetVector != Vector3.zero)
        {
            // Steer away from obstacles
            transform.Rotate(avoidanceOffsetVector * avoidanceStrength * Time.deltaTime * 10f);
        }
        else
        {
            // Smoothly rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeedFactor * Time.deltaTime * 5f);
        }

        // Apply constant forward movement (Reference style)
        physics.SetPhysicsInput(new Vector3(0, 0, throttleFactor), Vector3.zero);
}

    private bool TargetInfront()
    {
        if (player == null) return false;
        Vector3 directionToTarget = player.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle < 45f; // Polish: Only shoot if player is within a 45 degree cone
    }

    private bool HaveRaycastLineOfSight()
    {
        if (player == null) return false;
        Vector3 directionToTarget = player.position - transform.position;
        if (Physics.Raycast(transform.position, directionToTarget, out RaycastHit hit, attackRange))
        {
            return hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player") || hit.transform.GetComponentInParent<Ship>()?.isPlayer == true;
        }
        return false;
    }

    private void FireLasers()
    {
        if (laserPrefab == null) return;

        GameObject laser = Instantiate(laserPrefab, transform.position + transform.forward * 40f, transform.rotation);
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
