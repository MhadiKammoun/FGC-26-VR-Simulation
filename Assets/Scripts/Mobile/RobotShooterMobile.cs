using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RobotShooterMobile : MonoBehaviour
{
    public enum ActivationMode { Toggle, Hold }

    [Header("Mobile Input")]
    [Tooltip("UI Button used to activate/deactivate the shooter.")]
    [SerializeField] private Button shooterButton;
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
    private EventTrigger runtimeEventTrigger;

    private class BallCollisionTracker
    {
        public GameObject ball;
        public Collider ballCol;
        public Rigidbody rb;
    }

    void Awake()
    {
        robotColliders = GetComponentsInChildren<Collider>();

        if (storageTrigger != null)
        {
            StorageTriggerRelayMobile relay = storageTrigger.GetComponent<StorageTriggerRelayMobile>();
            if (relay == null)
                relay = storageTrigger.gameObject.AddComponent<StorageTriggerRelayMobile>();
            relay.owner = this;
        }
    }

    void OnEnable()
    {
        SetupButtonListeners();
    }

    void OnDisable()
    {
        CleanupButtonListeners();
        isShooterPowered = false;
    }

    private void SetupButtonListeners()
    {
        if (shooterButton == null) return;

        if (activationMode == ActivationMode.Toggle)
        {
            shooterButton.onClick.AddListener(OnToggleButtonClick);
        }
        else
        {
            runtimeEventTrigger = shooterButton.gameObject.GetComponent<EventTrigger>();
            if (runtimeEventTrigger == null)
            {
                runtimeEventTrigger = shooterButton.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDownEntry.callback.AddListener((data) => { isShooterPowered = true; });
            runtimeEventTrigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUpEntry.callback.AddListener((data) => { isShooterPowered = false; });
            runtimeEventTrigger.triggers.Add(pointerUpEntry);
        }
    }

    private void CleanupButtonListeners()
    {
        if (shooterButton == null) return;

        if (activationMode == ActivationMode.Toggle)
        {
            shooterButton.onClick.RemoveListener(OnToggleButtonClick);
        }
        else if (runtimeEventTrigger != null)
        {
            runtimeEventTrigger.triggers.Clear();
        }
    }

    private void OnToggleButtonClick()
    {
        isShooterPowered = !isShooterPowered;
    }

    void Update()
    {
        if (isShooterPowered)
        {
            SpinFlywheels();

            if (ballQueue.Count > 0)
            {
                ShootBall();
            }
        }

        MonitorBallHeights();
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
            ballCol.enabled = true;

            foreach (Collider rCol in robotColliders)
            {
                if (rCol != null && rCol != storageTrigger)
                    Physics.IgnoreCollision(ballCol, rCol, true);
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 shootDir = firePoint != null ? firePoint.forward : transform.forward;
            Vector3 launchVelocity = (shootDir * forwardForce) + (Vector3.up * upwardForce);
            rb.AddForce(launchVelocity, ForceMode.VelocityChange);

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

public class StorageTriggerRelayMobile : MonoBehaviour
{
    [HideInInspector] public RobotShooterMobile owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.HandleStorageTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (owner != null) owner.HandleStorageTriggerExit(other);
    }
}