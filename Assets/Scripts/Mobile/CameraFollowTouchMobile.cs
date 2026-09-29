using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class UniversalScreenTouchLook : MonoBehaviour
{
    [Header("Camera Control")]
    [Tooltip("Leave empty to automatically grab and follow whichever Camera is currently active / Camera.main")]
    [SerializeField] private Transform targetCamera;
    [Tooltip("If true, automatically re-targets whichever Camera is currently active/enabled in the scene")]
    [SerializeField] private bool autoTrackActiveCamera = true;

    [Header("Dead Zones / Exclusion Areas")]
    [Tooltip("RectTransforms where touches/drags will be completely ignored (e.g. Left Joystick, Right Joystick, Button clusters)")]
    [SerializeField] private RectTransform[] exclusionZones;

    [Header("Sensitivity & Response")]
    [SerializeField] private float sensitivity = 0.15f;
    [SerializeField] private float smoothSpeed = 35f;

    [Header("Pitch Limits (Vertical Clamp)")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Axis Inversion")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Editor & Device Debug Visualization")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color activeAreaColor = new Color(0f, 1f, 0f, 0.12f);
    [SerializeField] private Color exclusionColor = new Color(1f, 0f, 0f, 0.45f);

    private float targetRotX;
    private float targetRotY;
    private float currentRotX;
    private float currentRotY;

    private int activeFingerId = -999;
    private Transform cachedCamTransform;
    private Camera lastKnownCamera;

    private void Awake()
    {
        ResolveCameraTarget();
    }

    private void Start()
    {
        SyncStartingAngles();
    }

    private void Update()
    {
        if (autoTrackActiveCamera)
        {
            ResolveCameraTarget();
        }

        if (cachedCamTransform == null) return;

        ProcessTouchInput();
#if UNITY_EDITOR
        ProcessMouseEditorFallback();
#endif

        float dt = Time.deltaTime;
        currentRotX = Mathf.Lerp(currentRotX, targetRotX, dt * smoothSpeed);
        currentRotY = Mathf.Lerp(currentRotY, targetRotY, dt * smoothSpeed);

        cachedCamTransform.localRotation = Quaternion.Euler(currentRotX, currentRotY, 0f);
    }

    public void SetTargetCamera(Transform newCamera)
    {
        if (newCamera == null) return;
        targetCamera = newCamera;
        cachedCamTransform = newCamera;
        SyncStartingAngles();
    }

    private void ResolveCameraTarget()
    {
        if (targetCamera != null && targetCamera.gameObject.activeInHierarchy)
        {
            if (cachedCamTransform != targetCamera)
            {
                cachedCamTransform = targetCamera;
                SyncStartingAngles();
            }
            return;
        }

        Camera activeCam = Camera.main;
        if (activeCam == null || !activeCam.isActiveAndEnabled)
        {
            Camera[] allCams = Camera.allCameras;
            for (int i = 0; i < allCams.Length; i++)
            {
                if (allCams[i].isActiveAndEnabled)
                {
                    activeCam = allCams[i];
                    break;
                }
            }
        }

        if (activeCam != null && activeCam != lastKnownCamera)
        {
            lastKnownCamera = activeCam;
            cachedCamTransform = activeCam.transform;
            targetCamera = cachedCamTransform;
            SyncStartingAngles();
        }
    }

    private void SyncStartingAngles()
    {
        if (cachedCamTransform == null) return;

        Vector3 euler = cachedCamTransform.localEulerAngles;
        targetRotX = NormalizeAngle(euler.x);
        targetRotY = NormalizeAngle(euler.y);

        currentRotX = targetRotX;
        currentRotY = targetRotY;
    }

    private void ProcessTouchInput()
    {
        int touchCount = Input.touchCount;
        if (touchCount == 0)
        {
            activeFingerId = -999;
            return;
        }

        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase == TouchPhase.Began)
            {
                if (activeFingerId != -999) continue;

                if (IsPositionInsideAnyExclusionZone(touch.position))
                    continue;

                activeFingerId = touch.fingerId;
            }

            if (touch.fingerId == activeFingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    ApplyLookDelta(touch.deltaPosition.x, touch.deltaPosition.y);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    activeFingerId = -999;
                }
                break;
            }
        }
    }

#if UNITY_EDITOR
    private bool isMouseActive = false;

    private void ProcessMouseEditorFallback()
    {
        if (activeFingerId != -999) return;

        Vector2 mousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPositionInsideAnyExclusionZone(mousePos)) return;
            isMouseActive = true;
        }

        if (Input.GetMouseButton(0) && isMouseActive)
        {
            float dx = Input.GetAxis("Mouse X") * 10f;
            float dy = Input.GetAxis("Mouse Y") * 10f;
            ApplyLookDelta(dx, dy);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isMouseActive = false;
        }
    }
#endif

    private void ApplyLookDelta(float dx, float dy)
    {
        float signX = invertX ? -1f : 1f;
        float signY = invertY ? 1f : -1f;

        targetRotY += dx * sensitivity * signX;
        targetRotX -= dy * sensitivity * signY;
        targetRotX = Mathf.Clamp(targetRotX, minPitch, maxPitch);
    }

    public bool IsPositionInsideAnyExclusionZone(Vector2 screenPoint)
    {
        if (exclusionZones == null || exclusionZones.Length == 0) return false;

        for (int i = 0; i < exclusionZones.Length; i++)
        {
            RectTransform zone = exclusionZones[i];
            if (zone == null || !zone.gameObject.activeInHierarchy) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(zone, screenPoint, null))
            {
                return true;
            }
        }

        return false;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindObjectOfType<Canvas>();
        if (parentCanvas == null) return;

        // Draw Exclusion Zones
        if (exclusionZones != null)
        {
            Gizmos.color = exclusionColor;
            for (int i = 0; i < exclusionZones.Length; i++)
            {
                RectTransform zone = exclusionZones[i];
                if (zone == null) continue;

                Vector3[] corners = new Vector3[4];
                zone.GetWorldCorners(corners);

                Gizmos.DrawLine(corners[0], corners[1]);
                Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]);
                Gizmos.DrawLine(corners[3], corners[0]);

                Handles.color = exclusionColor;
                Handles.DrawSolidRectangleWithOutline(corners, exclusionColor, Color.red);
                Handles.Label(corners[1], $"Exclusion Zone: {zone.name}");
            }
        }
    }
#endif
}