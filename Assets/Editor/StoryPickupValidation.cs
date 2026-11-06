using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StoryPickupValidation
{
    [MenuItem("Tools/Farm Game/Validate story pickups")]
    public static void Validate()
    {
        FarmStoryObjects objects = UnityEngine.Object.FindObjectOfType<FarmStoryObjects>();
        PickupStorySequence sequence = UnityEngine.Object.FindObjectOfType<PickupStorySequence>();
        if (objects == null || sequence == null) throw new Exception("Open MainSceneFile: story components are missing.");
        if (sequence.player == null || sequence.viewCamera == null || sequence.follow == null
            || sequence.inspector == null || sequence.startupOverlay == null)
            throw new Exception("A story interaction reference is unassigned.");
        if (sequence.follow.GetComponent<Camera>() != sequence.viewCamera)
            throw new Exception("Story camera and follow camera do not match.");
        if (objects.clues == null || objects.clues.Length != 6
            || objects.clues.Count(c => c.inBarn) != 3)
            throw new Exception("Expected three clues in each building.");
        if (objects.clues.Select(c => c.title).Distinct().Count() != 6)
            throw new Exception("Each clue must have a unique title.");
        Physics.SyncTransforms();
        if (objects.savedItems != null && objects.savedItems.Length > 0)
        {
            if (objects.savedItems.Length != 6 || objects.savedItems.Any(item => item == null)
                || objects.savedItems.Distinct().Count() != 6)
                throw new Exception("Expected six distinct saved scene pickups.");
            foreach (StoryPickupItem item in objects.savedItems)
            {
                if (!item.gameObject.activeInHierarchy || item.GetComponent<Collider>() == null
                    || item.GetComponentsInChildren<Renderer>().Length == 0 || string.IsNullOrWhiteSpace(item.story))
                    throw new Exception("Incomplete saved pickup: " + item.name);
                bool supported = false;
                foreach (RaycastHit hit in Physics.RaycastAll(item.transform.position + Vector3.up * .25f,
                    Vector3.down, .65f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (hit.normal.y > .65f && hit.collider.GetComponentInParent<CharacterController>() == null)
                        supported = true;
                if (!supported) throw new Exception("No floor beneath saved pickup: " + item.name);
                Debug.Log("Saved story pickup supported: " + item.name + " at " + item.transform.position, item);
            }
        }
        else
        {
            foreach (FarmStoryObjects.Clue clue in objects.clues)
            {
                if (string.IsNullOrWhiteSpace(clue.sentence)) throw new Exception("Missing sentence: " + clue.title);
                if (!objects.TryGetPlacement(clue, out Vector3 position, out Quaternion rotation))
                    throw new Exception("No supported floor placement for " + clue.title);
                Debug.Log("Story clue floor found: " + clue.title + " at " + position, objects);
            }
        }
        Debug.Log("Story pickup configuration and six floor probes passed. Camera framing, approach clearance, and gameplay still need Play mode.");
    }

    public static void Batch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene/MainSceneFile.unity");
            Validate();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
