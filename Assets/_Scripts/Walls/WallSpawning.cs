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
    private bool spawningFlag = false;

    public void Setup()
    {
        GameObject wallGen = GameObject.FindGameObjectWithTag("WallGen");
        if (wallGen == null)
        {
            Debug.LogError("No Tilemap found with tag 'WallGen'!");
            return;
        }

        tilemap = wallGen.GetComponent<Tilemap>();

        // Store all valid tile positions
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
                allTiles.Add(pos);
        }
    }

    public void SpawnOverTime(GameObject wallPrefab, float interval)
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnOverTimeCoroutine(wallPrefab, interval));
    }

    private IEnumerator SpawnOverTimeCoroutine(GameObject wallPrefab, float interval)
    {
        spawningFlag = true;

        while (spawningFlag)
        {
            bool trySpawn = true;
            int safetyCounter = 0;

            while (trySpawn && safetyCounter < 20) // avoid infinite loop
            {
                safetyCounter++;
                trySpawn = false;

                Vector3Int startTile = allTiles[Random.Range(0, allTiles.Count)];
                if (startTile == Vector3Int.zero)
                    yield break;

                if (usedTiles.Contains(startTile))
                    continue;

                List<Vector3Int> connectedTiles = GetConnectedTiles(startTile);

                foreach (var tilePos in connectedTiles)
                {
                    // skip if already used
                    if (usedTiles.Contains(tilePos))
                        continue;

                    Vector3 worldPos = tilemap.CellToWorld(tilePos) + tilemap.tileAnchor;

                    if (WaveManager.Instance != null && WaveManager.Instance.CheckWallSpawnBlocked(worldPos))
                    {
                        trySpawn = true;
                        break;
                    }

                    usedTiles.Add(tilePos);

                    GameObject wallObject = Instantiate(wallPrefab, worldPos, Quaternion.identity);
                    wallObject.transform.SetParent(transform);

                    // give wall reference to spawner and tile position
                    WallObject wallScript = wallObject.GetComponent<WallObject>();
                    if (wallScript != null)
                    {
                        wallScript.Initialize(this, tilePos);
                    }
                }
            }
        }
        yield return new WaitForSeconds(interval);
    }

    public void StopSpawning()
    {
        spawningFlag = false;
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }

    public void ClearAllWalls()
    {
        StopSpawning();

        foreach (Transform child in transform)
        {
            if (child.GetComponent<WallObject>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        usedTiles.Clear();
    }

    // Flood fill to get all connected tiles in all directions
    private List<Vector3Int> GetConnectedTiles(Vector3Int start)
    {
        List<Vector3Int> connected = new List<Vector3Int>();
        Queue<Vector3Int> toCheck = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        toCheck.Enqueue(start);
        visited.Add(start);

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

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
        if (usedTiles.Contains(tilePos))
            usedTiles.Remove(tilePos);
    }
}
