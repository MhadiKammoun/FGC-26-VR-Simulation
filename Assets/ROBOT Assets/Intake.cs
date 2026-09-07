using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class SimpleIntake : MonoBehaviour
{
    [Header("Input Action")]
    [Tooltip("Input action to toggle intake (Keyboard 'F', Gamepad, or XR Grip)")]
    [SerializeField] private InputActionReference intakeToggleAction;

    [Header("Intake Physics")]
    [SerializeField] private float pushForce = 15f;
    [Tooltip("Direction relative to this transform where objects are pulled")]
    [SerializeField] private Vector3 pushDirection = Vector3.forward;
    [SerializeField] private string targetTag = "WildFire";

    [Header("Roller Visual")]
    [SerializeField] private Transform roller;
    [SerializeField] private float rollerSpeed = 360f;
    [SerializeField] private Vector3 rollerRotationAxis = Vector3.right; // Local X (1, 0, 0)
    [SerializeField] private bool invertRoller = true;                  // Inverted

    [Header("Status")]
    [SerializeField] private bool intakeActive = false;

    private readonly HashSet<Rigidbody> contactingBodies = new HashSet<Rigidbody>();

    private void OnEnable()
    {
        if (intakeToggleAction != null)
        {
            intakeToggleAction.action.Enable();
            intakeToggleAction.action.performed += OnTogglePressed;
        }
    }

    private void OnDisable()
    {
        if (intakeToggleAction != null)
        {
            intakeToggleAction.action.performed -= OnTogglePressed;
            intakeToggleAction.action.Disable();
        }
        contactingBodies.Clear();
    }

    private void OnTogglePressed(InputAction.CallbackContext context)
    {
        intakeActive = !intakeActive;
        Debug.Log("Intake " + (intakeActive ? "ACTIVE" : "INACTIVE"));
    }

    private void Update()
    {
        if (intakeActive && roller != null)
        {
            float directionSign = invertRoller ? -1f : 1f;
            roller.Rotate(rollerRotationAxis * (rollerSpeed * directionSign * Time.deltaTime), Space.Self);
        }
    }

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !contactingBodies.Contains(rb))
            {
                contactingBodies.Add(rb);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && contactingBodies.Contains(rb))
            {
                contactingBodies.Remove(rb);
            }
        }
    }
}