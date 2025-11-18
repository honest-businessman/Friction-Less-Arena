using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // ==== General Settings ====
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public bool fullscreen = true;
    public bool trainMode = false;
    public bool reduceShake = false;

    public event Action OnSettingsApplied;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
        ApplySettings();
    }

    // ==== Save/Load ====
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("TrainMode", trainMode ? 1 : 0);
        PlayerPrefs.SetInt("ReduceShake", reduceShake ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Settings saved.");
    }

    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        trainMode = PlayerPrefs.GetInt("TrainMode", 0) == 1;
        reduceShake = PlayerPrefs.GetInt("ReduceShake", 0) == 1;
        Debug.Log("Settings loaded.");
    }

    // ==== Apply ====
    public void ApplySettings()
    {
        AudioListener.volume = masterVolume;

        /*if(AudioManger.Instance != null)
        {
            AudioManger.Instance.SetMusicVolume(musicVolume);
            AudioManger.Instance.SetSFXVolume(sfxVolume);
        }*/


        Screen.fullScreen = fullscreen;

        if(GameManager.Instance != null)
        {
            GameManager.Instance.trainMode = trainMode;
        }
        if(CameraAnimationManager.Instance != null)
        {
           CameraAnimationManager.Instance.reduceShake = reduceShake;
        }

        OnSettingsApplied?.Invoke();
    }

    void OnApplicationQuit() => SaveSettings();
}
