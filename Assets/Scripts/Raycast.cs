using UnityEngine;
using UnityEngine.UI;

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

                // Turn ON the outline checkbox on the ball
                var outline = hit.collider.GetComponent<Outline>();
                if (outline != null) outline.enabled = true;

                // Turn OFF outline on previous ball
                if (previousTarget != null && previousTarget != hit.collider.gameObject)
                {
                    var prevOutline = previousTarget.GetComponent<Outline>();
                    if (prevOutline != null) prevOutline.enabled = false;
                }
                previousTarget = hit.collider.gameObject;
            }
            else
            {
                isInteractable = false;
                currentTarget = null;

                if (previousTarget != null)
                {
                    var prevOutline = previousTarget.GetComponent<Outline>();
                    if (prevOutline != null) prevOutline.enabled = false;
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
                var prevOutline = previousTarget.GetComponent<Outline>();
                if (prevOutline != null) prevOutline.enabled = false;
                previousTarget = null;
            }
        }
    }
}