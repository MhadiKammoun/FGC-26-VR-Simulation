using UnityEngine;

public class ClimbPipe : MonoBehaviour
{
    [Header("Pipe Anchor Points")]
    [Tooltip("Bottom entry point of the pipe/brace")]
    public Transform startPoint;

    [Tooltip("Top peak/exit point of the pipe/brace")]
    public Transform endPoint;

    [Header("Gizmo Display Settings")]
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private float pointGizmoRadius = 0.04f;

    /// <summary>
    /// Returns the total length of the pipe segment.
    /// </summary>
    public float Length
    {
        get
        {
            if (startPoint == null || endPoint == null) return 0f;
            return Vector3.Distance(startPoint.position, endPoint.position);
        }
    }

    /// <summary>
    /// Normalized direction pointing from startPoint to endPoint.
    /// </summary>
    public Vector3 Direction
    {
        get
        {
            if (startPoint == null || endPoint == null) return transform.forward;
            return (endPoint.position - startPoint.position).normalized;
        }
    }

    /// <summary>
    /// Gets the world-space coordinate along the pipe at a given distance from startPoint.
    /// </summary>
    public Vector3 GetPointAtDistance(float distance)
    {
        if (startPoint == null || endPoint == null) return transform.position;
        return startPoint.position + (Direction * Mathf.Clamp(distance, 0f, Length));
    }

    /// <summary>
    /// Projects any world position onto the pipe line segment and returns the distance from startPoint.
    /// </summary>
    public float GetProjectedDistance(Vector3 worldPoint)
    {
        if (startPoint == null || endPoint == null) return 0f;
        Vector3 toPoint = worldPoint - startPoint.position;
        float projected = Vector3.Dot(toPoint, Direction);
        return Mathf.Clamp(projected, 0f, Length);
    }

    private void OnDrawGizmos()
    {
        if (startPoint == null || endPoint == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(startPoint.position, endPoint.position);

        Gizmos.DrawWireSphere(startPoint.position, pointGizmoRadius);
        Gizmos.DrawWireSphere(endPoint.position, pointGizmoRadius);
    }
}