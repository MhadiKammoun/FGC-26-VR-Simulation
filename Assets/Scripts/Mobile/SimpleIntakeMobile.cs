using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class SimpleIntakeMobile : MonoBehaviour
{
    public enum IntakeState
    {
        Off = 0,
        Intake = 1,
        Reverse = 2
    }

    [Header("Subordinate Base Pusher")]
    [Tooltip("Drag the GameObject with SimpleIntakePusher here to sync them.")]
    [SerializeField] private SimpleIntakePusher basePusher;

    [Header("Mobile UI Buttons")]
    [Tooltip("Tap once to run forward, tap again to turn off.")]
    [SerializeField] private Button intakeButton;
    [Tooltip("Tap once to reverse/outtake, tap again to turn off.")]
    [SerializeField] private Button reverseButton;

    [Header("Intake Physics")]
    [SerializeField] private float pushForce = 15f;
    [Tooltip("Direction relative to this transform where objects are pulled during normal intake")]
    [SerializeField] private Vector3 pushDirection = Vector3.forward;
    [SerializeField] private string targetTag = "WildFire";

    [Header("Roller Visual")]
    [SerializeField] private Transform roller;
    [SerializeField] private float rollerSpeed = 360f;
    [SerializeField] private Vector3 rollerRotationAxis = Vector3.right;
    [SerializeField] private bool invertRoller = true;

    [Header("Runtime State")]
    [SerializeField] private IntakeState currentState = IntakeState.Off;

    private readonly HashSet<Rigidbody> contactingBodies = new HashSet<Rigidbody>();

    private void OnEnable()
    {
        if (intakeButton != null)
            intakeButton.onClick.AddListener(OnIntakePressed);

        if (reverseButton != null)
            reverseButton.onClick.AddListener(OnReversePressed);
    }

    private void OnDisable()
    {
        if (intakeButton != null)
            intakeButton.onClick.RemoveListener(OnIntakePressed);

        if (reverseButton != null)
            reverseButton.onClick.RemoveListener(OnReversePressed);

        SetState(IntakeState.Off);
    }

    private void OnIntakePressed()
    {
        SetState(currentState == IntakeState.Intake ? IntakeState.Off : IntakeState.Intake);
    }

    private void OnReversePressed()
    {
        SetState(currentState == IntakeState.Reverse ? IntakeState.Off : IntakeState.Reverse);
    }

    public void SetState(IntakeState newState)
    {
        currentState = newState;

        if (currentState == IntakeState.Off)
        {
            contactingBodies.Clear();
        }

        if (basePusher != null)
        {
            basePusher.SyncState((SimpleIntake.IntakeState)currentState);
        }
    }

    private void Update()
    {
        if (currentState == IntakeState.Off || roller == null) return;

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