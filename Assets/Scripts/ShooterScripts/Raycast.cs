using UnityEngine;

public class Raycast : MonoBehaviour
{
    public static float distanceFromTarget;
    public static bool isInteractable;
    public static GameObject currentTarget;

    [Header("Raycast Thickness")]
    [Tooltip("How thick the detection beam is. Increase for easier aiming.")]
    public float sphereRadius = 0.5f;

    [Tooltip("Maximum distance the player can target an object from.")]
    public float maxDistance = 10f;

    private GameObject previousTarget;

    void Update()
    {
        RaycastHit hit;

        // SphereCast creates a "thick" ray with a radius
        if (Physics.SphereCast(transform.position, sphereRadius, transform.forward, out hit, maxDistance))
        {
            distanceFromTarget = hit.distance;

            if (hit.collider.CompareTag("WildFire"))
            {
                isInteractable = true;
                currentTarget = hit.collider.gameObject;

                // === CHECK THE "OUTLINE" CHECKBOX in Surface Inputs ===
                var renderer = hit.collider.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.materials.Length > 1)
                {
                    Material outlineMat = renderer.materials[1];
                    outlineMat.SetFloat("_Outline", 1f);   // Checks the OUTLINE checkbox
                }

                // Uncheck on previous ball
                if (previousTarget != null && previousTarget != hit.collider.gameObject)
                {
                    var prevRenderer = previousTarget.GetComponent<MeshRenderer>();
                    if (prevRenderer != null && prevRenderer.materials.Length > 1)
                    {
                        prevRenderer.materials[1].SetFloat("_Outline", 0f);
                    }
                }
                previousTarget = hit.collider.gameObject;
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void ClearTarget()
    {
        isInteractable = false;
        currentTarget = null;

        if (previousTarget != null)
        {
            var prevRenderer = previousTarget.GetComponent<MeshRenderer>();
            if (prevRenderer != null && prevRenderer.materials.Length > 1)
            {
                prevRenderer.materials[1].SetFloat("_Outline", 0f);
            }
            previousTarget = null;
        }
    }
}