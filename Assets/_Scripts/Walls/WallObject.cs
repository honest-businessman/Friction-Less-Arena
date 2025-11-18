using UnityEngine;
using FMODUnity;

public class WallObject : MonoBehaviour
{
    protected Vector3Int tilePos;
    protected WallSpawning spawner;

    [Header("FMOD Events")]
    [SerializeField] private EventReference wallDestroySFX; // Assign your wall death sound

    // Flag to prevent SFX during mass cleanup
    public bool suppressSFX = false; // set true when cleaning waves

    public void Initialize(WallSpawning wallSpawner, Vector3Int tilePosition)
    {
        spawner = wallSpawner;
        tilePos = tilePosition;
    }


    protected virtual void OnDestroy()
    {
        if (!suppressSFX && !wallDestroySFX.IsNull)
        {
            RuntimeManager.PlayOneShot(wallDestroySFX, transform.position);
        }

        if (spawner != null)
        {
            spawner.FreeTile(tilePos);
        }
    }

}
