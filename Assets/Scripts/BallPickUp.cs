using UnityEngine;

public class BallPickUp : MonoBehaviour
{
    private Rigidbody rb;
    public bool isHolding = false;          // ? Changed to public

    public bool IsHolding => isHolding;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void TryPickUp()
    {
        if (isHolding) return;
        isHolding = true;
        rb.isKinematic = true;
        rb.useGravity = false;

        if (PlayerHand.holdPoint != null)
            transform.SetParent(PlayerHand.holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        GetComponent<Collider>().enabled = false;
        PlayerHand.currentHeldObject = gameObject;
    }
}