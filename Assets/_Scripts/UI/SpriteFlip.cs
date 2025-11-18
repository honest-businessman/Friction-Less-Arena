using UnityEngine;
using UnityEngine.UI;

public class SpriteFlip : MonoBehaviour
{
    private void Start()
    {
        FixAllSpritesInScene();
    }

    private void FixAllSpritesInScene()
    {
        // Fix all UI Images
        Image[] uiImages = FindObjectsOfType<Image>(true);
        foreach (var img in uiImages)
            NormalizeRectTransform(img.rectTransform);

        // Fix all SpriteRenderers
        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>(true);
        foreach (var sr in renderers)
            NormalizeTransform(sr.transform);

        Debug.Log("SpriteUnflipper: All sprites normalized.");
    }

    private void NormalizeRectTransform(RectTransform rt)
    {
        Vector3 scale = rt.localScale;
        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        rt.localScale = scale;
    }

    private void NormalizeTransform(Transform t)
    {
        Vector3 scale = t.localScale;
        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        t.localScale = scale;
    }
}
