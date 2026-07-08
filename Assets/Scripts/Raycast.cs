using UnityEngine;

public class Raycast : MonoBehaviour
{
    public static float distanceFromTarget;
    public static bool isInteractable;
    public static GameObject currentTarget;

    private GameObject previousTarget;

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
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
                    outlineMat.SetFloat("_Outline", 1f);   // ? This checks the OUTLINE checkbox
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
        else
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
}