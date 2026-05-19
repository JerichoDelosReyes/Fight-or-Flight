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
    [Tooltip("When true, the arrow keys are used for ship input (virtual mouse) and A/D can be used for strafing like in many arcade space sims.\n\nOtherwise, WASD/Arrows/Joystick + R/T are used for flying, representing a more traditional style space sim.")]
    public bool useMouseInput = true;
    [Tooltip("When using Keyboard/Joystick input, should roll be added to horizontal stick movement. This is a common trick in traditional space sims to help ships roll into turns and gives a more plane-like feeling of flight.")]
    public bool addRoll = true;

    [Space]

    [Range(-1, 1)]
    public float pitch;
    [Range(-1, 1)]
    public float yaw;
    [Range(-1, 1)]
    public float roll;
    [Range(-1, 1)]
    public float strafe;
    [Range(-1, 1)]
    public float throttle;

    // How quickly the throttle reacts to input.
    private const float THROTTLE_SPEED = 0.5f;

    // Keep a reference to the ship this is attached to just in case.
    private Ship ship;

    private Vector2 virtualMousePosition;
    public float virtualMouseSpeed = 1000f;
    [Tooltip("Screen fraction to move the virtual mouse per arrow key press.")]
    public float virtualMouseKeyStep = 0.08f;

    public Vector2 VirtualMousePosition => virtualMousePosition;

    private Vector2 virtualMouseOffset;
    public float virtualMouseCenteringSpeed = 10f;

    private void Awake()
    {
        ship = GetComponent<Ship>();
        
        // Initialize tuning from ScriptsReference
        mouseSensitivity = ScriptsReference.MouseSensitivity;
        mouseDeadzone = 0f;

        // Initialize virtual mouse position to center of screen
        virtualMousePosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        virtualMouseOffset = Vector2.zero;
    }

    private void Update()
    {
        // Reset inputs every frame to prevent values from "sticking"
        pitch = 0;
        yaw = 0;
        roll = 0;
        strafe = 0;

        if (useMouseInput)
        {
            // Use A/D for strafing to avoid conflict with arrow keys used for virtual mouse.
            if (Input.GetKey(KeyCode.D)) strafe += 1;
            if (Input.GetKey(KeyCode.A)) strafe -= 1;

            UpdateVirtualMousePosition();
            SetStickCommandsUsingVirtualMouse();
            UpdateKeyboardThrottle(KeyCode.W, KeyCode.S);

            // Add roll to turns if enabled
            if (addRoll)
                roll = -yaw * 0.5f;
        }
        else
        {            
            pitch = Input.GetAxis("Vertical");
            yaw = Input.GetAxis("Horizontal");

            if (addRoll)
                roll = -Input.GetAxis("Horizontal") * 0.5f;

            UpdateKeyboardThrottle(KeyCode.R, KeyCode.V);
        }
    }

    private void UpdateVirtualMousePosition()
    {
        // Don't update cursor position if Shift is held (used for camera zoom)
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            virtualMouseOffset = Vector2.Lerp(virtualMouseOffset, Vector2.zero, virtualMouseCenteringSpeed * Time.deltaTime);
            return;
        }

        float h = 0;
        float v = 0;

        float stepX = Screen.width * virtualMouseKeyStep;
        float stepY = Screen.height * virtualMouseKeyStep;

        if (Input.GetKeyDown(KeyCode.RightArrow)) virtualMouseOffset.x += stepX;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) virtualMouseOffset.x -= stepX;
        if (Input.GetKeyDown(KeyCode.UpArrow)) virtualMouseOffset.y += stepY;
        if (Input.GetKeyDown(KeyCode.DownArrow)) virtualMouseOffset.y -= stepY;

        if (Input.GetKey(KeyCode.RightArrow)) h += 1;
        if (Input.GetKey(KeyCode.LeftArrow)) h -= 1;
        if (Input.GetKey(KeyCode.UpArrow)) v += 1;
        if (Input.GetKey(KeyCode.DownArrow)) v -= 1;

        // Move offset based on keys, and lerp back to center independently for each axis
        if (h != 0)
        {
            virtualMouseOffset.x += h * virtualMouseSpeed * Time.deltaTime;
        }
        else
        {
            virtualMouseOffset.x = Mathf.Lerp(virtualMouseOffset.x, 0, virtualMouseCenteringSpeed * Time.deltaTime);
            if (Mathf.Abs(virtualMouseOffset.x) < 1.0f) virtualMouseOffset.x = 0;
        }

        if (v != 0)
        {
            virtualMouseOffset.y += v * virtualMouseSpeed * Time.deltaTime;
        }
        else
        {
            virtualMouseOffset.y = Mathf.Lerp(virtualMouseOffset.y, 0, virtualMouseCenteringSpeed * Time.deltaTime);
            if (Mathf.Abs(virtualMouseOffset.y) < 1.0f) virtualMouseOffset.y = 0;
        }

        // Clamp the offset to keep the cursor within a reasonable area of the screen
        float maxOffsetH = Screen.width * 0.4f;
        float maxOffsetV = Screen.height * 0.4f;
        virtualMouseOffset.x = Mathf.Clamp(virtualMouseOffset.x, -maxOffsetH, maxOffsetH);
        virtualMouseOffset.y = Mathf.Clamp(virtualMouseOffset.y, -maxOffsetV, maxOffsetV);
    }

    [Tooltip("Deadzone for mouse input to prevent drifting.")]
    public float mouseDeadzone = 0.0f;

    [Tooltip("Sensitivity for mouse input.")]
    public float mouseSensitivity = 1.0f;

    /// <summary>
    /// Freelancer style mouse controls. This uses the mouse to simulate a virtual joystick.
    /// When the mouse is in the center of the screen, this is the same as a centered stick.
    /// </summary>
    private void SetStickCommandsUsingVirtualMouse()
    {
        Vector3 shipScreenPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);

        if (Camera.main != null && Ship.PlayerShip != null)
        {
            // Calculate where the ship is aiming on screen
            Vector3 aimPoint = Ship.PlayerShip.transform.position + Ship.PlayerShip.transform.forward * 100f;
            shipScreenPos = Camera.main.WorldToScreenPoint(aimPoint);
            
            // If the ship is off-screen (behind), fall back to center or some sane value
            if (shipScreenPos.z < 0) shipScreenPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        }

        // The virtual mouse position is now always relative to the ship's aim point
        virtualMousePosition = (Vector2)shipScreenPos + virtualMouseOffset;

        // Figure out mouse position relative to ship's aiming point.
        float mouseX = virtualMouseOffset.x / (Screen.width * 0.5f);
        float mouseY = virtualMouseOffset.y / (Screen.height * 0.5f);

        // Apply sensitivity
        mouseX *= mouseSensitivity;
        mouseY *= mouseSensitivity;

        // Apply deadzone
        if (Mathf.Abs(mouseX) < mouseDeadzone) mouseX = 0;
        else mouseX = (mouseX - Mathf.Sign(mouseX) * mouseDeadzone) / (1.0f - mouseDeadzone);

        if (Mathf.Abs(mouseY) < mouseDeadzone) mouseY = 0;
        else mouseY = (mouseY - Mathf.Sign(mouseY) * mouseDeadzone) / (1.0f - mouseDeadzone);

        pitch = -Mathf.Clamp(mouseY, -1.0f, 1.0f);
        yaw = Mathf.Clamp(mouseX, -1.0f, 1.0f);
    }


    /// <summary>
    /// Uses R and F to raise and lower the throttle.
    /// </summary>
    private void UpdateKeyboardThrottle(KeyCode increaseKey, KeyCode decreaseKey)
    {
        float target = throttle;

        if (Input.GetKey(increaseKey))
            target = 1.0f;
        else if (Input.GetKey(decreaseKey))
            target = -1.0f;

        throttle = Mathf.MoveTowards(throttle, target, Time.deltaTime * ScriptsReference.ThrottleSpeed);
    }

    /// <summary>
    /// Uses the mouse wheel to control the throttle.
    /// </summary>
    private void UpdateMouseWheelThrottle()
    {
        throttle += Input.GetAxis("Mouse ScrollWheel");
        throttle = Mathf.Clamp(throttle, -1.0f, 1.0f);
    }
}