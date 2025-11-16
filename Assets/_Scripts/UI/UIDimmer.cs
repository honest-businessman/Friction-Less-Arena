using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UIDimmer : MonoBehaviour
{
    [Range(0f, 1f)]
    public float dimAmount = 0f; // 0 = normal, 1 = completely black

    private RawImage img;

    void OnValidate()
    {
        if (img == null) img = GetComponent<RawImage>();
        img.color = new Color(0, 0, 0, dimAmount);
    }
}
