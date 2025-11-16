using System;
using UnityEngine;

public static class EnemyEvents
{
    public static Action<GameObject> OnEnemySpawned;
    public static Action<GameObject> OnEnemyFire;
    public static Action<GameObject> OnEnemyMelee;
    public static Action<GameObject> OnEnemyExplode;
    public static Action<GameObject> OnEnemyDamaged;
    public static Action<GameObject> OnEnemyDeath;
}
