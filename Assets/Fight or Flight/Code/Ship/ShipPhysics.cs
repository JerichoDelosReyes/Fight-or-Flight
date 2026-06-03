
using UnityEngine;

public class ShipPhysics : MonoBehaviour
{
    [Tooltip("X: Lateral thrust\nY: Vertical thrust\nZ: Longitudinal Thrust")]
    public Vector3 linearForce = new Vector3(100.0f, 100.0f, 100.0f);

    [Tooltip("X: Pitch\nY: Yaw\nZ: Roll")]
    public Vector3 angularForce = new Vector3(100.0f, 100.0f, 100.0f);

    [Range(0.0f, 1.0f)]
    [Tooltip("Multiplier for longitudinal thrust when reverse thrust is requested.")]
    public float reverseMultiplier = 1.0f;

    [Tooltip("Multiplier for all forces. Can be used to keep force numbers smaller and more readable.")]
    public float forceMultiplier = 100.0f;

    [Tooltip("If true, clears Rigidbody rotation constraints at runtime.")]
    public bool autoUnfreezeRotation = true;

    [Tooltip("If true, ensures the Rigidbody is not kinematic at runtime.")]
    public bool autoDisableKinematic = true;

    [Tooltip("If true, uses angular acceleration (ignores mass) for more consistent rotation.")]
    public bool useAngularAcceleration = true;

    public Rigidbody Rigidbody { get { return rbody; } }
    public Vector3 CurrentLinearInput { get; private set; }

    private Vector3 appliedLinearForce = Vector3.zero;
private Vector3 appliedAngularForce = Vector3.zero;
    private Vector3 rawAngularInput = Vector3.zero;

    private Rigidbody rbody;

    private Ship ship;

    void Awake()
    {
        rbody = GetComponent<Rigidbody>();
        if (rbody == null)
        {
            Debug.LogWarning(name + ": ShipPhysics has no rigidbody.");
        }

        ship = GetComponent<Ship>();

        if (rbody != null)
        {
            if (autoDisableKinematic && rbody.isKinematic)
                rbody.isKinematic = false;

            if (autoUnfreezeRotation && (rbody.constraints & RigidbodyConstraints.FreezeRotation) != 0)
                rbody.constraints &= ~RigidbodyConstraints.FreezeRotation;
        }

        if (ship != null && ship.isPlayer)
        {
            linearForce = ScriptsReference.DefaultLinearForce;
            angularForce = ScriptsReference.DefaultAngularForce;
            forceMultiplier = ScriptsReference.DefaultForceMultiplier;
        }
    }

    void FixedUpdate()
    {
        if (rbody != null)
        {
            if (rawAngularInput.sqrMagnitude > 0.0001f)
                rbody.WakeUp();

            rbody.AddRelativeForce(appliedLinearForce * forceMultiplier, ForceMode.Force);

            float maxSpeed = ScriptsReference.MaxSpeed;
            if (!rbody.isKinematic && rbody.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                rbody.linearVelocity = rbody.linearVelocity.normalized * maxSpeed;
            }

            ForceMode torqueMode = useAngularAcceleration ? ForceMode.Acceleration : ForceMode.Force;
            rbody.AddRelativeTorque(appliedAngularForce * forceMultiplier, torqueMode);

            EnforceBoundaries();
        }
    }

    private void EnforceBoundaries()
    {
        float boundary = ScriptsReference.BoundaryLimit;
        if (transform.position.sqrMagnitude > boundary * boundary)
        {
            Vector3 pos = transform.position;
            Vector3 normalizedPos = pos.normalized;

            transform.position = normalizedPos * boundary;

            if (!rbody.isKinematic && Vector3.Dot(rbody.linearVelocity, normalizedPos) > 0)
            {
                rbody.linearVelocity = Vector3.ProjectOnPlane(rbody.linearVelocity, normalizedPos) * 0.5f;
            }
        }
    }

    public void SetLinearInput(Vector3 linearInput)
    {
        CurrentLinearInput = linearInput;
        appliedLinearForce = Vector3.Scale(linearInput, linearForce);

        if (linearInput.z < 0)
        {
            appliedLinearForce.z *= reverseMultiplier;
        }
    }

    public void SetAngularInput(Vector3 angularInput)
    {
        appliedAngularForce = Vector3.Scale(angularInput, angularForce);
        rawAngularInput = angularInput;
    }

    public void SetPhysicsInput(Vector3 linearInput, Vector3 angularInput)
    {
        SetLinearInput(linearInput);
        SetAngularInput(angularInput);
    }

    private Vector3 MultiplyByComponent(Vector3 a, Vector3 b)
    {
        Vector3 ret;

        ret.x = a.x * b.x;
        ret.y = a.y * b.y;
        ret.z = a.z * b.z;

        return ret;
    }
}