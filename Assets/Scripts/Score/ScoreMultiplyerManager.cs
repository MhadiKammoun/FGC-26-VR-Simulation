using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreMultiplyerManager : MonoBehaviour
{
    [SerializeField] private ScoreManager ScoreManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Plane") & other.CompareTag("Brace"))
        {
            ScoreManager.AddClimbMultiplier(0.05f);
        }
    }
}
