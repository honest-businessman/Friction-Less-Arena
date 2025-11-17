using System;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using System.Collections; 

public class PlayerController : CharacterBase
{
    [Header("Inventory")]
    public List<UpgradeItem> upgradeInventory = new List<UpgradeItem>();

    [Header("Movement Settings")]
    public bool mouseAiming = false;
    public float moveSpeed = 25f; // Movement speed
    public float maxSpeed = 5f; // Maximum speed the player can reach
    public float driftSpeed = 6f;
    public float maxDriftSpeed = 6f;
    public float defaultDrag = 8f;
    public float turnSpeed = 20f; // Rotation speed
    public float driftTurnSpeed = 10f;
    private float moveSpeedMultiplier = 100f; // Adjusted multiplier for movement speed
    public float treadOffset = 0.2f; // Offset for the rotation pivot point
    public float chargePerSecond = 40f;
    public float dischargePerSecond = 100f;
    public float chargeFadeDelay = 1f; // Delay before charge starts fading
    public float chargeAngle = 30f; // degrees
    public float minChargeMoveSpeed = 3f; // degrees
    private float turnSpeedMultiplier = 10f; // Adjusted multiplier for rotation speed
    private float chargeDelayModifier = 1f;

    [SerializeField] private TankDriftAudio driftAudio;

    //FMOD
    [SerializeField] private TankShotAudio shotAudio;
    public TankShotAudio ShotAudio => shotAudio;
    [Header("Drive Audio")]
    [SerializeField] private FMODUnity.EventReference fullDriveLoopEvent;
    private FMOD.Studio.EventInstance fullDriveLoopInstance;
    private bool isDriveLoopPlaying = false;
    [Header("Drift Audio")]
    [SerializeField] private FMODUnity.EventReference driftLoopEvent;
    private FMOD.Studio.EventInstance driftLoopInstance;
    private bool isDriftLoopPlaying = false;
    [Header("XP Audio")]
    [SerializeField] private FMODUnity.EventReference xpPickupEvent;
    public FMODUnity.EventReference XPPickupEvent => xpPickupEvent;






    [Header("Visuals")]
    // Reference to the TrackRight Animator
    [SerializeField] private Animator trackRightAnimator;
    [SerializeField] private Animator trackLeftAnimator;
    [SerializeField] private TrailRenderer trailLeft;
    [SerializeField] private TrailRenderer trailRight;
    [SerializeField] private float trailFadeOutTime = 0.5f;
    [SerializeField] private GameObject battery;
    [SerializeField] private SpriteRenderer batterySR;
    [SerializeField] private ParticleSystem fullChargeParticles;
    private bool particlesPlaying = false;
    private Color originalBatteryColor;
    private Coroutine fadeRoutine;
    private float chargePauseEndTime = 0f;


    private bool driftPressed;

    public GameObject turret;
    public TurretController turretController;

    public float pickupRadius = 1.5f; //distance of XP starting homing


    private enum MoveState
    {
        Idle,
        Moving,
        Drifting
    }
    private MoveState currentState = MoveState.Idle;
    private Vector2 playerInput;
    private Rigidbody2D rb;
    private FiringSystem firingSystem;
    private float timeLastCharge;
    private Vector2 aimInput;
    private Quaternion lastTurretDirection;

    void Awake()
    {
        // Find TrackRight and TrackLeft children by name and get their Animators
        if (trackRightAnimator == null)
        {
            Transform trackRightTransform = transform.Find("TrackRight");
            if (trackRightTransform != null)
                trackRightAnimator = trackRightTransform.GetComponent<Animator>();
        }

        if (trackLeftAnimator == null)
        {
            Transform trackLeftTransform = transform.Find("TrackLeft");
            if (trackLeftTransform != null)
                trackLeftAnimator = trackLeftTransform.GetComponent<Animator>();
        }

        DriveCharge = 0f;
        rb = GetComponent<Rigidbody2D>();
        firingSystem = GetComponent<FiringSystem>();
        defaultDrag = rb.linearDamping;

        firingSystem = GetComponent<FiringSystem>();
        if (turret != null)
        {
            turretController = turret.GetComponent<TurretController>();
        }

        batterySR = battery.GetComponent<SpriteRenderer>();
        originalBatteryColor = batterySR.material.color;
    }

    private void Start()
    {
        // Ensure trails are hidden when the game starts
        if (trailLeft != null) trailLeft.emitting = false;
        if (trailRight != null) trailRight.emitting = false;
    }


    private void OnEnable()
    {
        PlayerEvents.OnPlayerFireCharged += DelayChargingDrive;
    }
    private void OnDisable()
    {
        PlayerEvents.OnPlayerFireCharged -= DelayChargingDrive;

        if (driftLoopInstance.isValid())
        {
            driftLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            driftLoopInstance.release();
            isDriftLoopPlaying = false;
        }

        if (fullDriveLoopInstance.isValid())
        {
            fullDriveLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            fullDriveLoopInstance.release();
            isDriveLoopPlaying = false;
        }
    }



    public void Move(Vector2 moveVector)
    {
        playerInput = moveVector;

    }

    public void Drift(bool isPressed)
    {
        if (isPressed && !driftPressed && rb.linearVelocity.magnitude > 0.1f)
        {
            if (driftAudio != null)
                driftAudio.PlayDriftStart(transform.position);

            // Start FMOD drift loop
            if (!isDriftLoopPlaying)
            {
                driftLoopInstance = FMODUnity.RuntimeManager.CreateInstance(driftLoopEvent);
                driftLoopInstance.start();
                isDriftLoopPlaying = true;
            }
        }

        // Stop FMOD drift loop when drift ends
        if (!isPressed && isDriftLoopPlaying)
        {
            if (driftLoopInstance.isValid())
            {
                driftLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                driftLoopInstance.release();
                isDriftLoopPlaying = false;
            }
        }

        driftPressed = isPressed;
        HandleDriftTrails(isPressed);
    }



    public void Aim(Vector2 aimVector)
    {
        aimInput = aimVector;
    }

    private void ChangeMoveState()
    {
        if (driftPressed)
            currentState = MoveState.Drifting;
        else if (playerInput.magnitude > 0.1f)
            currentState = MoveState.Moving;
        else
            currentState = MoveState.Idle;
    }

    private void Update()
    {
        TurretRotate();
    }

    void FixedUpdate()
    {
        if (currentState == MoveState.Drifting)
        {
            rb.linearDamping = 0f; // Reduce linear damping when drifting
        }
        else
        {
            rb.linearDamping = defaultDrag; // Reset linear damping when not drifting
        }

        PlayerMovement();
        HandleChargeParticles();
    }

    private void PlayerMovement()
    {
        ChangeMoveState();

        Debug.DrawRay(transform.position, transform.up * 2, Color.green); // Draw ray pointing up from the player
        if (currentState == MoveState.Moving)
        {
            rb.AddForce(transform.up * playerInput.y * (1.5f - Mathf.Abs(playerInput.x)) * moveSpeed * moveSpeedMultiplier * Time.deltaTime, ForceMode2D.Force);
            FadeDrive();
        }
        else if (currentState == MoveState.Drifting)
        {
            rb.AddForce(transform.up * playerInput.y * driftSpeed * moveSpeedMultiplier * Time.deltaTime, ForceMode2D.Force);
            ChargeDrive();
        }
        else if (currentState == MoveState.Idle)
            FadeDrive();

        PlayerRotate();


        // Clamp the velocity to the maximum speed
        if (driftPressed && rb.linearVelocity.magnitude > maxDriftSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxDriftSpeed;
        }
        else if (!driftPressed && rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        bool isMoving = (currentState == MoveState.Moving);

        if (trackRightAnimator != null)
            trackRightAnimator.SetBool("isMoving", isMoving);

        if (trackLeftAnimator != null)
            trackLeftAnimator.SetBool("isMoving", isMoving);
    }

    //particle effects for full charge
    private void HandleChargeParticles()
    {
        if (DriveCharge >= 100f && !particlesPlaying)
        {
            fullChargeParticles.Play();
            particlesPlaying = true;

            // Start FMOD loop
            if (!isDriveLoopPlaying)
            {
                fullDriveLoopInstance = FMODUnity.RuntimeManager.CreateInstance(fullDriveLoopEvent);
                fullDriveLoopInstance.start();
                isDriveLoopPlaying = true;
            }
        }
        else if (DriveCharge < 100f && particlesPlaying)
        {
            fullChargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particlesPlaying = false;

            // Stop FMOD loop
            if (isDriveLoopPlaying && fullDriveLoopInstance.isValid())
            {
                fullDriveLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                fullDriveLoopInstance.release();
                isDriveLoopPlaying = false;
            }
        }
    }




    private void PlayerRotate()
    {
        float rotateSpeed;
        if (currentState == MoveState.Drifting)
            rotateSpeed = driftTurnSpeed;
        else
            rotateSpeed = turnSpeed;

        Vector2 rotationPoint = transform.position; // Point to rotate around
        Vector2 debugPoint = transform.position; // Point to draw debug line to
        if (Mathf.Abs(playerInput.y) > 0.1)
        {
            if (playerInput.x < 0)
            {
                rotationPoint += (Vector2)(-transform.right * treadOffset);
            }
            else if (playerInput.x > 0)
            {
                rotationPoint += (Vector2)transform.right * treadOffset;
            }
        }
        transform.RotateAround(rotationPoint, transform.forward, -playerInput.x * rotateSpeed * turnSpeedMultiplier * Time.deltaTime);
        rb.angularVelocity = 0f;
    }

    private void DelayChargingDrive(float delay)
    {
        chargePauseEndTime = Time.time + (delay / chargeDelayModifier);
        batterySR.material.color = Color.grey;
    }


    private void ChargeDrive()
    {
        if (Time.time < chargePauseEndTime)
            return; // charging paused
        batterySR.material.color = originalBatteryColor;

        Vector2 velocity = rb.linearVelocity;
        float mag = velocity.magnitude;
        Vector2 up = transform.up;

        if (mag > minChargeMoveSpeed)
        {
            float fAngle = Vector2.Angle(up, velocity);
            float bAngle = Vector2.Angle(-up, velocity);

            Debug.DrawRay(transform.position, velocity * 2, Color.red);
            if (fAngle > chargeAngle && bAngle > chargeAngle)
            {
                if (DriveCharge >= 100f)
                    DriveCharge = 100f; // Cap the charge at 100%
                else
                    DriveCharge += (chargePerSecond * Time.deltaTime) * (mag / maxDriftSpeed) * Mathf.Abs(playerInput.y);
                timeLastCharge = Time.time; // Reset the charge fade timer
            }
        }
    }

    private void FadeDrive()
    {
        if (Time.time < chargePauseEndTime)
            return; // fading paused

        if (Time.time - timeLastCharge > chargeFadeDelay && DriveCharge > 0f)
            DriveCharge -= (dischargePerSecond * Time.deltaTime) * Mathf.Clamp01((Time.time - timeLastCharge) / chargeFadeDelay);
        else
            DriveCharge = Mathf.Max(DriveCharge, 0f); // Ensure charge doesn't go below 0
    }


    void TurretRotate()
    {
        Vector2 direction;
        if (mouseAiming)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            direction = mousePos - (Vector2)transform.position;
        }
        else
        {
            if (aimInput.magnitude < 0.1f)
            {
                turret.transform.rotation = transform.rotation * lastTurretDirection * Quaternion.Euler(0f, 0f, -90f);
            }
            else
            {
                direction = new Vector2(aimInput.x, aimInput.y);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                turret.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

                // Store direction relative to player
                lastTurretDirection = Quaternion.Inverse(transform.rotation) * Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    public void Fire()
    {
        firingSystem.FireCommand();
    }

    //upgrade functions
    public void UpgradeMoveSpeed(float multiplier)
    {
        maxSpeed *= multiplier;
        maxDriftSpeed *= multiplier;
        turnSpeed *= (multiplier - 1) / 2 + 1;
        driftTurnSpeed *= (multiplier - 1) / 2 + 1;
        Debug.Log($"Speed upgraded! New speed: {moveSpeed}");
    }
    public void UpgradeDriveChargeSpeed(float multiplier)
    {
        chargePerSecond *= multiplier;
        chargeDelayModifier *= multiplier;
        Debug.Log($"Drive charge speed upgraded! New charge speed: {chargePerSecond}");
    }

    private void HandleDriftTrails(bool drifting)
    {
        if (drifting)
        {
            // Stop any fading routines
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            // Enable the trails
            trailLeft.emitting = true;
            trailRight.emitting = true;
        }
        else
        {
            // Detach trails so they linger independently
            DetachTrail(trailLeft);
            DetachTrail(trailRight);
        }
    }

    private void DetachTrail(TrailRenderer originalTrail)
    {
        // Create a temporary clone of the trail
        GameObject trailClone = new GameObject("TrailClone");
        trailClone.transform.position = originalTrail.transform.position;
        trailClone.transform.rotation = originalTrail.transform.rotation;

        TrailRenderer cloneRenderer = trailClone.AddComponent<TrailRenderer>();
        cloneRenderer.material = originalTrail.material;
        cloneRenderer.startWidth = originalTrail.startWidth;
        cloneRenderer.endWidth = originalTrail.endWidth;
        cloneRenderer.time = originalTrail.time;
        cloneRenderer.startColor = originalTrail.startColor;
        cloneRenderer.endColor = originalTrail.endColor;
        cloneRenderer.numCapVertices = originalTrail.numCapVertices;
        cloneRenderer.numCornerVertices = originalTrail.numCornerVertices;

        // Stop the original from emitting
        originalTrail.emitting = false;

        // Start fading the clone
        fadeRoutine = StartCoroutine(FadeOutAndDestroyTrail(cloneRenderer));
    }

    private IEnumerator FadeOutAndDestroyTrail(TrailRenderer trail)
    {
        float initialTime = trail.time;
        float elapsed = 0f;

        while (elapsed < trailFadeOutTime)
        {
            trail.time = Mathf.Lerp(initialTime, 0f, elapsed / trailFadeOutTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(trail.gameObject);
    }

    public void CancelDrift()
    {
        if (driftPressed)
        {
            driftPressed = false;
            HandleDriftTrails(false);
        }
    }
}
