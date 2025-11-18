using Pathfinding;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using FMODUnity;

public class AiBombController : AiMeleeBase
{
    [SerializeField]
    private ExplosionSettings explosionSettings;
    [SerializeField]
    private float explosionDelay = 1.5f;

    [Header("FMOD")]
    [SerializeField] private EventReference bombExplosionSFX;

    private bool hasExploded = false;
    private FactionController fc;
    private Rigidbody2D rb;

    protected override void Start()
    {
        base.Start(); // Runs base Start() logic from AiMeleeBase
        fc = GetComponent<FactionController>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        Debug.Log($"Triggered by: {other.gameObject.name}");

        if (other.TryGetComponent(out FactionController otherFC))
        {
            if (fc.IsSameFaction(otherFC) || otherFC.Faction == FactionController.Factions.Neutral) // Will not explode on neutral map objects
            {
                Debug.Log("Same faction - ignoring.");
                return;
            }

            hasExploded = true;

            if (AiPath != null)
            {
                AiPath.canMove = false;
                AiPath.canSearch = false;
                AiPath.destination = transform.position;
            }

            rb.bodyType = RigidbodyType2D.Static; // Prevents further movement from physics
            StartCoroutine(Explode());
        }

        IEnumerator Explode()
        {
            Debug.Log($"Bomb will explode after {explosionDelay} second delay.");
            yield return new WaitForSeconds(explosionDelay);

            Debug.Log("Exploding now!");

            // Play FMOD explosion SFX
            if (bombExplosionSFX.IsNull == false)
            {
                RuntimeManager.PlayOneShot(bombExplosionSFX, transform.position);
            }

            // Instantiate explosion object
            if (explosionSettings.explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionSettings.explosionPrefab, transform.position, Quaternion.identity);
                ExplosionSystem explosionSystem = explosion.GetComponent<ExplosionSystem>();
                explosionSystem.Explode(explosionSettings);
            }
            else
            {
                Debug.LogWarning("Explosion prefab is not assigned.");
            }

            Debug.Log("Destroying Bomb game object.");
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionSettings.radius);
    }
}
