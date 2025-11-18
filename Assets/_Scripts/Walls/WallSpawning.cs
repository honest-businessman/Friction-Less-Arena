using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class WallSpawning : MonoBehaviour
{
    private Coroutine spawnCoroutine;
    private Tilemap tilemap;
    private List<Vector3Int> allTiles = new List<Vector3Int>();
    private HashSet<Vector3Int> usedTiles = new HashSet<Vector3Int>();

    public void Setup()
    {
        GameObject wallGen = GameObject.FindGameObjectWithTag("WallGen");
        if (wallGen == null)
        {
            Debug.LogError("No Tilemap found with tag 'WallGen'!");
            return;
        }

        tilemap = wallGen.GetComponent<Tilemap>();
        allTiles.Clear();

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
                allTiles.Add(pos);
        }

        if (allTiles.Count == 0)
            Debug.LogWarning("WallSpawning: No tiles found in tilemap!");
    }

    public void SpawnOverTime(GameObject wallPrefab, float interval)
    {
        // Always stop previous first
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnRoutine(wallPrefab, interval));
    }

    private IEnumerator SpawnRoutine(GameObject wallPrefab, float interval)
    {
        WaitForSeconds delay = new WaitForSeconds(interval);

        while (true) // Simple infinite loop — we control stopping from outside
        {
            if (allTiles.Count == 0 || usedTiles.Count >= allTiles.Count)
            {
                yield return delay;
                continue;
            }

            int attempts = 0;
            bool spawnedThisTick = false;

            while (attempts < 20 && !spawnedThisTick)
            {
                attempts++;
                Vector3Int startTile = allTiles[Random.Range(0, allTiles.Count)];

                if (usedTiles.Contains(startTile))
                    continue;

                List<Vector3Int> connectedTiles = GetConnectedTiles(startTile);

                bool blocked = false;
                List<Vector3Int> tilesToUse = new List<Vector3Int>();

                foreach (var tilePos in connectedTiles)
                {
                    if (usedTiles.Contains(tilePos))
                    {
                        blocked = true;
                        break;
                    }

                    Vector3 worldPos = tilemap.CellToWorld(tilePos) + tilemap.tileAnchor;
                    if (WaveManager.Instance != null && WaveManager.Instance.CheckWallSpawnBlocked(worldPos))
                    {
                        blocked = true;
                        break;
                    }

                    tilesToUse.Add(tilePos);
                }

                if (blocked)
                    continue;

                // SUCCESS — spawn the wall segment
                foreach (var tilePos in tilesToUse)
                {
                    usedTiles.Add(tilePos);
                    Vector3 worldPos = tilemap.CellToWorld(tilePos) + tilemap.tileAnchor;
                    GameObject wallObject = Instantiate(wallPrefab, worldPos, Quaternion.identity);
                    wallObject.transform.SetParent(transform);

                    if (wallObject.TryGetComponent<WallObject>(out var wallScript))
                    {
                        wallScript.Initialize(this, tilePos);
                    }
                }

                spawnedThisTick = true;
            }

            // CRITICAL: Yield here every spawn cycle
            yield return delay;
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public void ClearAllWalls()
    {
        StopSpawning();

        foreach (Transform child in transform)
        {
            if (child != null && child.GetComponent<WallObject>() != null)
                Destroy(child.gameObject);
        }

        usedTiles.Clear();
    }

    private List<Vector3Int> GetConnectedTiles(Vector3Int start)
    {
        List<Vector3Int> connected = new List<Vector3Int>();
        Queue<Vector3Int> toCheck = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        toCheck.Enqueue(start);
        visited.Add(start);

        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (toCheck.Count > 0)
        {
            Vector3Int current = toCheck.Dequeue();
            connected.Add(current);

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;
                if (allTiles.Contains(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    toCheck.Enqueue(next);
                }
            }
        }

        return connected;
    }

    public void FreeTile(Vector3Int tilePos)
    {
        usedTiles.Remove(tilePos);
    }

    public List<WallObject> GetAllWalls()
    {
        List<WallObject> walls = new List<WallObject>();

        foreach (Transform child in transform)
        {
            if (child != null && child.TryGetComponent<WallObject>(out var wall))
            {
                walls.Add(wall);
            }
        }

        return walls;
    }


}