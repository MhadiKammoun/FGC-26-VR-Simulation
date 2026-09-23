using UnityEngine;

public class ScoreMultiplyerManager : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PipeClimberController climber;   // ← drag the robot that has PipeClimberController

    private string currentLocationTag;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Brace1") ||
            other.CompareTag("Brace2") ||
            other.CompareTag("Brace3"))
        {
            currentLocationTag = other.tag;
        }
    }

    public void CheckRobotLocation()
    {
        // Use the grounded function instead of "Plane" tag
        if (climber != null && climber.isGrounded)
        {
            scoreManager.AddClimbMultiplier(0.05f);
        }
        else if (currentLocationTag == "Brace1")
        {
            scoreManager.AddClimbMultiplier(0.1f);
        }
        else if (currentLocationTag == "Brace2")
        {
            scoreManager.AddClimbMultiplier(0.2f);
        }
        else if (currentLocationTag == "Brace3")
        {
            scoreManager.AddClimbMultiplier(0.3f);
        }
    }
}