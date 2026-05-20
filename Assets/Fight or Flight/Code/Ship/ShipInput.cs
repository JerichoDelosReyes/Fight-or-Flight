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
    [Tooltip("Mouse look sensitivity in Mouse+Keyboard mode. Scales raw Input.GetAxis(\"Mouse X/Y\") delta.")]
    public float mouseSensitivity = 0.002f;

    private const float SensitivityScale = 0.0001f;

    [Header("Input Values")]
    public float pitch;
    public float yaw;
    public float roll;
    public float strafe;
    public float throttle;

    public Vector2 VirtualMousePosition { get; private set; }

    private void OnEnable()
    {
        ApplyCursorState();
    }

    private void OnDisable()
    {
        ReleaseCursor();
    }

    private void Update()
    {
        pitch    = 0;
        yaw      = 0;
        roll     = 0;
        strafe   = 0;
        throttle = 0;

        if (ControlSchemeManager.IsMouseKeyboard)
            UpdateMouseKeyboard();
        else
            UpdateKeyboardOnly();

        VirtualMousePosition = Input.mousePosition;
    }

    private void UpdateKeyboardOnly()
    {
        // W/S = pitch (W tilts nose down per original convention), A/D = yaw, Q/E = roll
        pitch = Input.GetAxis("Vertical")   * pitchSensitivity * SensitivityScale;
        yaw   = Input.GetAxis("Horizontal") * yawSensitivity   * SensitivityScale;

        if (Input.GetKey(KeyCode.E)) roll = -1f * rollSensitivity * SensitivityScale;
        if (Input.GetKey(KeyCode.Q)) roll =  1f * rollSensitivity * SensitivityScale;

        if (Input.GetKey(KeyCode.LeftShift))
            throttle = 1.0f * movementSensitivity;
    }

    private void UpdateMouseKeyboard()
    {
        // WASD = throttle/strafe, Shift = boost
        float wsAxis = Input.GetAxisRaw("Vertical");
        float adAxis = Input.GetAxisRaw("Horizontal");

        throttle = wsAxis * movementSensitivity;
        strafe   = adAxis * movementSensitivity;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle *= boostMultiplier;
            if (Mathf.Approximately(throttle, 0f))
                throttle = boostMultiplier * movementSensitivity;
        }

        // FPS-style mouse: cursor is locked, raw deltas drive yaw and pitch.
        // No screen-edge spinning, no cursor visible, works exactly like Valorant/FPS.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw = mouseX * mouseSensitivity;

        // Positive pitch = nose down in this engine's convention, so negate
        // mouse Y so dragging up tilts the nose up (natural FPS feel).
        pitch = -mouseY * mouseSensitivity;
        if (ControlSchemeManager.InvertY) pitch = -pitch;

        // Q/E for manual roll
        if (Input.GetKey(KeyCode.E)) roll = -1f * rollSensitivity * SensitivityScale;
        if (Input.GetKey(KeyCode.Q)) roll =  1f * rollSensitivity * SensitivityScale;
    }

    // Called by PauseManager on resume to re-lock the cursor.
    public void ApplyCursorState()
    {
        if (ControlSchemeManager.IsMouseKeyboard)
            LockCursor();
        else
            ReleaseCursor();
    }

    public static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    public static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}
