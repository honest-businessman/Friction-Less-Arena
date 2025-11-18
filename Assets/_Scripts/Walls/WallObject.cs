using UnityEngine;
using FMODUnity;

public class WallObject : MonoBehaviour
{
    protected Vector3Int tilePos;
    protected WallSpawning spawner;

    [Header("FMOD Events")]
    [SerializeField] private EventReference wallDestroySFX; // Assign your wall death sound

    public void Initialize(WallSpawning wallSpawner, Vector3Int tilePosition)
    {
        spawner = wallSpawner;
        tilePos = tilePosition;
    }

    protected virtual void OnDestroy()
    {
        // Play FMOD sound when wall is destroyed
        if (!wallDestroySFX.IsNull)
        {
            RuntimeManager.PlayOneShot(wallDestroySFX, transform.position);
        }

        // Free tile when destroyed
        if (spawner != null)
        {
            spawner.FreeTile(tilePos);
        }
    }
}
