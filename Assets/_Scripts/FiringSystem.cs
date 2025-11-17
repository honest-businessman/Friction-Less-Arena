using Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.InputSystem;

public class FiringSystem : MonoBehaviour
{
    [SerializeField] LayerMask chargedHitList;
    [SerializeField] GameObject hitscanTrailPrefab;
    [SerializeField] private float trailSpeed = 300f;
    [SerializeField] private float bounceDelay = 0.5f;
    [SerializeField] private float chargeStartDelay = 3f;
    [SerializeField] private Transform muzzle;

    //FMOD
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TankChargedShotAudio tankTrailShotAudio;



    private float normalFiringTimer = 0f;
    private bool isPlayer = false;
    private bool canCharge = false;
    private GameObject turret;
    private TurretController turretController;
    private Vector2 firePosition;
    private Rigidbody2D rb;
    private FactionController fc;
    private CharacterBase cb;
    public delegate void ImpactAction();
    public static event ImpactAction OnImpact;
    private TrailRenderer tr;

    private void Awake()
    {
        cb = GetComponent<CharacterBase>();
        if (hitscanTrailPrefab == null) { Debug.Log("No Hitscan Prefab found."); }

        foreach (Transform childTransform in transform) // To do: Check if for loop is nessecary
        {
            turret = FindTagInChildren("Turret").gameObject; // Return the turret child object if tag matches game object
        }
        if (turret == null)
        {
            Debug.LogError("No Turret found on " + gameObject);
        }
        if (gameObject.CompareTag("Player")) // checks if parent GameObject is the player object
        {
            isPlayer = true;
        }

        rb = GetComponent<Rigidbody2D>();
        fc = GetComponent<FactionController>();
        turretController = turret.GetComponent<TurretController>();
    }

    private Transform FindTagInChildren(string tag)
    {
        foreach (Transform childTransform in transform)
        {
            // Check if the child's GameObject has the specified tag
            if (childTransform.CompareTag(tag))
            {
                return childTransform;
            }
        }
        return null;
    }

    // For AI or non-input based firing
    public void FireCommand() { TryFire(); }

    // For player input system
    public void FireCommand(InputValue value) { TryFire(); }


    // Attempts to fire a shot if the fire rate cooldown has elapsed.
    // Will fire a charged shot if drive is fully charged.
    void TryFire()
    {

        // Get current turret parameters, need to implement only getting new parameters on weapon change
        turretController = turret.GetComponent<TurretController>();
        if (cb.DriveCharge >= 100f)
        {
            Debug.Log("Fire Charged!");
            PlayerEvents.OnPlayerFireCharged?.Invoke(chargeStartDelay);
            firePosition = muzzle.position;
            FireCharged();
            cb.DrainDrive();
            return;
        }
        else if (Time.time - normalFiringTimer >= 1 / turretController.CurrentSettings.fireRate) // Divide fire rate by 1 to convert fire rate to shells per second
        {
            normalFiringTimer = Time.time;
            firePosition = muzzle.position;
            FireNormal();
        }
    }

    // Fires a normal shot
    public void FireNormal()
    {
        if (turretController.CurrentSettings.isNormalHitscan)
            StartCoroutine(FireHitscan(firePosition, firePosition, Vector2.zero, 0, 0, 0));
        else
            FireShell();
    }

    // Fires a charged shot
    public void FireCharged()
    {
        if (turretController.CurrentSettings.isChargedHitscan)
            StartCoroutine(FireHitscan(firePosition, firePosition, Vector2.zero, 0, 0, 0));
        else
            FireShell();
    }

    // Fires a physical projectile that can bounce.
    void FireShell()
    {
        GameObject shell = Instantiate(
            turretController.CurrentSettings.shellPrefab,
            muzzle.position,
            muzzle.rotation
        );

        shell.GetComponent<ProjectileController>().Initialize(
            turretController.CurrentSettings.shellBounces,
            turretController.CurrentSettings.shellDamage,
            turretController.CurrentSettings.shellLifetime,
            fc.Faction
        );

        shell.transform.localScale = new Vector2(
            turretController.CurrentSettings.shellSize,
            turretController.CurrentSettings.shellSize
        );

        shell.GetComponent<Rigidbody2D>().linearVelocity =
            shell.transform.up * turretController.CurrentSettings.shellSpeed;

        // --- FMOD CALL HERE ---
        if (playerController != null && playerController.ShotAudio != null)
            playerController.ShotAudio.PlayShot(muzzle.position);
    }



    // A recursive coroutine that handles hitscan firing, penetration, and bouncing.
    System.Collections.IEnumerator FireHitscan(Vector3 oldOrigin, Vector3 targetPosition, Vector2 hitNormal, int shotType, int currentBounces, int currentPens)
    {
        // Shot types: 0 - First shot, 1 - Penetration, 2 - Bounce
        Vector2 rayOrigin;
        Vector3 rayDirection;
        Quaternion trailRotation;
        int maxBounces = turretController.CurrentSettings.hitscanBounces;

        if (shotType == 0) // Ready raycast for first shot
        {
            rayOrigin = muzzle.position;
            rayDirection = muzzle.up;
            trailRotation = muzzle.rotation;
            Vector2 debugRayEndpoint = rayOrigin + (Vector2)rayDirection * turretController.CurrentSettings.hitscanRange;
            Debug.DrawLine(rayOrigin, debugRayEndpoint, Color.green, 3f);
        }
        else if (shotType == 1) // Ready next raycast for penetration
        {
            rayDirection = (targetPosition - oldOrigin).normalized; // Use previous ray direction, unsure how this works.
            rayOrigin = oldOrigin; // Start from previous hit point

            Vector2 debugRayEndpoint = rayOrigin + (Vector2)rayDirection * turretController.CurrentSettings.hitscanRange;
            Debug.DrawLine(rayOrigin, debugRayEndpoint, Color.yellow, 3f);
            trailRotation = Quaternion.FromToRotation(Vector3.up, rayDirection); // Keep same rotation as previous trail, unsure how necessary this is
        }
        else // Ready next raycast for bounce
        {
            yield return new WaitForSeconds(bounceDelay);

            // Use previous hit point and normal to calculate bounce direction
            Vector2 incomingDirection = (targetPosition - oldOrigin).normalized;
            Vector2 bounceDirection = Vector2.Reflect(incomingDirection, hitNormal);

            // Offset origin slightly to avoid starting inside collider
            Vector2 bounceOrigin = (Vector2)targetPosition + bounceDirection * 0.01f;

            // Visualize the bounce ray in the editor (full range)
            Debug.DrawLine(bounceOrigin, bounceOrigin + bounceDirection * turretController.CurrentSettings.hitscanRange, Color.red, 3f);

            rayOrigin = bounceOrigin;
            rayDirection = bounceDirection;

            // Calculate the angle from the bounce direction
            float angle = Mathf.Atan2(bounceDirection.y, bounceDirection.x) * Mathf.Rad2Deg;
            trailRotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, turretController.CurrentSettings.hitscanRange, chargedHitList);


        if (hit.collider != null)
        {
            Debug.Log("Hit: " + hit.transform.name);

            // Trail spawn and movement
            GameObject trail = Instantiate(hitscanTrailPrefab, rayOrigin, trailRotation);
            tr = trail.GetComponent<TrailRenderer>();
            if (tankTrailShotAudio != null)
            {
                tankTrailShotAudio.PlayTrailShot(rayOrigin);
            }

            Vector3 startPosition = tr.transform.position;
            float startingDistance = Vector3.Distance(startPosition, hit.point);
            float distance = startingDistance;
            while (distance > 0)
            {
                tr.transform.position = Vector3.Lerp(startPosition, hit.point, 1 - (distance / startingDistance));
                distance -= Time.deltaTime * trailSpeed;

                yield return null;
            }
            tr.transform.position = hit.point;
            OnImpact?.Invoke(); // Should activate a particle effect through this event

            hit.transform.TryGetComponent(out FactionController hitFc);
            hit.transform.gameObject.TryGetComponent(out HealthSystem hitHealthSystem);
            tryDamage();
            if (hitHealthSystem != null && currentPens < turretController.CurrentSettings.hitscanPenetrations)
            {
                int pensSpent;
                if (hitFc.Faction == FactionController.Factions.Neutral)
                    pensSpent = 0; // Prevent spending pens on Neutral entities.
                else
                    pensSpent = 1;

                Vector2 penetrationOrigin = hit.point + (Vector2)rayDirection * 0.01f;
                Vector2 penetrationEnd = penetrationOrigin + (Vector2)rayDirection * turretController.CurrentSettings.hitscanRange;
                yield return StartCoroutine(FireHitscan(penetrationOrigin, penetrationEnd, hit.normal, 1, currentBounces, currentPens + pensSpent));
                yield break;
            }
            
            if (maxBounces > currentBounces) // Start new hitscan for bounce
            {
                if (!hitFc == null && hitFc.Faction != FactionController.Factions.Neutral)
                {
                    yield break; // Stop bounce if damage was dealt to a non-neutral faction
                }
                yield return new WaitForSeconds(bounceDelay);
                yield return StartCoroutine(FireHitscan(rayOrigin, hit.point, hit.normal, 2, currentBounces + 1, currentPens));
            }

            void tryDamage()
            {
                if (hitFc != null && hitHealthSystem != null && !(fc.IsSameFaction(hitFc)))
                {
                    hitHealthSystem.TakeDamage(turretController.CurrentSettings.hitscanDamage);
                }
            }
        }
    }
}
