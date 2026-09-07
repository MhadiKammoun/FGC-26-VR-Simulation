using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CenterOfMassStabilizer : MonoBehaviour
{
    [Tooltip("Offset relative to the robot origin. Negative Y pulls weight down.")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.2f, 0f);

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizes center of mass in Scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMassOffset), 0.04f);
    }
}