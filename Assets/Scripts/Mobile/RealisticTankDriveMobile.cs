using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RealisticTankDriveMobile : MonoBehaviour
{
    [Header("Mobile Joysticks")]
    [SerializeField] private VariableJoystick leftStickJoystick;
    [SerializeField] private VariableJoystick rightStickJoystick;
    [SerializeField] private bool invertLeftStick = false;
    [SerializeField] private bool invertRightStick = false;

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

    private void FixedUpdate()
    {
        float leftRaw = leftStickJoystick != null ? leftStickJoystick.Vertical * (invertLeftStick ? -1f : 1f) : 0f;
        float rightRaw = rightStickJoystick != null ? rightStickJoystick.Vertical * (invertRightStick ? -1f : 1f) : 0f;

        // Preserves exact original stick polarity inversion (-ReadStickInput)
        float leftInput = -leftRaw;
        float rightInput = -rightRaw;

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
}