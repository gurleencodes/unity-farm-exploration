using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerSteeringMovement : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float turnSpeed = 180f;
    public float turnAcceleration = 1080f;
    public float gravity = -20f;

    [Min(0.01f)] public float acceleration = 18f;
    [Min(0.01f)] public float braking = 24f;
    [Min(0.01f)] public float animationSmoothTime = 0.08f;
    [SerializeField] private AnimationClip walkAnimation;
    [SerializeField] private AnimationClip runAnimation;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int WalkCycleRate = Animator.StringToHash("WalkCycleRate");
    private static readonly int RunCycleRate = Animator.StringToHash("RunCycleRate");

    private Animator animator;
    private CharacterController controller;
    private float currentSpeed;
    private float verticalVelocity;
    private float turnVelocity;
    private float walkCycleSpeed;
    private float runCycleSpeed;

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        controller.detectCollisions = true;
        if (animator != null)
            animator.applyRootMotion = false;

        // Account for the existing enlarged character when matching footfall timing.
        walkCycleSpeed = GetCycleSpeed(walkAnimation, walkSpeed);
        runCycleSpeed = GetCycleSpeed(runAnimation, runSpeed);
    }

    void OnDisable()
    {
        currentSpeed = 0f;
        verticalVelocity = 0f;
        turnVelocity = 0f;
        if (animator != null)
            animator.SetFloat(Speed, 0f);
    }

    void Update()
    {
        if (!controller.enabled || Time.deltaTime <= 0f)
            return;

        // Limit a single hitch's simulation time; sweep small steps through scenery.
        float deltaTime = Mathf.Min(Time.deltaTime, 0.1f);
        float turnInput = Input.GetAxisRaw("Horizontal");
        float moveInput = Input.GetAxisRaw("Vertical");

        // Left/right alone pivot in place. Combine with up/down to steer and move.
        turnVelocity = turnInput == 0f ? 0f : Mathf.MoveTowards(turnVelocity,
            turnInput * Mathf.Max(0f, turnSpeed), Mathf.Max(1f, turnAcceleration) * deltaTime);
        transform.rotation = Quaternion.Euler(0f,
            transform.eulerAngles.y + turnVelocity * deltaTime, 0f);

        bool running = moveInput != 0f && Input.GetKey(KeyCode.LeftShift);
        float desiredSpeed = moveInput * Mathf.Max(0f, running ? runSpeed : walkSpeed);
        if (moveInput == 0f)
        {
            // Releasing up/down stops translation immediately, including while turning.
            currentSpeed = 0f;
        }
        else
        {
            if (currentSpeed * desiredSpeed < 0f)
                currentSpeed = 0f;
            float response = Mathf.Abs(desiredSpeed) > Mathf.Abs(currentSpeed) ? acceleration : braking;
            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed,
                Mathf.Max(0.01f, response) * deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity = Mathf.Max(-35f, verticalVelocity + gravity * deltaTime);

        Vector3 velocity = transform.forward * currentSpeed;
        velocity.y = verticalVelocity;
        Vector3 displacement = velocity * deltaTime;
        Vector3 beforeMove = transform.position;
        int steps = Mathf.Clamp(Mathf.CeilToInt(displacement.magnitude / 0.15f), 1, 32);
        CollisionFlags collisions = CollisionFlags.None;
        for (int i = 0; i < steps; i++)
            collisions |= controller.Move(displacement / steps);

        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
        if ((collisions & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            verticalVelocity = -2f;

        Vector3 actualMovement = transform.position - beforeMove;
        actualMovement.y = 0f;
        float actualSpeed = actualMovement.magnitude / deltaTime;
        if ((collisions & CollisionFlags.Sides) != 0)
            currentSpeed = Mathf.Sign(currentSpeed) * Mathf.Min(Mathf.Abs(currentSpeed), actualSpeed);

        if (animator != null)
        {
            // Speed describes real displacement, so pushing into a wall returns to idle.
            float animationSpeed = actualSpeed <= walkSpeed
                ? actualSpeed / Mathf.Max(0.01f, walkSpeed)
                : 1f + (actualSpeed - walkSpeed) / Mathf.Max(0.01f, runSpeed - walkSpeed);
            animator.SetFloat(Speed, Mathf.Clamp(animationSpeed, 0f, 2f),
                Mathf.Max(0.01f, animationSmoothTime), deltaTime);
            animator.SetFloat(WalkCycleRate, Mathf.Clamp(actualSpeed / walkCycleSpeed, 0.2f, 2f),
                Mathf.Max(0.01f, animationSmoothTime), deltaTime);
            animator.SetFloat(RunCycleRate, Mathf.Clamp(actualSpeed / runCycleSpeed, 0.2f, 2f),
                Mathf.Max(0.01f, animationSmoothTime), deltaTime);
        }
    }

    private float GetCycleSpeed(AnimationClip clip, float fallback)
    {
        if (clip != null)
        {
            Vector3 sourceSpeed = clip.averageSpeed;
            sourceSpeed.y = 0f;
            float worldSpeed = sourceSpeed.magnitude * Mathf.Abs(transform.lossyScale.y);
            if (worldSpeed > 0.01f)
                return worldSpeed;
        }
        // In-place clips have no measurable root speed.
        return Mathf.Max(0.01f, fallback);
    }
}
