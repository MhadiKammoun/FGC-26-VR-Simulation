using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class SimpleIntake : MonoBehaviour
{
    public enum IntakeState
    {
        Off,
        Intake,
        Reverse
    }

    [Header("Subordinate Base Pusher")]
    [Tooltip("Drag the GameObject with SimpleIntakePusher here to sync them.")]
    [SerializeField] private SimpleIntakePusher basePusher;

    [Header("Input Actions")]
    [Tooltip("Press once to run forward, press again to turn off.")]
    [SerializeField] private InputActionReference intakeAction;
    [Tooltip("Press once to reverse/outtake, press again to turn off.")]
    [SerializeField] private InputActionReference reverseAction;

    [Header("Intake Physics")]
    [SerializeField] private float pushForce = 15f;
    [Tooltip("Direction relative to this transform where objects are pulled during normal intake")]
    [SerializeField] private Vector3 pushDirection = Vector3.forward;
    [SerializeField] private string targetTag = "WildFire";

    [Header("Roller Visual")]
    [SerializeField] private Transform roller;
    [SerializeField] private float rollerSpeed = 360f;
    [SerializeField] private Vector3 rollerRotationAxis = Vector3.right; // Local X
    [SerializeField] private bool invertRoller = true;

    [Header("Runtime State")]
    [SerializeField] private IntakeState currentState = IntakeState.Off;

    private readonly HashSet<Rigidbody> contactingBodies = new HashSet<Rigidbody>();

    private void OnEnable()
    {
        if (intakeAction != null)
        {
            intakeAction.action.Enable();
            intakeAction.action.performed += OnIntakePressed;
        }

        if (reverseAction != null)
        {
            reverseAction.action.Enable();
            reverseAction.action.performed += OnReversePressed;
        }
    }

    private void OnDisable()
    {
        if (intakeAction != null)
        {
            intakeAction.action.performed -= OnIntakePressed;
            intakeAction.action.Disable();
        }

        if (reverseAction != null)
        {
            reverseAction.action.performed -= OnReversePressed;
            reverseAction.action.Disable();
        }

        SetState(IntakeState.Off);
    }

    private void OnIntakePressed(InputAction.CallbackContext context)
    {
        // If already intaking, toggle off. Otherwise, switch to Intake.
        SetState(currentState == IntakeState.Intake ? IntakeState.Off : IntakeState.Intake);
    }

    private void OnReversePressed(InputAction.CallbackContext context)
    {
        // If already reversing, toggle off. Otherwise, switch to Reverse.
        SetState(currentState == IntakeState.Reverse ? IntakeState.Off : IntakeState.Reverse);
    }

    public void SetState(IntakeState newState)
    {
        currentState = newState;

        if (currentState == IntakeState.Off)
        {
            contactingBodies.Clear();
        }

        // Keep the base pusher in sync
        if (basePusher != null)
        {
            basePusher.SyncState(currentState);
        }
    }

    private void Update()
    {
        if (currentState == IntakeState.Off || roller == null) return;

        // Determine spin sign based on state
        float stateMultiplier = (currentState == IntakeState.Intake) ? 1f : -1f;
        float directionSign = invertRoller ? -1f : 1f;

        float finalSpeed = rollerSpeed * directionSign * stateMultiplier * Time.deltaTime;
        roller.Rotate(rollerRotationAxis * finalSpeed, Space.Self);
    }

    private void FixedUpdate()
    {
        if (currentState == IntakeState.Off || contactingBodies.Count == 0) return;

        float directionMultiplier = (currentState == IntakeState.Intake) ? 1f : -1f;
        Vector3 forceVector = transform.TransformDirection(pushDirection.normalized) * (pushForce * directionMultiplier);

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