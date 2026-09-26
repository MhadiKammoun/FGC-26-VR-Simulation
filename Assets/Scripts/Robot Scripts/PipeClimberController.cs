using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PipeClimberController : MonoBehaviour
{
    [Header("Climber Transforms")]
    public Transform climbingGear1;
    public Transform climbingGear2;

    [Header("Climber Dynamics")]
    public float gearSpinSpeed = 720f;
    public float climbSpeed = 2.0f;
    public float deadzone = 0.5f;

    [Header("Ground Detection Setup")]
    [Tooltip("Layer assigned to the floor / field carpet.")]
    public LayerMask groundLayer;

    [Tooltip("Total length of the downward rays from the wheels or contact points.")]
    [SerializeField] private float rayDistance = 0.25f;

    [Tooltip("Assign the 4 corner wheel transforms (FL, FR, RL, RR). If assigned, rays cast directly from the actual wheel positions.")]
    [SerializeField] private Transform[] cornerWheelTransforms;

    [Header("Manual Wheelbase Offsets (Used if Corner Transforms are empty)")]
    [Tooltip("Local offset from robot root pivot to the true geometric center of your wheels.")]
    [SerializeField] private Vector3 wheelbaseCenterOffset = Vector3.zero;

    [Tooltip("Half-width distance from wheelbase center to left/right wheel tracks.")]
    [SerializeField] private float treadHalfWidth = 0.4f;

    [Tooltip("Half-length distance from wheelbase center to front/rear wheel axles.")]
    [SerializeField] private float treadHalfLength = 0.5f;

    [Header("Input")]
    public InputActionReference climbStickAction;

    [Header("Status (Read-Only)")]
    public bool isTouchingBrace = false;
    public bool isClimbing = false;
    public bool isGrounded = true;

    private Rigidbody rb;
    private RealisticTankDrive tankDrive;
    private Vector3 pipeDirection;
    private ClimbPipe activePipe;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tankDrive = GetComponent<RealisticTankDrive>();
    }

    private void OnEnable()
    {
        climbStickAction?.action?.Enable();
    }

    private void OnDisable()
    {
        climbStickAction?.action?.Disable();

        // 1. Immediately cut all linear and angular momentum
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // If stopped mid-air on the brace, stay locked in place for settling check
            if (isTouchingBrace)
            {
                rb.useGravity = false;
            }
            else
            {
                rb.useGravity = true;
            }
        }

        // 2. Clear climbing active flags
        isClimbing = false;

        // 3. Return drivetrain drive control
        if (tankDrive != null)
        {
            tankDrive.isManagedByClimber = false;
        }
    }

    private void FixedUpdate()
    {
        // 1. Check ground contact at the true wheel positions
        isGrounded = CheckGroundContact();

        // 2. Read climb input
        float rawClimb = ReadStick(climbStickAction);
        float absClimb = Mathf.Abs(rawClimb);
        float climbInputRate = 0f;

        if (absClimb > deadzone)
        {
            float norm = Mathf.InverseLerp(deadzone, 1f, absClimb);
            float dir = (rawClimb > 0f) ? 1f : -1f; // Left = up (+1), Right = down (-1)
            climbInputRate = dir * norm;

            RotateGears(climbInputRate);
            isClimbing = isTouchingBrace;
        }
        else
        {
            isClimbing = false;
        }

        // 3. Coordinate chassis motion along the pipe
        if (isTouchingBrace)
        {
            if (tankDrive != null) tankDrive.isManagedByClimber = true;
            rb.useGravity = false;

            // Incline climb velocity along the pipe slope
            Vector3 climbVel = pipeDirection * (climbInputRate * climbSpeed);

            // Ground drive forces: only apply if wheels actually contact the carpet
            Vector3 groundVel = Vector3.zero;
            Vector3 angularVel = Vector3.zero;

            if (isGrounded && tankDrive != null)
            {
                groundVel = tankDrive.DesiredLinearVelocity;
                angularVel = tankDrive.DesiredAngularVelocity;
            }

            if (absClimb > deadzone)
            {
                // Incline travel + floor assist
                rb.velocity = climbVel + groundVel;
                rb.angularVelocity = angularVel;
            }
            else
            {
                // Ratchet hold:
                // If touching the floor, allow driving/steering away cleanly
                if (isGrounded && groundVel.sqrMagnitude > 0.01f)
                {
                    rb.velocity = groundVel;
                    rb.angularVelocity = angularVel;
                }
                else
                {
                    // Suspended in air: zero velocity holds robot rigid against gravity
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        else
        {
            if (tankDrive != null) tankDrive.isManagedByClimber = false;
            rb.useGravity = true;
        }
    }

    private bool CheckGroundContact()
    {
        Vector3[] origins = GetRayOrigins();

        for (int i = 0; i < origins.Length; i++)
        {
            // QueryTriggerInteraction.Ignore ensures intake triggers are never registered as floor
            if (Physics.Raycast(origins[i], Vector3.down, rayDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        return false;
    }

    private Vector3[] GetRayOrigins()
    {
        // Path A: Shoot directly from the 4 assigned corner wheel transforms
        if (cornerWheelTransforms != null && cornerWheelTransforms.Length > 0)
        {
            Vector3[] origins = new Vector3[cornerWheelTransforms.Length];
            for (int i = 0; i < cornerWheelTransforms.Length; i++)
            {
                origins[i] = cornerWheelTransforms[i] != null
                    ? cornerWheelTransforms[i].position
                    : transform.position;
            }
            return origins;
        }

        // Path B: Compute from true wheelbase center offset
        Vector3 wheelCenter = transform.TransformPoint(wheelbaseCenterOffset);
        Vector3 right = transform.right * treadHalfWidth;
        Vector3 forward = transform.forward * treadHalfLength;

        return new Vector3[]
        {
            wheelCenter + forward + right, // Front Right
            wheelCenter + forward - right, // Front Left
            wheelCenter - forward + right, // Rear Right
            wheelCenter - forward - right  // Rear Left
        };
    }

    private void RotateGears(float speedMultiplier)
    {
        float delta = gearSpinSpeed * speedMultiplier * Time.fixedDeltaTime;
        if (climbingGear1) climbingGear1.Rotate(delta, 0f, 0f, Space.Self);
        if (climbingGear2) climbingGear2.Rotate(delta, 0f, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Brace") || other.name.StartsWith("Brace"))
        {
            isTouchingBrace = true;

            ClimbPipe pipe = other.GetComponentInParent<ClimbPipe>();
            if (pipe != null && pipe.startPoint != null && pipe.endPoint != null)
            {
                activePipe = pipe;
                pipeDirection = pipe.Direction;
            }
            else
            {
                Vector3 slope = other.transform.forward;
                if (slope.y < 0) slope = -slope;
                pipeDirection = slope.normalized;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Brace") || other.name.StartsWith("Brace"))
        {
            isTouchingBrace = false;
            activePipe = null;
            if (tankDrive != null) tankDrive.isManagedByClimber = false;
            rb.useGravity = true;
        }
    }

    private float ReadStick(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
        {
            var val = actionRef.action.ReadValueAsObject();
            if (val is Vector2 v2) return v2.x;
            if (val is float f) return f;
        }
        return Input.GetAxisRaw("Horizontal");
    }

    private void OnDrawGizmosSelected()
    {
        Vector3[] origins = GetRayOrigins();
        Gizmos.color = isGrounded ? Color.green : Color.red;

        for (int i = 0; i < origins.Length; i++)
        {
            Gizmos.DrawRay(origins[i], Vector3.down * rayDistance);
            Gizmos.DrawWireSphere(origins[i] + (Vector3.down * rayDistance), 0.02f);
        }

        // Visualize manual wheelbase center if transforms are not assigned
        if (cornerWheelTransforms == null || cornerWheelTransforms.Length == 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(wheelbaseCenterOffset), 0.04f);
        }
    }
}