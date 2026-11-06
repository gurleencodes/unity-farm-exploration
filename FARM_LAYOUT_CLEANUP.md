# Farm layout cleanup

Open `Assets/Scenes/MainScene/MainSceneFile.unity` in Unity 2022.3.62f3. All layout changes are saved in the scene and visible before Play mode.

## Arrangement

Three new top-level Hierarchy folders organize the scene:

- **Barn - arranged supplies**: 31 original props moved indoors. Eight crates form two low displays and two taller stacks along the left side. Eight barrels form a front storage group and an inner edge to the hay store. Three square bales support seven round bales in three stacks along the opposite side. The cart, shovel, rake, smaller horizontal log, and trough occupy equipment corners. The center aisle and ladder working side are kept clear in the inspected source bounds.
- **House - interior props**: the original stool and bucket form a small utility corner against the accessible house room's edge, over its actual floor mesh.
- **House yard - perimeter fence**: the existing Fence plus 35 connected duplicates create a 36-panel perimeter aligned to House_Prefab. There are two openings about 6.54 world units wide, one toward the farm and another beside the house's side entrance. The existing gate is positioned swung open beside the main opening. This encloses the house yard rather than the whole terrain.

The oil can is displayed on a low crate and the loose letter on a barrel, both inside the barn. Their whole parent objects moved together; their individual pieces, stories, and pickup components were preserved. The user's upstairs ledger and all house story-object positions were preserved. All original props remain in the scene; none were deleted.

Original prop scales are retained except the oversized log, which is reduced to scale 3.5 and laid horizontally to fit storage. Fence/gate scales are adjusted for consistent fence height and panel connections. Player scale, lighting, cameras, buildings, terrain, trees, the ladder, source assets, and existing .meta GUIDs are unchanged.

## Files

- Changed: `Assets/Scenes/MainScene/MainSceneFile.unity`.
- New: `Assets/Editor/FarmLayoutValidation.cs` and its .meta. **Tools > Farm Game > Validate farm layout** checks group counts, prefab connections, solid fence colliders, sampled center-aisle capsule clearance, and reports fence ground-contact warnings. It never rearranges the scene automatically.
- New: this report.

All original prefab connections and existing added/removed-component overrides, including collider configuration, were preserved. Source Fence/Gate model imports already have Add Colliders enabled. New fence pieces reuse the original Fence prefab and its materials/collision setup.

## Verification

- Runtime scripts and all Editor tools compile against installed Unity 2022.3.62f3 assemblies without warnings/errors.
- Scene checks pass: unique IDs, valid group parent/child references and root lists, all source objects retained, original prefab links/component overrides preserved. Only the intended 35 original prefab records, two story-object transforms, and scene root list changed; all other original scene records are identical.
- Decoded source-mesh bounds for all 31 barn props fit within the inspected barn footprint and leave the center route clear. No pair of those arranged source bounds overlaps in all three axes. Stacks reach approximately 11.85 world units, below the loft's approximately 14-unit height. These are geometry checks, not native physics/gameplay tests.
- Stool/bucket floor support is calculated from the house floor's actual triangles. Fence dimensions and openings are calculated from the source Fence mesh and the house's saved transform.
- Native validation was attempted using the matching Editor, but licensing IPC failed (0x80000000) and the process was stopped after 55 seconds. Log: `/private/tmp/farm-layout-unity.log`. Actual terrain contact, rendering, imported mesh collision, pickup approach distances, full doorway/ladder clearance, and gameplay still need Unity verification. Fence bases are saved near ground height; check the warnings for terrain variation.

No commit or push. Git status confirms this folder is still not a Git repository. A before-edit scene copy is at `/private/tmp/farm-layout-backup/MainSceneFile.unity`.

## Unity review

1. Open MainSceneFile. Select one of the three new top-level folders and press **F** with the Scene window focused to frame that area. Review the arrangement from above and from player height.
2. Run **Tools > Farm Game > Validate farm layout**. Inspect Console warnings, especially any fence ground contact or imported collider issues. Also run **Validate story pickups** and **Validate animations and ladder**.
3. Press Play. Walk/run through the barn's center aisle from both ends. Approach the crate/barrel displays and collect the oil can and letter; confirm their dialogue and camera reveal still work.
4. Walk to the ladder, climb onto the loft, collect the upstairs ledger, and climb down. Confirm storage does not block mounting or landing.
5. Enter the house through its doors, walk past the stool/bucket, and collect its three story objects.
6. Walk through both yard openings and past the open gate. Try walking into the fence panels: they should stop the character. Check every side for floating/buried posts, corner gaps, or terrain intersections, and test the camera against fence corners. Verify trees inside/near the yard remain accessible around the route.
