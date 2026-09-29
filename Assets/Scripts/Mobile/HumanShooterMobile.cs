using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HumanShooterMobile : MonoBehaviour
{
    [Header("Raycast Reference")]
    [Tooltip("Drag the GameObject with the Raycast / RaycastBundle component here")]
    public RaycastBundle raycastSensor;

    [Header("Shooting Settings (Basketball Style)")]
    [Tooltip("How long the arc takes (seconds)")]
    public float throwDuration = 0.66f;

    [Tooltip("Max height of the arc")]
    public float arcHeight = 5f;

    [Header("Target (Extinguisher Hole)")]
    public Transform Target;

    [Header("Hold Point")]
    public Transform holdPoint;

    [Header("Mobile UI Controls")]
    [SerializeField] private Button pickUpButton;
    [SerializeField] private Button shootButton;

    private void OnEnable()
    {
        if (pickUpButton != null)
            pickUpButton.onClick.AddListener(OnPickUpPressed);

        if (shootButton != null)
            shootButton.onClick.AddListener(OnShootPressed);
    }

    private void OnDisable()
    {
        if (pickUpButton != null)
            pickUpButton.onClick.RemoveListener(OnPickUpPressed);

        if (shootButton != null)
            shootButton.onClick.RemoveListener(OnShootPressed);
    }

    void Start()
    {
        if (holdPoint != null)
            PlayerHand.holdPoint = holdPoint;

        if (raycastSensor == null)
            raycastSensor = GetComponentInChildren<RaycastBundle>();
    }

    private void OnPickUpPressed()
    {
        // === PICKUP (blocked if already holding a ball) ===
        if (raycastSensor != null &&
            raycastSensor.isInteractable &&
            PlayerHand.currentHeldObject == null)
        {
            if (raycastSensor.currentTarget != null)
            {
                var ball = raycastSensor.currentTarget.GetComponent<BallPickUpMobile>();
                if (ball != null)
                {
                    ball.TryPickUp();
                }
                else
                {
                    var originalBall = raycastSensor.currentTarget.GetComponent<BallPickUp>();
                    if (originalBall != null)
                        originalBall.TryPickUp();
                }
            }
        }
    }

    private void OnShootPressed()
    {
        // === SHOOT ===
        if (PlayerHand.currentHeldObject != null)
        {
            var ballMobile = PlayerHand.currentHeldObject.GetComponent<BallPickUpMobile>();
            if (ballMobile != null && ballMobile.isHolding)
            {
                StartThrow();
                return;
            }

            var ballOriginal = PlayerHand.currentHeldObject.GetComponent<BallPickUp>();
            if (ballOriginal != null && ballOriginal.isHolding)
            {
                StartThrow();
            }
        }
    }

    void StartThrow()
    {
        GameObject ball = PlayerHand.currentHeldObject;
        if (ball == null || Target == null) return;

        var ballMobile = ball.GetComponent<BallPickUpMobile>();
        var ballOriginal = ball.GetComponent<BallPickUp>();
        if (ballMobile == null && ballOriginal == null) return;

        // Release ball from hand state
        if (ballMobile != null) ballMobile.isHolding = false;
        if (ballOriginal != null) ballOriginal.isHolding = false;

        PlayerHand.currentHeldObject = null;
        ball.transform.SetParent(null);

        // Re-enable collider immediately
        Collider col = ball.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        StartCoroutine(AnimateBallArc(ball, ball.transform.position, Target.position));
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
            Vector3 arc = Vector3.up * arcHeight * Mathf.Sin(t01 * Mathf.PI);

            ball.transform.position = pos + arc;

            yield return null;
        }

        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}