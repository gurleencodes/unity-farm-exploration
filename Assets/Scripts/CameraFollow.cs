using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    public float distance = 10f;
    public float height = 7f;
    public float lookHeight = 3.5f;
    public float followSmoothness = 3f;

    public LayerMask obstacleLayers;
    public float cameraRadius = 0.25f;
    public float wallPadding = 0.1f;

    private Camera followCamera;

    void Awake()
    {
        followCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        Vector3 desiredPosition = target.position
            - target.forward * distance + Vector3.up * height;

        // Protect the near clipping plane, including its corners on wide screens.
        float near = followCamera.nearClipPlane;
        float halfHeight = followCamera.orthographic
            ? followCamera.orthographicSize
            : near * Mathf.Tan(followCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * followCamera.aspect;
        float radius = Mathf.Max(cameraRadius,
            Mathf.Sqrt(near * near + halfHeight * halfHeight + halfWidth * halfWidth));

        desiredPosition = ResolveObstacles(lookPoint, desiredPosition, radius);

        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, followSmoothness) * Time.deltaTime);
        Vector3 nextPosition = Vector3.Lerp(transform.position, desiredPosition, blend);

        // Recheck after smoothing: an old position can be behind a wall or corner.
        // Collision pulls in immediately; returning to the preferred distance is smooth.
        transform.position = ResolveObstacles(lookPoint, nextPosition, radius);

        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    private Vector3 ResolveObstacles(Vector3 origin, Vector3 destination, float radius)
    {
        Vector3 offset = destination - origin;
        float length = offset.magnitude;
        if (length <= 0.001f)
            return destination;

        Vector3 direction = offset / length;
        float safeDistance = length;

        // Player and scenery share Default in this project. Filter the player's
        // hierarchy rather than changing existing layers or hiding Default scenery.
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, length,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(target))
                continue;

            safeDistance = Mathf.Min(safeDistance,
                Mathf.Max(0f, hit.distance - Mathf.Max(0f, wallPadding)));
        }

        return origin + direction * safeDistance;
    }
}
