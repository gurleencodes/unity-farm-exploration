# Unity Farm Exploration

A work-in-progress third-person farm exploration game with character movement, environmental interaction, and six story collectibles placed around a house and barn.

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-222222?style=for-the-badge&logo=unity&logoColor=white)
![CSharp](https://img.shields.io/badge/C%23-Gameplay-512BD4?style=for-the-badge)

## Repository status

**Source and documentation snapshot — full asset upload pending.** The GitHub snapshot contains custom runtime and Editor scripts, package manifests, project settings, and development notes. Scene files, models, textures, animation clips, materials, and third-party asset packs still require transfer from the complete local project. A fresh clone is **not yet playable** and cannot reproduce the game until those files are added.

The complete working project is maintained locally. No downloadable build or gameplay video is published yet.

## Gameplay

- Walk, run, jump, crouch, and climb the barn ladder.
- Follow the player with a camera that pulls inward around obstacles.
- Identify nearby props and display location names around farm structures.
- Discover six saved clues in the house and barn.
- Inspect a clue through a camera reveal and typewritten story text, then return to exploration.
- Use a startup fade and a gradually changing sky for atmosphere.

Collectible progress lasts for the current Play session. There is no disk save, inventory menu, or recorded voice system.

## Tech stack and editor version

| Area | Technology / configuration |
| --- | --- |
| Editor | **Unity 2022.3.62f3 LTS**, revision `96770f904ca7` |
| Gameplay | C# MonoBehaviour components |
| Movement and collision | Unity CharacterController, Physics queries, colliders, and Humanoid animation |
| Input | Legacy Unity Input Manager (`activeInputHandler: 0`) |
| Rendering | Built-in Render Pipeline; imported nature, farm, character, and water assets |
| UI | Unity UI / uGUI 1.0.0 and TextMesh Pro 3.0.7 available in the package manifest |
| Additional packages | Timeline 1.7.7 and Visual Scripting 1.9.4; listed packages are not necessarily used by every feature |
| Authoring | Unity scenes, prefabs, materials, animation clips, and custom Editor validation commands |
| Version control | Git; large binary asset transfer remains pending |

Exact package resolutions are recorded in [Packages/manifest.json](Packages/manifest.json) and [Packages/packages-lock.json](Packages/packages-lock.json).

## Open the complete project

These steps require the full asset set; the current source snapshot alone is insufficient.

1. Install **Unity Hub** and **Unity 2022.3.62f3** with the build-support module for your intended target.
2. Add the complete project folder in Unity Hub and open it with that editor version.
3. Allow Unity to restore packages and import assets. Generated `Library/` data is intentionally excluded from Git.
4. Open `Assets/Scenes/MainScene/MainSceneFile.unity`, the scene enabled in Build Settings.
5. Confirm the Console has no compilation errors, then press **Play**.

Keep the existing `.meta` files with their assets so GUID references remain intact. Do not open in a newer editor and commit an automatic upgrade without reviewing the scene, package, and rendering changes.

## Controls

| Action | Input |
| --- | --- |
| Move / steer | Horizontal and Vertical axes, normally WASD or arrow keys |
| Run | Left Shift while moving forward |
| Jump | Space |
| Crouch | C or Left Ctrl |
| Grab / release ladder | E nearby |
| Climb | Up / Down movement input while on the ladder |
| Drop from ladder | Space |
| Identify a prop | Left click |
| Inspect a nearby story clue | R or left click |
| Reveal / advance story text | Enter or Space |
| Close story presentation | Esc |
| Inspection / pickup gestures | G / F; these gestures do not collect story items |

Story interactions check distance, line of sight, grounding, and ladder state. During a story presentation, movement pauses while gravity remains active.

## Script architecture

| Component | Responsibility |
| --- | --- |
| `FarmPlayerMotor` | Movement, turning, animation state selection, jumping, crouching, and ladder interaction |
| `CharacterGroundSafety` | Floor recovery and upward-only visual pose correction for ground penetration |
| `CameraFollow` | Third-person follow smoothing and obstacle checks |
| `BarnLadder` | Ladder grab points, climb direction, and supported landing points |
| `WorldItemInspector` / `InspectableItem` | Prop identification and optional authored labels |
| `StructureLocationMessages` | Location labels for farm structures |
| `StoryPickupItem` / `FarmStoryObjects` | Clue data, saved references, and supported placement |
| `PickupStorySequence` | Pickup eligibility, camera reveal, dialogue, and return to exploration |
| `BlackScreenStartup` / `SkyController` | Startup presentation and sky transitions |

Older movement, inspection, and fade alternatives remain in the source for reference. Keep superseded components disabled in the main scene so controllers do not compete. See [SCRIPT_GUIDE.md](SCRIPT_GUIDE.md) for component placement and implementation notes.

## Project structure

```text
Assets/
  Scripts/           Custom runtime components and their .meta files
  Editor/            Custom validation and authoring tools
  Scenes/            Main and demo scenes (asset upload pending)
  ...                Environment, character, animation, and visual assets (pending)
Packages/             Package manifest and lockfile
ProjectSettings/      Unity editor, input, physics, graphics, and build configuration
```

## Validation and playtesting

With the complete project open, use the **Tools > Farm Game** commands to validate animation/ladder setup, saved story pickups, and farm layout. Read the Console for each named issue.

Check walking, sprinting, jumping, crouching, ladder entry/exit, loft collisions, camera behavior near walls, all six clue interactions, dialogue dismissal, and movement recovery. Also test invalid interactions from behind walls, at a distance, in the air, or while climbing.

Previous development notes report script compilation and static scene checks. This upload does **not** establish a passing native Unity import, playtest, or platform build. Earlier automated Editor launches stalled on Unity licensing; rendering, retargeting, and native terrain support still need hands-on verification. No automated gameplay test suite is included.

## Build a player

After restoring all assets and resolving Console errors, open **File > Build Settings**, confirm `MainSceneFile` is enabled, select your target, and choose **Build**. Keep output under an ignored `Build/` or `Builds/` folder. No release target or performance benchmark has been validated. Current Player Settings still use `DefaultCompany` and `New Game`; review product identity and platform settings before distributing a build.

## Development notes

- [Script guide](SCRIPT_GUIDE.md)
- [Story collectibles and placement](STORY_PICKUPS.md)
- [Farm layout cleanup](FARM_LAYOUT_CLEANUP.md)
- [Gameplay implementation](FARM_GAMEPLAY_IMPLEMENTATION.md)
- [Movement and collision work](MOVEMENT_COLLISION_FIX.md)
- [Steering and location messages](STEERING_AND_LOCATION_FIX.md)
- [Earlier fix report](FIX_REPORT.md)

These notes describe specific development passes; their historical verification statements are not new test results for this upload.

## Asset provenance and large files

The local project includes imported content under folders such as `MarpaStudio`, `NatureStarterKit2`, `IgniteCoders/Simple Water Shader`, `countryhouse`, and the Bandit character assets. Folder names identify imported content, not a verified licensing inventory. Preserve vendor notices and confirm usage/redistribution terms before sharing a build or making the repository public. No blanket license is applied to third-party materials.

The full project has roughly 3.3 GB of assets, including two `.unitypackage` archives of about 323 MB and 426 MB. The archives may be redundant import packages; review them before deciding what to retain. Large binary assets need an appropriate transfer plan, such as Git LFS. The source-only upload does not contain placeholder LFS pointers that pretend the binaries are available.

## Next milestones

- Complete the scene and binary asset transfer and verify a clean checkout.
- Run native Unity validation and record the results.
- Add representative screenshots, a short gameplay video, and a tested build.
- Document target platform, asset credits, and performance measurements.
