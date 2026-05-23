//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;

/// <summary>
/// Applies linear and angular forces to a ship.
/// This is based on the ship physics from https://github.com/brihernandez/UnityCommon/blob/master/Assets/ShipPhysics/ShipPhysics.cs
/// </summary>
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

    // Keep a reference to the ship this is attached to just in case.
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

        // Initialize values from ScriptsReference if this is the player ship
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

            // Limit speed - use sqrMagnitude first for performance
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
        // Simple spherical boundary based on the background size
        float boundary = ScriptsReference.BoundaryLimit;
        if (transform.position.sqrMagnitude > boundary * boundary)
        {
            Vector3 pos = transform.position;
            Vector3 normalizedPos = pos.normalized;

            // Gently push back and clamp position
            transform.position = normalizedPos * boundary;

            // Reduce velocity component moving away from center
            if (!rbody.isKinematic && Vector3.Dot(rbody.linearVelocity, normalizedPos) > 0)
            {
                // Reflect or dampen velocity
                rbody.linearVelocity = Vector3.ProjectOnPlane(rbody.linearVelocity, normalizedPos) * 0.5f;
            }
        }
    }

    public void SetLinearInput(Vector3 linearInput)
    {
        CurrentLinearInput = linearInput;
        appliedLinearForce = Vector3.Scale(linearInput, linearForce);

        // Apply reverse multiplier to longitudinal thrust if moving backwards
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

    /// <summary>
    /// Sets the input for how much of linearForce and angularForce are applied
    /// to the ship.
    /// </summary>
    public void SetPhysicsInput(Vector3 linearInput, Vector3 angularInput)
    {
        SetLinearInput(linearInput);
        SetAngularInput(angularInput);
    }

    /// <summary>
    /// Returns a Vector3 where each component of Vector A is multiplied by the equivalent component of Vector B.
    /// </summary>
    private Vector3 MultiplyByComponent(Vector3 a, Vector3 b)
    {
        Vector3 ret;

        ret.x = a.x * b.x;
        ret.y = a.y * b.y;
        ret.z = a.z * b.z;

        return ret;
    }
}