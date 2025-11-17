using UnityEngine;
using System.Collections;

public class MenuMusicController : MonoBehaviour
{
    public static MenuMusicController Instance;

    [Header("FMOD Music Events")]
    [SerializeField] private FMODUnity.EventReference MenuMusic;
    [SerializeField] private FMODUnity.EventReference GameMusic;

    private FMOD.Studio.EventInstance menuInstance;
    private FMOD.Studio.EventInstance gameInstance;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f; // seconds

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    private void StopInstance(FMOD.Studio.EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
        }
    }

    // ==================================
    // Public Play Methods with Crossfade
    // ==================================

    public void PlayMenuMusic()
    {
        StartCoroutine(FadeToMenuMusic());
    }

    public void PlayGameMusic()
    {
        StartCoroutine(FadeToGameMusic());
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(FadeToMenuMusic());
    }

    // ==================================
    // Coroutine Crossfade Logic
    // ==================================

    private IEnumerator FadeToMenuMusic()
    {
        // Fade out game music
        if (gameInstance.isValid())
        {
            yield return StartCoroutine(FadeOut(gameInstance));
            StopInstance(gameInstance);
        }

        // Start menu music if not already
        if (!menuInstance.isValid())
        {
            menuInstance = FMODUnity.RuntimeManager.CreateInstance(MenuMusic);
            menuInstance.start();
        }

        // Fade in menu music
        yield return StartCoroutine(FadeIn(menuInstance));
    }

    private IEnumerator FadeToGameMusic()
    {
        // Fade out menu music
        if (menuInstance.isValid())
        {
            yield return StartCoroutine(FadeOut(menuInstance));
            StopInstance(menuInstance);
        }

        // Start game music if not already
        if (!gameInstance.isValid())
        {
            gameInstance = FMODUnity.RuntimeManager.CreateInstance(GameMusic);
            gameInstance.start();
        }

        // Fade in game music
        yield return StartCoroutine(FadeIn(gameInstance));
    }

    // ==================================
    // Generic Fade Coroutines
    // ==================================

    private IEnumerator FadeOut(FMOD.Studio.EventInstance instance)
    {
        float currentVolume;
        instance.getVolume(out currentVolume);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float newVolume = Mathf.Lerp(currentVolume, 0f, elapsed / fadeDuration);
            instance.setVolume(newVolume);
            yield return null;
        }

        instance.setVolume(0f);
    }

    private IEnumerator FadeIn(FMOD.Studio.EventInstance instance)
    {
        float elapsed = 0f;
        instance.setVolume(0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float newVolume = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            instance.setVolume(newVolume);
            yield return null;
        }

        instance.setVolume(1f);
    }
}
