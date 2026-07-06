using System.Collections.Generic;
using UnityEngine;

public class MovemntScript : MonoBehaviour
{
    [Header("Wheel Assignments")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider backLeftWheel;
    public WheelCollider backRightWheel;

    [Header("Movement")]
    public float maxMotorForce = 2000f;
    public float brakeForce = 500f;
    public float maxSteerAngle = 30f;

    [Header("Sliding")]
    public float strafeForce = 1000f;

    [Header("Physics")]
    public Rigidbody robotMainRigidbody;
    public Rigidbody rb;
    public float customLateralForceStrength = 3000f;

    [Header("Visuals (Optional)")]
    public List<WheelCollider> allWheelCollidersForVisuals = new List<WheelCollider>();
    public List<Transform> allVisualWheels = new List<Transform>();

    public GameObject c;

    void FixedUpdate()
    {
        
        //========================================
        // FORWARD / BACKWARD
        // W = Reverse
        // S = Forward
        //========================================

        float driveInput = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            driveInput = -1f;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            driveInput = 1f;

        // Gamepad overrides keyboard
        float vertical = Input.GetAxis("Vertical");
        if (Mathf.Abs(vertical) > 0.1f)
            driveInput = -vertical;

        //========================================
        // CAR STEERING
        // A/D, Left Arrow/Right Arrow and Joystick
        //========================================

        float steerInput = Input.GetAxis("Horizontal");

        // Keyboard fallback
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            steerInput = -1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            steerInput = 1f;

        frontLeftWheel.steerAngle = steerInput * maxSteerAngle;
        frontRightWheel.steerAngle = steerInput * maxSteerAngle;

        //========================================
        // DRIVE THE WHEELS
        //========================================

        List<WheelCollider> wheels = new List<WheelCollider>()
        {
            frontLeftWheel,
            frontRightWheel,
            backLeftWheel,
            backRightWheel
        };

        wheels.RemoveAll(w => w == null);

        foreach (WheelCollider wheel in wheels)
        {
            wheel.motorTorque = 0f;
            wheel.brakeTorque = brakeForce;
        }

        if (Mathf.Abs(driveInput) > 0.01f)
        {
            foreach (WheelCollider wheel in wheels)
            {
                wheel.brakeTorque = 0f;
                wheel.motorTorque = driveInput * maxMotorForce;
            }
        }

        //========================================
        // SLIDE
        // L = Left
        // M = Right
        //========================================

        float strafe = 0f;

        if (Input.GetKey(KeyCode.L))
            strafe = -1f;

        if (Input.GetKey(KeyCode.P))
            strafe = 1f;

        if (Mathf.Abs(strafe) > 0.01f)
        {
            Vector3 move =
                transform.right *
                strafe *
                strafeForce *
                Time.fixedDeltaTime;

            rb.MovePosition(rb.position + move);

            if (c != null)
                c.SetActive(true);
        }
        else
        {
            if (c != null)
                c.SetActive(false);
        }

        //========================================
        // UPDATE VISUAL WHEELS
        //========================================

        UpdateAllWheelVisuals();
    }

    void UpdateAllWheelVisuals()
    {
        if (allWheelCollidersForVisuals.Count != allVisualWheels.Count)
            return;

        for (int i = 0; i < allWheelCollidersForVisuals.Count; i++)
        {
            WheelCollider wc = allWheelCollidersForVisuals[i];
            Transform vw = allVisualWheels[i];

            if (wc == null || vw == null)
                continue;

            wc.GetWorldPose(out Vector3 pos, out Quaternion rot);

            vw.position = pos;
            vw.rotation = rot;
        }
    }
}