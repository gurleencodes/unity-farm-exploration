# Steering, structure names, and black startup

Three new scripts implement this follow-up. Existing scripts remain unchanged. The old Movement, BarnEntryMessage, and SceneStartupFade components are disabled in MainSceneFile to prevent conflicts.

- PlayerSteeringMovement.cs: Left/Right or A/D alone turn in place with zero forward/backward velocity. Up/Down or W/S move forward/backward; combining with Left/Right steers while moving. Left Shift runs. Releasing Up/Down stops translation immediately. Existing walk/run speeds (3/7), CharacterController collision, root-motion-off behavior, and scale are preserved.
- StructureLocationMessages.cs: one shared three-second banner shows structure names, without "Entering". It recognizes the scene's barn, windmill, both wells, houses, green house, old shed, shed, silo, outhouse, chicken house, and gate. Recognized copies also work. It discovers structures at startup and uses their footprints plus a three-unit approach margin, so labels work near wells and windmills as well as inside buildings. There are no extra physical colliders. Selection hysteresis reduces repeated messages at boundaries. Static structures added after startup are not discovered until the next scene start.
- BlackScreenStartup.cs: fades the saved Black Startup Screen CanvasGroup from opaque black over three seconds, after a 0.2-second hold. The overlay exists in the saved scene, before runtime UI creation. The active camera's fallback background and the built-game splash background are also black. The sky still appears normally after the fade.

Scene changes wire these scripts, disable the superseded components, add a full-screen saved black Canvas, and change only the active camera's fallback color. Existing lights, sky script, camera follow preferences, collider configuration, animation controller, and player scale are unchanged. ProjectSettings.asset changes only the splash background color. New script .meta files have new GUIDs; existing GUIDs are preserved. Unity remains 2022.3.62f3. No commit or push was made.

Validation: all three new scripts compile against the installed Unity 2022.3.62f3 assemblies. Eighteen executable tests passed against the actual structure classifier, covering all scene structure types, copy names, and excluded props/doors. Scene-reference, enabled-component, saved-overlay, GUID and file-preservation checks passed. These are compilation and data checks, not gameplay tests. Earlier Editor validation was blocked at Unity licensing; this follow-up was not played in Unity.

Test:

1. Open MainSceneFile and press Play. The Game view should begin black and fade to the scene. Outside Play mode the saved black overlay also covers Game preview; select Black Startup Screen in the Hierarchy if you need to identify it.
2. Hold Left alone: the character should stay in place and turn left. Repeat with Right. Hold Up+Left or Up+Right: she should walk while steering. Test Down combinations, release Up/Down while still turning, and hold Left Shift while moving.
3. Approach the barn, windmill, wells, each house, sheds, silo, outhouse, and chicken house. The banner should show only the name. Leave and return to check it can show again. Footprint-based detection is approximate and still needs visual checking around the rotating windmill and adjacent buildings.
4. Select BanditMain > Player Steering Movement to adjust Turn Speed (180 degrees/s), Turn Acceleration (1080 degrees/s²), or walk/run settings. Keep the old Movement component unchecked.
5. Select Sky and Barn Effects > Structure Location Messages to adjust Approach Distance, Display Seconds, and Fade Seconds. Keep Barn Entry Message unchecked.
6. Select FadeSceneManager > Black Screen Startup to adjust the fade. Keep the older fade components unchecked so there is only one startup overlay controller. Check the Console for errors.
