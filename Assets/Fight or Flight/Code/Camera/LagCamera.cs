
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LagCamera : MonoBehaviour
{
    [Tooltip("Speed at which the camera rotates. Lower values make the camera more stable and less 'snappy'.")]
    public float rotateSpeed = 120.0f;

    [Tooltip("If the parented object is using FixedUpdate for movement, check this box for smoother movement.")]
    public bool usedFixedUpdate = true;

    [Header("Zoom")]
    [Tooltip("Speed at which the camera zooms.")]
    public float zoomSpeed = 1.0f;
    [Tooltip("Minimum distance multiplier.")]
    public float minDistanceScale = 0.2f;
    [Tooltip("Maximum distance multiplier.")]
    public float maxDistanceScale = 5.0f;
    [Tooltip("Current distance multiplier. Defaults to 0.6 to be closer to the ship.")]
    public float currentDistanceScale = 0.6f;

    [Tooltip("Vertical offset to adjust the ship's position in the frame. Higher values move the ship lower.")]
    public float verticalOffset = 40.0f;

    private Transform target;
    private Vector3 startOffset;
    private Quaternion startRotationOffset;

    private void Start()
    {
        target = transform.parent;

        if (target == null)
            Debug.LogWarning(name + ": Lag Camera will not function correctly without a target.");
        if (transform.parent == null)
            Debug.LogWarning(name + ": Lag Camera will not function correctly without a parent to derive the initial offset from.");

        startOffset = transform.localPosition;
        startRotationOffset = transform.localRotation;
        transform.SetParent(null);
    }

    private void Update()
    {
        HandleZoom();
        if (!usedFixedUpdate)
            UpdateCamera();
    }

    private void FixedUpdate()
    {
        if (usedFixedUpdate)
            UpdateCamera();
    }

    private void HandleZoom()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKey(KeyCode.UpArrow)) currentDistanceScale -= zoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow)) currentDistanceScale += zoomSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.PageUp)) currentDistanceScale -= zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.PageDown)) currentDistanceScale += zoomSpeed * Time.deltaTime;

        currentDistanceScale = Mathf.Clamp(currentDistanceScale, minDistanceScale, maxDistanceScale);
    }

    [Header("Bob & Drift")]
    public float bobAmount = 2.0f;
    public float bobSpeed = 5.0f;
    public float driftAmount = 5.0f;
    public float driftSpeed = 3.0f;

    private float bobTimer = 0f;
    private Vector3 driftOffset;

    private void UpdateCamera()
    {
        if (target != null)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobY = Mathf.Sin(bobTimer) * bobAmount;

            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 localAngularVel = target.InverseTransformDirection(targetRb.angularVelocity);
                Vector3 targetDrift = new Vector3(-localAngularVel.y, localAngularVel.x, 0) * driftAmount;
                driftOffset = Vector3.Lerp(driftOffset, targetDrift, Time.deltaTime * driftSpeed);
            }

            Vector3 offset = startOffset + Vector3.up * verticalOffset;
            Vector3 finalOffset = (offset * currentDistanceScale) + new Vector3(0, bobY, 0) + driftOffset;

            transform.position = target.TransformPoint(finalOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation * startRotationOffset, rotateSpeed * Time.deltaTime);
        }
    }
}
