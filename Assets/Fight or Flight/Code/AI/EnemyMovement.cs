using UnityEngine;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    private enum State { Chase, FlyPast, Reposition }
    [SerializeField] private State _state = State.Chase;

    [SerializeField] private float _movementSpeed = 600f; // Reduced further
    [SerializeField] private float _turnSpeed = 4.5f;     // Faster turns for better tracking
    [SerializeField] private float _rayCastOffset = 500f;
    [SerializeField] private float _rayCastRange = 3000f;
    [SerializeField] private int _points = 100;

    [Header("Jousting Settings")]
    [SerializeField] private float _flyPastDist = 1500f;
    [SerializeField] private float _repositionDist = 5000f;
    [SerializeField] private float _flyPastTime = 2.0f;
    private float _stateTimer;

    private Transform _target;
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

        switch (_state)
        {
            case State.Chase:
                if (distToTarget < _flyPastDist)
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

        targetDir.y = 0;
        
        if (targetDir == Vector3.zero) return;
        
        Quaternion rotation = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, _turnSpeed * Time.deltaTime);
    }

    private void Move()
    {
        transform.position += transform.forward * _movementSpeed * Time.deltaTime;
    }

    private void Pathfinding()
    {
        RaycastHit hit;
        Vector3 rayCastOffset = Vector3.zero;

        Vector3 left = transform.position - transform.right * _rayCastOffset;
        Vector3 right = transform.position + transform.right * _rayCastOffset;
        Vector3 up = transform.position + transform.up * _rayCastOffset;
        Vector3 down = transform.position - transform.up * _rayCastOffset;

        if (Physics.Raycast(left, transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset += Vector3.right;
        }
        else if (Physics.Raycast(right, transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset -= Vector3.right;
        }

        if (Physics.Raycast(up, transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset -= Vector3.up;
        }
        else if (Physics.Raycast(down, transform.forward, out hit, _rayCastRange))
        {
            rayCastOffset = Vector3.up;
        }

        if (rayCastOffset != Vector3.zero)
        {
            transform.Rotate(rayCastOffset * 50f * Time.deltaTime);
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
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
            }
            else if (Ship.PlayerShip != null)
            {
                _target = Ship.PlayerShip.transform;
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
