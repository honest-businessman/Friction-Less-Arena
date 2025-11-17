using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using FMODUnity;

public class Button3D : MonoBehaviour
{
    public UnityEvent onActivate;

    [Header("FMOD Events")]
    [SerializeField] private EventReference hoverSound;
    [SerializeField] private EventReference clickSound;

    private MeshRenderer meshRenderer;
    private Material originalMaterial;

    private bool isActivated = false;
    private bool isHovered = false; // prevents repeated hover-sound spam

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        originalMaterial = meshRenderer.material;
        originalMaterial.color = ScreenManager.Instance.colorButton;
    }

    private void OnMouseEnter()
    {
        if (isActivated) return;
        Select(); // no direct PlayHoverSound here so controller Select() and mouse both go through Select()
    }

    private void OnMouseExit()
    {
        if (isActivated) return;
        Deselect();
    }

    private void OnMouseDown()
    {
        Press();
    }

    public void Press()
    {
        if (isActivated) return;

        PlayClickSound();  // click plays here for all input methods
        isHovered = false;  // reset hover state when pressed
        originalMaterial.color = ScreenManager.Instance.colorButtonActive;
        StartCoroutine(Activate());
    }

    public void Select()
    {
        if (isActivated || isHovered) return; // don't re-fire hover sound while already hovered
        isHovered = true;
        PlayHoverSound();
        originalMaterial.color = ScreenManager.Instance.colorButtonHover;
    }

    public void Deselect()
    {
        if (isActivated) return;
        isHovered = false;
        originalMaterial.color = ScreenManager.Instance.colorButton;
    }

    private IEnumerator Activate()
    {
        isActivated = true;
        onActivate.Invoke();
        yield return new WaitForSeconds(1f);
        originalMaterial.color = ScreenManager.Instance.colorButton;
        isActivated = false;
    }

    // ---------------------
    // FMOD AUDIO HELPERS
    // ---------------------

    private void PlayHoverSound()
    {
        if (!hoverSound.IsNull)
            RuntimeManager.PlayOneShot(hoverSound, transform.position);
    }

    private void PlayClickSound()
    {
        if (!clickSound.IsNull)
            RuntimeManager.PlayOneShot(clickSound, transform.position);
    }
}
