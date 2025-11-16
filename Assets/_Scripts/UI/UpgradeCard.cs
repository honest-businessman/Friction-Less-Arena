using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UpgradeItem;

public class UpgradeCard : MonoBehaviour
{
    public Image border;
    public Button button;
    TextMeshProUGUI title;
    Image icon;
    TextMeshProUGUI description;
    TextMeshProUGUI effectDesc;
    public UpgradeItem upgradeItem;

    [Header("Rarity Colors")]
    public Color defaultColor;
    public Color uncommonColor;
    public Color rareColor;
    public Color epicColor;
    public Color legendaryColor;

    public void Init(UpgradeItem uItem)
    {
        upgradeItem = uItem;

        border = transform.Find("Border").GetComponent<Image>();
        button = transform.Find("Button").GetComponent<Button>();
        title = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        description = transform.Find("Description").GetComponent<TextMeshProUGUI>();
        icon = transform.Find("Icon").GetComponent<Image>();
        effectDesc = transform.Find("Effect").GetComponent<TextMeshProUGUI>();

        Color rarityColor = GetRarityColor();
        Color textColor = UtilityScript.AdjustBrightness(rarityColor, 0.15f, UtilityScript.BrightnessMode.Add);
        border.color = rarityColor;
        SetButtonColor(rarityColor);
        title.text = upgradeItem.itemName;
        title.color = textColor;
        icon.sprite = upgradeItem.icon;
        icon.color = textColor;
        description.text = upgradeItem.description;
        description.color = textColor;
        effectDesc.text = upgradeItem.GetEffectDescription();
        effectDesc.color = textColor;

        border.gameObject.SetActive(false);
        Destroy(GetComponent<Canvas>());
    }
    public void SetButtonColor(Color color)
    {
        ColorBlock colorBlock = button.colors;
        colorBlock.normalColor = color;
        button.colors = colorBlock;
    }
    public Color GetRarityColor()
    {
        switch (upgradeItem.rarity)
        {
            case UpgradeRarity.Uncommon:
                return uncommonColor; // Uncommon color
            case UpgradeRarity.Rare:
                return rareColor; // Rare color
            case UpgradeRarity.Epic:
                return epicColor; // Epic color (purple)
            case UpgradeRarity.Legendary:
                return legendaryColor; // Legendary color
            default:
                return defaultColor; // Default color
        }
    }
}
