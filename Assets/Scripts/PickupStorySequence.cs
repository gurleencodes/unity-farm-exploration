using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class PickupStorySequence : MonoBehaviour
{
    public FarmPlayerMotor player;
    public Camera viewCamera;
    public CameraFollow follow;
    public WorldItemInspector inspector;
    public CanvasGroup startupOverlay;
    [Min(1f)] public float pickupRange = 4.5f;
    [Min(0.2f)] public float panSeconds = 1.2f;
    public List<string> collectedClues = new List<string>();
    private StoryPickupItem nearby, activeItem;
    private CharacterController controller;
    private bool presenting, followWasEnabled, inspectorWasEnabled, returning;
    private bool revealText;
    private float textStarted;
    private string sentence, title;
    private Vector3 itemPosition;
    private Quaternion itemRotation;
    private Coroutine sequence;
    private bool Covered => startupOverlay != null && startupOverlay.gameObject.activeInHierarchy && startupOverlay.alpha > .01f;
    private Vector3 Feet => player.transform.TransformPoint(controller.center - Vector3.up * controller.height * .5f);
    private Vector3 Focus => Feet + Vector3.up * (controller.height * Mathf.Abs(player.transform.lossyScale.y) * .55f);

    private void Start()
    {
        if (player == null || viewCamera == null || follow == null)
        {
            Debug.LogWarning("PickupStorySequence requires the player and active follow camera.", this);
            enabled = false;
            return;
        }
        controller = player.GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (controller == null || Covered || presenting) return;
        nearby = null;
        if (player.IsClimbing || !controller.isGrounded) return;
        float best = pickupRange;
        foreach (StoryPickupItem item in FindObjectsOfType<StoryPickupItem>())
        {
            if (item.collected) continue;
            float distance = Vector3.Distance(Feet, item.transform.position);
            if (distance <= best && Visible(item)) { best = distance; nearby = item; }
        }
        if (Input.GetMouseButtonDown(0)
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            StoryPickupItem clicked = ClickedItem();
            if (clicked != null && !clicked.collected
                && Vector3.Distance(Feet, clicked.transform.position) <= pickupRange && Visible(clicked))
                Begin(clicked);
        }
        if (!presenting && Input.GetKeyDown(KeyCode.R) && nearby != null) Begin(nearby);
    }

    private StoryPickupItem ClickedItem()
    {
        float nearest = float.PositiveInfinity;
        StoryPickupItem result = null;
        foreach (RaycastHit hit in Physics.RaycastAll(viewCamera.ScreenPointToRay(Input.mousePosition),
            viewCamera.farClipPlane, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.transform.IsChildOf(player.transform)) continue;
            StoryPickupItem item = hit.collider.GetComponentInParent<StoryPickupItem>();
            if (hit.collider.isTrigger && item == null) continue;
            if (hit.distance >= nearest) continue;
            nearest = hit.distance;
            result = item; // A wall in front of an item blocks selection.
        }
        return result;
    }

    private bool Visible(StoryPickupItem item)
    {
        Vector3 end = item.transform.position + Vector3.up * .25f;
        Vector3 offset = end - Focus;
        foreach (RaycastHit hit in Physics.RaycastAll(Focus, offset.normalized, offset.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(player.transform) && !hit.transform.IsChildOf(item.transform)) return false;
        return true;
    }

    private void Begin(StoryPickupItem item)
    {
        activeItem = item;
        itemPosition = item.transform.position;
        itemRotation = item.transform.rotation;
        item.collected = true;
        title = item.displayName;
        sentence = item.story ?? "";
        followWasEnabled = follow.enabled;
        inspectorWasEnabled = inspector != null && inspector.enabled;
        follow.enabled = false;
        if (inspector != null) inspector.enabled = false;
        player.SetStoryInteraction(true);
        presenting = true;
        returning = false;
        revealText = false;
        sequence = StartCoroutine(Present());
    }

    private IEnumerator Present()
    {
        Vector3 offset = viewCamera.transform.position - Focus;
        float sourceYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        float sourceRadius = new Vector2(offset.x, offset.z).magnitude;
        float targetYaw = player.transform.eulerAngles.y;
        float height = controller.height * Mathf.Abs(player.transform.lossyScale.y);
        float frontDistance = Mathf.Max(3f, height * .9f);
        float duration = Mathf.Max(.2f, panSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float yaw = Mathf.LerpAngle(sourceYaw, targetYaw, t) * Mathf.Deg2Rad;
            Vector3 desired = Focus + new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw))
                * Mathf.Lerp(sourceRadius, frontDistance, t) + Vector3.up * Mathf.Lerp(offset.y, height * .1f, t);
            PlaceCamera(desired, Focus);
            activeItem.transform.position = Vector3.Lerp(itemPosition, DisplayPosition(), t);
            activeItem.transform.rotation = Quaternion.Slerp(itemRotation, Quaternion.LookRotation(-player.transform.forward), t);
            yield return null;
        }
        textStarted = Time.unscaledTime;
        bool close = false;
        while (!close)
        {
            PlaceCamera(Focus + player.transform.forward * frontDistance + Vector3.up * height * .1f, Focus);
            activeItem.transform.position = DisplayPosition();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (!revealText && VisibleCharacters() < sentence.Length && !Input.GetKeyDown(KeyCode.Escape)) revealText = true;
                else close = true;
            }
            yield return null;
        }
        returning = true;
        if (!collectedClues.Contains(title)) collectedClues.Add(title);
        activeItem.gameObject.SetActive(false);
        Vector3 returnStart = viewCamera.transform.position;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector3 behind = player.transform.position - player.transform.forward * follow.distance + Vector3.up * follow.height;
            PlaceCamera(Vector3.Lerp(returnStart, behind, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration))),
                player.transform.position + Vector3.up * follow.lookHeight);
            yield return null;
        }
        RestoreControls();
        activeItem = null;
        sequence = null;
    }

    private Vector3 DisplayPosition()
    {
        // Show the prop in front of the torso, rather than pretend it is hand-rigged.
        Vector3 towardCamera = viewCamera.transform.position - Focus;
        return Focus + towardCamera.normalized * Mathf.Min(1.5f, towardCamera.magnitude * .45f)
            - Vector3.up * .35f;
    }

    private void PlaceCamera(Vector3 desired, Vector3 lookAt)
    {
        float near = viewCamera.nearClipPlane;
        float halfHeight = near * Mathf.Tan(viewCamera.fieldOfView * .5f * Mathf.Deg2Rad);
        float radius = Mathf.Max(follow.cameraRadius,
            Mathf.Sqrt(near * near + halfHeight * halfHeight * (1f + viewCamera.aspect * viewCamera.aspect)));
        Vector3 safe = Sweep(lookAt, desired, radius);
        safe = Sweep(viewCamera.transform.position, safe, radius);
        viewCamera.transform.position = safe;
        Vector3 direction = lookAt - safe;
        if (direction.sqrMagnitude > .001f) viewCamera.transform.rotation = Quaternion.LookRotation(direction);
    }

    private Vector3 Sweep(Vector3 origin, Vector3 destination, float radius)
    {
        Vector3 delta = destination - origin;
        float distance = delta.magnitude;
        if (distance < .001f) return destination;
        float allowed = distance;
        foreach (RaycastHit hit in Physics.SphereCastAll(origin, radius, delta / distance, distance,
            follow.obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(player.transform)) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - follow.wallPadding));
        }
        return origin + delta / distance * allowed;
    }

    private int VisibleCharacters() => revealText ? sentence.Length
        : Mathf.Min(sentence.Length, Mathf.FloorToInt(Mathf.Max(0f, Time.unscaledTime - textStarted) * 35f));

    private void OnGUI()
    {
        if (Covered || controller == null) return;
        GUIStyle style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.Max(16, Screen.height / 42),
            alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = false };
        float width = Mathf.Min(860f, Screen.width - 28f);
        if (presenting && !returning)
        {
            string body = textStarted > 0f ? sentence.Substring(0, VisibleCharacters()) : "Picking up...";
            GUI.Box(new Rect((Screen.width-width)/2f, Screen.height-230f, width, 190f),
                title + "\n\n" + body + "\n\nEnter / Space: reveal or continue   |   Esc: close", style);
        }
        else if (!presenting && nearby != null && !player.IsClimbing)
            GUI.Box(new Rect((Screen.width-width)/2f, Screen.height-145f, width, 60f),
                "R or click: pick up " + nearby.displayName + "   (" + collectedClues.Count + "/6 clues found)", style);
    }

    private void RestoreControls()
    {
        if (player != null) player.SetStoryInteraction(false);
        if (follow != null) follow.enabled = followWasEnabled;
        if (inspector != null) inspector.enabled = inspectorWasEnabled;
        presenting = false;
        returning = false;
        nearby = null;
        textStarted = 0f;
    }

    private void OnDisable()
    {
        if (!presenting) return;
        if (sequence != null) StopCoroutine(sequence);
        if (activeItem != null && !returning)
        {
            activeItem.collected = false;
            activeItem.transform.SetPositionAndRotation(itemPosition, itemRotation);
        }
        activeItem = null;
        RestoreControls();
    }
}
