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

    [Header("Gear Brace Raycast Bundle")]
    [Tooltip("LayerMask containing the brace/pipe objects.")]
    public LayerMask braceLayer = ~0;

    [Tooltip("Length of every gear ray.")]
    [SerializeField] private float gearRayDistance = 0.15f;

    [Tooltip("Diameter of the circular area covered by the gear ray bundle.")]
    [SerializeField] private float gearRayDiameter = 0.20f;

    [Tooltip("Number of rays around each ring.")]
    [SerializeField] private int gearRayCount = 12;

    [Tooltip("Number of circular rings of rays. 0 = center ray only.")]
    [SerializeField] private int gearRayRings = 2;

    [Tooltip("Fire an additional ray directly from the center of each gear.")]
    [SerializeField] private bool gearRayCenter = true;

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
    public bool gearsTouchingBrace = false;
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

        // Immediately stop all movement.
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // If stopped while touching the brace,
            // keep gravity disabled so the robot remains attached.
            if (isTouchingBrace)
            {
                rb.useGravity = false;
            }
            else
            {
                rb.useGravity = true;
            }
        }

        isClimbing = false;

        // Return drivetrain control.
        if (tankDrive != null)
        {
            tankDrive.isManagedByClimber = false;
        }
    }

    private void FixedUpdate()
    {
        // ---------------------------------------------------------
        // 1. Check ground contact
        // ---------------------------------------------------------

        isGrounded = CheckGroundContact();

        // ---------------------------------------------------------
        // 2. Check both gears against the brace
        // ---------------------------------------------------------

        gearsTouchingBrace = CheckGearsBraceContact();

        // ---------------------------------------------------------
        // 3. Read climbing input
        // ---------------------------------------------------------

        float rawClimb = ReadStick(climbStickAction);
        float absClimb = Mathf.Abs(rawClimb);
        float climbInputRate = 0f;

        if (absClimb > deadzone)
        {
            float norm = Mathf.InverseLerp(
                deadzone,
                1f,
                absClimb
            );

            // Left = up (+1)
            // Right = down (-1)
            float dir = rawClimb > 0f ? 1f : -1f;

            climbInputRate = dir * norm;

            RotateGears(climbInputRate);

            // Robot only counts as actively climbing when:
            // 1. The chassis is touching the brace.
            // 2. Both gears detect the brace.
            isClimbing =
                isTouchingBrace &&
                gearsTouchingBrace;
        }
        else
        {
            isClimbing = false;
        }

        // ---------------------------------------------------------
        // 4. Coordinate chassis movement along the pipe
        // ---------------------------------------------------------

        if (isTouchingBrace && gearsTouchingBrace)
        {
            if (tankDrive != null)
            {
                tankDrive.isManagedByClimber = true;
            }

            // Disable gravity while attached.
            rb.useGravity = false;

            // Movement along pipe.
            Vector3 climbVel =
                pipeDirection *
                (climbInputRate * climbSpeed);

            // Ground drive assistance.
            Vector3 groundVel = Vector3.zero;
            Vector3 angularVel = Vector3.zero;

            if (isGrounded && tankDrive != null)
            {
                groundVel = tankDrive.DesiredLinearVelocity;
                angularVel = tankDrive.DesiredAngularVelocity;
            }

            if (absClimb > deadzone)
            {
                // Climbing + ground movement.
                rb.velocity = climbVel + groundVel;
                rb.angularVelocity = angularVel;
            }
            else
            {
                // Ratchet hold.
                //
                // If wheels are touching the floor and the driver
                // wants to move, allow normal driving.
                if (isGrounded && groundVel.sqrMagnitude > 0.01f)
                {
                    rb.velocity = groundVel;
                    rb.angularVelocity = angularVel;
                }
                else
                {
                    // Suspended in air:
                    // completely hold the robot in place.
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        else
        {
            if (tankDrive != null)
            {
                tankDrive.isManagedByClimber = false;
            }

            rb.useGravity = true;
        }
    }

    // =============================================================
    // GEAR BRACE DETECTION
    // =============================================================

    private bool CheckGearsBraceContact()
    {
        if (climbingGear1 == null ||
            climbingGear2 == null)
        {
            return false;
        }

        bool gear1Hit =
            IsGearHittingBrace(climbingGear1);

        bool gear2Hit =
            IsGearHittingBrace(climbingGear2);

        // BOTH gears must detect the brace.
        return gear1Hit && gear2Hit;
    }

    private bool IsGearHittingBrace(Transform gear)
    {
        if (gear == null)
        {
            return false;
        }

        // IMPORTANT:
        // The gear itself rotates, so we do NOT use gear.up.
        //
        // All rays point downward relative to the robot.
        Vector3 rayDirection = -transform.up;

        // These two axes define the circular plane
        // where the rays will originate.
        //
        // The actual gear rotation does not affect the bundle.
        Vector3 planeRight = transform.right;
        Vector3 planeForward = transform.forward;

        float radius = Mathf.Max(
            0f,
            gearRayDiameter * 0.5f
        );

        // ---------------------------------------------------------
        // CENTER RAY
        // ---------------------------------------------------------

        if (gearRayCenter)
        {
            if (Physics.Raycast(
                gear.position,
                rayDirection,
                out RaycastHit centerHit,
                gearRayDistance,
                braceLayer,
                QueryTriggerInteraction.Ignore))
            {
                if (IsBraceCollider(centerHit.collider))
                {
                    return true;
                }
            }
        }

        // ---------------------------------------------------------
        // CIRCULAR RAY BUNDLE
        // ---------------------------------------------------------

        int rings = Mathf.Max(0, gearRayRings);
        int raysPerRing = Mathf.Max(1, gearRayCount);

        for (int ring = 1; ring <= rings; ring++)
        {
            // Spread the rings from the center to the outside.
            float ringRadius =
                radius *
                ((float)ring / rings);

            for (int i = 0; i < raysPerRing; i++)
            {
                float angle =
                    (360f / raysPerRing) *
                    i *
                    Mathf.Deg2Rad;

                Vector3 offset =
                    planeRight *
                    Mathf.Cos(angle) *
                    ringRadius
                    +
                    planeForward *
                    Mathf.Sin(angle) *
                    ringRadius;

                Vector3 rayOrigin =
                    gear.position + offset;

                if (Physics.Raycast(
                    rayOrigin,
                    rayDirection,
                    out RaycastHit hit,
                    gearRayDistance,
                    braceLayer,
                    QueryTriggerInteraction.Ignore))
                {
                    if (IsBraceCollider(hit.collider))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsBraceCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        return
            collider.CompareTag("Brace") ||
            collider.name.StartsWith("Brace");
    }

    // =============================================================
    // GROUND DETECTION
    // =============================================================

    private bool CheckGroundContact()
    {
        Vector3[] origins = GetRayOrigins();

        for (int i = 0; i < origins.Length; i++)
        {
            // Ignore triggers so things such as intake triggers
            // don't count as the floor.
            if (Physics.Raycast(
                origins[i],
                Vector3.down,
                rayDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        return false;
    }

    private Vector3[] GetRayOrigins()
    {
        // ---------------------------------------------------------
        // PATH A:
        // Use assigned wheel transforms.
        // ---------------------------------------------------------

        if (cornerWheelTransforms != null &&
            cornerWheelTransforms.Length > 0)
        {
            Vector3[] origins =
                new Vector3[cornerWheelTransforms.Length];

            for (int i = 0;
                i < cornerWheelTransforms.Length;
                i++)
            {
                origins[i] =
                    cornerWheelTransforms[i] != null
                        ? cornerWheelTransforms[i].position
                        : transform.position;
            }

            return origins;
        }

        // ---------------------------------------------------------
        // PATH B:
        // Calculate wheel positions manually.
        // ---------------------------------------------------------

        Vector3 wheelCenter =
            transform.TransformPoint(
                wheelbaseCenterOffset
            );

        Vector3 right =
            transform.right *
            treadHalfWidth;

        Vector3 forward =
            transform.forward *
            treadHalfLength;

        return new Vector3[]
        {
            // Front Right
            wheelCenter + forward + right,

            // Front Left
            wheelCenter + forward - right,

            // Rear Right
            wheelCenter - forward + right,

            // Rear Left
            wheelCenter - forward - right
        };
    }

    // =============================================================
    // GEAR ROTATION
    // =============================================================

    private void RotateGears(float speedMultiplier)
    {
        float delta =
            gearSpinSpeed *
            speedMultiplier *
            Time.fixedDeltaTime;

        if (climbingGear1 != null)
        {
            climbingGear1.Rotate(
                delta,
                0f,
                0f,
                Space.Self
            );
        }

        if (climbingGear2 != null)
        {
            climbingGear2.Rotate(
                delta,
                0f,
                0f,
                Space.Self
            );
        }
    }

    // =============================================================
    // BRACE TRIGGER
    // =============================================================

    private void OnTriggerEnter(Collider other)
    {
        if (IsBraceCollider(other))
        {
            isTouchingBrace = true;

            ClimbPipe pipe =
                other.GetComponentInParent<ClimbPipe>();

            if (pipe != null &&
                pipe.startPoint != null &&
                pipe.endPoint != null)
            {
                activePipe = pipe;
                pipeDirection = pipe.Direction;
            }
            else
            {
                Vector3 slope =
                    other.transform.forward;

                if (slope.y < 0)
                {
                    slope = -slope;
                }

                pipeDirection =
                    slope.normalized;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsBraceCollider(other))
        {
            isTouchingBrace = false;
            activePipe = null;

            if (tankDrive != null)
            {
                tankDrive.isManagedByClimber = false;
            }

            rb.useGravity = true;
        }
    }

    // =============================================================
    // INPUT
    // =============================================================

    private float ReadStick(InputActionReference actionRef)
    {
        if (actionRef != null &&
            actionRef.action != null)
        {
            var value =
                actionRef.action.ReadValueAsObject();

            if (value is Vector2 v2)
            {
                return v2.x;
            }

            if (value is float f)
            {
                return f;
            }
        }

        // Legacy fallback.
        return Input.GetAxisRaw("Horizontal");
    }

    // =============================================================
    // GIZMOS
    // =============================================================

    private void OnDrawGizmosSelected()
    {
        // ---------------------------------------------------------
        // 1. Ground raycasts
        // ---------------------------------------------------------

        Vector3[] origins = GetRayOrigins();

        Gizmos.color =
            isGrounded
                ? Color.green
                : Color.red;

        for (int i = 0;
            i < origins.Length;
            i++)
        {
            Gizmos.DrawRay(
                origins[i],
                Vector3.down * rayDistance
            );

            Gizmos.DrawWireSphere(
                origins[i] +
                Vector3.down * rayDistance,
                0.02f
            );
        }

        // Show manual wheelbase center.
        if (cornerWheelTransforms == null ||
            cornerWheelTransforms.Length == 0)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                transform.TransformPoint(
                    wheelbaseCenterOffset
                ),
                0.04f
            );
        }

        // ---------------------------------------------------------
        // 2. Gear ray bundles
        // ---------------------------------------------------------

        DrawGearGizmo(climbingGear1);
        DrawGearGizmo(climbingGear2);
    }

    private void DrawGearGizmo(Transform gear)
    {
        if (gear == null)
        {
            return;
        }

        Vector3 rayDirection =
            -transform.up;

        Vector3 planeRight =
            transform.right;

        Vector3 planeForward =
            transform.forward;

        float radius =
            Mathf.Max(
                0f,
                gearRayDiameter * 0.5f
            );

        int rings =
            Mathf.Max(0, gearRayRings);

        int raysPerRing =
            Mathf.Max(1, gearRayCount);

        // ---------------------------------------------------------
        // CENTER RAY
        // ---------------------------------------------------------

        if (gearRayCenter)
        {
            bool hit =
                Physics.Raycast(
                    gear.position,
                    rayDirection,
                    gearRayDistance,
                    braceLayer,
                    QueryTriggerInteraction.Ignore
                );

            Gizmos.color =
                hit
                    ? Color.green
                    : Color.cyan;

            Gizmos.DrawRay(
                gear.position,
                rayDirection *
                gearRayDistance
            );
        }

        // ---------------------------------------------------------
        // RAY RINGS
        // ---------------------------------------------------------

        for (int ring = 1;
            ring <= rings;
            ring++)
        {
            float ringRadius =
                radius *
                ((float)ring / rings);

            for (int i = 0;
                i < raysPerRing;
                i++)
            {
                float angle =
                    (360f / raysPerRing) *
                    i *
                    Mathf.Deg2Rad;

                Vector3 offset =
                    planeRight *
                    Mathf.Cos(angle) *
                    ringRadius
                    +
                    planeForward *
                    Mathf.Sin(angle) *
                    ringRadius;

                Vector3 rayOrigin =
                    gear.position +
                    offset;

                bool hit =
                    Physics.Raycast(
                        rayOrigin,
                        rayDirection,
                        gearRayDistance,
                        braceLayer,
                        QueryTriggerInteraction.Ignore
                    );

                Gizmos.color =
                    hit
                        ? Color.green
                        : Color.cyan;

                Gizmos.DrawRay(
                    rayOrigin,
                    rayDirection *
                    gearRayDistance
                );

                Gizmos.DrawWireSphere(
                    rayOrigin,
                    0.008f
                );
            }
        }

        // ---------------------------------------------------------
        // DRAW CIRCULAR DIAMETER
        // ---------------------------------------------------------

        Gizmos.color = Color.yellow;

        const int circleSegments = 32;

        Vector3 previousPoint =
            gear.position +
            planeRight * radius;

        for (int i = 1;
            i <= circleSegments;
            i++)
        {
            float angle =
                (360f / circleSegments) *
                i *
                Mathf.Deg2Rad;

            Vector3 point =
                gear.position
                +
                planeRight *
                Mathf.Cos(angle) *
                radius
                +
                planeForward *
                Mathf.Sin(angle) *
                radius;

            Gizmos.DrawLine(
                previousPoint,
                point
            );

            previousPoint = point;
        }
    }
}
