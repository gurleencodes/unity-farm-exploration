using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FarmGameplayValidation
{
    [MenuItem("Tools/Farm Game/Validate animations and ladder")]
    public static void Run()
    {
        int errors = Validate();
        Debug.Log("Farm gameplay validation: " + errors + " error(s). This checks assets and physics geometry, not a gameplay session.");
    }

    public static void Batch()
    {
        if (!Application.isBatchMode) return;
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene/MainSceneFile.unity");
        int errors = Validate();
        Debug.Log("Farm gameplay native validation complete. Errors: " + errors);
        EditorApplication.Exit(errors == 0 ? 0 : 1);
    }

    private static int Validate()
    {
        int errors = 0;
        string[] clips = { "Climbing Ladder", "Climbing To Top", "Crouched Walking", "Jumping", "Jumping Down", "Picking Up", "Picking Up Object", "Walking Backwards", "Left Turn", "Right Turn" };
        foreach (string clip in clips)
        {
            string path = "Assets/Animations/" + clip + ".fbx";
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Avatar avatar = assets.OfType<Avatar>().FirstOrDefault();
            AnimationClip motion = assets.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (importer == null || importer.animationType != ModelImporterAnimationType.Human
                || avatar == null || !avatar.isValid || !avatar.isHuman || motion == null || !motion.humanMotion)
            {
                Debug.LogError("Humanoid import needs checking: " + path);
                errors++;
            }
        }
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/FarmCharacterAnimator.controller");
        if (controller == null) { Debug.LogError("FarmCharacterAnimator is missing."); return errors + 1; }
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            if (child.state.motion == null)
            {
                Debug.LogError("Missing animation on state " + child.state.name);
                errors++;
            }
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/MainScene/MainSceneFile.unity")
        {
            Debug.LogWarning("Open MainSceneFile to check the ladder/player scene setup.");
            return errors;
        }
        FarmPlayerMotor[] players = Object.FindObjectsOfType<FarmPlayerMotor>(true);
        BarnLadder ladder = Object.FindObjectOfType<BarnLadder>();
        if (players.Length != 1 || !players[0].enabled || ladder == null)
        {
            Debug.LogError("Expected one enabled FarmPlayerMotor and one BarnLadder.");
            return errors + 1;
        }
        FarmPlayerMotor player = players[0];
        CharacterController capsule = player.GetComponent<CharacterController>();
        if (player.GetComponent<Animator>().applyRootMotion) { Debug.LogError("Apply Root Motion must be off."); errors++; }
        if (player.GetComponents<MonoBehaviour>().Any(c => c != null && c.enabled &&
            (c is Movement || c is PlayerSteeringMovement)))
        {
            Debug.LogError("An older movement component is still enabled.");
            errors++;
        }
        if (Object.FindObjectsOfType<WorldItemInspector>().Length != 1 || Object.FindObjectsOfType<BarnItemInspector>().Any(c => c.enabled))
        { Debug.LogError("Expected one world inspector and no enabled barn-only inspector."); errors++; }
        Physics.SyncTransforms();
        Vector3 landing = ladder.Landing(true);
        if (!Physics.Raycast(landing + Vector3.up * 0.3f, Vector3.down, out RaycastHit floor,
            1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        { Debug.LogError("No loft floor below the ladder landing."); errors++; }
        else Debug.Log("Loft landing supported by: " + floor.collider.name);

        float radius = capsule.radius * Mathf.Max(Mathf.Abs(capsule.transform.lossyScale.x), Mathf.Abs(capsule.transform.lossyScale.z)) * 0.97f;
        float height = capsule.height * Mathf.Abs(capsule.transform.lossyScale.y) * 0.6f;
        for (int i = 0; i <= 12; i++)
        {
            Vector3 feet = Vector3.Lerp(ladder.Top, landing, i / 12f);
            Vector3 bottom = feet + Vector3.up * (radius + 0.12f);
            Vector3 top = feet + Vector3.up * Mathf.Max(radius + 0.12f, height - radius);
            Collider obstruction = Physics.OverlapCapsule(bottom, top, radius, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore).FirstOrDefault(c => !c.transform.IsChildOf(player.transform));
            if (obstruction == null) continue;
            Debug.LogError("Loft exit clearance blocked by " + obstruction.name + " at sample " + i);
            errors++;
            break;
        }
        Debug.Log("Verify in Play mode: E to mount, Up/Down to climb, Space to jump, C to crouch, left-click props. Runtime movement and visual retargeting still need observation.");
        return errors;
    }
}
