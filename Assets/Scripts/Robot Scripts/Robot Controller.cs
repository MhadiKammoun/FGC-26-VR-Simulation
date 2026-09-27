using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class XROriginRobotManager : MonoBehaviour
{
    [Header("Input Setup")]
    [SerializeField] private InputActionAsset inputAsset;
    [SerializeField] private string robotActionMapName = "RobotControls";

    [Header("Monitored XR Origins")]
    [Tooltip("Add all your camera/angle XR Origins here.")]
    [SerializeField] private XROrigin[] xrOrigins;

    private InputActionMap m_RobotMap;
    private bool m_WasAnyOriginActive;

    void Awake()
    {
        if (inputAsset != null)
        {
            m_RobotMap = inputAsset.FindActionMap(robotActionMapName);
        }
    }

    void OnDisable()
    {
        // Safety: cut all robot controls if this manager is disabled or scene unloads
        m_RobotMap?.Disable();
        m_WasAnyOriginActive = false;
    }

    void Update()
    {
        if (m_RobotMap == null) return;

        bool isAnyOriginActive = CheckIfAnyOriginActive();

        // Only toggle the action map when the state actually changes
        if (isAnyOriginActive != m_WasAnyOriginActive)
        {
            m_WasAnyOriginActive = isAnyOriginActive;

            if (isAnyOriginActive)
            {
                m_RobotMap.Enable();
            }
            else
            {
                m_RobotMap.Disable();
            }
        }
    }

    private bool CheckIfAnyOriginActive()
    {
        if (xrOrigins == null || xrOrigins.Length == 0)
            return false;

        for (int i = 0; i < xrOrigins.Length; i++)
        {
            XROrigin origin = xrOrigins[i];

            // Checks if the GameObject is active in the hierarchy and the component is enabled
            if (origin != null && origin.gameObject.activeInHierarchy && origin.enabled)
            {
                return true;
            }
        }

        return false;
    }
}