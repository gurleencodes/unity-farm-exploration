using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StructureLocationMessages : MonoBehaviour
{
    public CharacterController player;
    [Min(0f)] public float approachDistance = 3f;
    [Min(0.5f)] public float displaySeconds = 3f;
    [Min(0.01f)] public float fadeSeconds = 0.4f;

    private sealed class Location
    {
        public Transform target;
        public string label;
        public Bounds bounds;
    }

    private readonly List<Location> locations = new List<Location>();
    private GameObject messageCanvas;
    private CanvasGroup visibility;
    private Text messageText;
    private int currentLocation = -1;
    private float shownAt = float.NegativeInfinity;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("Assign the player CharacterController to StructureLocationMessages.", this);
            enabled = false;
            return;
        }
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            Discover(root.transform);
        CreateMessageUI();
    }

    private void Discover(Transform candidate)
    {
        string label = LabelFor(candidate.name);
        if (label != null)
        {
            Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(candidate.position, Vector3.one);
            bool found = false;
            foreach (Renderer item in renderers)
            {
                if (!found) { bounds = item.bounds; found = true; }
                else bounds.Encapsulate(item.bounds);
            }
            // Windmill blades should not trigger a label far from its base.
            if (label == "Windmill" || label == "Well")
            {
                Vector3 size = bounds.size;
                size.x = Mathf.Min(size.x, 8f);
                size.z = Mathf.Min(size.z, 8f);
                bounds.size = size;
            }
            locations.Add(new Location { target = candidate, label = label, bounds = bounds });
            return; // A barn's nested doors and beams are not separate locations.
        }
        foreach (Transform child in candidate)
            Discover(child);
    }

    public static string LabelFor(string objectName)
    {
        string key = "";
        foreach (char character in objectName.Replace("(Clone)", ""))
            if (char.IsLetter(character)) key += char.ToLowerInvariant(character);
        switch (key)
        {
            case "barn": case "barnbase": return "Barn";
            case "windmill": return "Windmill";
            case "well": return "Well";
            case "house": case "houseprefab": return "House";
            case "housegreenprefab": case "greenhouse": return "Green House";
            case "oldshed": return "Old Shed";
            case "shed": return "Shed";
            case "silo": return "Silo";
            case "outhouse": case "outhousebase": return "Outhouse";
            case "chickenhouse": return "Chicken House";
            case "gate": return "Gate";
            case "bridge": return "Bridge";
            default: return null;
        }
    }

    private float DistanceTo(Location location, Vector3 feet)
    {
        feet.y = location.bounds.center.y;
        return Vector3.Distance(feet, location.bounds.ClosestPoint(feet));
    }

    private float Score(Location location, Vector3 feet)
    {
        Vector3 center = location.bounds.center;
        center.y = feet.y;
        return DistanceTo(location, feet) + 0.03f * Vector3.Distance(feet, center);
    }

    private void Update()
    {
        if (player == null || visibility == null)
            return;
        Vector3 feet = player.transform.TransformPoint(player.center
            - Vector3.up * player.height * 0.5f);
        int nearest = -1;
        float bestScore = float.PositiveInfinity;
        for (int i = 0; i < locations.Count; i++)
        {
            Location location = locations[i];
            if (location.target == null || !location.target.gameObject.activeInHierarchy)
                continue;
            if (DistanceTo(location, feet) > Mathf.Max(0f, approachDistance))
                continue;
            float score = Score(location, feet);
            if (score < bestScore) { bestScore = score; nearest = i; }
        }

        // Keep a small exit margin to prevent repeated popups on a boundary.
        if (currentLocation >= 0)
        {
            Location previous = locations[currentLocation];
            if (previous.target != null && previous.target.gameObject.activeInHierarchy
                && DistanceTo(previous, feet) <= Mathf.Max(0f, approachDistance) + 1f
                && (nearest < 0 || Score(previous, feet) <= bestScore + 0.25f))
                nearest = currentLocation;
        }
        if (nearest != currentLocation)
        {
            currentLocation = nearest;
            if (nearest >= 0)
            {
                messageText.text = locations[nearest].label;
                shownAt = Time.unscaledTime;
            }
        }

        float age = Time.unscaledTime - shownAt;
        float duration = Mathf.Max(0.5f, displaySeconds);
        float fade = Mathf.Clamp(fadeSeconds, 0.01f, duration * 0.5f);
        visibility.alpha = age >= duration ? 0f
            : Mathf.Min(Mathf.Clamp01(age / fade), Mathf.Clamp01((duration - age) / fade));
    }

    private void CreateMessageUI()
    {
        messageCanvas = new GameObject("Structure Location UI", typeof(Canvas),
            typeof(CanvasScaler), typeof(CanvasGroup));
        Canvas canvas = messageCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
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
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.85f);
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
        currentLocation = -1;
        shownAt = float.NegativeInfinity;
    }

    private void OnDestroy()
    {
        if (messageCanvas != null) Destroy(messageCanvas);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.8f);
        foreach (Location location in locations)
        {
            Bounds region = location.bounds;
            region.Expand(Mathf.Max(0f, approachDistance) * 2f);
            Gizmos.DrawWireCube(region.center, region.size);
        }
    }
}
