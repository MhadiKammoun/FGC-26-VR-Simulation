using UnityEngine;

public class RobotClimber : MonoBehaviour
{
    public bool canClimb = false;

    [SerializeField] private float climbSpeed = 3f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (canClimb && Input.GetKey(KeyCode.Z))
        {
            rb.velocity = new Vector3(
                rb.velocity.x,
                climbSpeed,
                rb.velocity.z
            );
        }
    }
}