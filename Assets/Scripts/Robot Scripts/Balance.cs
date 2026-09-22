using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CenterOfMassStabilizer : MonoBehaviour
{
    [Tooltip("Offset relative to the robot origin. Keep Y modest (-0.05 to -0.1) to avoid unnatural pendulum self-righting.")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.05f, 0f);

    [Tooltip("Toggle to enable/disable custom center of mass override.")]
    [SerializeField] private bool applyCustomCenterOfMass = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ApplyCenterOfMass();
    }

    private void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        ApplyCenterOfMass();
    }

    private void ApplyCenterOfMass()
    {
        if (rb == null) return;

        if (applyCustomCenterOfMass)
        {
            rb.centerOfMass = centerOfMassOffset;
        }
        else
        {
            rb.ResetCenterOfMass();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 pos = applyCustomCenterOfMass ? transform.TransformPoint(centerOfMassOffset) : transform.position;
        Gizmos.DrawSphere(pos, 0.04f);
    }
}