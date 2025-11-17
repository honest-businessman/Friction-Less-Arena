using UnityEngine;
using UnityEngine.ProBuilder;

public class ProjectileDamageTrigger : MonoBehaviour
{
    private ProjectileController parentProjectile;

    void Awake()
    {
        parentProjectile = GetComponentInParent<ProjectileController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentProjectile != null)
        {
            if(!other.isTrigger) parentProjectile.HandleTrigger(other);
        }
    }
}
