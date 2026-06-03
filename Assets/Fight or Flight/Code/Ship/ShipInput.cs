
using UnityEngine;

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
    [Tooltip("Multiplier applied to throttle while Left Shift is held.")]
    public float boostMultiplier = 1.75f;
    [Tooltip("Degrees of rotation per raw mouse-delta unit. Increase for higher sensitivity.")]
    public float directMouseSensitivity = 1.5f;
    [Tooltip("Roll speed in degrees per second when Q/E is held in Mouse+Keyboard mode.")]
    public float rollSpeed = 80f;

    private const float SensitivityScale = 0.0001f;
    private Rigidbody cachedRb;

    [Header("Input Values")]
    public float pitch;
    public float yaw;
    public float roll;
    public float strafe;
    public float throttle;

    public Vector2 VirtualMousePosition { get; private set; }

    private void Awake()
    {
        cachedRb = GetComponent<Rigidbody>();
    }

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
        float verticalInput = Input.GetAxis("Vertical");
        if (!ControlSchemeManager.InvertPitchKeyboard) verticalInput = -verticalInput;
        pitch = verticalInput * pitchSensitivity * SensitivityScale;
        yaw   = Input.GetAxis("Horizontal") * yawSensitivity   * SensitivityScale;

        if (Input.GetKey(KeyCode.E)) roll = -1f * rollSensitivity * SensitivityScale;
        if (Input.GetKey(KeyCode.Q)) roll =  1f * rollSensitivity * SensitivityScale;

        if (Input.GetKey(KeyCode.LeftShift))
            throttle = 1.0f * movementSensitivity;
    }

    private void UpdateMouseKeyboard()
    {
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

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        float pitchAngle = -mouseY * directMouseSensitivity;
        if (ControlSchemeManager.InvertY) pitchAngle = -pitchAngle;
        float yawAngle   =  mouseX * directMouseSensitivity;

        float rollAngle = 0f;
        if (Input.GetKey(KeyCode.Q)) rollAngle =  rollSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) rollAngle = -rollSpeed * Time.deltaTime;

        transform.Rotate(pitchAngle, yawAngle, rollAngle, Space.Self);

        if (cachedRb != null && !cachedRb.isKinematic)
            cachedRb.angularVelocity = Vector3.zero;

    }

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
