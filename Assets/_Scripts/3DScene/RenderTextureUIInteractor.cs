using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RenderTextureUIInteractor : MonoBehaviour
{
    [Header("References")]
    public RenderTexture renderTexture;
    public RectTransform clickIndicator;
    [SerializeField] private bool useClickIndicator = false;

    [Header("Debug")]
    public bool isUIActive = true;

    private Camera mainCam;
    private EventSystem eventSystem;

    private PointerEventData pointerData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    // Multi-canvas support
    private List<Canvas> trackedCanvases = new List<Canvas>();

    private GameObject currentHoverObject;
    private Slider currentDraggingSlider = null;

    [SerializeField] private float scale = 1f;
    [SerializeField] private float texAspect = 1f;
    [SerializeField] private float screenAspect = 1f;

    void Start()
    {
        mainCam = Camera.main;
        eventSystem = EventSystem.current;
        UpdateCanvasList();
    }

    void Update()
    {
        if (!isUIActive)
            return;

        trackedCanvases.RemoveAll(c => c == null);
        UpdateCanvasList();

        // Raycast against 3D plane
        if (!Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit) || hit.transform != transform)
        {
            ClearHover();
            currentDraggingSlider = null;
            if (clickIndicator != null && useClickIndicator)
                clickIndicator.gameObject.SetActive(false);
            return;
        }

        // Improved UV calculation
        Vector2 uv = hit.textureCoord;
        float scaleX = screenAspect > texAspect ? texAspect / screenAspect : 1f;
        float scaleY = screenAspect < texAspect ? screenAspect / texAspect : 1f;
        scaleX /= scale;
        scaleY /= scale;
        uv = (uv - Vector2.one * 0.5f) * new Vector2(scaleX, scaleY) + Vector2.one * 0.5f;

        if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            return;

        GameObject newHover = null;
        Vector2 lastScreenPos = Vector2.zero;

        foreach (var canvas in trackedCanvases)
        {
            if (canvas == null) continue;

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) continue;

            Camera uiCam = canvas.worldCamera;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            Vector2 localPos = new Vector2(
                uv.x * canvasRect.sizeDelta.x,
                uv.y * canvasRect.sizeDelta.y
            );
            localPos -= canvasRect.sizeDelta * canvasRect.pivot;


            // Click indicator support
            if (canvas == trackedCanvases[0] && clickIndicator != null && useClickIndicator)
            {
                clickIndicator.gameObject.SetActive(true);
                clickIndicator.anchoredPosition = localPos;
                if (Input.GetMouseButtonUp(0))
                    clickIndicator.gameObject.SetActive(false);
            }

            Vector2 screenPos = uiCam.WorldToScreenPoint(canvas.transform.TransformPoint(localPos));
            lastScreenPos = screenPos;
            pointerData = new PointerEventData(eventSystem) { position = screenPos };

            raycastResults.Clear();
            raycaster.Raycast(pointerData, raycastResults);

            // Skip "Blocker" objects in the results
            GameObject target = null;
            foreach (var result in raycastResults)
            {
                if (result.gameObject.name.Contains("Blocker")) continue;
                target = result.gameObject;
                break;
            }

            if (target == null) continue;

            Selectable selectable = target.GetComponentInParent<Selectable>();
            if (selectable != null)
                target = selectable.gameObject;

            newHover = target;

            Slider slider = target.GetComponentInParent<Slider>();
            if (slider != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    currentDraggingSlider = slider;
                    UpdateSliderValue(slider, screenPos, uiCam);
                }
                if (currentDraggingSlider == slider && Input.GetMouseButton(0))
                    UpdateSliderValue(slider, screenPos, uiCam);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
            }

            break; // stop after first valid UI hit
        }

        HandleHover(newHover);

        if (currentDraggingSlider != null && Input.GetMouseButtonUp(0))
        {
            UpdateSliderValue(currentDraggingSlider, lastScreenPos, currentDraggingSlider.GetComponentInParent<Canvas>().worldCamera);
            currentDraggingSlider = null;
        }
    }

    private void UpdateCanvasList()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (var c in allCanvases)
        {
            if (!trackedCanvases.Contains(c))
            {
                trackedCanvases.Add(c);
                if (c.GetComponent<GraphicRaycaster>() == null)
                    c.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    // Utility function to update the slider value based on the current pointer position
    private void UpdateSliderValue(Slider slider, Vector2 screenPos, Camera uiCam)
    {
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderRect, screenPos, uiCam, out localPoint);

        float pct = 0f;
        if (slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft)
            pct = Mathf.InverseLerp(sliderRect.rect.xMin, sliderRect.rect.xMax, localPoint.x);
        else
            pct = Mathf.InverseLerp(sliderRect.rect.yMin, sliderRect.rect.yMax, localPoint.y);

        if (slider.direction == Slider.Direction.RightToLeft || slider.direction == Slider.Direction.TopToBottom)
            pct = 1f - pct;

        slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, pct);
        slider.onValueChanged.Invoke(slider.value);
    }

    void HandleHover(GameObject target)
    {
        if (currentHoverObject != target)
        {
            if (currentHoverObject != null)
                ExecuteEvents.Execute(currentHoverObject, pointerData, ExecuteEvents.pointerExitHandler);

            if (target != null)
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);

            currentHoverObject = target;
        }
    }

    void ClearHover()
    {
        if (currentHoverObject != null)
        {
            ExecuteEvents.Execute(currentHoverObject, pointerData, ExecuteEvents.pointerExitHandler);
            currentHoverObject = null;
        }
    }
}