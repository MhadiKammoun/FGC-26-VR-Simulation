using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleIntakePusher : MonoBehaviour
{
    [Header("Intake Physics")]
    [SerializeField] private float pushForce = 25f;
    [SerializeField] private Vector3 pushDirection = Vector3.forward;
    [SerializeField] private string targetTag = "WildFire";

    [Header("Status (Starts ON for testing)")]
    [SerializeField] private bool intakeActive = true;

    private readonly HashSet<Rigidbody> contactingBodies = new HashSet<Rigidbody>();

    private void FixedUpdate()
    {
        if (!intakeActive || contactingBodies.Count == 0) return;

        Vector3 forceVector = transform.TransformDirection(pushDirection.normalized) * pushForce;

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
        Debug.Log($"[Intake] Collided with: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");

        if (collision.gameObject.CompareTag(targetTag))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null)
            {
                contactingBodies.Add(rb);
                Debug.Log("[Intake] Added Rigidbody to pushing queue!");
            }
            else
            {
                Debug.LogWarning("[Intake] Object has tag but NO Rigidbody attached!");
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
                Debug.Log("[Intake] Removed Rigidbody from queue.");
            }
        }
    }
}