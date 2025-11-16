using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class TankShotAudio : MonoBehaviour
{
    [Header("FMOD Events")]
    [SerializeField] private EventReference tankShotEvent; // Drag TankShot1 event here in Inspector

    private EventInstance shotInstance;

    private void Start()
    {
        if (tankShotEvent.IsNull)
            Debug.LogWarning("TankShot1 Event is not assigned in the Inspector.");
    }

    public void PlayShot(Vector3 position)
    {
        RuntimeManager.PlayOneShot(tankShotEvent, position);
    }
}
