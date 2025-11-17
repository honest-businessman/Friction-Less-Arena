using System.Drawing;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeItem", menuName = "Scriptable Objects/UpgradeItem")]
public class UpgradeItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public string description;
    public UpgradeType upgradeType;

    private Color32 effectHighlight = new Color32(234, 234, 234, 255);

    public enum UpgradeType
    {
        FireRate,
        ShellSpeed,
        ShellSize,
        AmmunitionPower,
        MoveSpeed,
        DriveChargeSpeed,
    }

    public UpgradeRarity rarity;

    public enum UpgradeRarity
    {
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public string GetEffectDescription()
    {
        string hex = ColorUtility.ToHtmlStringRGBA(effectHighlight);

        string effectDesc;
        switch (upgradeType)
        {
            case UpgradeType.FireRate:
                effectDesc = $"Increases fire rate by <color=#{hex}>{(value - 1) * 100:0.##}%</color>."; break;
            case UpgradeType.ShellSpeed:
                effectDesc = $"Increases shell speed by <color=#{hex}>{(value - 1) * 100:0.##}%</color>."; break;
            case UpgradeType.ShellSize:
                effectDesc = $"Increases shell size by <color=#{(value - 1) * 100:0.##} units</color>."; break;
            case UpgradeType.AmmunitionPower:
                effectDesc = $"Increases bounce count or periecing power of Charged Shot by <color=#{hex}>{1}</color>."; break;
            case UpgradeType.MoveSpeed:
                effectDesc = $"Increases move speed by <color=#{hex}>{(value - 1) * 100:0.##}%</color>."; break;
            case UpgradeType.DriveChargeSpeed:
                effectDesc = $"Increases drive charge speed by <color=#{hex}>{(value - 1) * 100:0.##}%</color>."; break;
            default:
                effectDesc = "No effect description available."; break;
        }
        return effectDesc;
    }

    public float value;
    public float valueScaling;
    
    [HideInInspector] public System.Action action;
}
