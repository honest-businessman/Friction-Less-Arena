using System;
using UnityEngine;

public static class PlayerEvents
{
    public static Action<GameObject> OnPlayerSpawned;
    public static Action<float> OnPlayerFireCharged; // float = chargeStartDelay
    public static Action<GameObject> OnPlayerDeath;
    public static Action<GameObject> OnPlayerHit;
    public static Action<GameObject> OnPlayerHeal;
    public static Action<GameObject> OnPlayerXPPickup;
    public static Action<GameObject> OnPlayerLevelup;
}
