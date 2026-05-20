//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;

/// <summary>
/// Class specifically to deal with input.
/// </summary>
public class ShipInput : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [Tooltip("Effective sensitivity is value * 0.0001. A value of 5.0 equals 0.0005.")]
    public float pitchSensitivity = 5.0f;
    [Tooltip("Effective sensitivity is value * 0.0001. A value of 5.0 equals 0.0005.")]
    public float yawSensitivity = 5.0f;
    [Tooltip("Effective sensitivity is value * 0.0001. A value of 5.0 equals 0.0005.")]
    public float rollSensitivity = 5.0f;
    public float movementSensitivity = 1.0f;

    [Header("Mouse + Keyboard")]
    [Tooltip("Multiplier applied to throttle while Left Shift is held in Mouse+Keyboard mode.")]
    public float boostMultiplier = 1.75f;
    [Tooltip("Yaw angle (degrees) at which mouse aim demands full yaw input.")]
    public float mouseYawFullInputAngle = 30f;
    [Tooltip("Extra responsiveness multiplier for mouse-driven rotation.")]
    public float mouseRotationGain = 3f;

    private const float SensitivityScale = 0.0001f;

    [Header("Input Values")]
    public float pitch;
    public float yaw;
    public float roll;
    public float strafe;
    public float throttle;

    public Vector2 VirtualMousePosition { get; private set; }

    private void Update()
    {
        // Reset inputs every frame
        pitch = 0;
        yaw = 0;
        roll = 0;
        strafe = 0;
        throttle = 0;

        if (ControlSchemeManager.IsMouseKeyboard)
            UpdateMouseKeyboard();
        else
            UpdateKeyboardOnly();

        VirtualMousePosition = Input.mousePosition;
    }

    private void UpdateKeyboardOnly()
    {
        // Pitch/Yaw/Roll controls with sensitivity
        // W/S keys (Vertical axis) now inverted: W tilts nose DOWN, S tilts nose UP
        pitch = Input.GetAxis("Vertical") * pitchSensitivity * SensitivityScale;
        yaw = Input.GetAxis("Horizontal") * yawSensitivity * SensitivityScale;

        // Explicit Q/E for roll
        if (Input.GetKey(KeyCode.E)) roll = -1f * rollSensitivity * SensitivityScale;
        if (Input.GetKey(KeyCode.Q)) roll = 1f * rollSensitivity * SensitivityScale;

        // Thrust (forward) while Left Shift is held
        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle = 1.0f * movementSensitivity;
        }
    }

    private void UpdateMouseKeyboard()
    {
        // WASD = move (W/S = throttle, A/D = strafe). Movement is plane-style and
        // mapped onto the ship's linear input so existing physics handles it.
        float wsdAxis = Input.GetAxisRaw("Vertical");    // W=+1, S=-1
        float adAxis  = Input.GetAxisRaw("Horizontal");  // D=+1, A=-1

        throttle = wsdAxis * movementSensitivity;
        strafe   = adAxis  * movementSensitivity;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle *= boostMultiplier;
            if (Mathf.Approximately(throttle, 0f))
                throttle = boostMultiplier * movementSensitivity; // boost from rest
        }

        // Mouse aim — raycast from main camera through the cursor onto a horizontal
        // plane at the ship's height. Ship rotates (via angular torque) to face that
        // point. Gun rotates with the ship since fire points are children.
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 aimPoint = ray.GetPoint(dist);
                Vector3 toAim = aimPoint - transform.position;
                toAim.y = 0f;

                if (toAim.sqrMagnitude > 0.0001f)
                {
                    float yawDelta = Vector3.SignedAngle(transform.forward, toAim, Vector3.up);
                    float yawNorm = Mathf.Clamp(yawDelta / Mathf.Max(1f, mouseYawFullInputAngle), -1f, 1f);
                    yaw = yawNorm * yawSensitivity * SensitivityScale * mouseRotationGain;
                }
            }
        }

        // Auto-level: drive pitch/roll back toward world-horizontal so the ship stays
        // flat while WASD-strafing. transform.forward.y > 0 means the nose is pointing
        // up → pitch nose down (positive pitch input per existing convention).
        float pitchAuto = transform.forward.y;
        if (ControlSchemeManager.InvertY) pitchAuto = -pitchAuto;
        pitch = pitchAuto * pitchSensitivity * SensitivityScale * mouseRotationGain;

        // transform.right.y > 0 means the ship is rolled with right wing up → roll
        // left (positive roll input matches the Q-key convention above).
        roll = transform.right.y * rollSensitivity * SensitivityScale * mouseRotationGain;
    }
}
