using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [Tooltip("Time between spawns in seconds")][SerializeField] private float _spawnTime = 5f;
    [SerializeField] private float _spawnRadius = 3000f;
    [SerializeField] private int _maxEnemies = 15;

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

        Vector3 spawnPos = Random.insideUnitSphere * _spawnRadius;
        
        // Ensure within boundaries
        if (spawnPos.magnitude > ScriptsReference.BoundaryLimit - 500f)
            spawnPos = spawnPos.normalized * (ScriptsReference.BoundaryLimit - 500f);

        // Keep them away from player initially
        if (Ship.PlayerShip != null && Vector3.Distance(spawnPos, Ship.PlayerShip.transform.position) < 800f)
        {
            spawnPos += (spawnPos - Ship.PlayerShip.transform.position).normalized * 800f;
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
