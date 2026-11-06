using System.Collections.Generic;
using UnityEngine;

// Builds lightweight props without changing imported models or their prefabs.
[DisallowMultipleComponent]
public class FarmStoryObjects : MonoBehaviour
{
    public enum PropKind { Book, Flashlight, Key, Ledger, OilCan, Letter }
    [System.Serializable]
    public class Clue
    {
        public string title;
        public bool inBarn;
        public Vector3 localPosition;
        public PropKind shape;
        [TextArea(2, 5)] public string sentence;
    }
    public Clue[] clues;
    [Tooltip("Saved scene pickups. When assigned, runtime spawning is disabled.")]
    public StoryPickupItem[] savedItems = new StoryPickupItem[0];
    private readonly List<Material> materials = new List<Material>();
    private GameObject container;

    private void Start()
    {
        if (savedItems != null && savedItems.Length > 0)
        {
            Physics.SyncTransforms();
            foreach (StoryPickupItem item in savedItems)
            {
                if (item == null) Debug.LogWarning("A saved story pickup reference is missing.", this);
                else SettleSavedItem(item);
            }
            return;
        }
        container = new GameObject("Story Collectibles (six clues)");
        container.transform.SetParent(transform, false);
        Physics.SyncTransforms();
        if (clues == null) return;
        foreach (Clue clue in clues)
        {
            if (clue == null) continue;
            if (!TryGetPlacement(clue, out Vector3 position, out Quaternion rotation))
            {
                Debug.LogWarning("Cannot place story clue: " + clue.title + ". Check its building and floor position.", this);
                continue;
            }
            CreateItem(clue, position, rotation, container.transform);
        }
    }

    private void SettleSavedItem(StoryPickupItem item)
    {
        // Keep saved props present even if a floor cannot be queried. This adjusts
        // only their height on startup, never their authored horizontal position.
        float nearest = float.PositiveInfinity;
        Vector3 origin = item.transform.position + Vector3.up * 2f;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < .65f || hit.distance >= nearest
                || hit.collider.GetComponentInParent<CharacterController>() != null
                || hit.collider.GetComponentInParent<BarnLadder>() != null
                || hit.collider.GetComponentInParent<StoryPickupItem>() != null) continue;
            nearest = hit.distance;
            Vector3 position = item.transform.position;
            position.y = hit.point.y + .04f;
            item.transform.position = position;
        }
        if (float.IsPositiveInfinity(nearest))
            Debug.LogWarning("No floor found beneath saved story pickup: " + item.displayName
                + ". Object kept at its saved position.", item);
    }

    // Used by the Editor authoring tool as well as the runtime fallback.
    public StoryPickupItem CreateItem(Clue clue, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject item = new GameObject(clue.title);
        item.transform.SetParent(parent, false);
        item.transform.SetPositionAndRotation(position, rotation);
        StoryPickupItem pickup = item.AddComponent<StoryPickupItem>();
        pickup.displayName = clue.title;
        pickup.story = clue.sentence;
        BuildShape(item.transform, clue.shape);
        // A generous click target, with no solid obstruction to walking.
        BoxCollider target = item.AddComponent<BoxCollider>();
        target.isTrigger = true;
        target.center = new Vector3(0f, 0.2f, 0f);
        target.size = new Vector3(1.5f, 0.5f, 1.2f);
        InspectableItem label = item.AddComponent<InspectableItem>();
        label.displayName = clue.title;
        label.description = "Approach and press R, or click nearby, to pick up.";
        return pickup;
    }

    public bool TryGetPlacement(Clue clue, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        Transform house = null, barn = null, interior = null;
        foreach (Transform candidate in FindObjectsOfType<Transform>())
        {
            if (candidate.gameObject.scene != gameObject.scene) continue;
            if (candidate.name == "House_Prefab") house = candidate;
            if (candidate.name == "BarnBase") barn = candidate;
        }
        if (house != null)
            foreach (Transform child in house.GetComponentsInChildren<Transform>())
                if (child.name == "mainHouse01") { interior = child; break; }
        Transform frame = clue.inBarn ? barn : interior;
        if (frame == null || !TryFloor(frame.TransformPoint(clue.localPosition),
            clue.inBarn ? null : house, out Vector3 floor)) return false;
        position = floor + Vector3.up * .04f;
        rotation = Quaternion.Euler(0f, frame.eulerAngles.y + 15f, 0f);
        return true;
    }

    private bool TryFloor(Vector3 position, Transform house, out Vector3 floor)
    {
        floor = position;
        float nearest = float.PositiveInfinity;
        bool found = false;
        // The authored point is above the ground floor, below the barn loft.
        foreach (RaycastHit hit in Physics.RaycastAll(position, Vector3.down, 12f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < 0.65f || hit.distance >= nearest) continue;
            if (hit.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (house != null && (!hit.transform.IsChildOf(house)
                || hit.transform.name.ToLowerInvariant() != "floor")) continue;
            if (house == null && hit.collider.GetComponentInParent<BarnLadder>() != null) continue;
            nearest = hit.distance;
            floor = hit.point;
            found = true;
        }
        return found;
    }

    private Material Paint(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Glossiness", 0.25f);
        materials.Add(material);
        return material;
    }

    private void Part(Transform parent, PrimitiveType shape, Vector3 position, Vector3 scale,
        Material material, Vector3 rotation = default(Vector3))
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = "Story prop " + shape;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = Quaternion.Euler(rotation);
        // Disable immediately; Destroy is deferred until end of frame.
        Collider solid = part.GetComponent<Collider>();
        solid.enabled = false;
        if (Application.isPlaying) Destroy(solid);
        else DestroyImmediate(solid);
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void BuildShape(Transform root, PropKind kind)
    {
        Material cream = Paint(new Color(0.9f, 0.82f, 0.61f));
        Material dark = Paint(new Color(0.12f, 0.14f, 0.16f));
        Material brass = Paint(new Color(0.75f, 0.48f, 0.14f));
        if (kind == PropKind.Book || kind == PropKind.Ledger)
        {
            Material cover = Paint(kind == PropKind.Book ? new Color(0.35f, 0.07f, 0.06f) : new Color(0.05f, 0.23f, 0.14f));
            Part(root, PrimitiveType.Cube, new Vector3(0,.12f,0), new Vector3(.85f,.15f,1.1f), cream);
            Part(root, PrimitiveType.Cube, new Vector3(0,.025f,0), new Vector3(.95f,.05f,1.2f), cover);
            Part(root, PrimitiveType.Cube, new Vector3(0,.22f,0), new Vector3(.95f,.05f,1.2f), cover);
            Part(root, PrimitiveType.Cube, new Vector3(-.45f,.12f,0), new Vector3(.08f,.24f,1.2f), cover);
            Part(root, PrimitiveType.Cube, new Vector3(0,.252f,0), new Vector3(.5f,.015f,.12f), brass);
        }
        else if (kind == PropKind.Flashlight)
        {
            Part(root, PrimitiveType.Cylinder, new Vector3(0,.18f,0), new Vector3(.25f,.5f,.25f), dark, new Vector3(90,0,0));
            Part(root, PrimitiveType.Cylinder, new Vector3(0,.18f,.5f), new Vector3(.43f,.12f,.43f), brass, new Vector3(90,0,0));
            Part(root, PrimitiveType.Cylinder, new Vector3(0,.18f,.63f), new Vector3(.34f,.015f,.34f), cream, new Vector3(90,0,0));
        }
        else if (kind == PropKind.Key)
        {
            Part(root, PrimitiveType.Cylinder, new Vector3(-.25f,.07f,0), new Vector3(.5f,.04f,.5f), brass);
            Part(root, PrimitiveType.Cube, new Vector3(.18f,.07f,0), new Vector3(.7f,.07f,.12f), brass);
            Part(root, PrimitiveType.Cube, new Vector3(.43f,.07f,.1f), new Vector3(.1f,.07f,.25f), brass);
        }
        else if (kind == PropKind.OilCan)
        {
            Material red = Paint(new Color(.55f,.12f,.05f));
            Part(root, PrimitiveType.Cylinder, new Vector3(0,.35f,0), new Vector3(.65f,.3f,.65f), red);
            Part(root, PrimitiveType.Cylinder, new Vector3(.2f,.8f,0), new Vector3(.07f,.35f,.07f), brass, new Vector3(0,0,-30));
            Part(root, PrimitiveType.Cube, new Vector3(-.4f,.4f,0), new Vector3(.1f,.45f,.15f), dark);
        }
        else
        {
            Part(root, PrimitiveType.Cube, new Vector3(0,.025f,0), new Vector3(1f,.05f,.65f), cream);
            Part(root, PrimitiveType.Cylinder, new Vector3(0,.06f,0), new Vector3(.15f,.015f,.15f), Paint(new Color(.5f,.04f,.03f)));
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (container != null) Destroy(container);
        foreach (Material material in materials) if (material != null) Destroy(material);
    }
}
