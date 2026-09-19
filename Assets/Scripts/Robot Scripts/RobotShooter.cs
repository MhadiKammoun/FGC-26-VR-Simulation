using UnityEngine;

public class RobotShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    public float throwDuration = 0.75f;
    public float arcHeight = 2.8f;
    public float shootDistance = 7f;
    public float exitSpeedMultiplier = 1f; // 1 = exact match to curve speed (no snap). Lower values will reintroduce a deceleration "stop".

    [Header("References")]
    public Transform firePoint;
    public Transform target;

    [Header("Storage Detection")]
    [Tooltip("The dedicated box collider (Is Trigger = on) marking the robot's ball storage compartment. Only a ball entering THIS collider will be picked up — not any other collider on the robot.")]
    public Collider storageTrigger;

    private bool isBallReady = false;
    private GameObject readyBall = null;

    private bool isBallFlying = false;
    private float t = 0f;
    private GameObject flyingBall = null;
    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 controlPoint;

    void Awake()
    {
        if (storageTrigger == null)
        {
            Debug.LogWarning($"[RobotShooter] '{name}' has no Storage Trigger assigned — ball pickup will not work until one is set.");
            return;
        }

        if (!storageTrigger.isTrigger)
        {
            Debug.LogWarning($"[RobotShooter] The Storage Trigger assigned on '{name}' does not have Is Trigger enabled.");
        }

        // Attach (or reuse) a relay directly on the storage trigger's own GameObject.
        // This is what actually restricts detection to that specific collider:
        // Unity fires OnTriggerEnter on whatever GameObject owns the entered
        // collider, so the listener HAS to live there, not on the robot body.
        StorageTriggerRelay relay = storageTrigger.GetComponent<StorageTriggerRelay>();
        if (relay == null)
            relay = storageTrigger.gameObject.AddComponent<StorageTriggerRelay>();
        relay.owner = this;
    }

    void Update()
    {
        if (isBallReady && readyBall != null && Input.GetMouseButtonDown(0) && !isBallFlying)
        {
            StartThrow();
        }

        if (isBallFlying && flyingBall != null)
        {
            t += Time.deltaTime;
            float t01 = Mathf.Clamp01(t / throwDuration);

            // Smooth Quadratic Bezier
            float u = 1f - t01;
            Vector3 pos = u * u * startPos + 2f * u * t01 * controlPoint + t01 * t01 * endPos;
            flyingBall.transform.position = pos;

            if (t01 >= 1f)
            {
                isBallFlying = false;

                Rigidbody rb = flyingBall.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // TRUE exit velocity: the exact tangent (derivative) of the
                    // quadratic Bezier curve at t=1, i.e. dB/dt = 2*(P2 - P1).
                    // Using this instead of an average straight-line speed keeps
                    // direction AND magnitude continuous at the handoff, so there's
                    // no sudden deceleration ("stop") when physics takes over.
                    Vector3 exitVelocity = 2f * (endPos - controlPoint) / throwDuration;

                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.velocity = exitVelocity * exitSpeedMultiplier;

                    // Smooths out the visual gap between fixed-timestep physics
                    // and the render frame rate right after the handoff.
                    rb.interpolation = RigidbodyInterpolation.Interpolate;

                    // Prevents the ball from tunneling into / snagging on
                    // anything right as it re-enables its collider.
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }

                Collider col = flyingBall.GetComponent<Collider>();
                if (col != null) col.enabled = true;

                flyingBall = null;
            }
        }
    }

    void StartThrow()
    {
        if (readyBall == null || firePoint == null) return;

        GameObject ball = readyBall;
        isBallReady = false;
        readyBall = null;

        ball.transform.SetParent(null);

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider col = ball.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        startPos = firePoint.position;

        if (target != null)
            endPos = target.position;
        else
            endPos = firePoint.position + firePoint.forward * shootDistance;

        controlPoint = (startPos + endPos) * 0.5f + Vector3.up * arcHeight;

        flyingBall = ball;
        t = 0f;
        isBallFlying = true;
    }

    // Called only by the StorageTriggerRelay sitting on storageTrigger's GameObject —
    // this is the only path that can set isBallReady now, so touching any other
    // collider on the robot (its frame, its front, etc.) no longer does anything.
    public void HandleStorageTriggerEnter(Collider other)
    {
        if (other.CompareTag("WildFire") && !isBallFlying)
        {
            readyBall = other.gameObject;
            isBallReady = true;
        }
    }

    public void HandleStorageTriggerExit(Collider other)
    {
        if (other.gameObject == readyBall)
        {
            readyBall = null;
            isBallReady = false;
        }
    }
}

// Lives on the storage trigger's own GameObject (added automatically by
// RobotShooter.Awake). Its only job is forwarding this specific collider's
// trigger events back to the shooter — nothing else on the robot can trigger it.
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