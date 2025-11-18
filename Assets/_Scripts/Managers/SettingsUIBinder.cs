using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;  
using FMODUnity;
using FMOD.Studio;

public class SettingsUIBinder : MonoBehaviour
{
    private const string MASTER_BUS_PATH = "bus:/";
    private const string MUSIC_BUS_PATH = "bus:/Music";  
    private const string SFX_BUS_PATH = "bus:/SFX";    

    [Header("Audio Sliders (0–1 range!)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Gameplay")]
    [SerializeField] private Toggle trainModeToggle;
    [SerializeField] private Toggle reduceShakeToggle;

    private static SettingsUIBinder instance;  // Singleton for global access

    private void Awake()
    {
        // Singleton pattern: Keep one instance across scenes
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); 

        ConfigureSlider(masterSlider);
        ConfigureSlider(musicSlider);
        ConfigureSlider(sfxSlider);
    }

    private void OnEnable()
    {
        ApplyAudioVolumes();
    }

    private void Start()
    {
        // Hook into scene changes to reapply volumes when MainMenu (or any scene) loads
        SceneManager.sceneLoaded += OnSceneLoaded;

        var sm = SettingsManager.Instance;
        if (sm == null)
        {
            Debug.LogError("SettingsManager instance is missing!");
            return;
        }

        LoadAndBindUI(sm);
        BindListeners(sm);
    }

    private void OnDestroy()
    {
        // Clean up scene hook
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Small delay to let FMOD events start, then reapply
        StartCoroutine(ReapplyOnSceneLoad());
    }

    private System.Collections.IEnumerator ReapplyOnSceneLoad()
    {
        yield return new WaitForEndOfFrame();  // Wait for scene/audio to settle
        ApplyAudioVolumes();
    }

    private void ConfigureSlider(Slider slider)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }
    }

    private void LoadAndBindUI(SettingsManager sm)
    {
        if (masterSlider) masterSlider.value = sm.masterVolume;
        if (musicSlider) musicSlider.value = sm.musicVolume;
        if (sfxSlider) sfxSlider.value = sm.sfxVolume;

        if (fullscreenToggle) fullscreenToggle.isOn = sm.fullscreen;
        if (trainModeToggle) trainModeToggle.isOn = sm.trainMode;
        if (reduceShakeToggle) reduceShakeToggle.isOn = sm.reduceShake;
    }

    private void BindListeners(SettingsManager sm)
    {
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => { sm.masterVolume = v; ApplyAudioVolumes(); });
        if (musicSlider) musicSlider.onValueChanged.AddListener(v => { sm.musicVolume = v; ApplyAudioVolumes(); });
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v => { sm.sfxVolume = v; ApplyAudioVolumes(); });

        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(v => { sm.fullscreen = v; ApplyNonAudioSettings(); });
        if (trainModeToggle) trainModeToggle.onValueChanged.AddListener(v => { sm.trainMode = v; ApplyNonAudioSettings(); });
        if (reduceShakeToggle) reduceShakeToggle.onValueChanged.AddListener(v => { sm.reduceShake = v; ApplyNonAudioSettings(); });
    }

    private void ApplyAudioVolumes()
    {
        if (SettingsManager.Instance == null) return;  // Safety

        var sm = SettingsManager.Instance;

        // Always re-fetch and set (works globally)
        RuntimeManager.GetBus(MASTER_BUS_PATH).setVolume(sm.masterVolume);
        RuntimeManager.GetBus(MUSIC_BUS_PATH).setVolume(sm.musicVolume);
        RuntimeManager.GetBus(SFX_BUS_PATH).setVolume(sm.sfxVolume);

        Debug.Log($"🔊 Volumes applied: Master={sm.masterVolume}, Music={sm.musicVolume}, SFX={sm.sfxVolume}");  
    }

    private void ApplyNonAudioSettings()
    {
        SettingsManager.Instance.ApplySettings();
        SettingsManager.Instance.SaveSettings();
    }

    public void ApplyChanges()
    {
        ApplyAudioVolumes();
        ApplyNonAudioSettings();
    }

    public void CancelChanges()
    {
        SettingsManager.Instance.LoadSettings();
        SettingsManager.Instance.ApplySettings();

        LoadAndBindUI(SettingsManager.Instance);
        ApplyAudioVolumes();
    }

    // Optional test
    [ContextMenu("TEST: Mute → Unmute Music (2 sec)")]
    private void TestMuteMusic()
    {
        RuntimeManager.GetBus(MUSIC_BUS_PATH).setVolume(0f);
        Invoke(nameof(UnmuteMusic), 2f);
    }
    private void UnmuteMusic() => RuntimeManager.GetBus(MUSIC_BUS_PATH).setVolume(1f);
}