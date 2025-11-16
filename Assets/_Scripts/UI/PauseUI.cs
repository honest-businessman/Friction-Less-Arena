using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PauseUI : MonoBehaviour
{
    [SerializeField] private Image border;
    [SerializeField] private Button resume;
    [SerializeField] private Button settings;
    [SerializeField] private Button restart;
    [SerializeField] private Button exit;
    

    [Header("Navigation Settings")]
    [SerializeField] private float navigateThreshold = 0.5f;
    [SerializeField] private float deadzone = 0.2f;
    [SerializeField] private bool inputLocked = false;

    private List<Button> buttons = new List<Button>();
    private int selectedIndex = 0;
    private bool navigateHeld = false;

    private void OnEnable()
    {
        resume.onClick.AddListener(OnResumeClicked);
        restart.onClick.AddListener(OnRestartClicked);
        exit.onClick.AddListener(OnExitClicked);
        buttons.Add(resume);
        if (settings != null)
        {
            settings.onClick.AddListener(OnSettingsClicked);
            buttons.Add(settings);
        }
        buttons.Add(restart);
        buttons.Add(exit);

        border.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        border.gameObject.SetActive(false);
        resume.onClick.RemoveListener(OnResumeClicked);
        if (settings != null)
        {
            settings.onClick.RemoveListener(OnSettingsClicked);
        }
        restart.onClick.RemoveListener(OnRestartClicked);
        exit.onClick.RemoveListener(OnExitClicked);
    }
    private void OnResumeClicked() => UIManager.Instance?.HandlePause();
    private void OnSettingsClicked() => UIManager.Instance?.ShowSettingsMenu();
    private void OnRestartClicked() => GameManager.Instance?.RestartGame();
    private void OnExitClicked() => GameManager.Instance?.EndGame();
    public void HandleNavigate(Vector2 direction)
    {
        if (inputLocked) return;

        float y = direction.y;
        Debug.Log("Navigate Input Detected: " + direction);

        if (Mathf.Abs(y) < deadzone)
        {
            navigateHeld = false; // reset when stick returns to center
            return;
        }

        // Only act once per stick movement
        if (navigateHeld) return;

        if (y > -navigateThreshold)
            SelectPreviousButton();
        else if (y < navigateThreshold)
            SelectNextButton();

        navigateHeld = true;
    }

    public void HandleSubmit()
    {
        if (inputLocked) return;

        Button selectedButton = buttons[selectedIndex];
        if (selectedButton != null)
        {
            selectedButton.onClick.Invoke(); // simulate click
        }
    }

    private void SelectNextButton()
    {
        selectedIndex = (selectedIndex + 1) % buttons.Count;
        UpdateButtonSelection();
    }

    private void SelectPreviousButton()
    {
        selectedIndex = (selectedIndex - 1 + buttons.Count) % buttons.Count;
        UpdateButtonSelection();
    }

    private void UpdateButtonSelection()
    {
        Debug.Log("Selected Pause Button Index: " + selectedIndex);
        border.gameObject.SetActive(true); // border is visible
        // Move border to selected button
        border.transform.position = buttons[selectedIndex].transform.position;

    }
}
