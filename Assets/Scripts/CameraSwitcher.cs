using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera List")]
    [Tooltip("Drag all the cameras you want to switch between into this list.")]
    public Camera[] cameras;

    [Header("Input")]
    [Tooltip("Input action used to switch to the next camera.")]
    public InputActionReference switchCameraAction;

    private int currentCameraIndex = 0;

    void OnEnable()
    {
        if (switchCameraAction != null)
            switchCameraAction.action.Enable();
    }

    void OnDisable()
    {
        if (switchCameraAction != null)
            switchCameraAction.action.Disable();
    }

    void Start()
    {
        // Ensure we have cameras assigned
        if (cameras == null || cameras.Length == 0)
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
        // Check if the Input Action was pressed
        if (switchCameraAction != null &&
            switchCameraAction.action.WasPressedThisFrame())
        {
            CycleCamera();
        }
    }

    void CycleCamera()
    {
        // Move to the next camera, wrapping back to 0
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;
        SetActiveCamera(currentCameraIndex);
    }

    void SetActiveCamera(int indexToEnable)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            bool isActive = i == indexToEnable;

            // Enable selected camera, disable all others
            cameras[i].gameObject.SetActive(isActive);

            // Toggle AudioListener
            AudioListener listener = cameras[i].GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled = isActive;
            }
        }
    }
}
