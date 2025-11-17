using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using FMODUnity;

public class UpgradeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject upgradeCardPrefab;
    [SerializeField] private HorizontalLayoutGroup upgradeCardContainer;

    [Header("Navigation Settings")]
    [SerializeField] private float navigateThreshold = 0.5f;
    [SerializeField] private float deadzone = 0.2f;
    [SerializeField] private bool inputLocked = false;

    [Header("FMOD Events")]
    [SerializeField] private EventReference hoverSound;
    [SerializeField] private EventReference selectSound;

    private int selectedIndex = 0;
    private bool navigateHeld = false;
    private int lastSelectedIndex = -1;

    private List<UpgradeCard> upgradeCards = new List<UpgradeCard>();

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    // Called when upgrades are offered
    public void ShowUpgradeOptions(UpgradeItem[] upgradeOptions)
    {
        if (upgradeOptions.Length != 3)
        {
            Debug.LogWarning("Upgrade options not equal to 3. This is an UpgradeUI limitation.");
            gameObject.SetActive(false);
            return;
        }

        GenerateCards(upgradeOptions);

        // Reset selection
        selectedIndex = 0;
        lastSelectedIndex = -1; // Force hover sound on first selection
        UpdateButtonSelection();
    }

    private void GenerateCards(UpgradeItem[] uItems)
    {
        // Clear previous cards first
        foreach (UpgradeCard uCard in upgradeCards)
        {
            uCard.button.onClick.RemoveAllListeners();
            Destroy(uCard.gameObject);
        }
        upgradeCards.Clear();

        // Generate new cards
        for (int i = 0; i < uItems.Length; i++)
        {
            GameObject uCardObj = Instantiate(upgradeCardPrefab, upgradeCardContainer.transform);
            UpgradeCard uCard = uCardObj.GetComponent<UpgradeCard>();
            uCard.Init(uItems[i]);
            uCard.button.onClick.AddListener(() => ChooseUpgrade(uCard.upgradeItem));
            upgradeCards.Add(uCard);
        }
    }

    private void ChooseUpgrade(UpgradeItem chosenUpgrade)
    {
        foreach (UpgradeCard uCard in upgradeCards)
        {
            uCard.button.onClick.RemoveAllListeners();
            Destroy(uCard.gameObject);
        }
        upgradeCards.Clear();

        GameManager.Instance.Player.GetComponent<PlayerController>().upgradeInventory.Add(chosenUpgrade);
        chosenUpgrade.action?.Invoke();
        UpgradeEvents.UpgradeSelected(chosenUpgrade);
    }

    // === Input Handlers ===
    public void HandleNavigate(Vector2 direction)
    {
        if (inputLocked) return;

        float x = direction.x;
        Debug.Log("Navigate Input Detected: " + direction);

        if (Mathf.Abs(x) < deadzone)
        {
            navigateHeld = false; // reset when stick returns to center
            return;
        }

        // Only act once per stick movement
        if (navigateHeld) return;

        if (x < -navigateThreshold)
            SelectPreviousButton();
        else if (x > navigateThreshold)
            SelectNextButton();

        navigateHeld = true;
    }

    public void HandleSubmit()
    {
        if (inputLocked) return;

        UpgradeCard selectedCard = upgradeCards[selectedIndex];
        if (selectedCard != null)
        {
           
                RuntimeManager.PlayOneShot(selectSound);

            selectedCard.button.onClick.Invoke(); // simulate click
        }
    }

    private void SelectNextButton()
    {
        selectedIndex = (selectedIndex + 1) % upgradeCards.Count;
        UpdateButtonSelection();
    }

    private void SelectPreviousButton()
    {
        selectedIndex = (selectedIndex - 1 + upgradeCards.Count) % upgradeCards.Count;
        UpdateButtonSelection();
    }

    private void UpdateButtonSelection()
    {
        // Play hover sound if selection changed
        if (selectedIndex != lastSelectedIndex)
        {
          
                RuntimeManager.PlayOneShot(hoverSound);

            lastSelectedIndex = selectedIndex;
        }

        // Deselect all, then highlight the current one
        for (int i = 0; i < upgradeCards.Count; i++)
            upgradeCards[i].border.gameObject.SetActive(false);

        if (upgradeCards.Count > 0)
            upgradeCards[selectedIndex].border.gameObject.SetActive(true);
    }
}
