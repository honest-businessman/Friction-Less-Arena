using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class RenderTextureUIInteractor : MonoBehaviour
{
    [Header("References")]
    public RenderTexture renderTexture;
    public Canvas uiCanvas;  // The Canvas rendering into the RenderTexture
    public RectTransform clickIndicator;  // Optional visual indicator for clicks
    [SerializeField] private bool useClickIndicator = false;

    [Header("Debug")]
    public bool isUIActive = true;

    private Camera mainCam;    // The camera viewing the 3D plane
    private Camera uiCam;      // The camera rendering the Canvas
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;

    private PointerEventData pointerData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    [SerializeField] private float scale;   // Scale factor for UV mapping
    [SerializeField] private float texAspect;   // Aspect ratio of the RenderTexture
    [SerializeField] private float screenAspect;  // Aspect ratio of the screen

    private GameObject currentHoverObject;
    private Slider currentDraggingSlider = null;  // Keep track of the slider currently being dragged

    void Start()
    {
        mainCam = Camera.main;
        if (uiCanvas != null)
        {
            // Set up references for the UI camera, raycaster and EventSystem
            uiCam = uiCanvas.worldCamera;
            raycaster = uiCanvas.GetComponent<GraphicRaycaster>();
            eventSystem = EventSystem.current;

            if (raycaster == null)
                Debug.LogError("No GraphicRaycaster on assigned UI Canvas");
            if (eventSystem == null)
                Debug.LogError("No EventSystem in scene");
        }
/*        if(useClickIndicator) { clickIndicator.gameObject.SetActive(true); }
        else { clickIndicator.gameObject.SetActive(false); }*/
    }

    void Update()
    {
        if (!isUIActive || uiCanvas == null || raycaster == null || eventSystem == null)
            return;

        // Raycast from the main camera to the 3D plane to find the hit point
        if (!Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            ClearHover();
            currentDraggingSlider = null;
            return;
        }

        // Check if the hit object is the plane we want to interact with
        if (hit.transform != transform)
        {
            ClearHover();
            currentDraggingSlider = null;
            return;
        }

        // Get the UV coordinates of the hit point on the plane
        Vector2 uv = hit.textureCoord;

        // Adjust UV coordinates based on screen and texture aspect ratios
        float scaleX = screenAspect > texAspect ? texAspect / screenAspect : 1f;
        float scaleY = screenAspect < texAspect ? screenAspect / texAspect : 1f;
        scaleX /= scale;
        scaleY /= scale;

        // Center UV coordinates and scale them
        uv = (uv - Vector2.one * 0.5f) * new Vector2(scaleX, scaleY) + Vector2.one * 0.5f;

        // Ignore UVs outside the visible UI area
        if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            return;

        // Convert UV coordinates to local position inside the Canvas
        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        Vector2 localPos = new Vector2(
            (uv.x - 0.5f) * canvasRect.sizeDelta.x,
            (uv.y - 0.5f) * canvasRect.sizeDelta.y
        );

        // Update the click indicator position if it exists
        if (clickIndicator != null && useClickIndicator)
        {
            clickIndicator.gameObject.SetActive(true);
            clickIndicator.anchoredPosition = localPos;
            if (Input.GetMouseButtonUp(0))
                clickIndicator.gameObject.SetActive(false);
        }

        // Convert local Canvas position to world position and then to screen coordinates
        Vector2 screenPos = uiCam.WorldToScreenPoint(uiCanvas.transform.TransformPoint(localPos));
        pointerData = new PointerEventData(eventSystem) { position = screenPos };

        // Raycast from the pointer to UI elements
        raycastResults.Clear();
        raycaster.Raycast(pointerData, raycastResults);

        if (raycastResults.Count > 0)
        {
            GameObject target = raycastResults[0].gameObject;

            // If the hit element is inside a Selectable UI element, select the top level parent
            Selectable selectable = target.GetComponentInParent<Selectable>();
            if (selectable != null)
                target = selectable.gameObject;

            HandleHover(target);

            // Check if the hit element is a slider
            Slider slider = target.GetComponentInParent<Slider>();

            // Start dragging slider on mouse down
            if (slider != null && Input.GetMouseButtonDown(0))
            {
                currentDraggingSlider = slider;
                UpdateSliderValue(slider, screenPos);
            }
            // Non-slider UI elements
            else if (slider == null && Input.GetMouseButtonDown(0))
            {
                // Manually trigger pointer down, up, and click events
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
            }
        }

        // If a slider is being dragged, update its value continuously while the mouse button is held
        if (currentDraggingSlider != null && Input.GetMouseButton(0))
        {
            UpdateSliderValue(currentDraggingSlider, screenPos);
        }

        // Stop dragging the slider when mouse button is released
        if (currentDraggingSlider != null && Input.GetMouseButtonUp(0))
        {
            UpdateSliderValue(currentDraggingSlider, screenPos);
            currentDraggingSlider = null;
        }
    }

    // Utility function to update the slider value based on the current pointer position
    private void UpdateSliderValue(Slider slider, Vector2 screenPos)
    {
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderRect, screenPos, uiCam, out localPoint);

        // Determine the percentage along the slider
        float pct = 0f;
        if (slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft)
            pct = Mathf.InverseLerp(sliderRect.rect.xMin, sliderRect.rect.xMax, localPoint.x);
        else
            pct = Mathf.InverseLerp(sliderRect.rect.yMin, sliderRect.rect.yMax, localPoint.y);

        // Adjust for sliders that go in the opposite direction
        if (slider.direction == Slider.Direction.RightToLeft || slider.direction == Slider.Direction.TopToBottom)
            pct = 1f - pct;

        // Set the slider value and invoke the onValueChanged event
        slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, pct);
        slider.onValueChanged.Invoke(slider.value);
    }

    // Handle hover events for UI elements
    void HandleHover(GameObject target)
    {
        if (currentHoverObject != target)
        {
            if (currentHoverObject != null)
                ExecuteEvents.Execute(currentHoverObject, pointerData, ExecuteEvents.pointerExitHandler);

            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);
            currentHoverObject = target;
        }
    }

    // Clear hover state when no UI element is under the pointer
    void ClearHover()
    {
        if (currentHoverObject != null)
        {
            ExecuteEvents.Execute(currentHoverObject, pointerData, ExecuteEvents.pointerExitHandler);
            currentHoverObject = null;
        }
    }

    // Setup function to assign a Canvas at runtime
    public void SetupCanvas(Canvas canvas)
    {
        uiCanvas = canvas;
        uiCam = uiCanvas.worldCamera;
        raycaster = uiCanvas.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;
        clickIndicator = uiCanvas.transform.Find("ClickIndicator") as RectTransform;

        if (raycaster == null)
            Debug.LogError("No GraphicRaycaster on assigned UI Canvas");
        if (eventSystem == null)
            Debug.LogError("No EventSystem in scene");
    }
}
