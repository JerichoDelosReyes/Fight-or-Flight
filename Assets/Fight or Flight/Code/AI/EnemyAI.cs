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
    public float laserSpawnForwardOffset = 100f;

    // ── Behaviour ranges (in game units; ArenaRadius = 12,000) ────────────────
    // User-scale 180 maps to ArenaRadius; the constants below are the
    // corresponding fractions: 80 (chase), 30 (orbit), 100 (return-patrol),
    // 175 (hard clamp).
    private static float ChaseTriggerDist => ScriptsReference.ArenaRadius * 0.45f; // ~5400
    private static float OrbitDist        => ScriptsReference.ArenaRadius * 0.17f; // ~2040
    private static float ReturnPatrolDist => ScriptsReference.ArenaRadius * 0.55f; // ~6600
    private static float HardClampDist    => ScriptsReference.ArenaRadius * 0.97f; // ~11640

    [Header("Behavior")]
    [Tooltip("Base turn speed multiplier")]
    public float turnSpeedFactor = 1.1f;
    [Tooltip("Throttle applied while chasing/orbiting")]
    public float throttleFactor = 0.14f;
    [Tooltip("How fast the enemy strafes laterally during attack")]
    public float strafeSpeed = 0.35f;
    [Tooltip("Direction the enemy is currently circling (+1 or -1)")]
    private float circleDir = 1f;

    // ── Patrol ─────────────────────────────────────────────────────────────────
    // Random-direction patrol: every 3-5 s the enemy picks a new direction.
    private Vector3 patrolDir;
    private float   patrolDirTimer;

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

        // Spawn facing a random direction.
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Randomize circle direction so enemies don't all orbit the same way.
        circleDir = (Random.value > 0.5f) ? 1f : -1f;
        strafeSign = circleDir;
        nextStrafeFlip = Time.time + Random.Range(2f, 5f);

        // Slightly larger enemies — more visible and easier to read at range.
        transform.localScale *= 1.4f;

        PickRandomPatrolDir();
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

        // Aggression — faster turns and more throttle so Hard enemies actively
        // close on the player. Orbit distance is a global constant now.
        turnSpeedFactor *= m.aggression;
        throttleFactor  *= m.aggression;
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
        if (transform.position.magnitude >= HardClampDist)
        {
            Vector3 toCentre = -transform.position.normalized;
            TurnToward(toCentre, turnSpeedFactor * 2f);
            physics.SetPhysicsInput(new Vector3(0, 0, throttleFactor), Vector3.zero);
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // ── State transitions (with hysteresis so enemies don't flicker) ─────
        switch (state)
        {
            case AIState.Patrol:
                if (distToPlayer < ChaseTriggerDist) state = AIState.Chase;
                break;
            case AIState.Chase:
                if (distToPlayer < OrbitDist)            state = AIState.Attack;
                else if (distToPlayer > ReturnPatrolDist) state = AIState.Patrol;
                break;
            case AIState.Attack:
                if (distToPlayer > ReturnPatrolDist)     state = AIState.Patrol;
                else if (distToPlayer > OrbitDist * 2f)  state = AIState.Chase;
                break;
        }

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
        // Count down the current direction's lifetime; pick new one when it expires.
        patrolDirTimer -= Time.deltaTime;
        if (patrolDirTimer <= 0f) PickRandomPatrolDir();

        Vector3 dir = patrolDir;

        // If we're getting close to the wall, flip the patrol direction inward
        // immediately rather than waiting for the timer.
        if (transform.position.magnitude > ScriptsReference.ArenaRadius * 0.75f)
        {
            Vector3 inward = -transform.position.normalized;
            if (Vector3.Dot(patrolDir, inward) < 0f)
            {
                PickRandomPatrolDir();
                // Bias the new direction inward.
                patrolDir = Vector3.Slerp(patrolDir, inward, 0.7f).normalized;
                dir = patrolDir;
            }
        }

        dir = ApplyBoundaryCorrection(dir);
        dir = ApplyObstacleAvoidance(dir);

        TurnToward(dir, turnSpeedFactor * 0.7f);
        physics.SetPhysicsInput(new Vector3(0, 0, throttleFactor * 0.6f), Vector3.zero);
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
        float throttle = (distToPlayer > OrbitDist * 1.1f) ? throttleFactor : throttleFactor * 0.4f;
        // Back off slightly if too close.
        if (distToPlayer < OrbitDist * 0.5f) throttle = -throttleFactor * 0.3f;

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

    private void PickRandomPatrolDir()
    {
        // Random unit direction; pinch the vertical component so enemies don't
        // patrol straight up or straight down (looks weird in a spaceship sim).
        Vector3 dir = Random.onUnitSphere;
        dir.y *= 0.35f;
        patrolDir      = dir.normalized;
        patrolDirTimer = Random.Range(3f, 5f);
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
