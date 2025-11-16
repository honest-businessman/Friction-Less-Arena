using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum GameState
    {
        MainMenu,
        InGame,
        Paused,
        GameOver
    }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("Behaviour")]
    [SerializeField] private float restartDelay = 3f;
    [SerializeField] private float waveDelay = 3f;

    [Header("2D Scene Setup")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private RenderTexture uiRenderTexture;
    [SerializeField] private Vector3 gameSceneOffset = new Vector3(10, 0, 0);

    [Header("Settings - Handled by Settings Manager")]
    public bool trainMode = false;

    public bool isMainScene;
    private GameObject player;
    public GameObject Player { get; private set; }

    private InputManager inputManager;
    private bool isUiSceneLoaded = false;
    private WaveManager waveManager;
    private string mainMenuSceneName = "MainMenu";
    private string gameSceneName = "FrictionLess";
    private string uiSceneName = "UIScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            isMainScene = true;
            StartMainMenu();
        }
        else if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            isMainScene = false;
            StartGame();
        }
        else
        {
            Debug.LogWarning("GameManager started in an unexpected scene.");
        }
    }

    public void StartMainMenu()
    {
        Time.timeScale = 1f;
        CleanupPlayer();
        CurrentState = GameState.MainMenu;
        if (!SceneManager.GetSceneByName(uiSceneName).isLoaded)  // Loads UI Scene
        { 
            isUiSceneLoaded = true;
            StartCoroutine(LoadSceneAsync(uiSceneName, LoadSceneMode.Additive, () =>
            {
                UIManager.Instance.SetRenderTextureMode(uiRenderTexture);
                UIManager.Instance.ShowMainMenu();
                ScreenManager.Instance.AssignUI();
                Debug.Log("UI Scene Loaded.");
                InputManager.Instance.EnableUIInput();
            }));
        }
        else { InputManager.Instance.EnableUIInput(); }
    }

    public IEnumerator EnterGame()
    {
        yield return LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
        GameEvents.OnGameStarted?.Invoke();
        StartGame();
    }

    public IEnumerator LoadGame()
    {
        yield return LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
    }

    public IEnumerator EnterMainMenu()
    {
        yield return LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);
    }

    public void StartGame()
    {
        if (CurrentState == GameState.InGame)
        {
            return;
        }
        CurrentState = GameState.InGame;
        if (!SceneManager.GetSceneByName(uiSceneName).isLoaded)
        {
            StartCoroutine(LoadSceneAsync(uiSceneName, LoadSceneMode.Additive, () =>
            {
                UIManager.Instance.HideAll();
            }));
        }

        else { UIManager.Instance.HideAll(); }
        Time.timeScale = 1f;
        CleanupPlayer();
        SpawnPlayer();
        InitializeInGameManagers();

        InputManager.Instance.EnablePlayerInput(Player.GetComponent<PlayerController>());
        StartCoroutine(WaveLoop());
    }

    private void CleanupPlayer()
    {
        if (Player != null)
        {
            HealthSystem healthSys = Player.GetComponent<HealthSystem>();
            if (healthSys != null)
                healthSys.OnDie -= HandlePlayerDeath;

            Destroy(Player);
            Player = null;
        }
    }

private void SpawnPlayer()
{
    Player = GameObject.FindWithTag("Player");

    if (Player != null)
    {
        Debug.Log("Player already exists in the scene.");
    }
    else
        {
            Vector3[] spawnPositionArray = GameObject.FindGameObjectsWithTag("Spawnpoint Player")
             .Select(sp => sp.transform.position)
             .ToArray();
            Vector3 spawnposition = spawnPositionArray[UnityEngine.Random.Range(0, spawnPositionArray.Length)];
            Player = Instantiate(playerPrefab, spawnposition, Quaternion.identity);
            Debug.Log("Player spawned.");
        }

        PlayerEvents.OnPlayerSpawned?.Invoke(Player);
        HealthSystem playerHealthSys = Player.GetComponent<HealthSystem>();
        playerHealthSys.OnDie += HandlePlayerDeath;
    }

    private void InitializeInGameManagers()
    {
        inputManager = GetComponentInChildren<InputManager>();

        waveManager = GetComponentInChildren<WaveManager>();
        waveManager.CleanWaves();


    }

    private IEnumerator WaveLoop()
    {
        Debug.Log("Starting Wave Loop...");
        bool waveDone = false;
        waveManager.OnWaveCompleted.AddListener(() => waveDone = true);
        while (true)
        {
            waveDone = false;
            yield return new WaitForSeconds(waveDelay);
            waveManager.NextWave();
            Debug.Log("Wave Loop Waiting for Next Wave...");
            yield return new WaitUntil(() => waveDone);
            Debug.Log($"Wave {waveManager.currentWave} completed.");
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("Player Died. Game Over!");
        RestartGame();
    }

    public void RestartGame()
    {
        Time.timeScale = 1.0f;
        CurrentState = GameState.GameOver;
        CleanupPlayer();
        StartCoroutine(RestartAfterDelay(0f));
    }
    public void EndGame()
    {
        Time.timeScale = 1.0f;
        inputManager.DisableUIInput();
        CurrentState = GameState.MainMenu;
        UIManager.Instance.HideAll();
        CleanupPlayer();
        waveManager.CleanWaves();
        if (isMainScene) 
        {
            SceneManager.UnloadSceneAsync(gameSceneName);
            ScreenManager.Instance.ExitToMenu();
            CameraAnimationManager.Instance.TransitionToMainMenu();
            inputManager.EnableUIInput();
        }
        else 
        {
            SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
        }
        
    }

    private IEnumerator RestartAfterDelay(float restartDelay)
    {
        Debug.Log($"Restarting in {restartDelay} seconds...");
        yield return new WaitForSeconds(restartDelay);
        // asynchronously reload the current scene
        waveManager.CleanWaves();
        yield return SceneManager.UnloadSceneAsync(1);
        yield return LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);

        // Once Loaded, restart the game
        GameEvents.OnGameRestarted?.Invoke();
        StartGame();
    }

    private IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, Action onComplete = null)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

        // Wait until scene is loaded to 90% (internal ready)
        while (!asyncLoad.isDone)
        {
            yield return null; // animations continue running
        }

        onComplete?.Invoke();
    }
}
