using UnityEngine;

public class CameraUIController : MonoBehaviour
{
    [Header("UI that appears when this camera is active")]
    [SerializeField] private GameObject[] uiToActivate;

    [Header("UI that disappears when this camera is active")]
    [SerializeField] private GameObject[] uiToDeactivate;

    private void OnEnable()
    {
        // Camera has been activated
        SetUI(uiToActivate, true);
        SetUI(uiToDeactivate, false);
    }

    private void OnDisable()
    {
        // Camera has been deactivated
        SetUI(uiToActivate, false);
        SetUI(uiToDeactivate, true);
    }

    private void SetUI(GameObject[] uiElements, bool state)
    {
        if (uiElements == null)
            return;

        foreach (GameObject ui in uiElements)
        {
            if (ui != null)
                ui.SetActive(state);
        }
    }
}
