using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BarnEntryMessage : MonoBehaviour
{
    public Transform barn;
    public CharacterController player;
    [Tooltip("Entry region in the barn's local coordinates; shown when selected.")]
    public Bounds interior = new Bounds(new Vector3(0f, 5.825f, 0f),
        new Vector3(17.2f, 11.65f, 15.4f));
    [TextArea] public string message = "Entering the barn";
    [Min(0.5f)] public float displaySeconds = 3f;
    [Min(0.01f)] public float fadeSeconds = 0.4f;

    private GameObject messageCanvas;
    private CanvasGroup visibility;
    private Text messageText;
    private bool initialized;
    private bool wasInside;
    private float shownAt = float.NegativeInfinity;

    private void OnEnable()
    {
        if (barn == null || player == null)
        {
            Debug.LogWarning("Assign the barn Transform and player CharacterController to BarnEntryMessage.", this);
            enabled = false;
            return;
        }
        if (messageCanvas == null)
            CreateMessageUI();
    }

    private void Update()
    {
        if (barn == null || player == null || visibility == null)
            return;

        // Use the player's foot position, not the follow camera or capsule radius.
        // A volume check needs no extra Rigidbody and cannot block a doorway.
        Vector3 feet = player.transform.TransformPoint(player.center
            - Vector3.up * player.height * 0.5f) + Vector3.up * 0.15f;
        Bounds region = interior;
        if (wasInside)
            region.Expand(new Vector3(0.5f, 0.5f, 0.5f));
        bool inside = player.gameObject.activeInHierarchy && region.Contains(barn.InverseTransformPoint(feet));
        if (initialized && inside && !wasInside)
        {
            messageText.text = message;
            shownAt = Time.unscaledTime;
        }
        initialized = true;
        wasInside = inside;

        float age = Time.unscaledTime - shownAt;
        float duration = Mathf.Max(0.5f, displaySeconds);
        float fade = Mathf.Min(Mathf.Max(0.01f, fadeSeconds), duration * 0.5f);
        visibility.alpha = age >= duration ? 0f
            : Mathf.Min(Mathf.Clamp01(age / fade), Mathf.Clamp01((duration - age) / fade));
    }

    private void CreateMessageUI()
    {
        messageCanvas = new GameObject("Barn Entry Message UI", typeof(Canvas),
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
        if (visibility != null)
            visibility.alpha = 0f;
        shownAt = float.NegativeInfinity;
        initialized = false;
        wasInside = false;
    }

    private void OnDestroy()
    {
        if (messageCanvas != null)
            Destroy(messageCanvas);
    }

    private void OnDrawGizmosSelected()
    {
        if (barn == null)
            return;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = barn.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.8f);
        Gizmos.DrawWireCube(interior.center, interior.size);
        Gizmos.matrix = previous;
    }
}
