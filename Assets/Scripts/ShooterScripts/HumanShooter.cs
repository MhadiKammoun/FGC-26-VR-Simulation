using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HumanShooter : MonoBehaviour
{
    [Header("Shooting Settings (Basketball Style)")]
    [Tooltip("How long the arc takes (seconds)")]
    public float throwDuration = 0.66f;

    [Tooltip("Max height of the arc")]
    public float arcHeight = 5f;

    [Header("Target (Extinguisher Hole)")]
    public Transform Target;          // ← Drag the hole here
    public InputActionReference pickUpAction;
    public InputActionReference ShootingAction;

    void OnEnable()
    {
        pickUpAction.action.Enable();
        ShootingAction.action.Enable();
    }

    void OnDisable()
    {
        pickUpAction.action.Disable();
        ShootingAction.action.Disable();
    }

    [Header("Hold Point")]
    public Transform holdPoint;            // ← Drag the empty child under the camera here

    void Start()
    {
        if (holdPoint != null)
            PlayerHand.holdPoint = holdPoint;
    }

    void Update()
    {
        // === PICKUP with C (blocked if already holding a ball) ===
        if (pickUpAction.action.WasPressedThisFrame() && Raycast.isInteractable && PlayerHand.currentHeldObject == null)
        {
            if (Raycast.currentTarget != null)
            {
                var ball = Raycast.currentTarget.GetComponent<BallPickUp>();
                if (ball != null)
                    ball.TryPickUp();
            }
        }

        // === SHOOT with X ===
        if (PlayerHand.currentHeldObject != null && ShootingAction.action.WasPressedThisFrame())
        {
            var ballScript = PlayerHand.currentHeldObject.GetComponent<BallPickUp>();
            if (ballScript != null && ballScript.isHolding)
            {
                StartThrow();
            }
        }
    }

    void StartThrow()
    {
        GameObject ball = PlayerHand.currentHeldObject;
        if (ball == null || Target == null) return;

        BallPickUp ballScript = ball.GetComponent<BallPickUp>();
        if (ballScript == null) return;

        // Release ball from hand state
        ballScript.isHolding = false;
        PlayerHand.currentHeldObject = null;
        ball.transform.SetParent(null);

        // Re-enable collider immediately
        Collider col = ball.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;      // We control position during arc
            rb.useGravity = false;
        }

        // Start an independent arc process specifically for THIS ball instance
        StartCoroutine(AnimateBallArc(ball, ball.transform.position, Target.position));
    }

    private IEnumerator AnimateBallArc(GameObject ball, Vector3 startPos, Vector3 endPos)
    {
        float timer = 0f;

        while (timer < throwDuration)
        {
            // Safety check in case the ball is destroyed mid-air
            if (ball == null) yield break;

            timer += Time.deltaTime;
            float t01 = Mathf.Clamp01(timer / throwDuration);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t01);
            Vector3 arc = Vector3.up * arcHeight * Mathf.Sin(t01 * Mathf.PI);

            ball.transform.position = pos + arc;

            yield return null; // Wait for the next frame
        }

        // Arc finished - restore physics on THIS specific ball
        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;       // ← Gravity re-enabled at target
            }
        }
    }
}