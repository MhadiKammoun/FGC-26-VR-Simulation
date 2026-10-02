using UnityEngine;

public class SciFiHover : MonoBehaviour
{
    [Header("Vertical Hover")]
    [Tooltip("Peak height distance up and down.")]
    [SerializeField] private float verticalAmplitude = 0.35f;
    [Tooltip("Speed of vertical oscillation.")]
    [SerializeField] private float verticalFrequency = 1.2f;

    [Header("Slight Lateral Drift")]
    [Tooltip("Subtle side-to-side drift distance.")]
    [SerializeField] private float horizontalAmplitude = 0.08f;
    [Tooltip("Speed of horizontal drift (kept slightly different to prevent rigid repetition).")]
    [SerializeField] private float horizontalFrequency = 0.7f;

    [Header("Optional Micro-Tilt")]
    [Tooltip("Subtle banking into the drift movement.")]
    [SerializeField] private float tiltAngle = 1.5f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float seedOffset;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Random offset so multiple platforms don't bob in sync
        seedOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = Time.time + seedOffset;

        // Primary vertical float
        float yOffset = Mathf.Sin(time * verticalFrequency) * verticalAmplitude;

        // Subtle side-to-side drift (local X axis)
        float xOffset = Mathf.Sin(time * horizontalFrequency) * horizontalAmplitude;

        // Apply position relative to starting spot
        Vector3 localOffset = transform.right * xOffset + transform.up * yOffset;
        transform.position = startPosition + localOffset;

        // Subtle tilt following the horizontal drift
        if (tiltAngle > 0f)
        {
            float zTilt = -Mathf.Cos(time * horizontalFrequency) * tiltAngle;
            transform.rotation = startRotation * Quaternion.Euler(0f, 0f, zTilt);
        }
    }
}