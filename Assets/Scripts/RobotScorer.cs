using UnityEngine;

public class RobotScorer : MonoBehaviour
{
    [Header("Main References")]
    public RobotShooter normalShooter;
    public Transform firePoint;

    [Header("Compression Unit Walls & Targets")]
    public GameObject wallA;
    public Transform targetA;

    public GameObject wallB;
    public Transform targetB;

    [Header("Shooting Settings")]
    public float throwDuration = 0.75f;
    public float arcHeight = 2.5f;
    public float exitSpeedMultiplier = 1f; // 1 = exact match to curve speed (no snap). Lower values will reintroduce a deceleration "stop".

    private int wallContactCount = 0;
    private Transform currentTarget = null;

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
        if (wallContactCount <= 0) return;

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

                    // Prevents the ball from tunneling into / snagging on the
                    // compression unit's colliders right as it re-enables.
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
        if (readyBall == null || firePoint == null || currentTarget == null) return;

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
        endPos = currentTarget.position;
        controlPoint = (startPos + endPos) * 0.5f + Vector3.up * arcHeight;

        flyingBall = ball;
        t = 0f;
        isBallFlying = true;
    }

    void SetScoringMode(bool active)
    {
        if (normalShooter != null)
            normalShooter.enabled = !active;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == wallA || (wallA != null && other.transform.IsChildOf(wallA.transform)))
        {
            wallContactCount++;
            currentTarget = targetA;
            SetScoringMode(true);
            return;
        }

        if (other.gameObject == wallB || (wallB != null && other.transform.IsChildOf(wallB.transform)))
        {
            wallContactCount++;
            currentTarget = targetB;
            SetScoringMode(true);
            return;
        }

        if (other.CompareTag("WildFire") && !isBallFlying)
        {
            readyBall = other.gameObject;
            isBallReady = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == wallA || (wallA != null && other.transform.IsChildOf(wallA.transform)) ||
            other.gameObject == wallB || (wallB != null && other.transform.IsChildOf(wallB.transform)))
        {
            wallContactCount--;
            if (wallContactCount < 0) wallContactCount = 0;

            if (wallContactCount == 0)
            {
                currentTarget = null;
                SetScoringMode(false);
            }
            return;
        }

        if (other.gameObject == readyBall)
        {
            readyBall = null;
            isBallReady = false;
        }
    }
}