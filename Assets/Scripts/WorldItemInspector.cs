using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldItemInspector : MonoBehaviour
{
    public Camera viewCamera;
    public CharacterController player;
    public CanvasGroup startupOverlay;
    [Tooltip("Maximum camera ray distance; zero uses the camera far plane.")]
    [Min(0f)] public float rayDistance = 0f;
    [Min(0.5f)] public float displaySeconds = 4f;

    [System.Serializable]
    public class ItemLabel
    {
        public Transform item;
        public string displayName;
    }
    [Tooltip("Optional custom labels, matched against the clicked object's parents.")]
    public ItemLabel[] customLabels = new ItemLabel[0];

    private GameObject messageCanvas;
    private CanvasGroup visibility;
    private Text messageText;
    private float shownAt = float.NegativeInfinity;

    private void OnEnable()
    {
        if (viewCamera == null || player == null)
        {
            Debug.LogWarning("WorldItemInspector needs the camera and player assigned.", this);
            enabled = false;
            return;
        }
        if (messageCanvas == null)
            CreateMessageUI();
    }

    private void Update()
    {
        if (visibility == null || viewCamera == null || player == null)
            return;

        float age = Time.unscaledTime - shownAt;
        float duration = Mathf.Max(0.5f, displaySeconds);
        visibility.alpha = age >= duration ? 0f
            : Mathf.Min(1f, Mathf.Clamp01((duration - age) / 0.4f));

        if (!Input.GetMouseButtonDown(0))
            return;
        if (startupOverlay != null && startupOverlay.gameObject.activeInHierarchy && startupOverlay.alpha > 0.01f)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance > 0f ? Mathf.Min(rayDistance, viewCamera.farClipPlane) : viewCamera.farClipPlane,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        float nearest = float.PositiveInfinity;
        RaycastHit selected = new RaycastHit();
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(player.transform))
                continue;
            if (hit.collider.isTrigger && hit.collider.GetComponentInParent<InspectableItem>() == null)
                continue;
            if (hit.distance < nearest)
            {
                selected = hit;
                nearest = hit.distance;
                found = true;
            }
        }

        // Keep the nearest solid hit: never skip a wall to select an item behind it.
        if (!found) return;
        Transform clicked = selected.collider.transform;
        if (clicked.GetComponentInParent<StoryPickupItem>() != null) return;
        string label = ResolveLabel(clicked);
        if (string.IsNullOrEmpty(label))
            return;
        messageText.text = label;
        shownAt = Time.unscaledTime;
        visibility.alpha = 1f;
    }

    private string ResolveLabel(Transform clicked)
    {
        InspectableItem explicitItem = clicked.GetComponentInParent<InspectableItem>();
        if (explicitItem != null)
            return explicitItem.displayName + (string.IsNullOrWhiteSpace(explicitItem.description)
                ? "" : "\n" + explicitItem.description);
        string label = null;
        for (Transform current = clicked; current != null; current = current.parent)
        {
            if (customLabels != null)
                foreach (ItemLabel custom in customLabels)
                    if (custom != null && custom.item == current && !string.IsNullOrWhiteSpace(custom.displayName))
                        return custom.displayName;
            if (IsNaturalScenery(current.name)) return null;
            string building = StructureLocationMessages.LabelFor(current.name);
            if (building != null) return label ?? building;
            string known = LabelFor(current.name);
            // Imported Box001 mesh names are not evidence that a building is a crate.
            if (Normalize(current.name) == "box" && current.name.IndexOf('(') < 0
                && current.name != "Box" && current.name != "box") continue;
            if (known != null) label = known;
        }
        return label;
    }

    public static string Normalize(string objectName)
    {
        string key = "";
        foreach (char character in objectName.Replace("(Clone)", ""))
            if (char.IsLetter(character)) key += char.ToLowerInvariant(character);
        return key;
    }

    public static bool IsNaturalScenery(string objectName)
    {
        string key = Normalize(objectName);
        return key.StartsWith("tree") || key.StartsWith("grass") || key.StartsWith("bush")
            || key.StartsWith("terrain") || key == "plane" || key == "ground" || key.StartsWith("flower");
    }

    public static string LabelFor(string objectName)
    {
        if (IsNaturalScenery(objectName)) return null;
        string prop = BarnItemInspector.LabelFor(objectName);
        if (prop != null) return prop;
        string building = StructureLocationMessages.LabelFor(objectName);
        if (building != null) return building;
        switch (Normalize(objectName))
        {
            case "waterbarrel": case "barrelofwater": return "Water barrel";
            case "watertank": case "watercontainer": return "Water container";
            case "fence": return "Fence";
            case "ladder": case "barnloftladder": return "Ladder";
            case "windmill": return "Windmill";
            default: return null;
        }
    }

    private void CreateMessageUI()
    {
        messageCanvas = new GameObject("World Item Identification UI", typeof(Canvas),
            typeof(CanvasScaler), typeof(CanvasGroup));
        Canvas canvas = messageCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = messageCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        visibility = messageCanvas.GetComponent<CanvasGroup>();
        visibility.alpha = 0f;
        visibility.interactable = false;
        visibility.blocksRaycasts = false;

        GameObject panel = new GameObject("Banner", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(messageCanvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.15f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(700f, 120f);
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.06f, 0.08f, 0.1f, 0.88f);
        background.raycastTarget = false;

        GameObject label = new GameObject("Message", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(panel.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 10f);
        labelRect.offsetMax = new Vector2(-20f, -10f);
        messageText = label.GetComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 27;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.white;
        messageText.raycastTarget = false;
    }

    private void OnDisable()
    {
        if (visibility != null) visibility.alpha = 0f;
        shownAt = float.NegativeInfinity;
    }

    private void OnDestroy()
    {
        if (messageCanvas != null) Destroy(messageCanvas);
    }
}
