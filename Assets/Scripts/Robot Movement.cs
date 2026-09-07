using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankDriveController : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Vector2 or 1D Axis for Left Stick Y")]
    [SerializeField] private InputActionProperty leftStickAction;
    [Tooltip("Vector2 or 1D Axis for Right Stick Y")]
    [SerializeField] private InputActionProperty rightStickAction;

    [Header("Chassis Specs")]
    [SerializeField] private float maxLinearSpeed = 3.5f;       // Meters per second
    [SerializeField] private float maxAngularSpeed = 180f;      // Degrees per second
    [SerializeField] private float trackWidth = 0.42f;          // Lateral distance between wheel tracks (meters)
    [SerializeField] private float acceleration = 9.0f;         // Velocity ramp rate
    [SerializeField] private float motorSlewRate = 12.0f;       // Input smoothing to simulate motor inductance

    [Header("Wheel Visuals")]
    [SerializeField] private float wheelRadius = 0.05f;         // Meters (~50mm)
    [SerializeField] private Transform[] leftWheels;            // Drag all 3 left wheel transforms here
    [SerializeField] private Transform[] rightWheels;           // Drag all 3 right wheel transforms here
    [SerializeField] private Vector3 wheelRotationAxis = Vector3.right; // Local spindle rotation axis

    private Rigidbody rb;
    private float currentLeftInput;
    private float currentRightInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Lower center of mass below axle height to prevent rolling on sharp pivots
        rb.centerOfMass = new Vector3(-0.205f, 0.015f, 0f);
    }

    private void OnEnable()
    {
        leftStickAction.action?.Enable();
        rightStickAction.action?.Enable();
    }

    private void OnDisable()
    {
        leftStickAction.action?.Disable();
        rightStickAction.action?.Disable();
    }

    private void FixedUpdate()
    {
        ReadAndFilterInput();
        ApplyChassisPhysics();
        UpdateWheelVisuals();
    }

    private void ReadAndFilterInput()
    {
        float targetLeft = ReadStickValue(leftStickAction);
        float targetRight = ReadStickValue(rightStickAction);

        // Ramp inputs smoothly to replicate DC motor current limits
        currentLeftInput = Mathf.MoveTowards(currentLeftInput, targetLeft, motorSlewRate * Time.fixedDeltaTime);
        currentRightInput = Mathf.MoveTowards(currentRightInput, targetRight, motorSlewRate * Time.fixedDeltaTime);
    }

    private float ReadStickValue(InputActionProperty prop)
    {
        if (prop.action == null) return 0f;

        if (prop.action.expectedControlType == "Vector2")
        {
            return prop.action.ReadValue<Vector2>().y;
        }
        return prop.action.ReadValue<float>();
    }

    private void ApplyChassisPhysics()
    {
        // Tank / Differential drive kinematics
        float targetForwardSpeed = ((currentLeftInput + currentRightInput) * 0.5f) * maxLinearSpeed;
        float targetTurnRate = ((currentRightInput - currentLeftInput) / trackWidth) * (maxAngularSpeed * Mathf.Deg2Rad);

        // Linear velocity step (Unity 2022 and older compatible)
        Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
        localVel.z = Mathf.MoveTowards(localVel.z, targetForwardSpeed, acceleration * Time.fixedDeltaTime);
        localVel.x = 0f; // Eliminate lateral drift
        rb.velocity = transform.TransformDirection(localVel);

        // Angular yaw step
        Vector3 targetAngVel = new Vector3(0f, -targetTurnRate, 0f);
        rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, targetAngVel, acceleration * 2f * Time.fixedDeltaTime);
    }

    private void UpdateWheelVisuals()
    {
        // Calculate ground-contact velocity per side: V = V_center +/- (Omega * half_width)
        float vLinear = transform.InverseTransformDirection(rb.velocity).z;
        float vAngular = -rb.angularVelocity.y;

        float leftSurfaceVel = vLinear - (vAngular * (trackWidth * 0.5f));
        float rightSurfaceVel = vLinear + (vAngular * (trackWidth * 0.5f));

        // Convert linear displacement to angular rotation: degrees = (meters / circumference) * 360
        float leftAngleDelta = (leftSurfaceVel * Time.fixedDeltaTime / (2f * Mathf.PI * wheelRadius)) * 360f;
        float rightAngleDelta = (rightSurfaceVel * Time.fixedDeltaTime / (2f * Mathf.PI * wheelRadius)) * 360f;

        for (int i = 0; i < leftWheels.Length; i++)
        {
            if (leftWheels[i] != null)
                leftWheels[i].Rotate(wheelRotationAxis, leftAngleDelta, Space.Self);
        }

        for (int i = 0; i < rightWheels.Length; i++)
        {
            if (rightWheels[i] != null)
                rightWheels[i].Rotate(wheelRotationAxis, rightAngleDelta, Space.Self);
        }
    }
}