using UnityEngine;

// Animator evaluates its pose before LateUpdate. Correct the visible skeleton after
// sampling, independently of the CharacterController's collision capsule.
[DefaultExecutionOrder(2000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class CharacterGroundSafety : MonoBehaviour
{
    [Min(0.1f)] public float probeAboveFeet = 0.65f;
    [Min(0.1f)] public float probeBelowFeet = 0.8f;
    [Min(0f)] public float floorPadding = 0.015f;
    public bool anchorClimbAndInspectionPoses = true;

    private CharacterController controller;
    private FarmPlayerMotor motor;
    private Animator animator;
    private Transform hips, leftFoot, rightFoot, leftToes, rightToes;
    private Vector3 previousFeet;
    private bool calibrated;
    private float jointToSole;
    private AnimatorCullingMode originalCulling;
    public float LastPoseLift { get; private set; }
    public float LastFloorCorrection { get; private set; }

    private Vector3 Feet => transform.TransformPoint(controller.center - Vector3.up * controller.height * 0.5f);

    private void OnEnable()
    {
        controller = GetComponent<CharacterController>();
        motor = GetComponent<FarmPlayerMotor>();
        animator = GetComponent<Animator>();
        previousFeet = Feet;
        calibrated = false;
        jointToSole = 0.06f * Mathf.Abs(transform.lossyScale.y);
        if (animator != null)
        {
            originalCulling = animator.cullingMode;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    private bool CacheBones()
    {
        if (hips != null && leftFoot != null && rightFoot != null) return true;
        if (animator == null || !animator.isInitialized || !animator.isHuman) return false;
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
        rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        return hips != null && leftFoot != null && rightFoot != null;
    }

    private float LowestJoint()
    {
        float y = Mathf.Min(leftFoot.position.y, rightFoot.position.y);
        if (leftToes != null) y = Mathf.Min(y, leftToes.position.y);
        if (rightToes != null) y = Mathf.Min(y, rightToes.position.y);
        return y;
    }

    private bool IsInspectionPose()
    {
        if (animator == null || !animator.isInitialized) return false;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Base Layer.PickUp") || state.IsName("Base Layer.ReachDown")) return true;
        if (!animator.IsInTransition(0)) return false;
        state = animator.GetNextAnimatorStateInfo(0);
        return state.IsName("Base Layer.PickUp") || state.IsName("Base Layer.ReachDown");
    }

    private bool FindFloor(Vector3 feet, float above, float below, out float floorY)
    {
        floorY = float.NegativeInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(feet + Vector3.up * above, Vector3.down,
            above + below, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.55f) continue;
            // Rungs are for hands/feet while climbing, not the character's ground plane.
            if (hit.collider.GetComponentInParent<BarnLadder>() != null) continue;
            if (hit.point.y > floorY) floorY = hit.point.y;
        }
        return !float.IsNegativeInfinity(floorY);
    }

    private void LateUpdate()
    {
        if (controller == null || !controller.enabled) return;
        LastFloorCorrection = LastPoseLift = 0f;
        Vector3 feet = Feet;
        float drop = Mathf.Max(0f, previousFeet.y - feet.y);
        // Sweep from the previous foot height after a normal movement step. Do not
        // treat a large external teleport as a request to return to an old floor.
        float above = Vector3.Distance(previousFeet, feet) > 8f
            ? probeAboveFeet : Mathf.Max(probeAboveFeet, drop + 0.1f);
        if (FindFloor(feet, above, probeBelowFeet, out float floor))
        {
            float correction = floor + floorPadding - feet.y;
            if (correction > 0.002f)
            {
                Vector3 before = transform.position;
                controller.Move(Vector3.up * correction);
                LastFloorCorrection = Mathf.Max(0f, transform.position.y - before.y);
                if (LastFloorCorrection > 0f && motor != null) motor.NotifyFloorCorrection();
                feet = Feet;
            }
        }
        previousFeet = feet;
        if (!CacheBones()) return;

        bool special = (motor != null && motor.IsClimbing) || IsInspectionPose();
        float jointY = LowestJoint();
        if (!calibrated && controller.isGrounded && !special && !animator.IsInTransition(0)
            && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"))
        {
            // Preserve the avatar's normal standing foot/shoe offset, not its hip height.
            jointToSole = Mathf.Clamp(jointY - feet.y, 0f, 0.2f * Mathf.Abs(transform.lossyScale.y));
            calibrated = true;
        }

        float minimumSole = float.NegativeInfinity;
        if (special && anchorClimbAndInspectionPoses)
            minimumSole = feet.y;
        float lowestSole = jointY - jointToSole;
        float below = Mathf.Max(probeBelowFeet, feet.y - lowestSole + 0.1f);
        if (FindFloor(feet, probeAboveFeet, below, out floor))
            minimumSole = Mathf.Max(minimumSole, floor + floorPadding);

        if (float.IsNegativeInfinity(minimumSole)) return;
        float lift = minimumSole - lowestSole;
        if (lift > 0.001f && !float.IsNaN(lift) && !float.IsInfinity(lift))
        {
            // Move the pose, not the player root: no double gravity, root motion,
            // collider-height change, or camera jump to compensate for a bad pose.
            hips.position += Vector3.up * lift;
            LastPoseLift = lift;
        }
    }

    private void OnDisable()
    {
        if (animator != null) animator.cullingMode = originalCulling;
    }
}
