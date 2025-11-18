using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
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

    //FMOD
    [Header("FMOD")]
    [SerializeField] private FMODUnity.EventReference waveStartSFX;


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
    public UnityEvent OnWaveStarted = new UnityEvent();
    public UnityEvent<GameObject> OnEnemySpawned = new UnityEvent<GameObject>();
    public UnityEvent OnWaveCompleted = new UnityEvent();

    private SpawnPointSpawning sps;
    private WallSpawning ws;
    private Coroutine waveRoutine;
    private bool waveActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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

    private void OnDestroy()
    {
        // Prevent memory leaks from persistent listeners
        OnWaveStarted.RemoveAllListeners();
        OnEnemySpawned.RemoveAllListeners();
        OnWaveCompleted.RemoveAllListeners();

        // Stop any running wave
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
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
        OnWaveStarted.Invoke();


        Debug.Log($"Starting Wave {currentWave} with budget {waveBudget} spawning {enemiesToSpawn.Count} enemies.");
        waveActive = true;

        WallObjectSettings wall = GenerateWallObject();
        if (wall != null)
        {
            float wallInterval = Mathf.Max(1f, baseWallSpawnInterval - (currentWave * wallSpawnScaling));
            StartWallSpawning(wall.wallPrefab, wallInterval);
        }

        if (!GameManager.Instance.trainMode)
        {
            float spawnInterval = enemiesToSpawn.Count > 0 ? (spawningDuration / enemiesToSpawn.Count) : 0f;
            float elapsed = 0f;

            while (elapsed < spawningDuration && enemiesToSpawn.Count > 0)
            {
                GameObject enemyPrefab = enemiesToSpawn[0];
                enemiesToSpawn.RemoveAt(0);

                GameObject enemySpawned = sps.SpawnAtRandomSpawnPoint(enemyPrefab);
                if (enemySpawned != null)
                {
                    OnEnemySpawned.Invoke(enemySpawned);
                    activeEnemies.Add(enemySpawned);

                    // Use WeakReference to avoid holding strong refs to the enemy GameObject
                    var enemyWeakRef = new WeakReference<GameObject>(enemySpawned);

                    if (enemySpawned.TryGetComponent(out HealthSystem health))
                    {
                        // Must store the delegate so it can be removed later
                        Action deathHandler = null;
                        deathHandler = () =>
                        {
                            // unsubscribe this handler (prevents leaks / double-calls)
                            health.OnDie -= deathHandler;
                            OnEnemyDied(enemyWeakRef, health);
                        };

                        health.OnDie += deathHandler;
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

        // Wait out remaining wave time
        float remainingTime = waveDuration - spawningDuration;
        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        StopWallSpawning();
        EndWave();
    }

    // Extracted method to handle enemy death safely
    private void OnEnemyDied(WeakReference<GameObject> enemyWeakRef, HealthSystem health)
    {
        if (enemyWeakRef.TryGetTarget(out GameObject enemy) && enemy != null)
        {
            activeEnemies.Remove(enemy);
        }

        // NOTE: the handler unsubscribes itself when it runs, so we don't try to remove it here.

        if (activeEnemies.Count == 0 && enemiesToSpawn.Count == 0)
        {
            EndWaveEarly();
        }
    }

    private void PlayWaveSFX()
    {
        if (!waveStartSFX.IsNull)
            FMODUnity.RuntimeManager.PlayOneShot(waveStartSFX);
    }
    private void EndWave()
    {
        if (!waveActive) return;

        waveActive = false;
        waveRoutine = null;

        enemiesToSpawn.Clear();
        OnWaveCompleted.Invoke();
        PlayWaveSFX();
    }

    private void EndWaveEarly()
    {
        EndWave();
        // Optional: Add bonus XP or reward here
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
                if (e.cost < minCost) minCost = e.cost;
            }
        }

        if (eligibleEnemies.Count == 0)
        {
            Debug.LogWarning("No eligible enemies for this wave!");
            return generated;
        }

        int safetyCounter = 0;
        while (waveBudget >= minCost && safetyCounter < 1000)
        {
            Enemy enemy = eligibleEnemies[UnityEngine.Random.Range(0, eligibleEnemies.Count)];
            if (waveBudget >= enemy.cost)
            {
                generated.Add(enemy.enemyPrefab);
                waveBudget -= enemy.cost;
            }
            safetyCounter++;
        }

        if (safetyCounter >= 1000)
            Debug.LogWarning("Wave generation safety counter triggered.");

        return generated;
    }

    private WallObjectSettings GenerateWallObject()
    {
        WallObjectSettings selected = null;
        foreach (WallObjectSettings wo in wallObjects)
        {
            if (currentWave >= wo.minimumWave &&
                (selected == null || wo.minimumWave > selected.minimumWave))
            {
                selected = wo;
            }
        }
        return selected;
    }

    public void CleanWaves()
    {
        // Safely destroy active enemies and clear list
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];
            if (enemy != null)
            {
                if (enemy.TryGetComponent<HealthSystem>(out var health))
                {
                    // Properly clear all OnDie subscribers via HealthSystem method
                    health.ClearOnDie();
                }
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
        enemiesToSpawn.Clear();

        ws.ClearAllWalls();
        wallDamageHistory.Clear();

        XPObject[] xpObjects = FindObjectsByType<XPObject>(FindObjectsSortMode.None);
        foreach (XPObject xp in xpObjects)
        {
            if (xp != null)
                Destroy(xp.gameObject);
        }

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
        ws.StopSpawning();
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
