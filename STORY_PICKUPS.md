# Saved story collectibles

All six are now ordinary saved objects in `Assets/Scenes/MainScene/MainSceneFile.unity`. They are visible before Play mode; the scene no longer depends on runtime spawning to create them.

## Find the objects

Expand the top-level **Story Collectibles (six clues)** in the Hierarchy:

- **House clues**: Family history book, Old flashlight, Brass house key.
- **Barn clues**: Harvest ledger, Dented oil can, Sealed family letter.

Select any named object, hover over the Scene window, and press **F** to frame it. In the Scene window, turn off Gizmos temporarily if the trigger boxes obscure the models.

Saved world positions (rounded):

| Object | X | Y | Z |
| --- | ---: | ---: | ---: |
| Family history book | 13.076 | 1.057 | 80.582 |
| Old flashlight | 12.571 | 1.057 | 76.527 |
| Brass house key | 12.065 | 1.057 | 72.473 |
| Harvest ledger | -65.381 | 0.120 | 41.514 |
| Dented oil can | -62.381 | 0.120 | 41.575 |
| Sealed family letter | -59.382 | 0.120 | 41.636 |

The house is the accessible **House_Prefab** with doors. Its floor is offset from the model origin: decoded floor triangles cover approximately local X -6.725 to -1.946. The old flashlight/key positions at X 0 and 2 were outside the floor and failed the spawn check. All three saved house objects now use X -4.5 and Z -2/0/2; triangle intersection checks find supporting floor at all three positions.

Barn objects retain their prior aisle X/Z positions, with a saved height near the barn base. On Play-mode startup, **FarmStoryObjects** refines each saved object's height against the nearest supported surface within its short vertical probe. It never deletes a saved object if the probe fails; it logs a named warning and keeps the saved position. Native barn terrain contact remains to be checked in Unity.

## Play and edit

1. Open MainSceneFile using Unity **2022.3.62f3**. Run **Tools > Farm Game > Validate story pickups**. This checks all six saved objects, their geometry/references, and native floor support. Read the Console for any named failure.
2. Press Play. Approach a clue within 4.5 world units and press **R**, or click it nearby. The object rises for display, movement pauses while gravity remains active, the camera pans toward the player's front with obstacle checks, and written dialogue appears.
3. **Enter/Space** reveals the remaining sentence; press again to collect and return to the normal camera. **Esc** closes it. Objects disappear after dismissal. Stop/Play resets collection; no disk save, inventory menu, or recorded voice is included.
4. Test all three house objects and all three barn objects, including camera reveals near walls. Confirm movement resumes and the player remains above the floor.
5. Try through-wall, distant, airborne, and ladder interactions: pickup should not begin. Ordinary props should still show their identification labels.

To move an object, edit its Transform directly outside Play mode. To edit its sentence, use that object's **Story Pickup Item > Story** field. The old Clues list on Sky and Barn Effects remains fallback authoring data; with Saved Items assigned, it does not overwrite the saved scene objects.

## Scripts and files

- `FarmStoryObjects.cs`: uses the six saved references without spawning duplicates; adjusts supported heights on startup; retains procedural creation for an explicit Editor authoring tool/fallback.
- `StoryPickupItem.cs`: collectible title, sentence, collected flag (unchanged by this placement fix).
- `PickupStorySequence.cs`: R/click interaction, camera reveal and dialogue (unchanged by this placement fix).
- `Editor/StoryPickupValidation.cs`: validates saved scene pickups and floor support.
- New `Editor/SaveStoryObjects.cs`: explicit authoring command to bake clues in a scene that has no saved pickups. It refuses to duplicate an existing saved set. No import/startup Editor scene mutation.
- New `Assets/StoryPickupMaterials/`: seven persistent, texture-free Standard materials with their own .meta files.
- MainSceneFile: six new pickup objects and their child meshes/group folders; corrected house fallback coordinates and Saved Items references. All other original scene records, prefab connections, camera preferences, lighting, player scale, colliders, and GUIDs are preserved.

## Verification and limits

All runtime and Editor scripts compile against Unity 2022.3.62f3 assemblies without warnings/errors. Static checks confirm six active saved pickups, geometry, trigger colliders, story text, valid material/script references, and unique IDs. House support was checked against the source FBX triangles.

Unity's batch launch again stalled connecting to its licensing client (error 0x80000000) and was stopped after 55 seconds, before native import/scene checks. Log: `/private/tmp/farm-save-story-unity.log`. The scene was therefore authored with focused YAML edits while Unity was closed. Native terrain support, rendering, camera framing, and gameplay have not been verified.

No commit or push. The folder is still not a Git repository. Before-edit copies of the scene and existing scripts/docs changed for this fix are under `/private/tmp/story-saved-backup`.
