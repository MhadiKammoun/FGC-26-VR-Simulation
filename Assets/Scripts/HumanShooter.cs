using UnityEngine;

public class HumanShooter : MonoBehaviour
{
    [Header("Shooting Settings (Basketball Style)")]
    [Tooltip("Forward power of the throw")]
    public float shootPower = 9f;

    [Tooltip("Upward force for the arc")]
    public float arcForce = 7f;

    [Header("Hold Point")]
    public Transform holdPoint;           // ? Drag your empty child object here

    void Start()
    {
        // This is where you assign the hold point
        if (holdPoint != null)
            PlayerHand.holdPoint = holdPoint;
    }

    void Update()
    {
        // Pickup with C
        if (Raycast.isInteractable && Input.GetKeyDown(KeyCode.C))
        {
            if (Raycast.currentTarget != null)
            {
                var ball = Raycast.currentTarget.GetComponent<BallPickUp>();
                if (ball != null)
                    ball.TryPickUp();
            }
        }

        // Shoot with X
        if (PlayerHand.currentHeldObject != null && Input.GetKeyDown(KeyCode.X))
        {
            var ballScript = PlayerHand.currentHeldObject.GetComponent<BallPickUp>();
            if (ballScript != null && ballScript.isHolding)
            {
                ShootBall();
            }
        }
    }

    void ShootBall()
    {
        GameObject ball = PlayerHand.currentHeldObject;
        if (ball == null) return;

        BallPickUp ballScript = ball.GetComponent<BallPickUp>();
        if (ballScript == null) return;

        ballScript.isHolding = false;
        PlayerHand.currentHeldObject = null;

        ball.transform.SetParent(null);

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;
        ball.GetComponent<Collider>().enabled = true;

        // Realistic basketball 3-pointer arc
        Vector3 direction = Camera.main.transform.forward;
        rb.AddForce(direction * shootPower + Vector3.up * arcForce, ForceMode.Impulse);
    }
}