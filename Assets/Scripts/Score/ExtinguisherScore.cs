using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtinguisherScore : MonoBehaviour

    
    
{
    [SerializeField] private ScoreManager ScoreManager;

    private bool gameStarted = false;
    // Called by XR Start Button
    public void StartExtinguisherSystem()
    {
        gameStarted = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameStarted && other.CompareTag("WildFire"))
        {
            ScoreManager.AddExtinguisherPoints(1);
        }
    }
}
