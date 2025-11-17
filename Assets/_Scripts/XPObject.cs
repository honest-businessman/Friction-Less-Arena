using UnityEngine;

public class XPObject : MonoBehaviour
{
    [Header("Behaviour Settings")]
    [SerializeField] public int xpValue = 1;
    [SerializeField] public float moveSpeed = 3f;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float pickupRadius = 1f;

    [Header("Sprite Settings")]
    [SerializeField] private float baseSize = 0.04f;
    [SerializeField] private float sizeIncreasePerValue = 0.04f;
    [Header("Audio")]
    [SerializeField] private FMODUnity.EventReference xpPickupEvent;

    private bool isAttracting = false;
    


    // Update is called once per frame
    void Update()
    {
        if (playerTarget == null) return;

        float dist = Vector3.Distance(transform.position, playerTarget.position);

        if (!isAttracting && dist <= pickupRadius) isAttracting = true;


        if(isAttracting)
        {
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;

            //if (Vector3.Distance(transform.position, playerTarget.position) <= 0.1f)
            //{
            //    LevelSystem levelSystem = playerTarget.GetComponent<LevelSystem>();
            //    if (levelSystem != null)
            //    {
            //        levelSystem.AddXP(xpValue);
            //    }
            //    Destroy(gameObject);
            //}
        }
    }

    public void Initialize(int value, Transform player, float radius)
    {
        xpValue = value;
        playerTarget = player;
        pickupRadius = radius;

        float scale = baseSize + (xpValue * sizeIncreasePerValue);
        transform.localScale = new Vector3(scale, scale, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelSystem levelSystem = other.GetComponent<LevelSystem>();
            if (levelSystem != null)
            {
                levelSystem.AddXP(xpValue);
            }

            // Play XP pickup sound via PlayerController
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                FMODUnity.EventReference xpEvent = player.XPPickupEvent;

                if (!string.IsNullOrEmpty(xpEvent.Guid.ToString())) // simple check
                {
                    FMOD.Studio.EventInstance xpInstance = FMODUnity.RuntimeManager.CreateInstance(xpEvent);
                    xpInstance.start();
                    xpInstance.release();
                }
            }

            Destroy(gameObject);
        }
    }



}
