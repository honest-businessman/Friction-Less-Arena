using System;
using UnityEngine;

public static class UpgradeEvents
{
    public static event Action<UpgradeItem[]> OnUpgradesAvailable;
    public static void UpgradesAvailable(UpgradeItem[] upgrades)
    {
        Debug.Log("Upgrades available event triggered.");
        OnUpgradesAvailable?.Invoke(upgrades);
    }
    public static event Action<UpgradeItem> OnUpgradeSelected;
    public static void UpgradeSelected(UpgradeItem upgrade)
    {
        Debug.Log("Upgrade selected event triggered.");
        OnUpgradeSelected?.Invoke(upgrade);
    }
}
