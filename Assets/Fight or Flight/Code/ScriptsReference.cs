using UnityEngine;

/// <summary>
/// Centralized reference for game mechanics, movement constants, and boundaries.
/// </summary>
public static class ScriptsReference
{
    // Boundary Settings
    public const float BoundaryLimit = 24000f; // Hard kill plane — ship gets clamped here
    /// <summary>
    /// Visible arena radius: asteroid wall lives here, player/enemy boundary warnings trigger.
    /// With MaxSpeed=1500 it takes ~8 s to cross. Adjust to taste — must be &lt; BoundaryLimit.
    /// </summary>
    public const float ArenaRadius = 12000f;

    // Ship Movement Settings
    public static readonly Vector3 DefaultLinearForce = new Vector3(500f, 500f, 3000f);
    public static readonly Vector3 DefaultAngularForce = new Vector3(300f, 300f, 150f);
    public const float DefaultForceMultiplier = 250f;

    // Boundary Mechanics
    public const float BoundaryBounceForce = 10f; // Optional: factor for keeping ship in bounds

    // Input Tuning
    public const float MouseSensitivity = 1.2f;
    public const float MouseDeadzone = 0.1f;
    public const float ThrottleSpeed = 0.5f;
    public const float MaxSpeed = 2500f;
    }
