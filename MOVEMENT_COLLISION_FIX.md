# Movement and environment collision follow-up

This follow-up supersedes the movement/collision portions of FIX_REPORT.md. Unity remains 2022.3.62f3. No commit or push was made. Git is still not initialized in this project because sandbox permissions block the project's .git directory.

## Changes

- Movement.cs now accelerates (18 units/s²) and brakes (24 units/s²), smooths yaw turns (0.1 s, maximum 540 degrees/s in the scene), moves along the facing direction, and slows for sharp turns instead of sliding sideways. WASD/arrows remain world-relative; Left Shift still runs. Saved walking/running speeds remain 3/7 and player scale is unchanged.
- Movement uses CharacterController.Move exclusively, explicitly enables its collision detection, sweeps in steps of at most 0.15 units at the saved settings, and limits a single frame's simulation to 0.1 s. It does not move by directly assigning position. Speed buildup is reduced when blocked.
- The Animator's Speed parameter follows measured horizontal displacement with damping. When fully blocked, it returns toward idle even if a movement key is held. Idle/walk/run transitions take 0.12 s, are interruptible, and begin walking at a low speed rather than waiting until half walking speed.
- Walk/run playback rates follow actual displacement and the referenced clips' average root speed, accounting for the character's existing scale. In-place clips fall back to configured walking/running speeds. Root motion stays off. Visual stride matching still needs checking in Play mode.
- Unity ModelImporter Generate Colliders is now enabled on the 37 farm model imports used by the scene, including all nested barn parts, barrels, boxes, fences, hay, sheds, and other farm props. Existing GUIDs and every other importer setting are preserved. Changes apply to every use of these same models, including imported demo scenes. Unity generates colliders when it reimports them; no new whole-barn box was added and mesh openings remain mesh openings.
- Trees already have enabled, non-trigger trunk CapsuleColliders. No scene overrides disable those colliders, the terrain tree-collider settings are enabled, and the physics layer matrix allows collision. Existing tree colliders were kept without adding duplicates. The separate well prefab already has an enabled non-trigger MeshCollider and needed no change.
- Existing custom prop colliders remain present. Where they overlap newly generated mesh collision, check for snagging during Play-mode testing. No existing collider was removed blindly.

## Files

- Assets/Scripts/Movement.cs
- Assets/Animations/BanditAnimator.controller
- Assets/Scenes/MainScene/MainSceneFile.unity (only its Movement component)
- The following model-import metadata files (only addColliders changed from 0 to 1):

- Assets/MarpaStudio/Mesh/Barell.fbx.meta
- Assets/MarpaStudio/Mesh/BarnBase.fbx.meta
- Assets/MarpaStudio/Mesh/BarnDoor.fbx.meta
- Assets/MarpaStudio/Mesh/BarnPillarOutside.fbx.meta
- Assets/MarpaStudio/Mesh/Box.fbx.meta
- Assets/MarpaStudio/Mesh/Bucket.fbx.meta
- Assets/MarpaStudio/Mesh/ChickenHouse.fbx.meta
- Assets/MarpaStudio/Mesh/Fence.fbx.meta
- Assets/MarpaStudio/Mesh/FoodTrough.fbx.meta
- Assets/MarpaStudio/Mesh/Gate.fbx.meta
- Assets/MarpaStudio/Mesh/HaiCylinder.fbx.meta
- Assets/MarpaStudio/Mesh/HaiSquare.fbx.meta
- Assets/MarpaStudio/Mesh/House.fbx.meta
- Assets/MarpaStudio/Mesh/InsidePillarBig.fbx.meta
- Assets/MarpaStudio/Mesh/InsidePillarSmall.fbx.meta
- Assets/MarpaStudio/Mesh/Log.fbx.meta
- Assets/MarpaStudio/Mesh/OldShed.fbx.meta
- Assets/MarpaStudio/Mesh/OutHouseBase.fbx.meta
- Assets/MarpaStudio/Mesh/OutHouseDoor.fbx.meta
- Assets/MarpaStudio/Mesh/Pillar01.fbx.meta
- Assets/MarpaStudio/Mesh/Pillar02.fbx.meta
- Assets/MarpaStudio/Mesh/PlankCeilingSmall.fbx.meta
- Assets/MarpaStudio/Mesh/Railing.fbx.meta
- Assets/MarpaStudio/Mesh/Rake.fbx.meta
- Assets/MarpaStudio/Mesh/RockingChair.fbx.meta
- Assets/MarpaStudio/Mesh/Shed.fbx.meta
- Assets/MarpaStudio/Mesh/Shovel.fbx.meta
- Assets/MarpaStudio/Mesh/SideDoor.fbx.meta
- Assets/MarpaStudio/Mesh/SidePillarsInside.fbx.meta
- Assets/MarpaStudio/Mesh/Silo.fbx.meta
- Assets/MarpaStudio/Mesh/SmallPlankCeiling.fbx.meta
- Assets/MarpaStudio/Mesh/Stool.fbx.meta
- Assets/MarpaStudio/Mesh/UpstairsPlanks.fbx.meta
- Assets/MarpaStudio/Mesh/Well.fbx.meta
- Assets/MarpaStudio/Mesh/WheelCart.fbx.meta
- Assets/MarpaStudio/Mesh/WindMill.fbx.meta
- Assets/MarpaStudio/Mesh/WoodPlankCeilingBig.fbx.meta

- MOVEMENT_COLLISION_FIX.md (this report)

## Verification and limits

The installed Unity 2022.3.62f3 Mono compiler compiled Movement, CameraFollow, and FadeInFadeOut successfully against that installation's .NET Standard and UnityEngine assemblies, without warnings/errors. Static checks verified Animator references/parameters, eligible idle/walk/run transitions, scene component boundaries, all 37 exact importer edits, GUID preservation, tree collider configuration, and the unchanged Unity version.

Exactly 40 pre-existing source files changed in this follow-up (three script/controller/scene files plus 37 metadata files). No existing source files were deleted; no new files were added under Assets, Packages, or ProjectSettings. Backups and a before-edit hash manifest are in:

`/private/tmp/new-game-movement-collision-20261021-124132`

A matching-version batch Editor launch was attempted with Unity closed. It stalled at licensing before importing the project and was stopped after 55 seconds. The startup log is `/private/tmp/new-game-movement-unity.log`. This is not a complete Unity import, build, or gameplay pass. Actual solid collision, mesh cooking, wall backfaces, doorway clearance, and animation feel have not been tested in Play mode. The preserved enlarged character capsule may still not fit some doorways. No tree traversal defect was established from the saved configuration alone.

## Test in Unity

1. Open this folder with Unity 2022.3.62f3 and wait for the farm model reimports and compilation to finish. Open Assets/Scenes/MainScene/MainSceneFile. Check Window > General > Console for errors, especially mesh-collider cooking warnings.
2. Select BanditMain. Under Movement, confirm walk/run 3/7, rotation speed 540, acceleration 18, braking 24, turn smooth time 0.1, and assigned Walk2/Running animation references. Animator Apply Root Motion should be off. Camera settings and player scale are unchanged.
3. Enter Play mode and click Game view. Walk with WASD/arrows, hold/release Left Shift, release all keys, and try diagonals and 90/180-degree turns. Confirm gradual speed changes, forward-facing movement, and smooth rotation. Watch idle/walk/run and Speed in Window > Animation > Animator.
4. Push into a barrel, box, and multiple visible tree trunks while walking and running, straight on and diagonally. Position should stop at the surface; animation should settle toward idle when fully blocked. Moving tangentially along a surface should still work. Test collisions in a narrow gap too.
5. Circle the barn outside, press into each wall from both directions where accessible, and test every corner at running speed. Enter through the open doorway and repeat wall checks from inside. A closed door mesh is expected to be solid. Verify the camera also pulls inward around the now-solid props and barn.
6. If any object is still traversable, pause at that object, select its exact instance in the Scene view, turn on Gizmos, and inspect its enabled non-trigger Collider. For farm models, select the source FBX > Model and confirm Generate Colliders is checked, then inspect the instance for generated MeshColliders. For trees, inspect the existing trunk capsule's fit. Record the exact object name and Console message; do not disable walls or floors to get around a bad collider.
7. Adjust Movement's Acceleration, Braking, Turn Smooth Time, or Rotation Speed outside Play mode if you prefer a different feel. Play-mode Inspector edits normally revert when Play mode ends. No gameplay tuning can be called final until you have tried it.
