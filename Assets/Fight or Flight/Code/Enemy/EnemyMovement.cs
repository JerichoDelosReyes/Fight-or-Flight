using UnityEngine;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    private enum State { Chase, FlyPast, Reposition }
    [SerializeField] private State _state = State.Chase;

    public float _movementSpeed = 600f;
    public float _turnSpeed = 4.5f;
    [SerializeField] private float _rayCastOffset = 500f;
    [SerializeField] private float _rayCastRange = 3000f;
    [SerializeField] private int _points = 100;

    [Header("Jousting Settings")]
    [SerializeField] private float _flyPastDist = 1500f;
    [SerializeField] private float _repositionDist = 5000f;
    [SerializeField] private float _flyPastTime = 2.0f;
    private float _stateTimer;

    private Transform _target;
    private Rigidbody _targetRb;
    private bool _isBlowingUp = false;

    public static List<EnemyMovement> allEnemies = new List<EnemyMovement>();

    private void OnEnable()
    {
        if (!allEnemies.Contains(this)) allEnemies.Add(this);
        GameEventManager.OnStartGame += SelfDestruct;
        GameEventManager.OnPlayerDestroyed += TargetMainCamera;
    }

    private void OnDisable()
    {
        allEnemies.Remove(this);
        GameEventManager.OnStartGame -= SelfDestruct;
        GameEventManager.OnPlayerDestroyed -= TargetMainCamera;
    }

    private void Update()
    {
        HardClampToArena();

        if (!TargetPlayer()) return;

        UpdateState();
        Pathfinding();
        Move();
    }

    private void UpdateState()
    {
        float distToTarget = Vector3.Distance(transform.position, _target.position);
        float playerVel = (_targetRb != null) ? _targetRb.linearVelocity.magnitude : 0f;

        switch (_state)
        {
            case State.Chase:
                // Only enter FlyPast if the player is moving or we are extremely close and fast.
                // This prevents "jousting" loops around a stationary player.
                if (distToTarget < _flyPastDist && playerVel > 1.0f)
                {
                    _state = State.FlyPast;
                    _stateTimer = Time.time + _flyPastTime;
                }
                break;

            case State.FlyPast:
                if (Time.time > _stateTimer || distToTarget > _repositionDist)
                {
                    _state = State.Reposition;
                }
                break;

            case State.Reposition:
                // If we are facing the player or far enough, go back to chase
                Vector3 toTarget = (_target.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toTarget);
                if (angle < 20f || distToTarget > _repositionDist)
                {
                    _state = State.Chase;
                }
                break;
        }
    }

    private void Turn()
    {
        if (!TargetPlayer()) return;

        Vector3 targetDir;

        if (_state == State.FlyPast)
        {
            // Just keep going or steer slightly away
            targetDir = transform.forward; 
        }
        else
        {
            targetDir = _target.position - transform.position;
        }

        // Fix: Removed targetDir.y = 0. This was causing enemies to circle the 
        // player's shadow on the XZ plane instead of pointing at the player in 3D.
        
        if (targetDir.sqrMagnitude < 0.001f) return;
        
        Quaternion rotation = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, _turnSpeed * Time.deltaTime);

        // Visual banking: Roll the ship slightly based on its turning direction
        Vector3 localTargetDir = transform.InverseTransformDirection(targetDir);
        float rollAngle = -localTargetDir.x * 0.1f; // Subtle bank
        transform.Rotate(0, 0, rollAngle * _turnSpeed * 10f * Time.deltaTime, Space.Self);
    }

    private void Move()
    {
        float speed = _movementSpeed;
        
        // Slow down as we approach a stationary player to stay on target and avoid overshooting.
        if (_target != null)
        {
            float playerVel = (_targetRb != null) ? _targetRb.linearVelocity.magnitude : 0f;
            float dist = Vector3.Distance(transform.position, _target.position);
            
            if (playerVel < 0.5f && dist < _flyPastDist)
            {
                speed = Mathf.Lerp(_movementSpeed * 0.25f, _movementSpeed, dist / _flyPastDist);
            }
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void Pathfinding()
    {
        RaycastHit hit;
        Vector3 rayCastRotation = Vector3.zero;

        Vector3 left = transform.position - transform.right * _rayCastOffset;
        Vector3 right = transform.position + transform.right * _rayCastOffset;
        Vector3 up = transform.position + transform.up * _rayCastOffset;
        Vector3 down = transform.position - transform.up * _rayCastOffset;

        // Corrected obstacle avoidance axes: 
        // If hit left, rotate positive Y (turn right).
        // If hit up, rotate positive X (pitch down).
        if (Physics.Raycast(left, transform.forward, out hit, _rayCastRange))
        {
            rayCastRotation.y += 1f;
        }
        else if (Physics.Raycast(right, transform.forward, out hit, _rayCastRange))
        {
            rayCastRotation.y -= 1f;
        }

        if (Physics.Raycast(up, transform.forward, out hit, _rayCastRange))
        {
            rayCastRotation.x += 1f;
        }
        else if (Physics.Raycast(down, transform.forward, out hit, _rayCastRange))
        {
            rayCastRotation.x -= 1f;
        }

        if (rayCastRotation != Vector3.zero)
        {
            transform.Rotate(rayCastRotation * 60f * Time.deltaTime);
        }
        else
        {
            Turn();
        }
    }

    private bool TargetPlayer()
    {
        if (_target == null)
        {
            if (Ship.PlayerShip != null)
            {
                _target = Ship.PlayerShip.transform;
                _targetRb = Ship.PlayerShip.GetComponent<Rigidbody>();
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

    private void TargetMainCamera()
    {
        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera != null)
        {
            _target = mainCamera.transform;
            _targetRb = null;
        }
    }

    private void SelfDestruct()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }

    private void HardClampToArena()
    {
        float radius = ScriptsReference.ArenaRadius;
        if (transform.position.magnitude > radius)
        {
            transform.position = transform.position.normalized * radius;
            Vector3 toCenter = -transform.position.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toCenter), _turnSpeed * Time.deltaTime * 5f);
        }
    }

    public void BlowUp()
    {
        if (!_isBlowingUp)
        {
            _isBlowingUp = true;
            GameEventManager.IncrementScore(_points);
            Explosion exp = GetComponent<Explosion>();
            if (exp != null) exp.BlowUp();
            else
            {
                ShipHealth health = GetComponent<ShipHealth>();
                if (health != null) health.TakeDamage(9999f);
                else SelfDestruct();
            }
        }
    }
}
