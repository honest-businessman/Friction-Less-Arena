using UnityEngine;

public class MenuMusicController : MonoBehaviour
{
    [SerializeField] private FMODUnity.EventReference MenuMusic;
    [SerializeField] private FMODUnity.EventReference GameMusic;

    private FMOD.Studio.EventInstance menuInstance;
    private FMOD.Studio.EventInstance gameInstance;

    void Start()
    {
        // Start main menu music when the scene loads
        menuInstance = FMODUnity.RuntimeManager.CreateInstance(MenuMusic);
        menuInstance.start();
    }

    public void PlayGameMusic()
    {
        // Stop menu music
        if (menuInstance.isValid())
        {
            menuInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            menuInstance.release();
        }

        // Start game music
        gameInstance = FMODUnity.RuntimeManager.CreateInstance(GameMusic);
        gameInstance.start();
    }
}
