using UnityEngine;

public class RotateObjectWithPivot : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Base rotation speed (degrees per second).")]
    public float baseSpeed = 50f;

    [Tooltip("1 = clockwise, -1 = counterclockwise.")]
    public float direction = 1f;

    [Tooltip("If assigned, rotation will happen around this pivot instead of the object's own pivot.")]
    public Transform customPivot;

    [Header("Variation Settings (Optional)")]
    [Tooltip("How much the rotation speed fluctuates.")]
    public float variation = 0f;

    [Tooltip("How fast the speed fluctuation occurs.")]
    public float variationFrequency = 1f;

    private float timeOffset;

    void Start()
    {
        // Random offset so multiple objects don't vary in sync
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Compute fluctuating speed if variation is enabled
        float currentSpeed = baseSpeed;
        if (variation > 0f)
        {
            float fluctuation = Mathf.Sin(Time.time * variationFrequency + timeOffset) * variation;
            currentSpeed += fluctuation;
        }

        float angleThisFrame = currentSpeed * direction * Time.deltaTime;

        // Determine rotation point and axis
        Vector3 pivotPoint = customPivot ? customPivot.position : transform.position;
        Vector3 rotationAxis = Vector3.up;

        // Rotate around pivot
        transform.RotateAround(pivotPoint, rotationAxis, angleThisFrame);
    }
}
