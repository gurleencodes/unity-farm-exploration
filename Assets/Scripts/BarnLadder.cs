using UnityEngine;

[DisallowMultipleComponent]
public class BarnLadder : MonoBehaviour
{
    public Transform barnFrame;
    public Vector3 bottomGrab = new Vector3(1.5f, 0.1f, -6.65f);
    public Vector3 topGrab = new Vector3(1.5f, 5.55f, -6.65f);
    public Vector3 bottomLanding = new Vector3(2.2f, 0.1f, -6.65f);
    public Vector3 loftLanding = new Vector3(-0.85f, 5.55f, -6.65f);
    [Min(0.5f)] public float grabDistance = 3.5f;
    [Min(0.1f)] public float climbSpeed = 3.5f;

    public Vector3 Bottom
    {
        get
        {
            Vector3 point = Frame.TransformPoint(bottomGrab);
            // Never ask the mounting path to pull the player's feet below the floor.
            if (TryFloor(point, 2f, 4f, out Vector3 supported))
                point.y = Mathf.Max(point.y, supported.y);
            return point;
        }
    }
    public Vector3 Top => Frame.TransformPoint(topGrab);
    public Vector3 Facing => Vector3.ProjectOnPlane(Frame.TransformDirection(Vector3.left), Vector3.up).normalized;
    private Transform Frame => barnFrame != null ? barnFrame : transform;

    private void OnEnable()
    {
        if (barnFrame != null)
        {
            transform.SetPositionAndRotation(barnFrame.position, barnFrame.rotation);
            transform.localScale = barnFrame.lossyScale;
        }
    }

    public Vector3 Landing(bool top)
    {
        Vector3 landing;
        return TryLanding(top, out landing) ? landing : Frame.TransformPoint(top ? loftLanding : bottomLanding);
    }

    public bool TryLanding(bool top, out Vector3 landing)
    {
        Vector3 candidate = Frame.TransformPoint(top ? loftLanding : bottomLanding);
        return TryFloor(candidate, top ? 0.5f : 2f, top ? 1.5f : 4f, out landing);
    }

    private bool TryFloor(Vector3 candidate, float above, float distance, out Vector3 supported)
    {
        supported = candidate;
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(candidate + Vector3.up * above,
            Vector3.down, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<CharacterController>() != null
                || hit.collider.GetComponentInParent<BarnLadder>() != null || hit.normal.y < 0.5f)
                continue;
            if (hit.distance >= nearest) continue;
            nearest = hit.distance;
            supported.y = hit.point.y + 0.12f;
        }
        return nearest < float.PositiveInfinity;
    }

    public bool CanGrab(Vector3 feet, out bool fromTop)
    {
        float lower = Vector3.Distance(feet, Landing(false));
        float upper = Vector3.Distance(feet, Landing(true));
        fromTop = upper < lower;
        return Mathf.Min(lower, upper) <= Mathf.Max(0.5f, grabDistance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Bottom, Top);
        Gizmos.DrawWireSphere(Bottom, grabDistance);
        Gizmos.DrawWireSphere(Top, grabDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Top, Frame.TransformPoint(loftLanding));
        Gizmos.DrawSphere(Frame.TransformPoint(loftLanding), 0.2f);
    }
}
