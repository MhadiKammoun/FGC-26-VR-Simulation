using UnityEngine;
using UnityEngine.InputSystem;

public class RobotClimberSimple : MonoBehaviour
{
    [Header("Climbing Gears")]
    [Tooltip("The two climbing gears that rotate")]
    public Transform climbingGear1;
    public Transform climbingGear2;
    public float baseGearRotationSpeed = 360f; // Base degrees per second
    public float inputMultiplier = 2.0f;       // Custom speed multiplier applied at max deflection

    [Header("Climb Settings")]
    public float climbSpeed = 0.8f;      // Velocity pulling the robot along the pipe

    [Header("Input System (Stick)")]
    [Tooltip("Assign an InputActionReference (Vector2 or Axis) for the stick")]
    public InputActionReference stickAction;

    [Header("Status (Read-Only)")]
    public bool isTouchingBrace = false;
    public bool isClimbing = false;
    [Range(-1f, 1f)] public float rawStickX = 0f;
    public float activeNormalizedSpeed = 0f;

    private Rigidbody rb;
    private Collider currentBraceCollider;
    private Vector3 climbDirection;

    private const float DeadzoneThreshold = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (stickAction != null)
        {
            stickAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (stickAction != null)
        {
            stickAction.action.Disable();
        }
    }

    private void Update()
    {
        rawStickX = ReadStickX();
        float absX = Mathf.Abs(rawStickX);

        // Movement activates only past 0.5, scaling from 0 to 1 as absX goes from 0.5 to 1.0
        if (absX > DeadzoneThreshold)
        {
            float normalizedT = Mathf.InverseLerp(DeadzoneThreshold, 1.0f, absX);

            // Stick Left (rawStickX < 0): original direction (+1)
            // Stick Right (rawStickX > 0): reversed direction (-1)
            float directionSign = rawStickX < 0f ? 1f : -1f;

            // Scaled speed factor incorporating your multiplier
            activeNormalizedSpeed = directionSign * normalizedT * inputMultiplier;

            RotateGears(activeNormalizedSpeed);

            // Engage climbing when contacting the Brace
            if (isTouchingBrace)
            {
                if (!isClimbing)
                {
                    StartClimbing();
                }
            }
            else if (isClimbing)
            {
                StopClimbing(keepHoldingInAir: false);
            }
        }
        else
        {
            activeNormalizedSpeed = 0f;

            if (isClimbing)
            {
                // Stick released below deadzone: hold elevation against gravity
                StopClimbing(keepHoldingInAir: true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (isClimbing && currentBraceCollider != null && Mathf.Abs(activeNormalizedSpeed) > 0.001f)
        {
            // Moving along the pipe based on stick direction & intensity
            Vector3 targetDelta = climbDirection * (climbSpeed * activeNormalizedSpeed * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + targetDelta);
        }
    }

    private float ReadStickX()
    {
        if (stickAction != null)
        {
            var value = stickAction.action.ReadValueAsObject();
            if (value is Vector2 vec) return vec.x;
            if (value is float f) return f;
        }

        // Fallback to legacy Horizontal axis (Left/Right, A/D, Left Stick X)
        return Input.GetAxisRaw("Horizontal");
    }

    private void RotateGears(float speedFactor)
    {
        // Positive speedFactor rotates with original positive X delta
        float xDelta = baseGearRotationSpeed * speedFactor * Time.deltaTime;

        if (climbingGear1 != null)
        {
            climbingGear1.Rotate(xDelta, 0f, 0f, Space.Self);
        }

        if (climbingGear2 != null)
        {
            climbingGear2.Rotate(xDelta, 0f, 0f, Space.Self);
        }
    }

    private void StartClimbing()
    {
        isClimbing = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;

        climbDirection = currentBraceCollider.transform.forward;

        if (climbDirection.y < 0f)
        {
            climbDirection = -climbDirection;
        }

        if (climbDirection == Vector3.zero)
        {
            climbDirection = transform.forward;
        }
    }

    private void StopClimbing(bool keepHoldingInAir)
    {
        isClimbing = false;
        rb.velocity = Vector3.zero;
        rb.useGravity = !keepHoldingInAir;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.StartsWith("Brace"))
        {
            isTouchingBrace = true;
            currentBraceCollider = other;

            if (Mathf.Abs(ReadStickX()) > DeadzoneThreshold && !isClimbing)
            {
                StartClimbing();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag.StartsWith("Brace") && other == currentBraceCollider)
        {
            isTouchingBrace = false;
            currentBraceCollider = null;

            if (isClimbing)
            {
                StopClimbing(keepHoldingInAir: false);
            }
        }
    }
}