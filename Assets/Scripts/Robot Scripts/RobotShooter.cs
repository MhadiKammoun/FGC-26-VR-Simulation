using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RobotShooter : MonoBehaviour
{
    public enum ActivationMode { Toggle, Hold }

    [Header("Input System")]
    [Tooltip("Input action to activate/deactivate the shooter.")]
    public InputActionProperty shooterActiveAction;
    public ActivationMode activationMode = ActivationMode.Toggle;

    [Header("Flywheel Visuals")]
    public Transform flywheelLeft;
    public Transform flywheelRight;
    public float flywheelSpinSpeed = 2160f;
    public bool invertLeftWheel = false;
    public bool invertRightWheel = true;

    [Header("Physical Launch Settings")]
    [Tooltip("Forward launch velocity or speed multiplier.")]
    public float forwardForce = 16f;
    [Tooltip("Upward launch velocity.")]
    public float upwardForce = 8f;

    [Header("Collision Management")]
    [Tooltip("When ball.position.y reaches this threshold, collision with the robot chassis is re-enabled.")]
    public float reactivationYThreshold = 14f;

    [Header("References")]
    public Transform firePoint;
    public Collider storageTrigger;

    [Header("Read-Only Status")]
    [SerializeField] private bool isShooterPowered = false;

    private readonly Queue<GameObject> ballQueue = new Queue<GameObject>();
    private readonly List<BallCollisionTracker> activeBalls = new List<BallCollisionTracker>();
    private Collider[] robotColliders;

    private class BallCollisionTracker
    {
        public GameObject ball;
        public Collider ballCol;
        public Rigidbody rb;
    }

    void Awake()
    {
        // Cache all colliders belonging to this robot chassis
        robotColliders = GetComponentsInChildren<Collider>();

        if (storageTrigger != null)
        {
            StorageTriggerRelay relay = storageTrigger.GetComponent<StorageTriggerRelay>();
            if (relay == null)
                relay = storageTrigger.gameObject.AddComponent<StorageTriggerRelay>();
            relay.owner = this;
        }
    }

    void OnEnable() => shooterActiveAction.action?.Enable();
    void OnDisable()
    {
        shooterActiveAction.action?.Disable();
        isShooterPowered = false;
    }

    void Update()
    {
        HandleInput();

        if (isShooterPowered)
        {
            SpinFlywheels();

            if (ballQueue.Count > 0)
            {
                ShootBall();
            }
        }

        // Track in-flight balls and re-enable collisions once above Y = 14
        MonitorBallHeights();
    }

    void HandleInput()
    {
        if (shooterActiveAction.action == null) return;

        if (activationMode == ActivationMode.Toggle)
        {
            if (shooterActiveAction.action.WasPressedThisFrame())
                isShooterPowered = !isShooterPowered;
        }
        else
        {
            isShooterPowered = shooterActiveAction.action.IsPressed();
        }
    }

    void SpinFlywheels()
    {
        float deltaAngle = flywheelSpinSpeed * Time.deltaTime;
        if (flywheelLeft != null)
            flywheelLeft.Rotate(0f, 0f, deltaAngle * (invertLeftWheel ? -1f : 1f), Space.Self);
        if (flywheelRight != null)
            flywheelRight.Rotate(0f, 0f, deltaAngle * (invertRightWheel ? -1f : 1f), Space.Self);
    }

    void ShootBall()
    {
        while (ballQueue.Count > 0 && ballQueue.Peek() == null) ballQueue.Dequeue();
        if (ballQueue.Count == 0) return;

        GameObject ball = ballQueue.Dequeue();
        ball.transform.SetParent(null);

        if (firePoint != null)
        {
            ball.transform.position = firePoint.position;
        }

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        Collider ballCol = ball.GetComponent<Collider>();

        if (rb != null && ballCol != null)
        {
            // Keep collider fully ON so it hits walls immediately
            ballCol.enabled = true;

            // Ignore only the robot chassis colliders while inside
            foreach (Collider rCol in robotColliders)
            {
                if (rCol != null && rCol != storageTrigger)
                    Physics.IgnoreCollision(ballCol, rCol, true);
            }

            // Real physical dynamic launch
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Clear any residual velocities
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Apply forward + upward launch impulse
            Vector3 shootDir = firePoint != null ? firePoint.forward : transform.forward;
            Vector3 launchVelocity = (shootDir * forwardForce) + (Vector3.up * upwardForce);
            rb.AddForce(launchVelocity, ForceMode.VelocityChange);

            // Register tracker to monitor when Y >= 14
            activeBalls.Add(new BallCollisionTracker
            {
                ball = ball,
                ballCol = ballCol,
                rb = rb
            });
        }
    }

    void MonitorBallHeights()
    {
        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            BallCollisionTracker tracker = activeBalls[i];

            if (tracker.ball == null)
            {
                activeBalls.RemoveAt(i);
                continue;
            }

            // Once the ball climbs past Y = 14, restore collision with the robot
            if (tracker.ball.transform.position.y >= reactivationYThreshold)
            {
                foreach (Collider rCol in robotColliders)
                {
                    if (rCol != null && tracker.ballCol != null)
                        Physics.IgnoreCollision(tracker.ballCol, rCol, false);
                }
                activeBalls.RemoveAt(i);
            }
        }
    }

    public void HandleStorageTriggerEnter(Collider other)
    {
        if (other.CompareTag("WildFire"))
        {
            if (!ballQueue.Contains(other.gameObject))
            {
                ballQueue.Enqueue(other.gameObject);
            }
        }
    }

    public void HandleStorageTriggerExit(Collider other)
    {
        if (ballQueue.Contains(other.gameObject))
        {
            Queue<GameObject> temp = new Queue<GameObject>();
            while (ballQueue.Count > 0)
            {
                GameObject item = ballQueue.Dequeue();
                if (item != other.gameObject) temp.Enqueue(item);
            }
            while (temp.Count > 0) ballQueue.Enqueue(temp.Dequeue());
        }
    }
}

public class StorageTriggerRelay : MonoBehaviour
{
    [HideInInspector] public RobotShooter owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.HandleStorageTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner != null) owner.HandleStorageTriggerExit(other);
    }
}