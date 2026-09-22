using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RealisticTankDrive : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftStickAction;
    [SerializeField] private InputActionReference rightStickAction;

    [Header("Chassis Physics Settings")]
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float linearAcceleration = 16.0f;
    [SerializeField] private float maxTurnRate = 180.0f;
    [SerializeField] private float turnAcceleration = 280.0f;

    [Header("Wheel Visuals")]
    [SerializeField] private Transform[] leftWheels;
    [SerializeField] private Transform[] rightWheels;
    [SerializeField] private float wheelSpinSpeed = 1400f;
    [SerializeField] private float wheelAcceleration = 1400.0f;
    [SerializeField] private bool invertLeft = true;
    [SerializeField] private bool invertRight = false;

    // Public properties consumed by the Climber Controller
    [HideInInspector] public Vector3 DesiredLinearVelocity;
    [HideInInspector] public Vector3 DesiredAngularVelocity;
    [HideInInspector] public bool isManagedByClimber = false;

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
        float leftInput = -ReadStickInput(leftStickAction);
        float rightInput = -ReadStickInput(rightStickAction);

        // Visual wheels ALWAYS spin based on stick inputs
        UpdateWheels(leftInput, rightInput);

        // Calculate target speeds
        float targetForward = ((leftInput + rightInput) / 2.0f) * maxSpeed;
        float targetTurn = ((rightInput - leftInput) / 2.0f) * maxTurnRate;

        // Anti-sticking compensation
        float actualForwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        if (Mathf.Abs(actualForwardSpeed) < Mathf.Abs(currentForwardSpeed) * 0.5f && Mathf.Abs(currentForwardSpeed) > 0.5f)
        {
            currentForwardSpeed = actualForwardSpeed;
        }

        // Ramp internal speeds
        currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetForward, linearAcceleration * Time.fixedDeltaTime);
        currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, targetTurn, turnAcceleration * Time.fixedDeltaTime);

        // Store desired velocities so climber can inspect or blend them
        DesiredLinearVelocity = transform.forward * currentForwardSpeed;
        Vector3 targetYawVelocity = transform.up * (currentTurnSpeed * Mathf.Deg2Rad);
        Vector3 tiltAngularVelocity = Vector3.ProjectOnPlane(rb.angularVelocity, transform.up);
        DesiredAngularVelocity = targetYawVelocity + tiltAngularVelocity;

        // If the climber is controlling the chassis, hand off velocity application to it
        if (isManagedByClimber) return;

        // Normal ground driving: keep existing vertical physics velocity intact
        Vector3 finalVel = DesiredLinearVelocity;
        finalVel.y = rb.velocity.y;
        rb.velocity = finalVel;
        rb.angularVelocity = DesiredAngularVelocity;
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
            if (wheels[i] != null) wheels[i].Rotate(0f, 0f, step, Space.Self);
        }
    }

    private float ReadStickInput(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return 0f;
        if (actionRef.action.activeControl?.valueType == typeof(Vector2))
            return actionRef.action.ReadValue<Vector2>().y;
        return actionRef.action.ReadValue<float>();
    }
}