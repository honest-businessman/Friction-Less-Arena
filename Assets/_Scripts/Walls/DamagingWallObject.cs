using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageWallObject : WallObject
{
    [SerializeField] int damage = 1;
    [SerializeField] float damageCooldown = 1f;
    [SerializeField] Collider2D damageCollider;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!other.IsTouching(damageCollider))
            return;

        if (!WaveManager.Instance.wallDamageHistory.ContainsKey(other.gameObject))
            WaveManager.Instance.wallDamageHistory[other.gameObject] = -999f;

        if (Time.time - WaveManager.Instance.wallDamageHistory[other.gameObject] >= damageCooldown)
        {
            WaveManager.Instance.wallDamageHistory[other.gameObject] = Time.time;

            var health = other.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
}
