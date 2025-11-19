using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Device;
using TMPro;
using System;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance { get; private set; }

    [Header("UI Setup")]
    [SerializeField] 
    private List<Button3D> buttons = new List<Button3D>();
    public string gameSceneName;

    [Header("Button Colors")]
    public Color colorButton = Color.white;
    public Color colorButtonActive = Color.yellow;
    public Color colorButtonHover = Color.cyan;

    [Header("Navigation Settings")]
    [SerializeField] private float navigateThreshold = 0.5f;
    [SerializeField] private float deadzone = 0.2f;

    [Header("Events")]
    public UnityEvent OnStartSequence = new UnityEvent();

    [Header("Screen Materials")]
    public Material osAnimationMaterial;
    public Material game2DMaterial;
    public Material settingsMaterial;

    [Header("Title Text Objects")]
    [SerializeField] private List<TextMeshPro> titleTexts;


    public enum ScreenType
    {
        Menu,
        Game2D,
        Settings,
        Info
    }

    private GameObject screenObj;
    private MeshRenderer screenMeshRenderer;
    private SpriteSheetAnimator ssAnimator;
    private ScreenType lastScreen;
    private Action listener;
    private int selectedIndex = 0;
    private bool inputLocked = false;
    private bool navigateHeld = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(Instance);
        }

        screenObj = GameObject.FindWithTag("TankScreen");
        screenMeshRenderer = screenObj.GetComponent<MeshRenderer>();
        ssAnimator = screenObj.GetComponent<SpriteSheetAnimator>();

        StartOSAnimation();
    }

    public void Start()
    {
        if (buttons.Count > 0)
        {
            selectedIndex = 0;
            buttons[selectedIndex].Select();
        }
    }

    public void AssignUI()
    {
        Canvas canvas = UIManager.Instance.GetUICanvas();
        screenObj.GetComponent<RenderTextureUIInteractor>();
    }

    public void SetScreen(ScreenType screenType)
    {
        if (screenType == ScreenType.Menu)
        {
            UIManager.Instance.ShowMainMenu();
            screenMeshRenderer.sharedMaterial = osAnimationMaterial;
            StartOSAnimation();
            lastScreen = ScreenType.Menu;
        }
        else if (screenType == ScreenType.Game2D)
        {
            if (lastScreen == ScreenType.Menu) { StopOSAnimation(); }
            GameEvents.OnGameStarted -= listener;
            screenMeshRenderer.sharedMaterial = game2DMaterial;
            lastScreen = ScreenType.Game2D;

        }
        else if (screenType == ScreenType.Settings)
        {
            if (lastScreen == ScreenType.Menu) { StopOSAnimation(); }
            UIManager.Instance.ShowSettingsMenu();
            screenMeshRenderer.sharedMaterial = settingsMaterial;
            lastScreen = ScreenType.Settings;
        }
        else if (screenType == ScreenType.Info)
        {
            if (lastScreen == ScreenType.Menu) { StopOSAnimation(); }
            UIManager.Instance.ShowInfoMenu();
            screenMeshRenderer.sharedMaterial = settingsMaterial;
            lastScreen = ScreenType.Info;
        }
    }
    private void StartOSAnimation()
    {
        foreach (var text in titleTexts)
        {
            text.gameObject.SetActive(true);
        }
        ssAnimator.SetupAnimation();
        ssAnimator.PlayAnimation();
    }
    private void StopOSAnimation()
    {
        foreach (var text in titleTexts)
        {
            text.gameObject.SetActive(false);
        }
        ssAnimator.StopAnimation();
    }

    public void StartPressed()
    {
        if (inputLocked) return;
        inputLocked = true;

        InputManager.Instance.DisablePlayerInput();
        StartCoroutine(GameManager.Instance.EnterGame());
        OnStartSequence?.Invoke();
        listener = () => SetScreen(ScreenType.Game2D);
        GameEvents.OnGameStarted += listener;
    }

    public void ExitPressed()
    {
        if (inputLocked) return;
        inputLocked = true;

        Debug.Log("Quitting Game");
        UnityEngine.Device.Application.Quit();
    }
    public void ExitToMenu()
    {
        GameEvents.OnGameStarted -= listener;
        SetScreen(ScreenType.Menu);
        inputLocked = false;
    }

    public void SettingsPressed()
    {
        if (inputLocked) return;
        if (UIManager.Instance.currentFocus == UIManager.UIFocus.Settings)
        {
            SetScreen(ScreenType.Menu);
            MenuEvents.OnSettingsClosed?.Invoke();
        }
        else
        {
           SetScreen(ScreenType.Settings);
           MenuEvents.OnSettingsOpened?.Invoke();
        }
        
        Debug.Log("Opening Settings");
    }
    public void InfoPressed()
    {
        if (inputLocked) return;
        if (UIManager.Instance.currentFocus == UIManager.UIFocus.Info)
        {
            SetScreen(ScreenType.Menu);
            MenuEvents.OnSettingsClosed?.Invoke();
        }
        else
        {
            SetScreen(ScreenType.Info);
            MenuEvents.OnSettingsOpened?.Invoke();
        }

        Debug.Log("Opening Info");
    }

    public void HandleNavigate(Vector2 direction)
    {
        if (inputLocked || buttons.Count == 0) return;

        float x = direction.x;

        if (Mathf.Abs(x) < deadzone)
        {
            navigateHeld = false; // reset when stick returns to center
            return;
        }

        // Only act once per stick movement
        if (navigateHeld) return;

        if (x < navigateThreshold)
            SelectPreviousButton();
        else if (x > -navigateThreshold)
            SelectNextButton();

        navigateHeld = true;
    }
    public void HandleSubmit()
    {
        if (inputLocked || buttons.Count == 0) return;

        var selectedButton = buttons[selectedIndex];
        if (selectedButton != null)
        {
            selectedButton.Press();
        }
    }
    private void SelectNextButton()
    {
        buttons[selectedIndex].Deselect();
        selectedIndex = (selectedIndex + 1) % buttons.Count;
        buttons[selectedIndex].Select();
    }

    private void SelectPreviousButton()
    {
        buttons[selectedIndex].Deselect();
        selectedIndex = (selectedIndex - 1 + buttons.Count) % buttons.Count;
        buttons[selectedIndex].Select();
    }

}
