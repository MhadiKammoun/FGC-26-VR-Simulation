using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupressionUnit : MonoBehaviour
{
    [SerializeField] private ScoreManager ScoreManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WildFire"))
        {
            ScoreManager.AddSuppressionUnitPoints(1);
        }
    }
}
