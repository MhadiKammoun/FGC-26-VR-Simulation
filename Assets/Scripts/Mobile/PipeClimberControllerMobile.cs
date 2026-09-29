using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PipeClimberControllerMobile : MonoBehaviour
{
    [Header("Climber Transforms")]
    public Transform climbingGear1;
    public Transform climbingGear2;

    [Header("Climber Dynamics")]
    public float gearSpinSpeed = 720f;
    public float climbSpeed = 2.0f;
    public float deadzone = 0.5f;

    [Header("Gear Brace Raycast Setup")]
    [Tooltip("LayerMask containing the brace/pipe objects. Default includes all layers.")]
    public LayerMask braceLayer = ~0;

    [Tooltip("Length of the ray cast downward locally from each climbing gear.")]
    [SerializeField] private float gearRayDistance = 0.15f;

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

    [Header("Mobile Joystick Input")]
    [SerializeField] private VariableJoystick climbJoystick;
    [SerializeField] private bool invertClimb = false;

    [Header("Status (Read-Only)")]
    public bool isTouchingBrace = false;
    public bool gearsTouchingBrace = false;
    public bool isClimbing = false;
    public bool isGrounded = true;

    private Rigidbody rb;
    private RealisticTankDriveMobile tankDriveMobile;
    private RealisticTankDrive tankDriveOriginal;
    private Vector3 pipeDirection;
    private ClimbPipe activePipe;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tankDriveMobile = GetComponent<RealisticTankDriveMobile>();
        tankDriveOriginal = GetComponent<RealisticTankDrive>();
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

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

        SetTankDriveManagedByClimber(false);
    }

    private void FixedUpdate()
    {
        // 1. Check ground contact at the true wheel positions
        isGrounded = CheckGroundContact();

        // 2. Verify gear contact with the brace
        gearsTouchingBrace = CheckGearsBraceContact();

        // 3. Read climb input from VariableJoystick (Horizontal axis matches original X stick input)
        float rawClimb = 0f;
        if (climbJoystick != null)
        {
            rawClimb = climbJoystick.Horizontal * (invertClimb ? -1f : 1f);
        }

        float absClimb = Mathf.Abs(rawClimb);
        float climbInputRate = 0f;

        if (absClimb > deadzone)
        {
            float norm = Mathf.InverseLerp(deadzone, 1f, absClimb);
            float dir = (rawClimb > 0f) ? 1f : -1f;
            climbInputRate = dir * norm;

            RotateGears(climbInputRate);
            isClimbing = isTouchingBrace && gearsTouchingBrace;
        }
        else
        {
            isClimbing = false;
        }

        // 4. Coordinate chassis motion along the pipe
        if (isTouchingBrace && gearsTouchingBrace)
        {
            SetTankDriveManagedByClimber(true);
            rb.useGravity = false;

            Vector3 climbVel = pipeDirection * (climbInputRate * climbSpeed);

            Vector3 groundVel = Vector3.zero;
            Vector3 angularVel = Vector3.zero;

            if (isGrounded)
            {
                if (tankDriveMobile != null)
                {
                    groundVel = tankDriveMobile.DesiredLinearVelocity;
                    angularVel = tankDriveMobile.DesiredAngularVelocity;
                }
                else if (tankDriveOriginal != null)
                {
                    groundVel = tankDriveOriginal.DesiredLinearVelocity;
                    angularVel = tankDriveOriginal.DesiredAngularVelocity;
                }
            }

            if (absClimb > deadzone)
            {
                rb.velocity = climbVel + groundVel;
                rb.angularVelocity = angularVel;
            }
            else
            {
                if (isGrounded && groundVel.sqrMagnitude > 0.01f)
                {
                    rb.velocity = groundVel;
                    rb.angularVelocity = angularVel;
                }
                else
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        else
        {
            SetTankDriveManagedByClimber(false);
            rb.useGravity = true;
        }
    }

    private void SetTankDriveManagedByClimber(bool state)
    {
        if (tankDriveMobile != null) tankDriveMobile.isManagedByClimber = state;
        if (tankDriveOriginal != null) tankDriveOriginal.isManagedByClimber = state;
    }

    private bool CheckGearsBraceContact()
    {
        if (climbingGear1 == null || climbingGear2 == null) return false;

        bool gear1Hit = IsGearHittingBrace(climbingGear1);
        bool gear2Hit = IsGearHittingBrace(climbingGear2);

        return gear1Hit && gear2Hit;
    }

    private bool IsGearHittingBrace(Transform gear)
    {
        Vector3 rayDir = -transform.up;
        if (Physics.Raycast(gear.position, rayDir, out RaycastHit hit, gearRayDistance, braceLayer, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag("Brace") || hit.collider.name.StartsWith("Brace");
        }
        return false;
    }

    private bool CheckGroundContact()
    {
        Vector3[] origins = GetRayOrigins();

        for (int i = 0; i < origins.Length; i++)
        {
            if (Physics.Raycast(origins[i], Vector3.down, rayDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        return false;
    }

    private Vector3[] GetRayOrigins()
    {
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

        Vector3 wheelCenter = transform.TransformPoint(wheelbaseCenterOffset);
        Vector3 right = transform.right * treadHalfWidth;
        Vector3 forward = transform.forward * treadHalfLength;

        return new Vector3[]
        {
            wheelCenter + forward + right,
            wheelCenter + forward - right,
            wheelCenter - forward + right,
            wheelCenter - forward - right
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
            SetTankDriveManagedByClimber(false);
            rb.useGravity = true;
        }
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

        if (cornerWheelTransforms == null || cornerWheelTransforms.Length == 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(wheelbaseCenterOffset), 0.04f);
        }

        DrawGearGizmo(climbingGear1);
        DrawGearGizmo(climbingGear2);
    }

    private void DrawGearGizmo(Transform gear)
    {
        if (gear == null) return;

        Vector3 rayDir = -transform.up;
        bool hitsBrace = IsGearHittingBrace(gear);

        Gizmos.color = hitsBrace ? Color.green : Color.cyan;
        Gizmos.DrawRay(gear.position, rayDir * gearRayDistance);
        Gizmos.DrawWireSphere(gear.position + (rayDir * gearRayDistance), 0.015f);
    }
}