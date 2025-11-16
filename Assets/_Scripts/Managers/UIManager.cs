using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Canvas uiCanvas;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject pausePanel;
    public GameObject upgradePanel;

    private UpgradeUI upgradeUI;
    private PauseUI pauseUI;
    private bool isSettingsOpen = false;
    private Action<UpgradeItem[]> upgradeAvailableHandler;
    private Action<UpgradeItem> upgradeSelectedHandler;

    public UIFocus currentFocus = UIFocus.None;
    public enum UIFocus
    {
        None,
        MainMenu,
        Settings,
        Pause,
        Upgrades
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        uiCamera = GameObject.FindWithTag("2D Camera").GetComponent<Camera>();
        pauseUI = pausePanel.GetComponent<PauseUI>();
        upgradeUI = upgradePanel.GetComponent<UpgradeUI>();
    }

    private void OnEnable()
    {
        upgradeAvailableHandler = (uItems) =>
        {
            GameManager.Instance.Player.GetComponent<PlayerController>().CancelDrift(); // Cancel drift when upgrade UI shows up, prevents held drift after resuming.
            Time.timeScale = 0f;
            upgradeUI.gameObject.SetActive(true);
            upgradeUI.ShowUpgradeOptions(uItems);
            currentFocus = UIFocus.Upgrades;
            InputManager.Instance.EnableUIInput();
        };
        UpgradeEvents.OnUpgradesAvailable += upgradeAvailableHandler;
        upgradeSelectedHandler = (uItem) =>
        {
            currentFocus = UIFocus.None;
            InputManager.Instance.EnablePlayerInput(GameManager.Instance.Player.GetComponent<PlayerController>());
            upgradeUI.gameObject.SetActive(false);
            Time.timeScale = 1f;
        };
        UpgradeEvents.OnUpgradeSelected += upgradeSelectedHandler;
    }

    private void OnDisable()
    {
        UpgradeEvents.OnUpgradesAvailable -= upgradeAvailableHandler;
        UpgradeEvents.OnUpgradeSelected -= upgradeSelectedHandler;
    }

    public void HandlePause()
    {
        Debug.Log("Pause Toggled");
        if (GameManager.Instance.CurrentState != GameManager.GameState.InGame)
            return;

        // If pause menu is already open
        if (currentFocus == UIFocus.Pause)
        {
            // Hitting pause with settings open returns to pause menu
            if (currentFocus == UIFocus.Settings)
            {
                ShowPauseMenu();
                RecalculateFocus();
                return;
            }

            // Otherwise leaving pause menu returns to Upgrades or None
            HideAll();
            InputManager.Instance.EnablePlayerInput(GameManager.Instance.Player.GetComponent<PlayerController>());
            Time.timeScale = 1f;

            RecalculateFocus();
            GameEvents.ResumedGame();
            return;
        }

        // In Game Pause Logic
        ShowPauseMenu();
        InputManager.Instance.EnableUIInput();
        Time.timeScale = 0f;
        RecalculateFocus();
        GameEvents.PausedGame();
    }


    public void HandleNavigate(Vector2 direction)
    {
        Debug.Log($"Trying to navigate with focus {currentFocus.ToString()}");
        if (pauseUI.isActiveAndEnabled && currentFocus == UIFocus.Pause) { pauseUI.HandleNavigate(direction); }
        else if (upgradeUI.isActiveAndEnabled && currentFocus == UIFocus.Upgrades) { upgradeUI.HandleNavigate(direction); }
    }

    public void HandleSubmit()
    {
        if (pauseUI.isActiveAndEnabled && currentFocus == UIFocus.Pause) { pauseUI.HandleSubmit(); }
        else if (upgradeUI.isActiveAndEnabled && currentFocus == UIFocus.Upgrades) { upgradeUI.HandleSubmit(); }

    }


    public void SetRenderTextureMode(RenderTexture target)
    {
        uiCamera.targetTexture = target;
        uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        uiCanvas.worldCamera = uiCamera;
        uiCanvas.planeDistance = 2;
        uiCanvas.sortingOrder = 20;
    }

    public void ShowMainMenu()
    {
        HideAll();
        mainMenuPanel.SetActive(true);
        currentFocus = UIFocus.MainMenu;
    }
    public void ShowSettingsMenu()
    {
        HideAll();
        settingsPanel.SetActive(true);
        isSettingsOpen = true;
        currentFocus = UIFocus.Settings;
    }
    public void ShowPauseMenu()
    {
        HideAll();
        pausePanel.SetActive(true);
        currentFocus = UIFocus.Pause;
    }

    public void HideAll()
    {
        mainMenuPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        isSettingsOpen = false;
        currentFocus = UIFocus.None;
    }

    public Canvas GetUICanvas() { return uiCanvas; }
    public void RecalculateFocus()
    {
        // Priority order (highest -> lowest):
        // 1. Settings
        // 2. Pause
        // 3. Upgrades
        // 4. Main Menu
        // 5. None

        // SETTINGS OPEN
        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            currentFocus = UIFocus.Settings;
            return;
        }

        // PAUSE MENU OPEN
        if (pausePanel != null && pausePanel.activeInHierarchy)
        {
            currentFocus = UIFocus.Pause;
            return;
        }

        // UPGRADE UI OPEN
        if (upgradeUI != null && upgradeUI.isActiveAndEnabled)
        {
            currentFocus = UIFocus.Upgrades;
            return;
        }

        // MAIN MENU OPEN
        if (mainMenuPanel != null && mainMenuPanel.activeInHierarchy)
        {
            currentFocus = UIFocus.MainMenu;
            return;
        }

        // DEFAULT
        currentFocus = UIFocus.None;
    }

}
