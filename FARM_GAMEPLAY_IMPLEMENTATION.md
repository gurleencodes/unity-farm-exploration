# Barn ladder, new animations, jumping, and map-wide item identification

This replaces the enabled steering motor and barn-only item inspector with new scripts. The earlier scripts and original BanditAnimator remain in the project, disabled/unassigned where superseded. Unity stays on 2022.3.62f3. No commit or push was made.

## Inspection and animation setup

The new FBX files contain 65-bone Mixamo skeletons with a `mixamo.com` take at 30 fps. They were imported as Generic, while BanditMain is Humanoid. Ten animation .meta files now specify Humanoid/Create From This Model, explicit clip IDs/frame ranges, and appropriate looping. Existing GUIDs and binary FBX files are preserved. Root motion remains off; actual travel comes from the CharacterController.

| Clip | Source frames | Use |
|---|---:|---|
| Climbing Ladder | 0–23 | Sampled climbing pose; reverses smoothly when descending and freezes when stopped |
| Climbing To Top | 0–120 | Pose follows the exit path onto the loft; sampled backward when mounting from above |
| Crouched Walking | 0–31 | Hold C or Left Ctrl; paused pose when crouched and stationary |
| Jumping | 0–57 | Space to jump |
| Jumping Down | 0–104 | Falling/descending |
| Picking Up | 0–287 | G inspection/pickup gesture; movement cancels |
| Picking Up Object | 0–103 | F alternate reach-down gesture; movement cancels |
| Walking Backwards | 0–29 | Down/S |
| Left Turn / Right Turn | 0–35 each | Turn in place |

The existing Idle, Walk2, and Running clips complete the new 13-state FarmCharacterAnimator controller. The old Jogging clip is unused. The new controller has no unconditional state transitions; FarmPlayerMotor chooses states and crossfades. Clip root translation/rotation are extracted rather than used to move the actor, and ladder poses are sampled by travel progress so descending does not depend on unsupported negative Animator playback.

G/F are gestures only: no inventory, item collection, or item deletion was added.

## Controls

| Input | Action |
|---|---|
| Left/Right or A/D alone | Stop translation and turn in place |
| Up/Down or W/S | Walk forward/backward |
| Up/Down plus Left/Right | Move while steering |
| Left Shift while moving forward | Run |
| Space | Jump; while climbing, jump away from the ladder |
| C or Left Ctrl (hold) | Crouch; standing waits for head clearance |
| E near a ladder endpoint | Mount from the barn floor or loft |
| Up/Down while climbing | Climb up/down; no input pauses on the ladder |
| E while climbing | Let go and fall |
| G / F | Pickup/reach-down inspection gestures; move to cancel |
| Left mouse button | Identify a visible map prop |

Gravity, head collisions, grounding/coyote time, jump buffering, and small swept CharacterController moves remain active. Climbing does not disable the controller or teleport through walls. If a mount/dismount path is blocked, movement stops and the hint offers release/jump controls.

## Ladder and loft geometry

The barn already has two upstairs plank strips at barn-local heights 5.24 and 5.36. The selected strip is centered at X -1.349, Z 0.05, with local half-width 1.2511 and half-length 8.2835. The adjacent railing begins near Z -5.103, leaving an opening at the far end.

A visible wooden ladder (two rails, seventeen rungs) is placed at local X -0.08, Z -6.65, with its rails meeting the loft edge. The climb line stays in front of it at X 0.62. The loft landing is X -0.85, Y 5.55, Z -6.65, inside the existing floor and away from the nearest roof beam. Its full player-capsule footprint fits the plank strip and railing gap in source geometry. Native floor probing adjusts the exact landing height at runtime.

The rails and rungs have fitted solid BoxColliders. A separate trigger provides a selectable ladder region without blocking traversal. The climb line stays in front of the rungs; the upper exit passes above the highest rung between the rails. The enlarged player automatically uses the crouched capsule while on the ladder/loft transition, then stands only when safe. Player scale, existing loft/wall collision, and prefab connections were not changed.

This placement is based on inspected mesh bounds and transforms, not a completed native physics test. It still needs an actual climb/descend test after Unity imports the assets.

## Clickable objects throughout the map

WorldItemInspector replaces BarnItemInspector in the scene. It covers the scene's crates, barrels, hay bales, trough, cart, tools, bucket, stool, chair, log, fences, gates, buildings, windmill, wells, barn parts, and new ladder. Grass, trees, bushes, terrain, ground planes, and flowers are excluded. The nearest solid hit blocks selection of objects behind walls. Player colliders and unrelated triggers are skipped. There is no player-distance restriction for identification; the camera's far plane limits the ray by default.

Ordinary `Barell` assets display "Barrel". An object explicitly named WaterBarrel/BarrelOfWater displays "Water barrel"; the code does not guess contents. For a specific water barrel or another custom object, add InspectableItem to its prefab/root and assign Display Name/Description, or use World Item Inspector > Custom Labels. A collider is required for ray selection. Existing farm prop colliders are preserved.

## Files

New runtime scripts:

- Assets/Scripts/FarmPlayerMotor.cs
- Assets/Scripts/BarnLadder.cs
- Assets/Scripts/WorldItemInspector.cs
- Assets/Scripts/InspectableItem.cs

New assets/tools:

- Assets/Animations/FarmCharacterAnimator.controller
- Assets/Editor/FarmGameplayValidation.cs
- Matching new .meta files (and Editor folder metadata if not already present)

Changed existing files: MainSceneFile.unity plus ten animation-import .meta files listed in the animation table. Original scripts, original animator controller, FBX binaries, other collider imports, camera preferences, lighting, startup fade, structure popups, and ProjectSettings are unchanged.

## Validation and limits

- All runtime scripts compile against installed Unity 2022.3.62f3 assemblies; the Editor validation tool also compiles.
- Thirty-seven executable tests against the actual world-label code passed, covering all scene prop types, water-barrel names, and natural scenery exclusions.
- Scene/controller reference checks passed: unique IDs, valid clip/material/script GUIDs, explicit clip IDs, correct active components, and preserved prefab connections.
- Source-geometry checks passed for the loft landing's capsule footprint and railing gap.
- Exactly eleven existing source files changed: the scene and ten animation metadata files. No existing source files were deleted or .meta GUIDs changed. A before-edit snapshot/hash manifest is under the temporary directory recorded in `/private/tmp/new-game-ladder-backup.txt`; temporary backups are not permanent version history.
- A matching-version native validation run was attempted while the project was closed. Unity stalled at licensing and was terminated after 55 seconds, before import/validation. Log: `/private/tmp/new-game-ladder-unity.log`. Avatar validity, visual retargeting, mesh cooking, native ladder clearance, actual gameplay, and build output have NOT been verified.

## Exact Unity checks

1. Open the project with Unity 2022.3.62f3 and wait for all animation reimports and compilation. Open Assets/Scenes/MainScene/MainSceneFile. Inspect Window > General > Console.
2. Run Tools > Farm Game > Validate animations and ladder. It checks native Avatar/humanoid status, assigned motions, active controllers, loft floor support, and the crouched exit path. Resolve any named obstruction/import error before treating this as gameplay-ready.
3. Select BanditMain: Farm Player Motor should be enabled; older Movement and Player Steering Movement should be unchecked. Its Animator should use FarmCharacterAnimator with Apply Root Motion off. Open Window > Animation > Animator to observe the states.
4. Press Play. Confirm the black startup fade and structure-name messages still work. Test turning alone, forward/backward movement, running, and Space jumping. Hold/release C under and away from low beams; the capsule must not expand into a ceiling.
5. Walk to the ladder at the far end of the barn, beside the loft railing opening. The hint should offer E. Press E, then Up to climb; release Up to pause. At the top, continue Up to climb onto the loft. Walk around and confirm floor support.
6. Return to the landing and press E to mount from above. Use Down to descend and step off at the bottom. Test E release and Space jump-off partway up; confirm gravity and floor collisions stop the fall. Test jumping in place and jumping from a loft edge. There is no fall-damage system.
7. Press G and F while grounded to preview the two pickup gestures. They can be cancelled with movement or Space; objects remain in place.
8. Click visible crates/barrels and tools outside the barn and upstairs. Verify grass/trees produce no label and walls prevent selecting concealed items. Click the ladder for its instructions. Check any named water barrels and custom labels.
9. If anything clips or a motion is invalid, record the exact object/state and the Console message. Select Barn Loft Ladder with Gizmos enabled to inspect climb points and the landing. Do not disable existing floors/walls to force a blocked path.
