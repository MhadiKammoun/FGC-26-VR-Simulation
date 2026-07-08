using UnityEngine;
using UnityEngine.InputSystem;
public class SpawnOnPress : MonoBehaviour
{
    public InputActionReference primaryButton;
    void OnEnable() => primaryButton.action.Enable();
    void OnDisable() => primaryButton.action.Disable();
    void Update()
    {
        if (primaryButton.action.WasPressedThisFrame())
        {
            Debug.Log("Button is clicked");
        }
    }
}
