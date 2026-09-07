using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RealisticTankDrive : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Input action for the left stick (Vector2 or Axis)")]
    [SerializeField] private InputActionReference leftStickAction;

    [Tooltip("Input action for the right stick (Vector2 or Axis)")]
    [SerializeField] private InputActionReference rightStickAction;

    [Header("Chassis Physics Settings")]
    [Tooltip("Maximum linear driving speed (m/s)")]
    [SerializeField] private float maxSpeed = 7f;

    [Tooltip("Linear acceleration rate")]
    [SerializeField] private float linearAcceleration = 16.0f;

    [Tooltip("Maximum turning rate in degrees/sec")]
    [SerializeField] private float maxTurnRate = 180.0f;

    [Tooltip("Angular turning acceleration rate")]
    [SerializeField] private float turnAcceleration = 280.0f;

    [Header("Wheel Visuals")]
    [Tooltip("Assign the left side wheel transforms")]
    [SerializeField] private Transform[] leftWheels;

    [Tooltip("Assign the right side wheel transforms")]
    [SerializeField] private Transform[] rightWheels;

    [Tooltip("Visual wheel spin speed multiplier in degrees per second")]
    [SerializeField] private float wheelSpinSpeed = 1400;

    [Tooltip("Smoothing/ramp rate for the wheel visual rotation")]
    [SerializeField] private float wheelAcceleration = 1400.0f;

    [Tooltip("Flip left wheel spin direction if backward")]
    [SerializeField] private bool invertLeft = true;

    [Tooltip("Flip right wheel spin direction")]
    [SerializeField] private bool invertRight = false;

    private Rigidbody rb;
    private float currentForwardSpeed;
    private float currentTurnSpeed;
    private float currentLeftWheelSpeed;
    private float currentRightWheelSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        leftStickAction?.action.Enable();
        rightStickAction?.action.Enable();
    }

    private void OnDisable()
    {
        leftStickAction?.action.Disable();
        rightStickAction?.action.Disable();
    }

    private void FixedUpdate()
    {
        // Inverted inputs to fix forward/backward direction
        float leftInput = -ReadStickInput(leftStickAction);
        float rightInput = -ReadStickInput(rightStickAction);

        // Fixed steering polarity: Left stick forward + Right stick back now turns chassis RIGHT
        float targetForward = ((leftInput + rightInput) / 2.0f) * maxSpeed;
        float targetTurn = ((rightInput - leftInput) / 2.0f) * maxTurnRate;

        // Smooth acceleration ramps
        currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetForward, linearAcceleration * Time.fixedDeltaTime);
        currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, targetTurn, turnAcceleration * Time.fixedDeltaTime);

        // 1. Move chassis linear (maintaining Y velocity for gravity)
        Vector3 targetVelocity = transform.forward * currentForwardSpeed;
        targetVelocity.y = rb.velocity.y;
        rb.velocity = targetVelocity;

        // 2. Turn chassis along local Y
        rb.angularVelocity = new Vector3(0f, currentTurnSpeed * Mathf.Deg2Rad, 0f);

        // 3. Spin wheels visually
        UpdateWheels(leftInput, rightInput);
    }

    private void UpdateWheels(float leftInput, float rightInput)
    {
        float targetLeft = leftInput * wheelSpinSpeed * (invertLeft ? -1f : 1f);
        float targetRight = rightInput * wheelSpinSpeed * (invertRight ? -1f : 1f);

        currentLeftWheelSpeed = Mathf.MoveTowards(currentLeftWheelSpeed, targetLeft, wheelAcceleration * Time.fixedDeltaTime);
        currentRightWheelSpeed = Mathf.MoveTowards(currentRightWheelSpeed, targetRight, wheelAcceleration * Time.fixedDeltaTime);

        RotateSide(leftWheels, currentLeftWheelSpeed);
        RotateSide(rightWheels, currentRightWheelSpeed);
    }

    private void RotateSide(Transform[] wheels, float speed)
    {
        if (wheels == null || wheels.Length == 0) return;

        float step = speed * Time.fixedDeltaTime;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
            {
                wheels[i].Rotate(0f, 0f, step, Space.Self);
            }
        }
    }

    private float ReadStickInput(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return 0f;

        if (actionRef.action.activeControl?.valueType == typeof(Vector2))
        {
            return actionRef.action.ReadValue<Vector2>().y;
        }

        return actionRef.action.ReadValue<float>();
    }
}
