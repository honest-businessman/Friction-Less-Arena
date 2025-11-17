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

        // Ensure components are found even if inactive
        pauseUI = pausePanel.GetComponentInChildren<PauseUI>(true);
        upgradeUI = upgradePanel.GetComponentInChildren<UpgradeUI>(true);
    }

    private void Start()
    {
        // Keep all panels active in hierarchy but hide as needed
        mainMenuPanel.SetActive(true);   // or false if you don't want main menu immediately
        settingsPanel.SetActive(false);
        pausePanel.SetActive(false);
        upgradePanel.SetActive(true);
    }

    private void OnEnable()
    {
        // Upgrade available
        upgradeAvailableHandler = (uItems) =>
        {
            ForceShowUpgradeUI(uItems);
        };
        UpgradeEvents.OnUpgradesAvailable += upgradeAvailableHandler;

        // Upgrade selected
        upgradeSelectedHandler = (uItem) =>
        {
            currentFocus = UIFocus.None;
            InputManager.Instance.EnablePlayerInput(GameManager.Instance.Player.GetComponent<PlayerController>());
            upgradePanel.SetActive(false);
            Time.timeScale = 1f;
        };
        UpgradeEvents.OnUpgradeSelected += upgradeSelectedHandler;
    }

    private void OnDisable()
    {
        UpgradeEvents.OnUpgradesAvailable -= upgradeAvailableHandler;
        UpgradeEvents.OnUpgradeSelected -= upgradeSelectedHandler;
    }

    // ==============================
    // Upgrade UI Forced Show Method
    // ==============================
    public void ForceShowUpgradeUI(UpgradeItem[] items)
    {
        GameManager.Instance.Player.GetComponent<PlayerController>().CancelDrift();
        Time.timeScale = 0f;

        // Force canvas active and correct render
        uiCanvas.gameObject.SetActive(true);
        uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        uiCanvas.worldCamera = uiCamera;
        uiCanvas.planeDistance = 2;
        uiCanvas.sortingOrder = 999; // very high to render above everything

        // Force upgrade panel active and top of hierarchy
        upgradePanel.SetActive(true);
        upgradePanel.transform.SetAsLastSibling();
        upgradeUI.ShowUpgradeOptions(items);

        currentFocus = UIFocus.Upgrades;
        InputManager.Instance.EnableUIInput();

        // Optionally hide other panels
        mainMenuPanel.SetActive(false);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    // ==============================
    // Pause / Settings / Main Menu
    // ==============================
    public void HandlePause()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.InGame)
            return;

        if (currentFocus == UIFocus.Pause)
        {
            HideAll();
            InputManager.Instance.EnablePlayerInput(GameManager.Instance.Player.GetComponent<PlayerController>());
            Time.timeScale = 1f;
            RecalculateFocus();
            GameEvents.ResumedGame();
            return;
        }

        ShowPauseMenu();
        InputManager.Instance.EnableUIInput();
        Time.timeScale = 0f;
        RecalculateFocus();
        GameEvents.PausedGame();
    }

    public void HandleNavigate(Vector2 direction)
    {
        if (pauseUI.isActiveAndEnabled && currentFocus == UIFocus.Pause)
            pauseUI.HandleNavigate(direction);
        else if (upgradeUI.isActiveAndEnabled && currentFocus == UIFocus.Upgrades)
            upgradeUI.HandleNavigate(direction);
    }

    public void HandleSubmit()
    {
        if (pauseUI.isActiveAndEnabled && currentFocus == UIFocus.Pause)
            pauseUI.HandleSubmit();
        else if (upgradeUI.isActiveAndEnabled && currentFocus == UIFocus.Upgrades)
            upgradeUI.HandleSubmit();
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

        MenuMusicController.Instance.ReturnToMainMenu();
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
        upgradePanel?.SetActive(false);

        isSettingsOpen = false;
        currentFocus = UIFocus.None;
    }

    public Canvas GetUICanvas() { return uiCanvas; }

    public void RecalculateFocus()
    {
        // Priority: Settings > Pause > Upgrades > Main Menu > None
        if (settingsPanel.activeInHierarchy)
        {
            currentFocus = UIFocus.Settings;
            return;
        }
        if (pausePanel.activeInHierarchy)
        {
            currentFocus = UIFocus.Pause;
            return;
        }
        if (upgradeUI != null && upgradeUI.isActiveAndEnabled)
        {
            currentFocus = UIFocus.Upgrades;
            return;
        }
        if (mainMenuPanel.activeInHierarchy)
        {
            currentFocus = UIFocus.MainMenu;
            return;
        }
        currentFocus = UIFocus.None;
    }
}
