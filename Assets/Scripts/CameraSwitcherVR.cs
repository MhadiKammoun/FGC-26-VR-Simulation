using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class CameraSwitcherVR : MonoBehaviour
{
    [Header("Camera List")]
    [Tooltip("Drag all the VR cameras into this list. The script will locate and toggle each camera's full XR Origin rig.")]
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
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogError("No cameras assigned to the CameraSwitcher script!", this);
            enabled = false;
            return;
        }

        SetActiveCamera(currentCameraIndex);
    }

    void Update()
    {
        if (switchCameraAction != null &&
            switchCameraAction.action.WasPressedThisFrame())
        {
            CycleCamera();
        }
    }

    void CycleCamera()
    {
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;
        SetActiveCamera(currentCameraIndex);
    }

    void SetActiveCamera(int indexToEnable)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null) continue;

            bool isActive = (i == indexToEnable);

            // 1. Find the XR Origin root in the parent hierarchy
            XROrigin origin = cameras[i].GetComponentInParent<XROrigin>(true);

            // Target the XROrigin GameObject, or fallback to the root GameObject
            GameObject rigObject = origin != null
                ? origin.gameObject
                : cameras[i].transform.root.gameObject;

            // 2. Prevent self-deactivation if CameraSwitcher is attached directly onto one of the rigs
            if (rigObject == gameObject && !isActive)
            {
                Debug.LogWarning("CameraSwitcher is attached to a rig that is being deactivated! Place CameraSwitcher on an independent Manager GameObject in your scene.", this);
            }

            // 3. Toggle the entire rig GameObject
            rigObject.SetActive(isActive);

            // 4. Toggle the AudioListener
            AudioListener listener = cameras[i].GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = isActive;
            }
        }
    }
}