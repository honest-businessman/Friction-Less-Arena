using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using FMODUnity;

public class HealthSystem : MonoBehaviour
{
    public bool vulnerable = true;
    public int health = 3;
    public int maxHealth = 3;
    public bool regenerateHealth = false;
    public int regenAmount = 3;
    public float regenDelay = 5f;
    public event Action<int, int> OnDamageTaken;
    public event Action OnDie;
    public UnityEvent OnRegenStart;
    public UnityEvent OnRegenFinish;

    [Header("FMOD Events")]
    [SerializeField] private EventReference DamageEvent;         // Player takes damage
    [SerializeField] private EventReference PlayerDeathEvent;   // Player dies
    [SerializeField] private EventReference EnemyDeathEvent;    // Enemy dies
    [SerializeField] private EventReference RegenHealthEvent;   // Health regeneration SFX

    [Header("Death VFX")]
    [SerializeField] GameObject DeathVFX;

    [Header("XP Settings")]
    [SerializeField] private XPContainer xpContainer;
    [SerializeField] private bool dropXP = false;

    [Header("Sprite Change Settings")]
    [SerializeField] private SpriteRenderer targetRenderer1;
    [SerializeField] private SpriteRenderer targetRenderer2;
    [SerializeField] private SpriteRenderer targetRenderer3;
    [SerializeField] private SpriteRenderer targetRenderer4;

    [SerializeField] private Sprite spriteAt3HP;
    [SerializeField] private Sprite spriteAt2HP;
    [SerializeField] private Sprite spriteAt1HP;

    private Coroutine regenCoroutine;

    void Start()
    {
        UpdateSprite();
    }

    public void TakeDamage(int damage)
    {
        if (!vulnerable) return;

        Debug.Log($"{gameObject.name} taken {damage} damage!");
        health -= damage;
        OnDamageTaken?.Invoke(health, maxHealth);

        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        if (health <= 0)
        {
            health = 0;
            Die();
        }
        else
        {
            UpdateSprite();

            if (regenerateHealth)
            {
                regenCoroutine = StartCoroutine(StartRegenerateDelay());
                OnRegenStart?.Invoke();
            }

            if (CompareTag("Player"))
            {
                RuntimeManager.PlayOneShot(DamageEvent, transform.position);

                GlitchFlash glitch = GetComponent<GlitchFlash>();
                if (glitch != null)
                    glitch.TriggerGlitch();
            }
        }
    }

    public void GainHealth(int gainedHealth)
    {
        health = Mathf.Min(health + gainedHealth, maxHealth);
        UpdateSprite();
    }

    public void GainMaxHealth()
    {
        health = maxHealth;
        UpdateSprite();
    }

    private void Die()
    {
        if (DeathVFX != null)
        {
            GameObject explosion = Instantiate(DeathVFX, transform.position, Quaternion.identity);
            Destroy(explosion, 2f);
        }

        if (CompareTag("Player"))
            RuntimeManager.PlayOneShot(PlayerDeathEvent, transform.position);
        else
            RuntimeManager.PlayOneShot(EnemyDeathEvent, transform.position);

        if (!CompareTag("Player") && xpContainer != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                xpContainer.SpawnXP(transform.position, player.transform);
        }

        OnDie?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator StartRegenerateDelay()
    {
        Debug.Log($"{gameObject.name} is regenerating {regenAmount} health in {regenDelay} seconds!");
        yield return new WaitForSeconds(regenDelay);

        regenCoroutine = null;
        GainHealth(regenAmount);

        // Play regen SFX
        if (RegenHealthEvent.IsNull == false)
            RuntimeManager.PlayOneShot(RegenHealthEvent, transform.position);

        OnRegenFinish?.Invoke();
        Debug.Log($"{gameObject.name} has regenerated {regenAmount} health!");
    }

    private void OnDestroy() { }

    public void ClearOnDie()
    {
        OnDie = null;
    }

    private void UpdateSprite()
    {
        Sprite newSprite = null;

        if (health >= 3) newSprite = spriteAt3HP;
        else if (health == 2) newSprite = spriteAt2HP;
        else if (health == 1) newSprite = spriteAt1HP;

        ApplySprite(targetRenderer1, newSprite);
        ApplySprite(targetRenderer2, newSprite);
        ApplySprite(targetRenderer3, newSprite);
        ApplySprite(targetRenderer4, newSprite);
    }

    private void ApplySprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer != null) renderer.sprite = sprite;
    }
}
