using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScoringZone : MonoBehaviour
{
    public enum ZoneTarget
    {
        CompressionUnit,
        Extinguisher
    }

    [Header("Configuration")]
    [SerializeField] private ZoneTarget targetType;
    [SerializeField] private ScoreManagerUI scoreManagerUI;
    [SerializeField] private string ballTag = "WildFire";

    // Pre-allocated HashSet: O(1) lookup, zero GC allocations during gameplay
    private readonly HashSet<int> scoredBallIDs = new HashSet<int>(500);

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (scoreManagerUI == null)
        {
            scoreManagerUI = FindObjectOfType<ScoreManagerUI>();
        }

        // Register with the manager for global resetting
        if (scoreManagerUI != null)
        {
            scoreManagerUI.RegisterScoringZone(this);
        }
    }

    private void OnDestroy()
    {
        if (scoreManagerUI != null)
        {
            scoreManagerUI.UnregisterScoringZone(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ballTag)) return;

        int ballID = other.gameObject.GetInstanceID();

        if (scoredBallIDs.Add(ballID))
        {
            if (targetType == ZoneTarget.CompressionUnit)
            {
                scoreManagerUI.AddCompressionBall();
            }
            else if (targetType == ZoneTarget.Extinguisher)
            {
                scoreManagerUI.AddExtinguisherBall();
            }
        }
    }

    public void ResetZone()
    {
        scoredBallIDs.Clear();
    }
}