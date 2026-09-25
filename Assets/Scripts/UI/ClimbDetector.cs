using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClimbDetector : MonoBehaviour
{
    [Header("Controller Reference")]
    [Tooltip("Reference to the PipeClimberController on the robot chassis.")]
    [SerializeField] private PipeClimberController climberController;

    [Header("Tag Identification")]
    [Tooltip("Prefix or tag for Zone 1 (e.g. 'Brace1' or 'Zone1')")]
    [SerializeField] private string zone1Tag = "Brace1";
    [SerializeField] private string zone2Tag = "Brace2";
    [SerializeField] private string zone3Tag = "Brace3";

    // Tracks all active brace zones currently overlapping this trigger
    private readonly HashSet<int> activeZones = new HashSet<int>();

    private void Awake()
    {
        // Enforce trigger on this detector's sphere collider
        GetComponent<Collider>().isTrigger = true;

        if (climberController == null)
        {
            climberController = GetComponentInParent<PipeClimberController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        int zone = GetZoneIndexFromCollider(other);
        if (zone > 0)
        {
            activeZones.Add(zone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        int zone = GetZoneIndexFromCollider(other);
        if (zone > 0)
        {
            activeZones.Remove(zone);
        }
    }

    private int GetZoneIndexFromCollider(Collider col)
    {
        if (col.CompareTag(zone3Tag) || col.name.Contains(zone3Tag)) return 3;
        if (col.CompareTag(zone2Tag) || col.name.Contains(zone2Tag)) return 2;
        if (col.CompareTag(zone1Tag) || col.name.Contains(zone1Tag)) return 1;
        return 0;
    }

    /// <summary>
    /// Called by MatchFlowManager after the 5s settling period.
    /// Evaluates highest contacted brace zone and ground status from PipeClimberController.
    /// </summary>
    public float EvaluateClimbMultiplier(out string zoneName)
    {
        // 1. Read ground contact directly from PipeClimberController
        bool isGrounded = climberController != null && climberController.isGrounded;

        // 2. Determine highest zone currently touched
        int highestZone = 0;
        foreach (int z in activeZones)
        {
            if (z > highestZone) highestZone = z;
        }

        // 3. Evaluate multiplier based on highest zone and grounded state
        switch (highestZone)
        {
            case 3:
                if (!isGrounded)
                {
                    zoneName = "Zone 3 (Suspended Summit)";
                    return 0.30f;
                }
                else
                {
                    // If somehow in Zone 3 but wheels still touch ground
                    zoneName = "Zone 3 (Grounded)";
                    return 0.05f;
                }

            case 2:
                if (!isGrounded)
                {
                    zoneName = "Zone 2 (Suspended)";
                    return 0.20f;
                }
                else
                {
                    zoneName = "Zone 2 (Grounded)";
                    return 0.05f;
                }

            case 1:
                if (!isGrounded)
                {
                    zoneName = "Zone 1 (Elevated)";
                    return 0.10f;
                }
                else
                {
                    zoneName = "Zone 1 (Contacting Carpet)";
                    return 0.05f;
                }

            default:
                zoneName = isGrounded ? "Field Carpet (Parked)" : "Off Carpet (No Brace Contact)";
                return 0.00f;
        }
    }

    /// <summary>
    /// Resets tracked zones if resetting field or restarting match.
    /// </summary>
    public void ResetDetector()
    {
        activeZones.Clear();
    }
}