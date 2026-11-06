using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Read-only checks for the authored layout; never rearranges props on import.
public static class FarmLayoutValidation
{
    [MenuItem("Tools/Farm Game/Validate farm layout")]
    public static void Validate()
    {
        Transform barn = FindGroup("Barn - arranged supplies", 31);
        FindGroup("House - interior props", 2);
        Transform fence = FindGroup("House yard - perimeter fence", 37);
        Physics.SyncTransforms();
        foreach (Transform panel in fence)
        {
            bool solid = false;
            foreach (Collider collider in panel.GetComponentsInChildren<Collider>())
                if (collider.enabled && !collider.isTrigger) solid = true;
            if (!solid) throw new Exception("Fence/gate has no enabled solid collider: " + panel.name);
            CheckGround(panel);
        }
        BarnLadder ladder = UnityEngine.Object.FindObjectOfType<BarnLadder>();
        FarmPlayerMotor motor = UnityEngine.Object.FindObjectOfType<FarmPlayerMotor>();
        if (ladder == null || ladder.barnFrame == null || motor == null)
            throw new Exception("Missing barn frame or player reference.");
        CharacterController player = motor.GetComponent<CharacterController>();
        float radius = player.radius * Mathf.Max(Mathf.Abs(player.transform.lossyScale.x),
            Mathf.Abs(player.transform.lossyScale.z)) * .97f;
        float height = player.height * Mathf.Abs(player.transform.lossyScale.y);
        for (int z = -7; z <= 7; z += 2)
        {
            Vector3 probe = ladder.barnFrame.TransformPoint(new Vector3(1.2f, 1f, z));
            if (!Physics.Raycast(probe, Vector3.down, out RaycastHit floor, 6f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                throw new Exception("No barn aisle floor at local Z " + z);
            Vector3 feet = floor.point + Vector3.up * .12f;
            foreach (Collider hit in Physics.OverlapCapsule(feet + Vector3.up * radius,
                feet + Vector3.up * Mathf.Max(radius, height - radius), radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(motor.transform)) continue;
                if (hit.transform.IsChildOf(barn))
                    throw new Exception("Arranged prop blocks the center aisle at Z " + z + ": " + hit.name);
            }
        }
        Debug.Log("Farm layout structure, solid fence colliders and sampled barn aisle checks passed. Inspect ground warnings and test both yard entrances, story pickups and ladder in Play mode.");
    }

    private static Transform FindGroup(string name, int expected)
    {
        GameObject group = GameObject.Find(name);
        if (group == null || group.transform.childCount != expected)
            throw new Exception("Expected " + expected + " objects under " + name);
        foreach (Transform child in group.transform)
            if (!PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                throw new Exception("Prefab connection missing for " + child.name);
        return group.transform;
    }

    private static void CheckGround(Transform item)
    {
        Vector3 origin = item.position + Vector3.up * 3f;
        float nearest = float.PositiveInfinity;
        float floorHeight = 0f;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 7f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(item) || hit.normal.y < .65f || hit.distance >= nearest) continue;
            nearest = hit.distance;
            floorHeight = hit.point.y;
        }
        if (float.IsPositiveInfinity(nearest) || Mathf.Abs(item.position.y - floorHeight) > .25f)
            Debug.LogWarning("Visually check fence ground contact: " + item.name + " at " + item.position, item);
    }

    public static void Batch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene/MainSceneFile.unity");
            Validate();
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
