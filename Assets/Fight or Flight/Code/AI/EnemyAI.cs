using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    private Transform player;
    private ShipPhysics physics;

    // ── Combat ─────────────────────────────────────────────────────────────────
    [Header("Combat")]
    public GameObject laserPrefab;
    public float fireRate = 1.2f;
    public float attackRange = 6000f;
    public float laserSpawnForwardOffset = 100f;

    // ── Behavior ───────────────────────────────────────────────────────────────
    [Header("Behavior")]
    public float detectionRange = 12000f;
    [Tooltip("Desired orbit radius around the player when attacking")]
    public float orbitRadius = 1800f;
    [Tooltip("Base turn speed multiplier")]
    public float turnSpeedFactor = 1.1f;
    [Tooltip("Throttle applied while chasing/orbiting")]
    public float throttleFactor = 0.14f;
    [Tooltip("How fast the enemy strafes laterally during attack")]
    public float strafeSpeed = 0.35f;
    [Tooltip("Direction the enemy is currently circling (+1 or -1)")]
    private float circleDir = 1f;

    // ── Patrol ─────────────────────────────────────────────────────────────────
    [Header("Patrol")]
    public float patrolRadius = 8000f;
    public float waypointReachedDistance = 500f;
    private Vector3 patrolTarget;

    // ── Obstacle Avoidance ──────────────────────────────────────────────────────
    [Header("Obstacle Avoidance")]
    public float avoidanceOffset = 400f;
    public float avoidanceRange = 4000f;

    // ── Visuals ────────────────────────────────────────────────────────────────
    [Header("Visuals")]
    public TrailRenderer trail;
    public Light glow;

    // ── State ──────────────────────────────────────────────────────────────────
    private enum AIState { Patrol, Chase, Attack }
    private AIState state = AIState.Patrol;

    private float nextFireTime;
    private float strafeSign = 1f;
    private float nextStrafeFlip;

    public static List<EnemyAI> allEnemies = new List<EnemyAI>();

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        physics = GetComponent<ShipPhysics>();
        _rb     = GetComponent<Rigidbody>();
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

    private void Start()
    {
        if (Ship.PlayerShip != null) player = Ship.PlayerShip.transform;

        // Randomize circle direction so enemies don't all orbit the same way.
        circleDir = (Random.value > 0.5f) ? 1f : -1f;
        strafeSign = circleDir;
        nextStrafeFlip = Time.time + Random.Range(2f, 5f);

        // Slightly larger enemies — more visible and easier to read at range.
        transform.localScale *= 1.4f;

        PickNewPatrolTarget();
        SetupVisuals();
        ApplyDifficulty();
        if (GetComponent<EnemyHealthBar>() == null)
            gameObject.AddComponent<EnemyHealthBar>();
    }

    private void ApplyDifficulty()
    {
        var m = DifficultyManager.GetMultipliers();

        // Health — must reset currentHealth too because ShipHealth.Awake already
        // snapped it to maxHealth before we got here.
        var health = GetComponent<ShipHealth>();
        if (health != null)
        {
            health.maxHealth     *= m.health;
            health.currentHealth  = health.maxHealth;
        }

        // Speed — scale both linear and angular force so Easy enemies feel sluggish
        // and Hard enemies turn/accelerate harder.
        if (physics != null)
        {
            physics.linearForce  *= m.speed;
            physics.angularForce *= m.speed;
        }

        // Fire rate — m.fireRate multiplies the INTERVAL, so >1 = slower shots.
        fireRate *= m.fireRate;

        // Aggression — push Hard enemies into tighter orbits, faster turns, and
        // more throttle so they actively close on the player.
        turnSpeedFactor *= m.aggression;
        throttleFactor  *= m.aggression;
        orbitRadius     /= Mathf.Max(0.01f, m.aggression);
    }

    private void SetupVisuals()
    {
        if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
        if (glow == null) glow = GetComponentInChildren<Light>();

        if (glow != null)
        {
            glow.range = 300f;
            glow.intensity = 5f;
            glow.color = Color.red;
        }
    }

    private void OnPlayerDestroyed()
    {
        // Fall back to camera position so the enemy doesn't freeze.
        if (Camera.main != null) player = Camera.main.transform;
    }

    // ── Main Update ───────────────────────────────────────────────────────────

    private void Update()
    {
        // HARD CLAMP runs first — before any AI logic can move the ship.
        HardClampToArena();

        // Re-acquire player reference if lost.
        if (player == null)
        {
            if (Ship.PlayerShip != null) player = Ship.PlayerShip.transform;
            else { Patrol(); return; }
        }

        // If we're at or past the boundary, completely override AI behaviour
        // and steer toward centre this frame so we can't keep thrusting outward.
        if (transform.position.magnitude >= ScriptsReference.ArenaRadius * 0.97f)
        {
            Vector3 toCentre = -transform.position.normalized;
            TurnToward(toCentre, turnSpeedFactor * 2f);
            physics.SetPhysicsInput(new Vector3(0, 0, throttleFactor), Vector3.zero);
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // ── State transitions ──────────────────────────────────────────────────
        if (distToPlayer <= attackRange)
            state = AIState.Attack;
        else if (distToPlayer <= detectionRange)
            state = AIState.Chase;
        else
            state = AIState.Patrol;

        // ── Execute state ──────────────────────────────────────────────────────
        switch (state)
        {
            case AIState.Patrol: Patrol(); break;
            case AIState.Chase:  Chase();  break;
            case AIState.Attack: Attack(); break;
        }
    }

    // Runs after ShipPhysics applies forward thrust (which happens in
    // FixedUpdate too) — guarantees the clamp survives the physics step.
    private void FixedUpdate()
    {
        HardClampToArena();
    }

    // ── States ────────────────────────────────────────────────────────────────

    private void Patrol()
    {
        // Pick a new waypoint once we're close enough.
        if (Vector3.Distance(transform.position, patrolTarget) < waypointReachedDistance)
            PickNewPatrolTarget();

        Vector3 dir = (patrolTarget - transform.position).normalized;
        dir = ApplyBoundaryCorrection(dir);
        dir = ApplyObstacleAvoidance(dir);

        TurnToward(dir, turnSpeedFactor * 0.6f);
        physics.SetPhysicsInput(new Vector3(0, 0, throttleFactor * 0.5f), Vector3.zero);
    }

    private void Chase()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir = ApplyBoundaryCorrection(dir);
        dir = ApplyObstacleAvoidance(dir);

        TurnToward(dir, turnSpeedFactor);
        physics.SetPhysicsInput(new Vector3(0, 0, throttleFactor), Vector3.zero);
    }

    private void Attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Flip strafe direction occasionally for unpredictability.
        if (Time.time >= nextStrafeFlip)
        {
            strafeSign = -strafeSign;
            nextStrafeFlip = Time.time + Random.Range(1.5f, 4f);
        }

        // Aim slightly ahead of the player (lead-target).
        Vector3 aimPoint = PredictPlayerPosition();
        Vector3 toAim = (aimPoint - transform.position).normalized;

        // Add a lateral strafe component so the enemy circles around.
        Vector3 strafeVec = transform.right * strafeSign * strafeSpeed;
        Vector3 desiredDir = (toAim + strafeVec).normalized;

        desiredDir = ApplyBoundaryCorrection(desiredDir);
        desiredDir = ApplyObstacleAvoidance(desiredDir);
        TurnToward(desiredDir, turnSpeedFactor * 1.2f);

        // Throttle: close the gap if too far, hold orbit distance if inside it.
        float throttle = (distToPlayer > orbitRadius * 1.1f) ? throttleFactor : throttleFactor * 0.4f;
        // Back off slightly if too close.
        if (distToPlayer < orbitRadius * 0.5f) throttle = -throttleFactor * 0.3f;

        physics.SetPhysicsInput(new Vector3(0, 0, throttle), Vector3.zero);

        // ── Fire ───────────────────────────────────────────────────────────────
        if (Time.time >= nextFireTime && PlayerInFiringArc())
        {
            FireLasers();
            nextFireTime = Time.time + fireRate;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Mirrors ScriptsReference.ArenaRadius — keeps enemies inside the asteroid wall.
    private static float EnemyRoamRadius => ScriptsReference.ArenaRadius;

    private void PickNewPatrolTarget()
    {
        // Patrol around the player so enemies feel present and threatening.
        // When no player is available, fall back to roaming near current position.
        Vector3 center = (player != null) ? player.position : transform.position;
        float   radius = patrolRadius * 0.45f; // tighter orbit around player
        Vector3 offset = Random.insideUnitSphere * radius;
        Vector3 candidate = center + offset;

        float limit = EnemyRoamRadius * 0.9f;
        if (candidate.magnitude > limit)
            candidate = candidate.normalized * limit;
        patrolTarget = candidate;
    }

    private Vector3 PredictPlayerPosition()
    {
        if (player == null) return transform.position;
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb == null) return player.position;

        float dist = Vector3.Distance(transform.position, player.position);
        // Laser speed from ShipLaserProjectile default (1000 u/s).
        float travelTime = dist / 1000f;
        return player.position + playerRb.linearVelocity * travelTime;
    }

    private bool PlayerInFiringArc()
    {
        if (player == null) return false;
        return Vector3.Angle(transform.forward, player.position - transform.position) < 35f;
    }

    private Vector3 ApplyBoundaryCorrection(Vector3 dir)
    {
        float distToCenter = transform.position.magnitude;
        float softLimit = EnemyRoamRadius * 0.75f;

        if (distToCenter > softLimit)
        {
            Vector3 toCenter = -transform.position.normalized;
            // Ramp up strongly: at softLimit t=0 (no push), at EnemyRoamRadius t=1 (fully toward center).
            float t = Mathf.InverseLerp(softLimit, EnemyRoamRadius, distToCenter);
            dir = Vector3.Slerp(dir, toCenter, Mathf.Clamp01(t * 3f)).normalized;
        }

        // Hard cap: if at or past the arena boundary, steer straight back to center.
        if (distToCenter >= ScriptsReference.ArenaRadius)
            dir = -transform.position.normalized;

        return dir;
    }

    private Rigidbody _rb;

    /// <summary>
    /// Guaranteed hard clamp at the arena radius — runs in both Update (before
    /// AI logic) and FixedUpdate (after ShipPhysics applies force). Uses
    /// Rigidbody.position so the physics engine doesn't snap the body back to
    /// where it thinks it should be next step.
    /// </summary>
    private void HardClampToArena()
    {
        float radius = ScriptsReference.ArenaRadius;
        Vector3 pos = transform.position;
        float dist = pos.magnitude;
        if (dist <= radius) return;

        Vector3 outward = pos / dist; // = pos.normalized, already have |pos|
        Vector3 clamped = outward * radius;

        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.position           = clamped;
            transform.position     = clamped;
            // Zero outward velocity component completely.
            float dotOut = Vector3.Dot(_rb.linearVelocity, outward);
            if (dotOut > 0f)
                _rb.linearVelocity -= outward * dotOut;
            // Kill spin so the AI can't keep rotating outward.
            _rb.angularVelocity = Vector3.zero;
            // Big inward kick — has to dominate AI forward thrust this frame.
            _rb.AddForce(-outward * 8000f, ForceMode.Impulse);
        }
        else
        {
            transform.position = clamped;
        }
    }

    private Vector3 ApplyObstacleAvoidance(Vector3 dir)
    {
        Vector3 avoidVec = Vector3.zero;
        Vector3[] offsets =
        {
            transform.right  *  avoidanceOffset,
            transform.right  * -avoidanceOffset,
            transform.up     *  avoidanceOffset,
            transform.up     * -avoidanceOffset,
        };

        foreach (var off in offsets)
        {
            if (Physics.Raycast(transform.position + off + transform.forward * 200f,
                                transform.forward, avoidanceRange))
                avoidVec -= off.normalized;
        }

        if (avoidVec != Vector3.zero)
            dir = (dir + avoidVec * 2f).normalized;

        return dir;
    }

    private void TurnToward(Vector3 dir, float speedFactor)
    {
        if (dir == Vector3.zero) return;
        Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, target, speedFactor * Time.deltaTime * 3f);
    }

    private void FireLasers()
    {
        if (laserPrefab == null) return;

        Vector3 left  = transform.position + transform.forward * laserSpawnForwardOffset - transform.right * 60f;
        Vector3 right = transform.position + transform.forward * laserSpawnForwardOffset + transform.right * 60f;

        SpawnLaser(left);
        SpawnLaser(right);
    }

    private void SpawnLaser(Vector3 pos)
    {
        // Fire toward the predicted player position rather than raw forward.
        Vector3 aimPoint = PredictPlayerPosition();
        Vector3 aimDir = (aimPoint - pos).normalized;

        Quaternion rot = (aimDir != Vector3.zero)
            ? Quaternion.LookRotation(aimDir)
            : transform.rotation;

        GameObject laser = Instantiate(laserPrefab, pos, rot);
        var script = laser.GetComponent<ShipLaserProjectile>();
        if (script != null)
        {
            script.targetTag = "Player";
            script.Initialize(aimDir);
        }
    }
}
