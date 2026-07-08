using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExringuisherScore : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WildFire"))
        {
            scoreManager.AddExtinguisherPoints(1);
        }
    }
}
