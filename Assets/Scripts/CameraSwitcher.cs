using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera List")]
    [Tooltip("Drag all the cameras you want to switch between into this list.")]
    public Camera[] cameras;

    private int currentCameraIndex = 0;

    void Start()
    {
        // Ensure we have cameras assigned, otherwise disable the script
        if (cameras.Length == 0)
        {
            Debug.LogError("No cameras assigned to the CameraSwitcher script!", this);
            enabled = false;
            return;
        }

        // Initialize by enabling only the first camera
        SetActiveCamera(currentCameraIndex);
    }

    void Update()
    {
        // Check for the "N" key press
        if (Input.GetKeyDown(KeyCode.U))
        {
            CycleCamera();
        }
    }

    void CycleCamera()
    {
        // Move to the next camera index, wrapping back to 0 if we hit the end
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;
        SetActiveCamera(currentCameraIndex);
    }

    void SetActiveCamera(int indexToEnable)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            // Enable the selected camera, disable all others
            cameras[i].gameObject.SetActive(i == indexToEnable);

            // Optional: If cameras have AudioListeners, toggle them too to avoid Unity warnings
            AudioListener listener = cameras[i].GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = (i == indexToEnable);
            }
        }
    }
}