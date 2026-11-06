**Current gameplay scripts**

| Script | What it does | Where it is used |
|---|---|---|
| CharacterGroundSafety.cs | New in the ground-sinking fix. Checks floors after movement, attempts upward controller recovery from floor penetration, and raises the animated hip/leg pose when feet sink below the floor or the controller's climbing/inspection foot reference. It does not move the character downward or replace normal movement. | BanditMain |
| FarmPlayerMotor.cs | Owns walking, running, turning, jumping, crouching, ladder movement, and animation-state selection. G/F play inspection/pickup gestures without collecting items. | BanditMain |
| BarnLadder.cs | Defines ladder grab points, climbing direction, and supported floor/loft landing points. Its lower grab point now rises to the detected floor instead of pulling the player toward a buried authored point. | Barn Loft Ladder |
| WorldItemInspector.cs | Left-click identification across the map. Selects visible props, respects walls, and excludes grass, trees, bushes, and terrain. | Sky and Barn Effects |
| InspectableItem.cs | Gives an object an explicit display name and optional description. Overrides the automatic name. | Ladder; can be added to other props |
| StructureLocationMessages.cs | Shows names such as Barn, Windmill, Well, and House when the player approaches them. | Sky and Barn Effects |
| SkyController.cs | Slowly blends sky colors using a runtime skybox copy, leaving the original skybox material unchanged. | Sky and Barn Effects |
| BlackScreenStartup.cs | Reveals the scene by fading the saved full-screen black overlay. | FadeSceneManager |
| FarmGameplayValidation.cs | Editor-only menu tool checking Humanoid imports, animation links, active movement components, loft support, and ladder-exit clearance. It is not included in the game build. | Tools > Farm Game > Validate animations and ladder |

CameraFollow.cs existed before these additions; it was improved earlier to follow behind the character and pull inward around obstacles.

**Older alternatives retained but disabled in MainSceneFile**

- PlayerSteeringMovement.cs: earlier steering controller; replaced by FarmPlayerMotor.
- BarnItemInspector.cs: earlier barn-only click identification; replaced by WorldItemInspector. Its static label helper is still reused by the world inspector.
- BarnEntryMessage.cs: earlier "Entering the barn" popup; replaced by structure names.
- SceneStartupFade.cs: earlier runtime-created overlay; replaced by the saved black overlay and BlackScreenStartup.
- Movement.cs and FadeInFadeOut.cs: original project scripts, retained with their older scene components disabled.

Leave superseded components unchecked so they do not compete with the current components.

**Ground-sinking fix**

- Added CharacterGroundSafety.cs and its .meta file; attached it to the player through a scene prefab override.
- Changed only heightFromFeet from 0 to 1 in the import settings for Climbing Ladder, Climbing To Top, Picking Up, and Picking Up Object. Clip names, frame ranges, root-motion settings, FBX binaries, and GUIDs are preserved.
- FarmPlayerMotor resets downward momentum after a guard correction and keeps inspection grounding consistent with normal movement.
- BarnLadder probes the actual floor at the lower grab/exit, ignores character and ladder colliders, and prevents the lower grab point from being below that supported floor.
- Ladder placement, existing floor/wall colliders, player scale, animator controller, camera settings, startup fade, and other scripts are unchanged.

The visual guard runs after the Animator, separately from physical collision. It calibrates the normal standing foot offset during grounded Idle, then prevents upward-only pose correction from changing the controller root or camera. It also protects against visible foot penetration in ordinary poses. Physical recovery uses existing solid floor colliders; it cannot create a floor where none exists or guarantee recovery from an arbitrary deep teleport.

**How to check the fix**

1. Open Unity 2022.3.62f3 and allow the four animation clips to reimport.
2. Select BanditMain and confirm Character Ground Safety is enabled, along with Farm Player Motor. Keep older movement controllers disabled.
3. Press Play. On flat ground, press G and F and watch both feet and the character's shadow during bending. Move to cancel each gesture; confirm walking remains normal.
4. At the barn ladder, press E, climb with Up/Down, pause on it, step onto the loft, and climb down again. The lower mounting target should remain above the actual floor.
5. Check Space jumping, dropping from the ladder, crouching, and walking off the loft. The guard should not hold the character in midair or snap her onto the upstairs floor while she is below it.
6. With Scene Gizmos visible, compare the controller capsule and model feet if anything still sinks. A capsule above the floor with only the model sinking indicates a pose/retargeting issue; both below indicates a physical collision/target issue. Report which one occurs.

All runtime scripts compile against installed Unity 2022.3.62f3, and source/scene preservation checks pass. Native retargeting and gameplay still require testing in Unity; automated Editor validation has been blocked by licensing startup in this environment.

## Story collectibles

- **StoryPickupItem.cs**: title, story sentence, and collected flag for a pickup.
- **FarmStoryObjects.cs**: references the six saved pickups, avoids duplicate runtime spawning, and refines their floor height at startup. Find them under the top-level Story Collectibles (six clues) folder. Edit each object's Transform and Story Pickup Item fields directly.
- **PickupStorySequence.cs**: R/click pickup, movement pause, camera reveal, typewritten dialogue, and return to normal controls. Collected clues last for the current Play session.
- **Editor/StoryPickupValidation.cs**: validates the saved story setup and six native floor probes from Tools > Farm Game.

See `STORY_PICKUPS.md` for controls, placement details, and testing steps.

The latest placement fix also adds **Editor/SaveStoryObjects.cs**, an explicit authoring command for scenes without saved pickups. The current main scene already contains all six; no command is required to create them.

**Editor/FarmLayoutValidation.cs** adds Tools > Farm Game > Validate farm layout. It checks the arranged-prop groups, prefab links, fence colliders, center-aisle clearance, and warns about fence ground contact. It does not move scene objects. See FARM_LAYOUT_CLEANUP.md for the new layout and review steps.
