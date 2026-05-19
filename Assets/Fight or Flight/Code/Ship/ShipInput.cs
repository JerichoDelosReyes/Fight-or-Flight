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
    public float pitchSensitivity = 0.0005f;
    public float yawSensitivity = 0.0005f;
    public float rollSensitivity = 0.0005f;
    public float movementSensitivity = 1.0f;

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

        // Pitch/Yaw/Roll controls with sensitivity
        // Negative pitch to ensure "Up" tilts the nose "Up" (non-inverted)
        pitch = -Input.GetAxis("Vertical") * pitchSensitivity;
        yaw = Input.GetAxis("Horizontal") * yawSensitivity;

        // Explicit Q/E for roll
        if (Input.GetKey(KeyCode.E)) roll = -1f * rollSensitivity;
        if (Input.GetKey(KeyCode.Q)) roll = 1f * rollSensitivity;

        // Thrust (forward) while Left Shift is held
        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle = 1.0f * movementSensitivity;
        }

        // The circle follows the cursor
        VirtualMousePosition = Input.mousePosition;
    }
}
