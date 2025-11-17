using FMODUnity;
using UnityEngine;

public class FMODBankLoader : MonoBehaviour
{
    void Awake()
    {
        // These two lines fix literally 95 % of "FMOD volume sliders do nothing" bugs in 2025
        RuntimeManager.LoadBank("Master.strings", true);
        RuntimeManager.LoadBank("Master", true);

        // Optional: load the rest of your banks here too
        // RuntimeManager.LoadBank("Music");
        // RuntimeManager.LoadBank("SFX");
        // etc.
    }
}