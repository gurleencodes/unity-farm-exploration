using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BarnItemInspector : MonoBehaviour
{
    public Camera viewCamera;
    public CharacterController player;
    public Transform barn;
    public CanvasGroup startupOverlay;
    [Tooltip("Barn region in local coordinates. Includes the walls and loft.")]
    public Bounds barnRegion = new Bounds(new Vector3(0f, 5.825f, 0f),
        new Vector3(19.2f, 11.65f, 17.5f));
    [Min(1f)] public float interactionDistance = 20f;
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
        if (viewCamera == null || player == null || barn == null)
        {
            Debug.LogWarning("BarnItemInspector needs the camera, player and barn assigned.", this);
            enabled = false;
            return;
        }
        if (messageCanvas == null)
            CreateMessageUI();
    }

    private void Update()
    {
        if (visibility == null || viewCamera == null || player == null || barn == null)
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
        RaycastHit[] hits = Physics.RaycastAll(ray, viewCamera.farClipPlane,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        RaycastHit selected = new RaycastHit();
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(player.transform))
                continue;
            if (hit.distance < nearest)
            {
                selected = hit;
                nearest = hit.distance;
                found = true;
            }
        }

        // Keep the nearest solid hit: never skip a wall to select an item behind it.
        if (!found || Vector3.Distance(player.bounds.ClosestPoint(selected.point), selected.point)
            > Mathf.Max(1f, interactionDistance))
            return;
        Transform clicked = selected.collider.transform;
        if (!clicked.IsChildOf(barn) && !barnRegion.Contains(barn.InverseTransformPoint(selected.point)))
            return;

        string label = ResolveLabel(clicked);
        if (string.IsNullOrEmpty(label))
            return;
        messageText.text = label;
        shownAt = Time.unscaledTime;
        visibility.alpha = 1f;
    }

    private string ResolveLabel(Transform clicked)
    {
        string label = null;
        for (Transform current = clicked; current != null; current = current.parent)
        {
            if (customLabels != null)
                foreach (ItemLabel custom in customLabels)
                    if (custom != null && custom.item == current && !string.IsNullOrWhiteSpace(custom.displayName))
                        return custom.displayName;
            if (current == barn)
                return label ?? "Barn structure";
            string known = LabelFor(current.name);
            // Outer prefab names override generic imported child names like Box001.
            if (known != null)
                label = known;
        }
        return label;
    }

    public static string LabelFor(string objectName)
    {
        string key = "";
        foreach (char character in objectName.Replace("(Clone)", ""))
            if (char.IsLetter(character)) key += char.ToLowerInvariant(character);
        switch (key)
        {
            case "barell": case "barrel": return "Barrel";
            case "box": case "crate": return "Crate";
            case "haisquare": case "haybale": return "Hay bale";
            case "haicylinder": return "Round hay bale";
            case "foodtrough": return "Feeding trough";
            case "wheelcart": return "Farm cart";
            case "shovel": return "Shovel";
            case "rake": return "Rake";
            case "bucket": return "Bucket";
            case "stool": return "Stool";
            case "rockingchair": return "Rocking chair";
            case "log": return "Log";
            case "barndoor": return "Barn door";
            case "sidedoor": return "Side door";
            case "railing": return "Railing";
            case "upstairsplanks": return "Loft floorboards";
            case "smallplankceiling": case "plankceilingsmall": case "woodplankceilingbig": return "Wooden ceiling";
            case "insidepillarbig": case "insidepillarsmall": case "sidepillarsinside":
            case "barnpillaroutside": case "pillar": return "Support post";
            case "barnbase": return "Barn structure";
            default: return null;
        }
    }

    private void CreateMessageUI()
    {
        messageCanvas = new GameObject("Barn Item Identification UI", typeof(Canvas),
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
        panelRect.sizeDelta = new Vector2(600f, 86f);
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
        messageText.fontSize = 30;
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
