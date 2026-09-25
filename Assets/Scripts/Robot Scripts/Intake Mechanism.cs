using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleIntakePusher : MonoBehaviour
{
    [Header("Intake Physics")]
    [SerializeField] private float pushForce = 25f;
    [Tooltip("Direction relative to this transform where objects are pushed during normal intake")]
    [SerializeField] private Vector3 pushDirection = Vector3.forward;
    [SerializeField] private string targetTag = "WildFire";

    [Header("Status (Managed by SimpleIntake)")]
    [SerializeField] private SimpleIntake.IntakeState currentState = SimpleIntake.IntakeState.Off;

    private readonly HashSet<Rigidbody> contactingBodies = new HashSet<Rigidbody>();

    /// <summary>
    /// Called automatically by the master SimpleIntake controller.
    /// </summary>
    public void SyncState(SimpleIntake.IntakeState newState)
    {
        currentState = newState;

        if (currentState == SimpleIntake.IntakeState.Off)
        {
            contactingBodies.Clear();
        }
    }

    private void FixedUpdate()
    {
        if (currentState == SimpleIntake.IntakeState.Off || contactingBodies.Count == 0) return;

        float directionMultiplier = (currentState == SimpleIntake.IntakeState.Intake) ? 1f : -1f;
        Vector3 forceVector = transform.TransformDirection(pushDirection.normalized) * (pushForce * directionMultiplier);

        foreach (Rigidbody rb in contactingBodies)
        {
            if (rb != null)
            {
                rb.AddForce(forceVector, ForceMode.Acceleration);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null && !contactingBodies.Contains(rb))
            {
                contactingBodies.Add(rb);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null && contactingBodies.Contains(rb))
            {
                contactingBodies.Remove(rb);
            }
        }
    }
}