using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputSystem inputSystem;
    [SerializeField] private PlayerController player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        inputSystem = new PlayerInputSystem();
    }

    private void OnEnable()
    {
        inputSystem.Enable();

        // Global input (always available)
        inputSystem.UI.Pause.performed += OnPause;
        inputSystem.Player.Pause.performed += OnPause;
    }

    private void OnDisable()
    {
        inputSystem.Disable();
        inputSystem.UI.Pause.performed -= OnPause;
        inputSystem.Player.Pause.performed -= OnPause;

    }

    public void EnablePlayerInput(PlayerController playerController)
    {
        this.player = playerController;

        DisableUIInput();
        inputSystem.Player.Enable();
        Debug.Log("Player Input Enabled");

        inputSystem.Player.Move.performed += OnMove;
        inputSystem.Player.Move.canceled += OnMoveCancel;
        inputSystem.Player.Drift.performed += OnDriftStart;
        inputSystem.Player.Drift.canceled += OnDriftEnd;
        inputSystem.Player.Aim.performed += OnAim;
        inputSystem.Player.Aim.canceled += OnAimCancel;
        inputSystem.Player.Fire.performed += OnFire;
        inputSystem.Player.ChangeWeapon.performed += OnChangeWeapon;
    }

    public void DisablePlayerInput()
    {
        inputSystem.Player.Move.performed -= OnMove;
        inputSystem.Player.Move.canceled -= OnMoveCancel;
        inputSystem.Player.Drift.performed -= OnDriftStart;
        inputSystem.Player.Drift.canceled -= OnDriftEnd;
        inputSystem.Player.Aim.performed -= OnAim;
        inputSystem.Player.Aim.canceled -= OnAimCancel;
        inputSystem.Player.Fire.performed -= OnFire;
        inputSystem.Player.ChangeWeapon.performed -= OnChangeWeapon;

        inputSystem.Player.Disable();


    }

    public void EnableUIInput()
    {
        DisablePlayerInput();
        inputSystem.UI.Enable();
        Debug.Log("UI Input Enabled");
        inputSystem.UI.Navigate.performed += OnNavigate;
        inputSystem.UI.Submit.performed += OnSubmit;
    }

    public void DisableUIInput()
    {
        inputSystem.UI.Disable();
        inputSystem.UI.Navigate.performed -= OnNavigate;
        inputSystem.UI.Submit.performed -= OnSubmit;
    }

    // Private callbacks
    private void OnMove(InputAction.CallbackContext ctx) => player.Move(ctx.ReadValue<Vector2>());
    private void OnMoveCancel(InputAction.CallbackContext ctx) => player.Move(Vector2.zero);
    private void OnDriftStart(InputAction.CallbackContext ctx) => player.Drift(true);
    private void OnDriftEnd(InputAction.CallbackContext ctx) => player.Drift(false);
    private void OnAim(InputAction.CallbackContext ctx) => player.Aim(ctx.ReadValue<Vector2>());
    private void OnAimCancel(InputAction.CallbackContext ctx) => player.Aim(Vector2.zero);
    private void OnFire(InputAction.CallbackContext ctx) => player.Fire();
    private void OnChangeWeapon(InputAction.CallbackContext ctx) => player.turretController.ChangeTurret(ctx.ReadValue<float>());

    private void OnPause(InputAction.CallbackContext ctx)
    {
        if (UIManager.Instance != null) { UIManager.Instance.HandlePause(); }
    }
    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if(ScreenManager.Instance != null) { ScreenManager.Instance.HandleNavigate(ctx.ReadValue<Vector2>()); }
        if (UIManager.Instance != null) { UIManager.Instance.HandleNavigate(ctx.ReadValue<Vector2>()); }
    }
    private void OnSubmit(InputAction.CallbackContext ctx) 
    {
        if (ScreenManager.Instance != null) { ScreenManager.Instance.HandleSubmit(); }
        if (UIManager.Instance != null) { UIManager.Instance.HandleSubmit(); }
    }

    public PlayerInputSystem GetInputSystem() => inputSystem;
}
