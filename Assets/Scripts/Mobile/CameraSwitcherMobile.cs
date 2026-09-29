using UnityEngine;
using UnityEngine.UI;

public class CameraSwitcherMobile : MonoBehaviour
{
    [Header("Camera List")]
    [Tooltip("Drag all the cameras you want to switch between into this list.")]
    public Camera[] cameras;

    [Header("Mobile UI Input")]
    [Tooltip("UI Button used to switch to the next camera.")]
    [SerializeField] private Button switchCameraButton;

    private int currentCameraIndex = 0;

    void OnEnable()
    {
        if (switchCameraButton != null)
            switchCameraButton.onClick.AddListener(CycleCamera);
    }

    void OnDisable()
    {
        if (switchCameraButton != null)
            switchCameraButton.onClick.RemoveListener(CycleCamera);
    }

    void Start()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogError("No cameras assigned to the CameraSwitcherMobile script!", this);
            enabled = false;
            return;
        }

        SetActiveCamera(currentCameraIndex);
    }

    public void CycleCamera()
    {
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;
        SetActiveCamera(currentCameraIndex);
    }

    void SetActiveCamera(int indexToEnable)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            bool isActive = i == indexToEnable;

            cameras[i].gameObject.SetActive(isActive);

            AudioListener listener = cameras[i].GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled = isActive;
            }
        }
    }
}