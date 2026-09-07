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

    private bool isBallReady = false;
    private GameObject readyBall = null;

    private bool isBallFlying = false;
    private float t = 0f;
    private GameObject flyingBall = null;
    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 controlPoint;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WildFire") && !isBallFlying)
        {
            readyBall = other.gameObject;
            isBallReady = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == readyBall)
        {
            readyBall = null;
            isBallReady = false;
        }
    }
}
