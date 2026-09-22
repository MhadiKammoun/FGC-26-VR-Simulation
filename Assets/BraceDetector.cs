using UnityEngine;

public class BraceDetector : MonoBehaviour
{
    private RobotClimber robotClimber;

    private void Start()
    {
        robotClimber = GetComponentInParent<RobotClimber>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Brace1") || other.CompareTag("Brace2") || other.CompareTag("Brace3"))
        {
            robotClimber.canClimb = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Brace1") || other.CompareTag("Brace2") || other.CompareTag("Brace3"))
        {
            robotClimber.canClimb = false;
        }
    }
}