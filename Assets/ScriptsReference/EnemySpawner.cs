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

        // Spawn in a 30–80 unit ring (user scale) around the player.
        float arenaR  = ScriptsReference.ArenaRadius;
        float safeMax = arenaR * 0.85f;

        Vector3 spawnPos;
        if (Ship.PlayerShip != null)
        {
            Vector3 playerPos = Ship.PlayerShip.transform.position;
            float   minDist   = arenaR * 0.17f;
            float   maxDist   = arenaR * 0.45f;
            Vector3 dir       = Random.onUnitSphere;
            dir.y *= 0.35f;
            dir = dir.normalized;
            spawnPos = playerPos + dir * Random.Range(minDist, maxDist);
        }
        else
        {
            spawnPos = Random.insideUnitSphere * (arenaR * 0.55f);
        }

        if (spawnPos.magnitude > safeMax)
            spawnPos = spawnPos.normalized * safeMax;

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
