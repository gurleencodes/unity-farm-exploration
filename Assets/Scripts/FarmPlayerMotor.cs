using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class FarmPlayerMotor : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 7f;
    public float crouchSpeed = 1.6f;
    public float turnSpeed = 180f;
    public float turnAcceleration = 1080f;
    public float acceleration = 18f;
    public float gravity = -20f;
    public float jumpHeight = 3f;
    public BarnLadder ladder;
    public CanvasGroup startupOverlay;
    public AnimationClip walkClip, runClip, backwardClip, crouchClip, climbTopClip, pickupClip, reachClip;

    private bool storyInteraction;
    public void SetStoryInteraction(bool active)
    {
        storyInteraction = active;
        speed = yawVelocity = 0f;
        actionEnds = 0f;
        jumpQueued = -100f;
        pushOff = Vector3.zero;
    }

    public bool IsClimbing => mode != LadderMode.None;
    private enum LadderMode { None, MountBottom, MountTop, Climbing, ExitTop, ExitBottom }
    private LadderMode mode;
    private CharacterController controller;
    private Animator animator;
    private float originalHeight;
    private Vector3 originalCenter;
    private bool crouching;
    private float speed, yawVelocity, verticalVelocity, lastGrounded = -100f, jumpQueued = -100f;
    private float climbProgress, climbPhase, poseTime, pathProgress;
    private Vector3 pathStart, pathEnd, pushOff;
    private float actionEnds;
    private string actionState;
    private string currentState;
    private float motionRate = 1f;
    private string hint;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        originalHeight = controller.height;
        originalCenter = controller.center;
        controller.detectCollisions = true;
        if (animator != null) animator.applyRootMotion = false;
    }

    private Vector3 Feet => transform.TransformPoint(controller.center - Vector3.up * controller.height * 0.5f);
    private bool Covered => startupOverlay != null && startupOverlay.gameObject.activeInHierarchy && startupOverlay.alpha > 0.01f;

    private void Update()
    {
        if (!controller.enabled || Time.deltaTime <= 0f) return;
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        hint = null;
        if (Covered || storyInteraction)
        {
            speed = 0f;
            if (controller.isGrounded) verticalVelocity = -2f;
            verticalVelocity = Mathf.Max(-35f, verticalVelocity + gravity * dt);
            Move(Vector3.up * verticalVelocity * dt);
            Animate("Idle", 1f);
            return;
        }
        if (IsClimbing)
        {
            UpdateLadder(dt);
            return;
        }

        float turn = Input.GetAxisRaw("Horizontal");
        float forward = Input.GetAxisRaw("Vertical");
        if (Input.GetKeyDown(KeyCode.Space)) jumpQueued = Time.time;
        if (controller.isGrounded) lastGrounded = Time.time;
        if (Input.GetKeyDown(KeyCode.E) && ladder != null && ladder.CanGrab(Feet, out bool top))
        {
            BeginMount(top);
            return;
        }
        if (ladder != null && ladder.CanGrab(Feet, out _)) hint = "E: climb ladder   |   Space: jump";

        bool wantsCrouch = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
        if (wantsCrouch) SetCrouched(true);
        else TryStand();
        if (crouching && !wantsCrouch) hint = "Low ceiling: crouching until there is room to stand";

        if (turn != 0f || forward != 0f || Input.GetKeyDown(KeyCode.Space)) actionEnds = 0f;
        if (Time.time < actionEnds)
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity = Mathf.Max(-35f, verticalVelocity + gravity * dt);
            CollisionFlags actionFlags = Move(Vector3.up * verticalVelocity * dt);
            if ((actionFlags & CollisionFlags.Below) != 0) verticalVelocity = -2f;
            Animate(actionState, 1f);
            hint = "Inspecting... move to cancel";
            return;
        }
        if (controller.isGrounded && (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.F)))
        {
            bool reach = Input.GetKeyDown(KeyCode.F);
            actionState = reach ? "ReachDown" : "PickUp";
            AnimationClip clip = reach ? reachClip : pickupClip;
            actionEnds = Time.time + (clip != null ? clip.length : 1.5f);
            speed = 0f;
            Animate(actionState, 1f);
            return;
        }

        yawVelocity = turn == 0f ? 0f : Mathf.MoveTowards(yawVelocity,
            turn * turnSpeed, turnAcceleration * dt);
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + yawVelocity * dt, 0f);
        bool running = !crouching && forward > 0f && Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = forward * (crouching ? crouchSpeed : running ? runSpeed : walkSpeed);
        if (forward == 0f) speed = 0f;
        else
        {
            if (speed * targetSpeed < 0f) speed = 0f;
            speed = Mathf.MoveTowards(speed, targetSpeed, acceleration * dt);
        }
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (Time.time - jumpQueued <= 0.12f && Time.time - lastGrounded <= 0.12f)
        {
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Max(0.1f, jumpHeight));
            jumpQueued = -100f;
            lastGrounded = -100f;
            grounded = false;
        }
        verticalVelocity = Mathf.Max(-35f, verticalVelocity + gravity * dt);
        pushOff = Vector3.MoveTowards(pushOff, Vector3.zero, 8f * dt);
        Vector3 before = transform.position;
        CollisionFlags flags = Move((transform.forward * speed + pushOff + Vector3.up * verticalVelocity) * dt);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        grounded = (flags & CollisionFlags.Below) != 0;
        if (grounded) { verticalVelocity = -2f; lastGrounded = Time.time; }
        Vector3 moved = transform.position - before;
        moved.y = 0f;
        float actualSpeed = moved.magnitude / dt;
        if ((flags & CollisionFlags.Sides) != 0) speed = Mathf.Sign(speed) * Mathf.Min(Mathf.Abs(speed), actualSpeed);

        if (!grounded && Time.time - lastGrounded > 0.08f)
            Animate(verticalVelocity > 0f ? "JumpUp" : "JumpDown", 1f);
        else if (crouching)
            Animate("Crouch", actualSpeed < 0.08f ? 0f : CycleRate(crouchClip, actualSpeed, crouchSpeed));
        else if (actualSpeed > 0.08f)
        {
            string gait = forward < 0f ? "WalkBack" : running && actualSpeed > walkSpeed + 0.5f ? "Run" : "Walk";
            AnimationClip clip = gait == "Run" ? runClip : gait == "WalkBack" ? backwardClip : walkClip;
            Animate(gait, CycleRate(clip, actualSpeed, gait == "Run" ? runSpeed : walkSpeed));
        }
        else Animate(turn < 0f ? "TurnLeft" : turn > 0f ? "TurnRight" : "Idle", 1f);
    }

    private CollisionFlags Move(Vector3 displacement)
    {
        int steps = Mathf.Clamp(Mathf.CeilToInt(displacement.magnitude / 0.12f), 1, 64);
        CollisionFlags flags = CollisionFlags.None;
        for (int i = 0; i < steps; i++) flags |= controller.Move(displacement / steps);
        return flags;
    }

    // Called by the independent ground guard after recovering a penetrated floor.
    public void NotifyFloorCorrection()
    {
        if (verticalVelocity < 0f) verticalVelocity = -2f;
        lastGrounded = Time.time;
    }

    private void SetCrouched(bool value)
    {
        if (crouching == value) return;
        crouching = value;
        controller.height = value ? originalHeight * 0.6f : originalHeight;
        controller.center = originalCenter - Vector3.up * ((originalHeight - controller.height) * 0.5f);
    }

    private void TryStand()
    {
        if (!crouching) return;
        Vector3 scale = transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.97f;
        float height = originalHeight * Mathf.Abs(scale.y);
        Vector3 feet = Feet;
        Vector3 bottom = feet + Vector3.up * (radius + 0.12f);
        Vector3 top = feet + Vector3.up * Mathf.Max(radius + 0.12f, height - radius);
        foreach (Collider hit in Physics.OverlapCapsule(bottom, top, radius,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform)) return;
        SetCrouched(false);
    }

    private void BeginMount(bool fromTop)
    {
        speed = verticalVelocity = yawVelocity = 0f;
        actionEnds = 0f;
        pushOff = Vector3.zero;
        SetCrouched(true); // Keep the enlarged capsule clear of the loft beams.
        transform.rotation = Quaternion.LookRotation(ladder.Facing, Vector3.up);
        mode = fromTop ? LadderMode.MountTop : LadderMode.MountBottom;
        pathStart = Feet;
        pathEnd = fromTop ? ladder.Top : ladder.Bottom;
        pathProgress = 0f;
        climbProgress = fromTop ? 1f : 0f;
    }

    private void UpdateLadder(float dt)
    {
        if (ladder == null) { ReleaseLadder(false); return; }
        hint = "Up/Down: climb   |   E: let go   |   Space: jump off";
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            ReleaseLadder(Input.GetKeyDown(KeyCode.Space));
            return;
        }
        if (mode != LadderMode.Climbing)
        {
            float length = Vector3.Distance(pathStart, pathEnd);
            float duration = mode == LadderMode.ExitTop || mode == LadderMode.MountTop
                ? Mathf.Max(1f, climbTopClip != null ? climbTopClip.length : 2f) : 0.8f;
            Vector3 before = Feet;
            Vector3 next = Vector3.MoveTowards(before, pathEnd, Mathf.Max(0.5f, length / duration) * dt);
            Move(next - before);
            pathProgress = length <= 0.01f ? 1f : Mathf.Clamp01(Vector3.Distance(pathStart, Feet) / length);
            if (mode == LadderMode.ExitTop || mode == LadderMode.MountTop)
            {
                poseTime = mode == LadderMode.MountTop ? 1f - pathProgress : pathProgress;
                Animate("ClimbTop", 1f);
            }
            else Animate(mode == LadderMode.ExitBottom ? "WalkBack" : "Walk", 0.7f);
            if (Vector3.Distance(Feet, pathEnd) <= 0.12f)
            {
                if (mode == LadderMode.MountTop || mode == LadderMode.MountBottom) mode = LadderMode.Climbing;
                else ReleaseLadder(false);
            }
            else if (Vector3.Distance(before, Feet) < 0.001f)
                hint = "Ladder path blocked. E: release   |   Space: jump off";
            return;
        }

        float input = Input.GetAxisRaw("Vertical");
        Vector3 line = ladder.Top - ladder.Bottom;
        float lengthOfLadder = Mathf.Max(0.01f, line.magnitude);
        float wanted = Mathf.Clamp01(climbProgress + input * ladder.climbSpeed * dt / lengthOfLadder);
        Vector3 previous = Feet;
        Move(Vector3.Lerp(ladder.Bottom, ladder.Top, wanted) - previous);
        climbProgress = Mathf.Clamp01(Vector3.Dot(Feet - ladder.Bottom, line) / line.sqrMagnitude);
        float travelled = Vector3.Dot(Feet - previous, line.normalized);
        climbPhase = Mathf.Repeat(climbPhase + travelled / 2.4f, 1f);
        Animate("Climb", 1f);
        if (input > 0f && climbProgress >= 0.995f) BeginExit(true);
        else if (input < 0f && climbProgress <= 0.005f) BeginExit(false);
    }

    private void BeginExit(bool top)
    {
        if (!ladder.TryLanding(top, out Vector3 landing))
        {
            hint = "No safe landing ahead. Climb back or press Space to jump.";
            return;
        }
        mode = top ? LadderMode.ExitTop : LadderMode.ExitBottom;
        pathStart = Feet;
        pathEnd = landing;
        pathProgress = 0f;
    }

    private void ReleaseLadder(bool jump)
    {
        bool safeExit = mode == LadderMode.ExitTop || mode == LadderMode.ExitBottom;
        mode = LadderMode.None;
        verticalVelocity = jump ? Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight) : -2f;
        if (jump && ladder != null) pushOff = -ladder.Facing * 4f;
        jumpQueued = -100f;
        lastGrounded = safeExit && !jump ? Time.time : -100f;
        speed = 0f;
        TryStand();
        Animate(jump ? "JumpUp" : "Idle", 1f);
    }

    private float CycleRate(AnimationClip clip, float actual, float fallback)
    {
        float source = clip != null ? Vector3.ProjectOnPlane(clip.averageSpeed, Vector3.up).magnitude * Mathf.Abs(transform.lossyScale.y) : 0f;
        return Mathf.Clamp(actual / Mathf.Max(0.1f, source > 0.01f ? source : fallback), 0.25f, 2f);
    }

    private void Animate(string state, float rate)
    {
        if (animator == null || !animator.isInitialized) return;
        if (currentState != state)
        {
            animator.CrossFadeInFixedTime("Base Layer." + state, 0.12f, 0);
            currentState = state;
        }
        motionRate = Mathf.MoveTowards(motionRate, rate, Time.deltaTime * 8f);
        animator.SetFloat("MotionRate", motionRate);
        animator.SetFloat("ClimbPhase", climbPhase);
        animator.SetFloat("PoseTime", poseTime);
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(hint) || Covered) return;
        float width = Mathf.Min(760f, Screen.width - 24f);
        GUIStyle style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height - 70f, width, 50f), hint, style);
    }

    private void OnDisable()
    {
        speed = verticalVelocity = yawVelocity = 0f;
        mode = LadderMode.None;
        currentState = null;
        hint = null;
    }
}
