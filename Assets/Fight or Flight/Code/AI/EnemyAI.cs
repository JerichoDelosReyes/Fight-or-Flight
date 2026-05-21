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

    // ── Behaviour ranges (game units; ArenaRadius = 120 user units = 12,000) ──
    // > ChaseTriggerDist  : Chase   (fly straight at player at full throttle)
    // ≤ ChaseTriggerDist  : Attack  (circle + strafe + shoot)
    // < BackAwayDist      : Attack  (still shooting, but reverse-thrust)
    private static float ChaseTriggerDist => ScriptsReference.ArenaRadius * 0.50f; // user "60"
    private static float BackAwayDist     => ScriptsReference.ArenaRadius * 0.17f; // user "20"

    [Header("Behavior")]
    [Tooltip("Base turn speed multiplier")]
    public float turnSpeedFactor = 1.1f;
    [Tooltip("Throttle applied while chasing/orbiting")]
    public float throttleFactor = 0.14f;
    [Tooltip("How fast the enemy strafes laterally during attack")]
    public float strafeSpeed = 0.35f;
    [Tooltip("Direction the enemy is currently circling (+1 or -1)")]
    private float circleDir = 1f;

    // ── Obstacle Avoidance ──────────────────────────────────────────────────────
    [Header("Obstacle Avoidance")]
    public float avoidanceOffset = 400f;
    public float avoidanceRange = 4000f;

    // ── Visuals ────────────────────────────────────────────────────────────────
    [Header("Visuals")]
    public TrailRenderer trail;
    public Light glow;

    // ── State ──────────────────────────────────────────────────────────────────
    private enum AIState { Chase, Attack }
    private AIState state = AIState.Chase;

    private float nextFireTime;
    private float strafeSign = 1f;
    private float nextStrafeFlip;
    private float playerReacquireTimer; // refresh player ref every 0.5s
    private Rigidbody _rb;

    public Transform firePoint;

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

        Transform tp = transform.Find("TailPoint");
        Vector3 tailPos = (tp != null) ? tp.localPosition : new Vector3(0, 0, -100);

        // If still null, create them programmatically for game-feel
        if (trail == null)
        {
            GameObject trailGo = new GameObject("AI_Trail");
            trailGo.transform.SetParent(transform, false);
            trailGo.transform.localPosition = tailPos;

            trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 40f; // Reduced from 100f
            trail.endWidth = 0f;
trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.red;
            trail.endColor = new Color(1, 0, 0, 0);
        }

        if (glow == null)
        {
            GameObject glowGo = new GameObject("AI_Glow");
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.localPosition = tailPos;
            
            glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 300f; // Reduced from 2000f to match scale
            glow.intensity = 8f;
            glow.color = Color.red;
        }
        else
        {
            // Update existing glow position if found in children
            glow.transform.localPosition = tailPos;
            glow.range = 300f;
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

        // Re-acquire the player every 0.5s so enemies never lose track.
        playerReacquireTimer -= Time.deltaTime;
        if (player == null || playerReacquireTimer <= 0f)
        {
            playerReacquireTimer = 0.5f;
            if (Ship.PlayerShip != null) player = Ship.PlayerShip.transform;
        }

        // No player alive — idle.
        if (player == null)
        {
            physics.SetPhysicsInput(Vector3.zero, Vector3.zero);
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Two-state machine: always hunting.
        state = (distToPlayer > ChaseTriggerDist) ? AIState.Chase : AIState.Attack;

        if (state == AIState.Chase) Chase();
        else                        Attack();
    }

    // Runs after ShipPhysics applies forward thrust (which happens in
    // FixedUpdate too) — guarantees the clamp survives the physics step.
    private void FixedUpdate()
    {
        HardClampToArena();
    }

    // ── States ────────────────────────────────────────────────────────────────

    private void Chase()
    {
        // Fly straight at the player at full throttle.
        Vector3 dir = (player.position - transform.position).normalized;
        dir = ApplyBoundaryCorrection(dir);
        dir = ApplyObstacleAvoidance(dir);

        TurnToward(dir, turnSpeedFactor * 1.1f);
        physics.SetLinearInput(new Vector3(0, 0, throttleFactor));
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

        // Throttle: back away if very close to the player, otherwise hold orbit.
        float throttle;
        if (distToPlayer < BackAwayDist) throttle = -throttleFactor * 0.7f;  // reverse
        else                              throttle =  throttleFactor * 0.5f;  // gentle forward
        physics.SetLinearInput(new Vector3(0, 0, throttle));

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
        // Narrowed arc (from 35 to 15 degrees) so they don't look sideways when firing.
        return Vector3.Angle(transform.forward, player.position - transform.position) < 15f;
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

    /// <summary>
/// Absolute hard clamp at the arena boundary — runs in both Update (before
    /// AI logic) and FixedUpdate (after ShipPhysics applies force).
    ///
    /// User-spec: if the enemy gets further than 120 user units (ArenaRadius)
    /// from origin, snap to 119 user units (ArenaRadius * 119/120) along the
    /// outward direction, and ZERO both linear and angular velocity entirely
    /// so the ship cannot drift any further outward.
    /// </summary>
    private void HardClampToArena()
    {
        float radius = ScriptsReference.ArenaRadius;
        Vector3 pos = transform.position;
        float dist = pos.magnitude;
        if (dist <= radius) return;

        // Snap to 119/120 of the radius — keeps the enemy strictly inside.
        Vector3 outward = pos / dist;
        Vector3 clamped = outward * (radius * (119f / 120f));

        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.position        = clamped;
            transform.position  = clamped;
            _rb.linearVelocity  = Vector3.zero; // total velocity wipe, not just outward
            _rb.angularVelocity = Vector3.zero;
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

        // Calculate relative rotation needed
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * targetRot;
        Vector3 rotationError = relativeRot.eulerAngles;

        // Normalize angles to -180 to 180
        if (rotationError.x > 180) rotationError.x -= 360;
        if (rotationError.y > 180) rotationError.y -= 360;
        if (rotationError.z > 180) rotationError.z -= 360;

        // Map errors to physics inputs (Pitch, Yaw, Roll)
        // Note: x is pitch, y is yaw, z is roll
        float pInput = Mathf.Clamp(rotationError.x / 45f, -1f, 1f);
        float yInput = Mathf.Clamp(rotationError.y / 45f, -1f, 1f);
        
        // Dynamic roll based on yaw to look "aerodynamic"
        float rInput = Mathf.Clamp(-yInput * 1.5f, -1f, 1f);

        physics.SetAngularInput(new Vector3(pInput, yInput, rInput) * speedFactor);
    }

    private void FireLasers()
    {
        if (laserPrefab == null) return;

        // Firing from the single central firepoint.
        Vector3 pos = (firePoint != null) ? firePoint.position : transform.position + transform.forward * laserSpawnForwardOffset;
        SpawnLaser(pos);
    }

    private void SpawnLaser(Vector3 pos)
    {
        // NO MORE MAGIC AIMING: Lasers fire exactly where the ship is pointing.
        Vector3 fireDir = (firePoint != null) ? firePoint.forward : transform.forward;

        Quaternion rot = Quaternion.LookRotation(fireDir);

        GameObject laser = Instantiate(laserPrefab, pos, rot);
        var script = laser.GetComponent<ShipLaserProjectile>();
        if (script != null)
        {
            script.targetTag = "Player";
            script.Initialize(fireDir);
        }
    }
}
