using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [Tooltip("Time between spawns in seconds")][SerializeField] private float _spawnTime = 5f;
    [SerializeField] private float _spawnRadius = 3000f;
    [SerializeField] private int _maxEnemies = 15;

    /// <summary>Public read-only access to the enemy prefab so the WaveManager
    /// can take it over when it disables the legacy spawner.</summary>
    public GameObject EnemyPrefab { get { return _enemyPrefab; } }

    private int _currentEnemyCount = 0;

    private void OnEnable()
    {
        GameEventManager.OnStartGame += StartSpawning;
        GameEventManager.OnPlayerDestroyed += StopSpawning;
    }

    private void OnDisable()
    {
        GameEventManager.OnStartGame -= StartSpawning;
        GameEventManager.OnPlayerDestroyed -= StopSpawning;
    }

    private void Start()
    {
        // Do NOT spawn here — wait for the OnStartGame event.
        // Calling StartSpawning() in Start() caused enemies to appear on the main menu
        // because the spawner GameObject can be present before the game begins.
    }

    private void Update()
{
        // Safety check to ensure count is somewhat accurate
        // (Optional, but helps if destruction tracking fails)
        if (Time.frameCount % 60 == 0)
        {
            _currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        }
    }

    private void StartSpawning()
    {
        CancelInvoke("SpawnEnemy");
        InvokeRepeating("SpawnEnemy", 2f, _spawnTime);
    }

    private void SpawnEnemy()
    {
        if (_enemyPrefab == null) return;
        if (_currentEnemyCount >= _maxEnemies) return;

        // Spawn well inside the arena — never near the boundary wall.
        float safeRadius = ScriptsReference.ArenaRadius * 0.55f;
        float useRadius  = Mathf.Min(_spawnRadius, safeRadius);
        Vector3 spawnPos = Random.insideUnitSphere * useRadius;

        // Hard clamp inside the safe zone in case Random produced an edge value.
        if (spawnPos.magnitude > safeRadius)
            spawnPos = spawnPos.normalized * safeRadius;

        // Keep them away from player initially
        if (Ship.PlayerShip != null && Vector3.Distance(spawnPos, Ship.PlayerShip.transform.position) < 800f)
        {
            spawnPos += (spawnPos - Ship.PlayerShip.transform.position).normalized * 800f;
            // Re-clamp after the player-push offset.
            if (spawnPos.magnitude > safeRadius)
                spawnPos = spawnPos.normalized * safeRadius;
        }

        Instantiate(_enemyPrefab, spawnPos, Quaternion.identity);
        _currentEnemyCount++;
    }

    public void OnEnemyDestroyed()
    {
        _currentEnemyCount = Mathf.Max(0, _currentEnemyCount - 1);
    }

    private void StopSpawning()
    {
        CancelInvoke("SpawnEnemy");
    }
    }
