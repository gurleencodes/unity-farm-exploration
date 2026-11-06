# Player, camera, and house fixes

## Scope and preservation

Unity remains 2022.3.62f3. No commit or push was made. The project had no Git repository. `git init -b fix/player-camera-house` was attempted, but the execution sandbox denied creation of the project's `.git` directory. Run the following in your own Terminal to complete that step:

```sh
cd "/Users/gurleenkaur/New Game"
git init -b fix/player-camera-house
git status --short --branch
```

The new `.gitignore` excludes root Library, Temp, Obj/obj, Logs, UserSettings, build output, and generated IDE files. It preserves Assets, Packages, ProjectSettings, and .meta files. Since this is a new repository with no commit, existing source files will initially appear as untracked; no previous history or baseline exists.

Before editing, copies of the five existing files changed below were saved under `/private/tmp/new-game-fix-backup`, along with SHA-256 hashes of every existing file in Assets, Packages, and ProjectSettings. Temporary files can be cleared by macOS; they are not a permanent backup.

## Files changed

- `.gitignore` (new): Unity and IDE generated-file exclusions.
- `Assets/Scripts/CameraFollow.cs`: environment collision checks with player-hierarchy filtering, a collision radius that protects the near clipping plane, and collision checks before and after smoothing.
- `Assets/Scripts/FadeInFadeOut.cs`: assignable SpriteRenderer, safe self-disable if missing, a single yielding fade loop, and cancellation when disabled.
- `Assets/Animations/BanditAnimator.controller`: three states (idle, walk, run), idle default, Speed default 0, six conditional transitions without exit-time waits, and no directional blend or unconditional turn transitions. Existing idle/walk/run state IDs and animation assets are preserved.
- `Assets/Scenes/MainScene/MainSceneFile.unity`: active camera obstacle mask, six added house boxes disabled, and unassigned fade component disabled.
- `ProjectSettings/EditorBuildSettings.asset`: MainSceneFile added as an enabled build scene with its existing GUID.
- `FIX_REPORT.md` (new): this handoff.

Movement.cs, player scale/rotation, CharacterController settings, lights, prefab assets/connections, animation clips, existing .meta files/GUIDs, existing layer assignments, inactive camera scene settings, and unresolved components were left unchanged.

## Camera

The active camera retains Distance 10, Height 7, Look Height 3.5, and Follow Smoothness 3. Its obstacle mask now includes environment layers except Ignore Raycast and UI, and ignores triggers. Because the player and scenery share Default, the collision code filters every collider in the target player's hierarchy rather than changing layers or excluding all Default scenery.

The camera checks all hits, skips the player, and uses the nearest environment hit. The effective sphere radius is at least Camera Radius and large enough to contain the near clipping plane's corners. It resolves the preferred position before smoothing and rechecks afterward so smoothing does not deliberately leave the camera beyond an intervening wall. Obstructions pull in immediately; the return outward is smoothed.

No runtime assignment to Distance or Height was found. Obstruction changes the camera Transform, not those fields. There is no confirmed settings-reset bug in the inspected code. To investigate further, record the selected camera's instance, whether Play mode is active, and whether you changed values during Play mode. Inspector changes made during Play mode normally revert when Play mode ends.

The follow/collision behavior still needs Play-mode verification, especially corners, low ceilings, fast turns, and situations where the look point itself is inside geometry. The larger near-plane protection radius may pull the camera closer than the old radius alone.

## House geometry evidence

The binary source `Assets/countryhouse/country house01/Models/House.fbx` was decoded without modifying it. Mesh IDs were matched through its existing .meta file and House_Prefab's MeshCollider references. The source geometry's centimetre coordinates, mirrored X on Unity import, match the added boxes' local extents. The boxes span the full bounds of hollow rooms/building surfaces, filling walkable voids rather than following only walls.

Examples in Unity mesh-local units:

- mainHouse01: the point (0, 1.5, 1) is inside its added solid box; the wall mesh is roughly 6.9 units away along X and has open rays upward/downward and forward at this point.
- Room01: the box-center sample (-0.6592, 2.1794, -2.3679) is about 3.08 units from either X wall and 1.98 units from the rear Z surface, with open vertical rays.
- Room08: the box-center sample (4.4455, 2.1773, -0.6373) is about 1.88/2.27 units from X surfaces and 3.71 units from Z surfaces, with open vertical rays.

Exactly these scene-added BoxColliders were disabled: mainHouse01, Room01, Room02, Room05, Room08, and hallway. Their existing non-convex MeshColliders remain enabled. The already-disabled added door_frames box remains disabled. Original floor, wall, room, door-frame, terrain, and Plane Mesh/TerrainColliders remain intact. No prefab collider was removed or rewritten.

These are source-geometry checks, not Unity physics tests. Door panel colliders, imported mesh cooking, and the current character capsule's clearance still require an Editor check. The player is scaled approximately 3x; its local height-2 controller therefore has roughly height-6 world dimensions. A doorway can still be too small for that capsule even after the room boxes are disabled. Player scale and controller dimensions were intentionally preserved.

## Missing post-processing scripts

Both missing references were positively identified against Unity's own Post Processing v2 metadata:

- `948f4100a11a5c24981795d21301da5c`: [PostProcessLayer.cs.meta](https://github.com/Unity-Technologies/PostProcessing/blob/v2/PostProcessing/Runtime/PostProcessLayer.cs.meta), on the active camera.
- `8b9a305e18de0c04dbd257a21cd47087`: [PostProcessVolume.cs.meta](https://github.com/Unity-Technologies/PostProcessing/blob/v2/PostProcessing/Runtime/PostProcessVolume.cs.meta), on Post-process Volume.

The corresponding package is absent from the current manifest/cache. No package was installed and neither component was removed. In Unity, select the active camera and Post-process Volume and check for Missing (Mono Script). Open Window > Package Manager > Packages: In Project and check whether Post Processing is available. Decide whether you want to restore that visual-effects package before making further changes; this is separate from the movement/camera collision fixes.

## Fade setup

FadeSceneManager had no SpriteRenderer, so its FadeInFadeOut component is now unchecked. To use it, create or identify the SpriteRenderer that should fade, drag that renderer into the component's Sprite Renderer field, and then enable the component. This implementation fades a sprite; it does not implement a Canvas/Image screen overlay.

## Validation

- The installed Unity 2022.3.62f3 Mono compiler successfully compiled CameraFollow.cs, FadeInFadeOut.cs, and unchanged Movement.cs against that installation's UnityEngine Core, Physics, Animation, InputLegacy, and .NET Standard 2.1 reference assemblies. No errors or warnings on the successful check. This is a standalone C# compilation check, not a complete Editor import or build.
- Animator local references resolve; there are exactly three states and six conditional transitions. From every state, Speed 0, 1, and 2 select idle, walk, and run respectively, with no competing eligible transitions.
- The inactive camera, player transform/root-motion override, movement/controller data, unresolved components, saved active camera Distance/Height, and Unity version were compared with the before-edit snapshot and preserved.
- The .gitignore was exercised with git check-ignore in a temporary fixture repository: generated root folders are ignored; source folders, nested Assets folders, and .meta files remain eligible for tracking.
- Source-file hash comparison found only the five intended existing file changes; no existing files were deleted and no .meta files changed.
- A matching-version batch Editor startup was attempted with the project closed. It could not connect to LicenseClient-gurleenkaur (error 0x80000000), launched the licensing client, and made no further startup progress. The final attempt was terminated after 55 seconds. The log is `/private/tmp/new-game-fix-unity-final.log`. No Editor import/build validation or gameplay test completed.

## Exact Editor testing steps

1. Open this folder with Unity 2022.3.62f3. Wait for import/compilation, then open Assets > Scenes > MainScene > MainSceneFile. Open Window > General > Console. Inspect any red errors before testing.
2. Select the active Main Camera (the other Main Camera remains inactive). Confirm Target = BanditMain, Distance = 10, Height = 7, and Obstacle Layers includes Default. Confirm only the active camera is enabled.
3. Select BanditMain. Confirm one Movement and one CharacterController, and Apply Root Motion unchecked on Animator. Turn on Scene-view Gizmos and compare the controller capsule with the character and doorway height/width. Do not change scale as part of this test.
4. Enter Play mode, click the Game view, and test W/A/S/D and all arrow directions. The character should turn toward movement. Hold Left Shift while moving to run; release it to walk. Try diagonals and release all keys to stop.
5. Open Window > Animation > Animator while BanditMain is selected. Watch Speed become 0/1/2 and the highlighted state become idle/walk/run. Test starting directly into a run and stopping directly from a run. Check the character visually as well as the state display.
6. Approach House_Prefab's open doorway. Walk through, stop on its floor, and press into exterior/interior walls from several directions. Repeat for the green house. Confirm you remain supported by the floor and walls stop you.
7. If a doorway still blocks entry, pause Play mode. Expand House_Prefab; select mainHouse01, Room01, Room02, Room05, Room08, and hallway. Confirm their added BoxCollider checkboxes are off and MeshCollider checkboxes are on. Select floor and confirm its MeshCollider is on. Select door_frames and the specific door panel/prefab in that opening to identify any remaining obstruction. With Scene Gizmos, compare the player capsule against the actual empty doorway; report the object name if a closed panel or insufficient clearance blocks it. Do not disable a floor/wall collider to force entry.
8. Walk beside a wall, then turn until the wall lies between the character and the camera. The camera should pull closer, clear the wall, and ease back outward when you move away. Repeat near corners and inside a house, including running and turning. Watch for seeing through walls, extreme snapping, or near-plane clipping. Confirm the camera ignores the player.
9. While a wall pulls the camera inward, watch Camera Follow's Distance/Height: they should remain 10/7 even though the camera Transform changes. Exit Play mode, reopen/reselect the active camera, and confirm those saved values remain. Record separately any changes made during Play mode.
10. Exit Play mode. Open File > Build Settings and confirm MainSceneFile is enabled under Scenes In Build. FadeSceneManager's unassigned fade stays disabled. Inspect the two missing post-processing components separately as described above.
