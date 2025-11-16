using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class UpgradeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject upgradeCardPrefab;
    [SerializeField] HorizontalLayoutGroup upgradeCardContainer;

    [Header("Navigation Settings")]
    [SerializeField] private float navigateThreshold = 0.5f;
    [SerializeField] private float deadzone = 0.2f;
    [SerializeField] private bool inputLocked = false;

    private int selectedIndex = 0;
    private bool navigateHeld = false;

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
            Debug.LogWarning("Upgrade options not equald to 3. This is an UpgradeUI limitation.");
            gameObject.SetActive(false);
            return;
        }

        GenerateCards(upgradeOptions);

        // Reset selection
        selectedIndex = 0;
        UpdateButtonSelection();
    }

    private void GenerateCards(UpgradeItem[] uItems)
    {
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
        Debug.Log("Selected Upgrade Index: " + selectedIndex);
        // Deselect all, then highlight the current one
        for (int i = 0; i < upgradeCards.Count; i++)
        {
            upgradeCards[i].border.gameObject.SetActive(false);
        }

        upgradeCards[selectedIndex].border.gameObject.SetActive(true); // ensures proper UI focus
    }
}
