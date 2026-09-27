using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class VRWildfireController : MonoBehaviour
{
    [Header("Pickup Zone Filter")]
    [Tooltip("Drag the trigger collider here. Only balls inside this volume can be selected.")]
    public Collider validPickupZone;

    [Header("Raycast Laser Settings")]
    [Tooltip("Maximum length of the targeting laser.")]
    public float maxRayDistance = 15f;

    [Tooltip("Radius of the spherecast beam for easier targeting in VR.")]
    public float beamRadius = 0.08f;

    [Tooltip("Layers to check (include environment and WildFire balls).")]
    public LayerMask hitLayers = ~0;

    [Header("Laser Colors")]
    public Color normalLaserColor = new Color(0f, 0.8f, 1f, 0.4f);
    public Color hoverLaserColor = new Color(1f, 0.5f, 0f, 0.8f);

    [Header("Target")]
    [Tooltip("Target location (e.g. extinguisher hole) where the ball flies when shot.")]
    public Transform shootTarget;

    [Header("Ballistic Arc Settings")]
    [Tooltip("Time in seconds for the ball to reach the target.")]
    public float throwDuration = 0.65f;

    [Tooltip("Peak arc height during flight.")]
    public float arcHeight = 3.5f;

    [Tooltip("Forward velocity injected into the ball on arrival so it carries into the hole.")]
    public float impactForwardSpeed = 4f;

    [Tooltip("Downward velocity injected on arrival to mimic falling out of the arc.")]
    public float impactDownwardSpeed = 1.5f;

    [Header("VR Input Actions")]
    [Tooltip("Action used to pick up the ball (typically Grip).")]
    public InputActionReference pickUpAction;

    [Tooltip("Action used to launch the ball (typically Trigger).")]
    public InputActionReference shootAction;

    public event Action<GameObject> OnTargetDetected;
    public event Action OnTargetLost;

    public GameObject currentTarget { get; private set; }
    public GameObject heldBall { get; private set; }

    private LineRenderer lineRenderer;
    private MaterialPropertyBlock propBlock;
    private static readonly int OutlinePropId = Shader.PropertyToID("_Outline");

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (pickUpAction?.action != null) pickUpAction.action.Enable();
        if (shootAction?.action != null) shootAction.action.Enable();
    }

    private void OnDisable()
    {
        if (pickUpAction?.action != null) pickUpAction.action.Disable();
        if (shootAction?.action != null) shootAction.action.Disable();
    }

    private void Update()
    {
        if (heldBall == null)
        {
            lineRenderer.enabled = true;
            UpdateRaycastBeam();
            HandlePickupInput();
        }
        else
        {
            lineRenderer.enabled = false;
            ClearTarget();
            HandleShootInput();
        }
    }

    // --- TARGETING & RAYCAST ---

    private void UpdateRaycastBeam()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        Vector3 beamEnd = origin + (direction * maxRayDistance);
        GameObject detectedTarget = null;

        if (Physics.SphereCast(origin, beamRadius, direction, out RaycastHit hit, maxRayDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            beamEnd = hit.point;

            if (hit.collider.CompareTag("WildFire"))
            {
                if (IsInsidePickupZone(hit.collider))
                {
                    detectedTarget = hit.collider.gameObject;
                }
            }
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, beamEnd);

        EvaluateTargetChange(detectedTarget);
        UpdateLaserColor(detectedTarget != null);
    }

    private bool IsInsidePickupZone(Collider ballCollider)
    {
        if (validPickupZone == null) return true;

        Vector3 ballCenter = ballCollider.bounds.center;
        Vector3 closestPoint = validPickupZone.ClosestPoint(ballCenter);

        return (closestPoint - ballCenter).sqrMagnitude < 0.0001f;
    }

    private void EvaluateTargetChange(GameObject newTarget)
    {
        if (newTarget == currentTarget) return;

        if (currentTarget != null)
        {
            SetOutline(currentTarget, 0f);
            OnTargetLost?.Invoke();
        }

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            SetOutline(currentTarget, 1f);
            OnTargetDetected?.Invoke(currentTarget);
        }
    }

    private void UpdateLaserColor(bool isHovering)
    {
        Color activeColor = isHovering ? hoverLaserColor : normalLaserColor;
        lineRenderer.startColor = activeColor;
        lineRenderer.endColor = activeColor;
    }

    private void ClearTarget()
    {
        if (currentTarget != null)
        {
            SetOutline(currentTarget, 0f);
            OnTargetLost?.Invoke();
            currentTarget = null;
        }
    }

    private void SetOutline(GameObject obj, float state)
    {
        if (obj != null && obj.TryGetComponent<Renderer>(out var rend))
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetFloat(OutlinePropId, state);
            rend.SetPropertyBlock(propBlock);
        }
    }

    // --- PICKUP LOGIC ---

    private void HandlePickupInput()
    {
        if (pickUpAction == null || !pickUpAction.action.WasPressedThisFrame()) return;
        if (currentTarget == null) return;

        GrabBall(currentTarget);
    }

    public void GrabBall(GameObject ball)
    {
        heldBall = ball;
        ClearTarget();

        // Disable physics while carried
        if (ball.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable collider so it doesn't bump the controller
        if (ball.TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }

        // Parent to controller and snap to local 0,0,0
        ball.transform.SetParent(transform);
        ball.transform.localPosition = Vector3.zero;
        ball.transform.localRotation = Quaternion.identity;

        // Sync with BallPickUp script if present
        if (ball.TryGetComponent<BallPickUp>(out var ballScript))
        {
            ballScript.isHolding = true;
        }
    }

    // --- SHOOTING LOGIC ---

    private void HandleShootInput()
    {
        if (shootAction == null || !shootAction.action.WasPressedThisFrame()) return;
        if (heldBall == null || shootTarget == null) return;

        LaunchHeldBall();
    }

    private void LaunchHeldBall()
    {
        GameObject ballToLaunch = heldBall;
        heldBall = null;

        // Unparent from controller
        ballToLaunch.transform.SetParent(null);

        if (ballToLaunch.TryGetComponent<BallPickUp>(out var ballScript))
        {
            ballScript.isHolding = false;
        }

        if (ballToLaunch.TryGetComponent<Collider>(out var col))
        {
            col.enabled = true;
        }

        StartCoroutine(AnimateBallArc(ballToLaunch, ballToLaunch.transform.position, shootTarget.position));
    }

    private IEnumerator AnimateBallArc(GameObject ball, Vector3 startPos, Vector3 endPos)
    {
        float timer = 0f;

        while (timer < throwDuration)
        {
            if (ball == null) yield break;

            timer += Time.deltaTime;
            float t01 = Mathf.Clamp01(timer / throwDuration);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t01);
            Vector3 arc = Vector3.up * (arcHeight * Mathf.Sin(t01 * Mathf.PI));

            ball.transform.position = pos + arc;
            yield return null;
        }

        // Restore physics and inject arrival velocity
        if (ball != null && ball.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // Direction vector along the flat trajectory toward the target
            Vector3 horizontalDir = (endPos - startPos);
            horizontalDir.y = 0f;

            if (horizontalDir.sqrMagnitude > 0.001f)
            {
                horizontalDir.Normalize();
            }
            else
            {
                horizontalDir = transform.forward;
            }

            // Carry forward momentum into the hole + slight downward push
            Vector3 exitVelocity = (horizontalDir * impactForwardSpeed) + (Vector3.down * impactDownwardSpeed);
            rb.velocity = exitVelocity;
        }
    }
}