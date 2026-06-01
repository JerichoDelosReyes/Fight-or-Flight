using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    private Transform player;
    private Rigidbody playerRb;
    private ShipPhysics physics;

    // ── Combat ─────────────────────────────────────────────────────────────────
    [Header("Combat")]
    public GameObject laserPrefab;
    public float fireRate = 1.2f;
    public float laserSpawnForwardOffset = 100f;

    // ── Behaviour ranges (game units; ArenaRadius = 120 user units = 12,000) ──
    private static float ChaseTriggerDist => ScriptsReference.ArenaRadius * 0.50f;
    private static float BackAwayDist     => ScriptsReference.ArenaRadius * 0.08f;

    [Header("Behavior")]
    public float turnSpeedFactor = 1.3f;
    public float throttleFactor = 0.25f;
    public float strafeSpeed = 0.45f;
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
    private float playerReacquireTimer;
    private Rigidbody _rb;

    public Transform firePoint;

    public static List<EnemyAI> allEnemies = new List<EnemyAI>();

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
        RefreshPlayerRef();
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        circleDir = (Random.value > 0.5f) ? 1f : -1f;
        strafeSign = circleDir;
        nextStrafeFlip = Time.time + Random.Range(2f, 5f);
        transform.localScale *= 1.4f;

        SetupVisuals();
        ApplyDifficulty();
        if (GetComponent<EnemyHealthBar>() == null)
            gameObject.AddComponent<EnemyHealthBar>();
    }

    private void ApplyDifficulty()
    {
        var m = DifficultyManager.GetMultipliers();
        var health = GetComponent<ShipHealth>();
        if (health != null)
        {
            health.maxHealth     *= m.health;
            health.currentHealth  = health.maxHealth;
        }
        if (physics != null)
        {
            physics.linearForce  *= m.speed;
            physics.angularForce *= m.speed;
        }
        fireRate *= m.fireRate;
        turnSpeedFactor *= m.aggression;
        throttleFactor  *= m.aggression;
    }

    private void SetupVisuals()
    {
        if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
        if (glow == null) glow = GetComponentInChildren<Light>();

        Transform tp = transform.Find("TailPoint");
        Vector3 tailPos = (tp != null) ? tp.localPosition : new Vector3(0, 0, -100);

        if (trail == null)
        {
            GameObject trailGo = new GameObject("AI_Trail");
            trailGo.transform.SetParent(transform, false);
            trailGo.transform.localPosition = tailPos;
            trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 40f;
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
            glow.range = 300f;
            glow.intensity = 8f;
            glow.color = Color.red;
        }
        else
        {
            glow.transform.localPosition = tailPos;
            glow.range = 300f;
        }
    }

    private void OnPlayerDestroyed()
    {
        if (Camera.main != null) player = Camera.main.transform;
        playerRb = null;
    }

    private void RefreshPlayerRef()
    {
        if (Ship.PlayerShip != null)
        {
            player = Ship.PlayerShip.transform;
            playerRb = player.GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        HardClampToArena();

        playerReacquireTimer -= Time.deltaTime;
        if (player == null || playerReacquireTimer <= 0f)
        {
            playerReacquireTimer = 0.5f;
            RefreshPlayerRef();
        }

        if (player == null)
        {
            physics.SetPhysicsInput(Vector3.zero, Vector3.zero);
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        float distToPlayerSq = toPlayer.sqrMagnitude;
        float chaseTriggerSq = ChaseTriggerDist * ChaseTriggerDist;
        state = (distToPlayerSq > chaseTriggerSq) ? AIState.Chase : AIState.Attack;

        float distToPlayer = Mathf.Sqrt(distToPlayerSq);

        if (state == AIState.Chase) Chase();
        else                        Attack(distToPlayer);
    }

    private void FixedUpdate()
    {
        HardClampToArena();
    }

    private void Chase()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir = ApplyBoundaryCorrection(dir);
        dir = ApplyObstacleAvoidance(dir);
        TurnToward(dir, turnSpeedFactor * 1.1f);
        physics.SetLinearInput(new Vector3(0, 0, throttleFactor));
    }

    private void Attack(float distToPlayer)
    {
        float playerVelSq = (playerRb != null) ? playerRb.linearVelocity.sqrMagnitude : 0f;

        if (Time.time >= nextStrafeFlip)
        {
            strafeSign = -strafeSign;
            nextStrafeFlip = Time.time + Random.Range(1.5f, 4f);
        }

        Vector3 aimPoint = PredictPlayerPosition(distToPlayer, playerVelSq);
        Vector3 toAim = (aimPoint - transform.position).normalized;

        float effectiveStrafe = (playerVelSq < 0.25f) ? 0f : strafeSpeed;
        Vector3 strafeVec = transform.right * strafeSign * effectiveStrafe;
        Vector3 desiredDir = (toAim + strafeVec).normalized;

        desiredDir = ApplyBoundaryCorrection(desiredDir);
        desiredDir = ApplyObstacleAvoidance(desiredDir);
        TurnToward(desiredDir, turnSpeedFactor * 1.2f);

        float throttle = (distToPlayer < BackAwayDist) ? -throttleFactor * 0.5f : throttleFactor * 0.9f;
        physics.SetLinearInput(new Vector3(0, 0, throttle));

        if (Time.time >= nextFireTime && PlayerInFiringArc())
        {
            FireLasers();
            nextFireTime = Time.time + fireRate;
        }
    }

    private static float EnemyRoamRadius => ScriptsReference.ArenaRadius;

    private Vector3 PredictPlayerPosition(float dist, float playerVelSq)
    {
        if (player == null) return transform.position;
        if (playerRb == null || playerVelSq < 0.25f) return player.position;

        float travelTime = dist / 5000f;
        return player.position + playerRb.linearVelocity * travelTime;
    }

    private bool PlayerInFiringArc()
    {
        if (player == null) return false;
        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude < 0.001f) return true;

        float dot = Vector3.Dot(transform.forward, toPlayer.normalized);
        return dot > 0.9659258f; // cos(15 degrees)
    }

    private Vector3 ApplyBoundaryCorrection(Vector3 dir)
    {
        float distToCenterSq = transform.position.sqrMagnitude;
        float softLimit = EnemyRoamRadius * 0.75f;
        float softLimitSq = softLimit * softLimit;

        if (distToCenterSq > softLimitSq)
        {
            float distToCenter = Mathf.Sqrt(distToCenterSq);
            Vector3 toCenter = -transform.position / distToCenter;
            float t = Mathf.InverseLerp(softLimit, EnemyRoamRadius, distToCenter);
            dir = Vector3.Slerp(dir, toCenter, Mathf.Clamp01(t * 3f)).normalized;
        }

        if (distToCenterSq >= EnemyRoamRadius * EnemyRoamRadius)
            dir = -transform.position.normalized;

        return dir;
    }

    private void HardClampToArena()
    {
        float radius = ScriptsReference.ArenaRadius;
        float distSq = transform.position.sqrMagnitude;
        if (distSq <= radius * radius) return;

        float dist = Mathf.Sqrt(distSq);
        Vector3 outward = transform.position / dist;
        Vector3 clamped = outward * (radius * (119f / 120f));

        if (_rb != null)
        {
            _rb.position        = clamped;
            transform.position  = clamped;
            _rb.linearVelocity  = Vector3.zero;
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
        Vector3 fwd = transform.forward;
        Vector3 rgt = transform.right;
        Vector3 up = transform.up;
        Vector3 pos = transform.position + fwd * 200f;

        if (Physics.Raycast(pos + rgt * avoidanceOffset, fwd, avoidanceRange)) avoidVec -= rgt;
        if (Physics.Raycast(pos - rgt * avoidanceOffset, fwd, avoidanceRange)) avoidVec += rgt;
        if (Physics.Raycast(pos + up * avoidanceOffset, fwd, avoidanceRange)) avoidVec -= up;
        if (Physics.Raycast(pos - up * avoidanceOffset, fwd, avoidanceRange)) avoidVec += up;

        if (avoidVec != Vector3.zero)
            dir = (dir + avoidVec.normalized * 2f).normalized;

        return dir;
    }

    private void TurnToward(Vector3 dir, float speedFactor)
    {
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * targetRot;
        Vector3 rotationError = relativeRot.eulerAngles;

        if (rotationError.x > 180) rotationError.x -= 360;
        if (rotationError.y > 180) rotationError.y -= 360;
        if (rotationError.z > 180) rotationError.z -= 360;

        float pInput = Mathf.Clamp(rotationError.x / 45f, -1f, 1f);
        float yInput = Mathf.Clamp(rotationError.y / 45f, -1f, 1f);
        float rInput = Mathf.Clamp(-yInput * 1.5f, -1f, 1f);

        physics.SetAngularInput(new Vector3(pInput, yInput, rInput) * speedFactor);
    }

    private void FireLasers()
    {
        Vector3 pos = (firePoint != null) ? firePoint.position : transform.position + transform.forward * laserSpawnForwardOffset;
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
