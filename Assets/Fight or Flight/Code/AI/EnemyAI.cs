using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    private Transform player;
    private ShipPhysics physics;
    
    [Header("Combat")]
    public GameObject laserPrefab;
    public float fireRate = 0.8f;
    public float attackRange = 8000f;
    public float stopDistanceOverride = 600f; // Closer
    public float laserSpawnForwardOffset = 100f;
    
    [Header("Behavior")]
    public float turnSpeedFactor = 1.0f;
    public float throttleFactor = 0.15f; // Reasonable speed
    public float detectionRange = 15000f;
    
    [Header("Obstacle Avoidance")]
    public float avoidanceOffset = 400f;
    public float avoidanceRange = 4000f;
    public float avoidanceStrength = 20f;

    [Header("Visuals")]
    public TrailRenderer trail;
    public Light glow;

    private float nextFireTime;
    public static List<EnemyAI> allEnemies = new List<EnemyAI>();

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
        if (Camera.main != null) player = Camera.main.transform;
    }

    private void Start()
    {
        if (Ship.PlayerShip != null) player = Ship.PlayerShip.transform;
        SetupVisuals();
        
        if (glow != null)
        {
            glow.range = 300f;
            glow.intensity = 5f;
            glow.color = Color.red;
        }
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

        PathfindingAndNavigate();

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < attackRange && TargetInfront())
        {
            if (Time.time >= nextFireTime)
            {
                FireLasers();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    private void PathfindingAndNavigate()
    {
        Vector3 directionToTarget = (player.position - transform.position).normalized;
        
        // Boundary check
        float distanceToCenter = transform.position.magnitude;
        if (distanceToCenter > ScriptsReference.BoundaryLimit * 0.8f)
        {
            Vector3 toCenter = -transform.position.normalized;
            float factor = (distanceToCenter - ScriptsReference.BoundaryLimit * 0.8f) / (ScriptsReference.BoundaryLimit * 0.2f);
            directionToTarget = Vector3.Slerp(directionToTarget, toCenter, factor).normalized;
        }

        // Raycast Obstacle Avoidance
        RaycastHit hit;
        Vector3 avoidanceOffsetVector = Vector3.zero;
        Vector3[] rayOffsets = { transform.right * avoidanceOffset, -transform.right * avoidanceOffset, transform.up * avoidanceOffset, -transform.up * avoidanceOffset };

        foreach (var offset in rayOffsets)
        {
            if (Physics.Raycast(transform.position + offset + transform.forward * 200f, transform.forward, out hit, avoidanceRange))
                avoidanceOffsetVector -= offset.normalized;
        }

        if (avoidanceOffsetVector != Vector3.zero)
        {
            Vector3 avoidanceDir = (directionToTarget + avoidanceOffsetVector * 2f).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(avoidanceDir, Vector3.up), turnSpeedFactor * Time.deltaTime * 5f);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget, Vector3.up), turnSpeedFactor * Time.deltaTime * 2f);
        }

        float currentThrottle = (Vector3.Distance(transform.position, player.position) > stopDistanceOverride) ? throttleFactor : 0f;
        physics.SetPhysicsInput(new Vector3(0, 0, currentThrottle), Vector3.zero);
    }

    private bool TargetInfront()
    {
        if (player == null) return false;
        return Vector3.Angle(transform.forward, player.position - transform.position) < 45f;
    }

    private void FireLasers()
    {
        if (laserPrefab == null) return;
        
        // Spawn 2 lasers forward
        Vector3 leftPos = transform.position + transform.forward * laserSpawnForwardOffset - transform.right * 60f;
        Vector3 rightPos = transform.position + transform.forward * laserSpawnForwardOffset + transform.right * 60f;

        SpawnLaser(leftPos);
        SpawnLaser(rightPos);
    }

    private void SpawnLaser(Vector3 pos)
    {
        GameObject laser = Instantiate(laserPrefab, pos, transform.rotation);
        var script = laser.GetComponent<ShipLaserProjectile>();
        if (script != null)
        {
            script.targetTag = "Player";
            script.Initialize(Vector3.zero);
        }
    }
}