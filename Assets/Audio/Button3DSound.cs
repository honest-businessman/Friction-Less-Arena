using UnityEngine;
using FMODUnity;

public class Button3DSound : MonoBehaviour
{
    public EventReference hoverSound;
    public EventReference clickSound;

    private bool hasHovered = false;

    void OnMouseEnter()
    {
        if (!hasHovered)
        {
            RuntimeManager.PlayOneShot(hoverSound);
            hasHovered = true;
        }
    }

    void OnMouseExit()
    {
        hasHovered = false;
    }

    void OnMouseDown()
    {
        RuntimeManager.PlayOneShot(clickSound);
    }
}
