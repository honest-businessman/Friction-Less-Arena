using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Pathfinding.Examples;
using System;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Settings")]
    public List<Enemy> enemies = new List<Enemy>();
    public List<WallObjectSettings> wallObjects = new List<WallObjectSettings>();
    public float waveDuration = 20f;
    public float spawningDuration = 10f;
    public float baseWallSpawnInterval = 15f;
    public float wallSpawnScaling = 1.1f;
    [SerializeField] int baseBudget = 4;
    [SerializeField] float budgetMultiplier = 1.2f;

    [Header("Spawn Settings")]
    public LayerMask spawnBlockingWallMask;
    public LayerMask spawnBlockingEnemyMask;
    public float blockSpawnCheckRadius = 2f;

    [Header("Runtime State")]
    public int currentWave = 0;
    public float waveBudget;
    public List<GameObject> enemiesToSpawn = new List<GameObject>();
    public List<GameObject> activeEnemies = new List<GameObject>();
    public Dictionary<GameObject, float> wallDamageHistory = new Dictionary<GameObject, float>();

    [Header("Events")]
    public UnityEvent OnWaveStarted;
    public UnityEvent<GameObject> OnEnemySpawned;
    public UnityEvent OnWaveCompleted;

    private SpawnPointSpawning sps;
    private WallSpawning ws;
    private LayerMask wallObjectMask; // For prevent walls spawning on each otherd
    private Coroutine waveRoutine;
    private bool waveActive;

    private void OnDisable()
    {
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (!TryGetComponent(out sps))
            sps = gameObject.AddComponent<SpawnPointSpawning>();
        sps.ResetSpawnPoints();
        if (!TryGetComponent(out ws))
            ws = gameObject.AddComponent<WallSpawning>();

        if (spawningDuration > waveDuration)
        {
            Debug.LogWarning("Spawning duration larger than wave duration, clamping to prevent errors");
            spawningDuration = waveDuration;
        }
    }

    public void NextWave()
    {
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);

        currentWave++;
        waveRoutine = StartCoroutine(RunWave());
    }

    private IEnumerator RunWave()
    {
        Debug.Log("Generating Wave...");
        GenerateWave();
        OnWaveStarted?.Invoke();
        Debug.Log($"Starting Wave {currentWave} with budget {waveBudget} spawning {enemiesToSpawn.Count} enemies.");
        waveActive = true;

        WallObjectSettings wall = GenerateWallObject();
        if(wall != null)
        {
            float wallInterval = Mathf.Max(1f, baseWallSpawnInterval - (currentWave * wallSpawnScaling));
            StartWallSpawning(wall.wallPrefab, wallInterval);
        }

        if (!GameManager.Instance.trainMode)
        {
            float spawnInterval = enemiesToSpawn.Count > 0 ? (spawningDuration / enemiesToSpawn.Count) : 0f;
            float elapsed = 0f;

            // Spawn enemies gradually
            while (elapsed < spawningDuration && enemiesToSpawn.Count > 0)
            {
                GameObject enemyPrefab = enemiesToSpawn[0];
                GameObject enemySpawned = sps.SpawnAtRandomSpawnPoint(enemyPrefab);
                enemiesToSpawn.RemoveAt(0);
                if (enemySpawned != null)
                {
                    OnEnemySpawned?.Invoke(enemySpawned);
                    activeEnemies.Add(enemySpawned);
                    if (enemySpawned.TryGetComponent(out HealthSystem health))
                    {
                        System.Action handler = null;
                        handler = () =>
                        {
                            activeEnemies.Remove(enemySpawned);
                            health.OnDie -= handler; // unsubscribe immediately
                            if (activeEnemies.Count == 0 && enemiesToSpawn.Count == 0)
                                EndWaveEarly();
                        };
                        health.OnDie += handler;
                    }
                }

                yield return new WaitForSeconds(spawnInterval);
                elapsed += spawnInterval;
            }
        }
        else
        {
            Debug.Log("Training Mode Active. No enemies will be spawned.");
        }
        // Wait out remaining wave duration
        yield return new WaitForSeconds(waveDuration - spawningDuration);
        StopWallSpawning();
        EndWave();
    }

    private void EndWave()
    {
        if (!waveActive) return;
        waveActive = false;

        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        enemiesToSpawn.Clear();
        OnWaveCompleted?.Invoke();
    }

    private void EndWaveEarly()
    {
        EndWave();
        // Possibly add extra logic for early completion player rewards
    }

    private void GenerateWave()
    {
        waveBudget = (currentWave * budgetMultiplier) * baseBudget;
        enemiesToSpawn = GenerateEnemies();
        ws.Setup();
    }

    private List<GameObject> GenerateEnemies()
    {
        List<GameObject> generated = new List<GameObject>();

        List<Enemy> eligibleEnemies = new List<Enemy>();
        int minCost = int.MaxValue;
        foreach (Enemy e in enemies)
        {
            if (currentWave >= e.minimumWave)
            {
                eligibleEnemies.Add(e);
                if (e.cost < minCost) { minCost = e.cost; }
            }
        }
        if (eligibleEnemies.Count == 0)
        {
            Debug.LogWarning("No eligible enemies for this wave!");
            return generated;
        }


        int safetyCounter = 0;
        while (waveBudget > minCost)
        {
            int randEnemyID = UnityEngine.Random.Range(0, eligibleEnemies.Count);
            Enemy enemy = eligibleEnemies[randEnemyID];
            if (waveBudget - enemy.cost >= 0)
            {
                generated.Add(enemy.enemyPrefab);
                waveBudget -= enemy.cost;
            }
            safetyCounter++;
            if (safetyCounter > 1000)
            {
                Debug.LogWarning("Wave generation safety counter triggered, breaking loop to prevent infinite loop.");
                break;
            }
        }
        return generated;
    }

    WallObjectSettings GenerateWallObject()
    {
        if (wallObjects.Count == 0)
            return null;
        WallObjectSettings selectedWall = null;
        foreach (WallObjectSettings wo in wallObjects)
        {
            if (currentWave >= wo.minimumWave && ( selectedWall == null || wo.minimumWave > selectedWall.minimumWave ))
            {
                selectedWall = wo;
            }
        }
        return selectedWall;
    }

    public void CleanWaves()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();
        enemiesToSpawn.Clear();
        ws.ClearAllWalls();
        XPObject[] xpObjects = FindObjectsByType<XPObject>(FindObjectsSortMode.None);
        foreach (XPObject xpObject in xpObjects) { Destroy(xpObject.gameObject); }
        waveActive = false;
        currentWave = 0;
        waveBudget = 0;
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        sps.ResetSpawnPoints();
    }

    private void StartWallSpawning(GameObject wallPrefab, float interval)
    {
        ws.SpawnOverTime(wallPrefab, interval);
    }
    private void StopWallSpawning()
    {
        ws.StopSpawning();
    }

    public bool CheckWallSpawnBlocked(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, blockSpawnCheckRadius, spawnBlockingWallMask);
    }
    public bool CheckEnemySpawnBlocked(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, blockSpawnCheckRadius, spawnBlockingEnemyMask);
    }
}


[System.Serializable]
public class Enemy
{
    public GameObject enemyPrefab;
    public int cost;
    public int minimumWave;
}
[System.Serializable]
public class WallObjectSettings
{
    public GameObject wallPrefab;
    public int minimumWave;
}
