using UnityEngine;

public static class ScriptsReference
{
    public const float BoundaryLimit = 24000f;
    public const float ArenaRadius = 12000f;

    public static readonly Vector3 DefaultLinearForce = new Vector3(500f, 500f, 3000f);
    public static readonly Vector3 DefaultAngularForce = new Vector3(300f, 300f, 150f);
    public const float DefaultForceMultiplier = 250f;

    public const float BoundaryBounceForce = 10f;

    public const float MouseSensitivity = 1.2f;
    public const float MouseDeadzone = 0.1f;
    public const float ThrottleSpeed = 0.5f;
    public const float MaxSpeed = 2500f;
    }
