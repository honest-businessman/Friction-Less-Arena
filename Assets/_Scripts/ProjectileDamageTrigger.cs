using UnityEngine;

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
