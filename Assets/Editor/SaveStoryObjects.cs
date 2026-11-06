using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit authoring command: no automatic scene mutation on import or Editor startup.
public static class SaveStoryObjects
{
    public static void Batch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene/MainSceneFile.unity");
            Bake();
            StoryPickupValidation.Validate();
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Farm Game/Save six story objects into scene")]
    public static void Bake()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode before saving story objects.");
        FarmStoryObjects source = UnityEngine.Object.FindObjectOfType<FarmStoryObjects>();
        if (source == null || source.clues == null || source.clues.Length != 6)
            throw new Exception("Open MainSceneFile with its six configured clues.");
        if (source.savedItems != null && source.savedItems.Length > 0)
            throw new Exception("Saved pickups already exist. Move/edit them directly in the Hierarchy.");
        Physics.SyncTransforms();
        var positions = new List<Vector3>();
        var rotations = new List<Quaternion>();
        // Resolve every floor before making any scene changes.
        foreach (FarmStoryObjects.Clue clue in source.clues)
        {
            bool found = false;
            Vector3 chosen = Vector3.zero;
            Quaternion facing = Quaternion.identity;
            Vector3 original = clue.localPosition;
            for (int ring = 0; ring <= 12 && !found; ring++)
            {
                int samples = ring == 0 ? 1 : 24;
                for (int i = 0; i < samples && !found; i++)
                {
                    float angle = i * Mathf.PI * 2f / samples;
                    clue.localPosition = original + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring * .5f;
                    if (!source.TryGetPlacement(clue, out chosen, out facing)) continue;
                    bool separated = true;
                    foreach (Vector3 existing in positions)
                        if (Vector3.Distance(existing, chosen) < 2f) { separated = false; break; }
                    if (!separated) continue;
                    found = true;
                }
            }
            clue.localPosition = original;
            if (!found) throw new Exception("No supported, separated floor position for " + clue.title);
            positions.Add(chosen);
            rotations.Add(facing);
            Debug.Log("Saving clue: " + clue.title + " at " + chosen);
        }
        string folder = "Assets/StoryPickupMaterials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "StoryPickupMaterials");
        var palette = new Dictionary<Color, Material>();
        var root = new GameObject("Story Collectibles (six clues)");
        Undo.RegisterCreatedObjectUndo(root, "Save story collectibles");
        var items = new List<StoryPickupItem>();
        for (int i = 0; i < source.clues.Length; i++)
        {
            StoryPickupItem item = source.CreateItem(source.clues[i], positions[i], rotations[i], root.transform);
            items.Add(item);
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
            {
                Material generated = renderer.sharedMaterial;
                if (!palette.TryGetValue(generated.color, out Material saved))
                {
                    string path = folder + "/StoryColor" + palette.Count + ".mat";
                    saved = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (saved == null)
                    {
                        saved = new Material(generated);
                        AssetDatabase.CreateAsset(saved, path);
                    }
                    palette.Add(generated.color, saved);
                }
                renderer.sharedMaterial = saved;
            }
        }
        Undo.RecordObject(source, "Assign saved story objects");
        source.savedItems = items.ToArray();
        EditorUtility.SetDirty(source);
        EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(source.gameObject.scene);
        Selection.activeGameObject = root;
        Debug.Log("Saved all six story objects. They are now visible outside Play mode.");
    }
}
