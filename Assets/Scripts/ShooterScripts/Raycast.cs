using System;
using System.Collections.Generic;
using UnityEngine;

public class RaycastBundle : MonoBehaviour
{
    [Header("Bundle Geometry")]
    [Tooltip("Radius of the raycast cylinder.")]
    public float radius = 0.5f;

    [Tooltip("Maximum distance to check.")]
    public float maxDistance = 10f;

    [Tooltip("Distance between parallel rays.")]
    [Range(0.05f, 0.5f)]
    public float raySpacing = 0.15f;

    public LayerMask hitLayers = ~0;

    [Header("Targeting State")]
    public float distanceFromTarget;
    public bool isInteractable;
    public GameObject currentTarget;

    // Events to decouple outline/interaction logic
    public event Action<GameObject> OnTargetDetected;
    public event Action OnTargetLost;

    private readonly List<Vector2> diskSampleOffsets = new List<Vector2>();
    private MaterialPropertyBlock propBlock;
    private static readonly int OutlinePropId = Shader.PropertyToID("_Outline");

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        RebuildSampleGrid();
    }

    private void OnValidate()
    {
        // Recompute in editor when tweaking variables
        RebuildSampleGrid();
    }

    private void Update()
    {
        GameObject bestTarget = null;
        float closestHitDistance = maxDistance;

        for (int i = 0; i < diskSampleOffsets.Count; i++)
        {
            Vector3 originOffset = (transform.right * diskSampleOffsets[i].x) + (transform.up * diskSampleOffsets[i].y);
            Vector3 rayStart = transform.position + originOffset;

            if (Physics.Raycast(rayStart, transform.forward, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("WildFire") && hit.distance < closestHitDistance)
                {
                    closestHitDistance = hit.distance;
                    bestTarget = hit.collider.gameObject;
                }
            }
        }

        EvaluateTargetChange(bestTarget, closestHitDistance);
    }

    private void EvaluateTargetChange(GameObject newTarget, float distance)
    {
        if (newTarget == currentTarget)
        {
            if (currentTarget != null) distanceFromTarget = distance;
            return;
        }

        // Target changed or lost
        if (currentTarget != null)
        {
            SetOutline(currentTarget, 0f);
            OnTargetLost?.Invoke();
        }

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            distanceFromTarget = distance;
            isInteractable = true;
            SetOutline(currentTarget, 1f);
            OnTargetDetected?.Invoke(currentTarget);
        }
        else
        {
            isInteractable = false;
            distanceFromTarget = 0f;
        }
    }

    private void SetOutline(GameObject obj, float state)
    {
        if (obj.TryGetComponent<Renderer>(out var rend))
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetFloat(OutlinePropId, state);
            rend.SetPropertyBlock(propBlock);
        }
    }

    public void RebuildSampleGrid()
    {
        diskSampleOffsets.Clear();
        diskSampleOffsets.Add(Vector2.zero);

        int rings = Mathf.CeilToInt(radius / Mathf.Max(0.01f, raySpacing));
        for (int r = 1; r <= rings; r++)
        {
            float currentRadius = (radius / rings) * r;
            float circumference = 2f * Mathf.PI * currentRadius;
            int countOnRing = Mathf.Max(6, Mathf.RoundToInt(circumference / raySpacing));

            for (int i = 0; i < countOnRing; i++)
            {
                float angle = i * (2f * Mathf.PI / countOnRing);
                diskSampleOffsets.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentRadius);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (diskSampleOffsets.Count == 0) RebuildSampleGrid();

        Gizmos.color = Color.cyan;
        for (int i = 0; i < diskSampleOffsets.Count; i++)
        {
            Vector3 originOffset = (transform.right * diskSampleOffsets[i].x) + (transform.up * diskSampleOffsets[i].y);
            Vector3 rayStart = transform.position + originOffset;
            Gizmos.DrawLine(rayStart, rayStart + transform.forward * maxDistance);
        }
    }
}