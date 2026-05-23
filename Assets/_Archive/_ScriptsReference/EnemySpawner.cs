using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [Tooltip("Time between spawns in seconds")][SerializeField] private float _spawnTime = 5f;
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

        // Spawn within 60 user units of Vector3.zero (= ArenaRadius * 0.5).
        float spawnLimit = ScriptsReference.ArenaRadius * 0.5f;
        Vector3 spawnPos = Random.insideUnitSphere * spawnLimit;
        spawnPos.y *= 0.5f;
        if (spawnPos.magnitude > spawnLimit)
            spawnPos = spawnPos.normalized * spawnLimit;

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
