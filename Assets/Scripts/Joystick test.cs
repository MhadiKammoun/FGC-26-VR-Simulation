using UnityEngine;
using UnityEngine.InputSystem;
public class VRMove : MonoBehaviour
{
    public InputActionReference moveAction;
    public Transform xrOrigin;
    public Transform headCamera;
    public float speed = 2f;
    void OnEnable() => moveAction.action.Enable();
    void OnDisable() => moveAction.action.Disable();
    void Update()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 forward = new Vector3(headCamera.forward.x, 0, headCamera.forward.z).normalized; 
        xrOrigin.position += forward * input.y * speed * Time.deltaTime;
    }
}