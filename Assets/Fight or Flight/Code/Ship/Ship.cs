
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShipPhysics))]
[RequireComponent(typeof(ShipInput))]
public class Ship : MonoBehaviour
{
    public bool isPlayer = false;

    private ShipInput input;
    private ShipPhysics physics;

    public static Ship PlayerShip { get { return playerShip; } }
    private static Ship playerShip;

    public bool UsingMouseInput { get { return false; } }
public Vector3 Velocity { get { return physics.Rigidbody.linearVelocity; } }
    public float Throttle { get { return input.throttle; } }

    private void Awake()
    {
        input = GetComponent<ShipInput>();
        physics = GetComponent<ShipPhysics>();
    }

    public AudioClip engineRumbleSound;
    private AudioSource rumbleSource;

    private void Start()
    {
        if (engineRumbleSound != null)
        {
            rumbleSource = gameObject.AddComponent<AudioSource>();
            rumbleSource.clip = engineRumbleSound;
            rumbleSource.loop = true;
            rumbleSource.playOnAwake = true;
            rumbleSource.spatialBlend = 1.0f;
            rumbleSource.volume = 0.2f;
            rumbleSource.Play();
        }
    }

    private void Update()
    {
        physics.SetPhysicsInput(new Vector3(input.strafe, 0.0f, input.throttle), new Vector3(input.pitch, input.yaw, input.roll));

        if (rumbleSource != null)
        {
            float absThrottle = Mathf.Abs(input.throttle);

            if (absThrottle > 0.01f && !rumbleSource.isPlaying)
            {
                rumbleSource.Play();
            }

            rumbleSource.pitch = 0.5f + absThrottle * 0.7f;
            rumbleSource.volume = absThrottle * 0.6f;
            }

        if (isPlayer)
        {
            playerShip = this;
            CheckBoundaryWarning();
        }
    }

    private float lastBoundaryWarnTime = -999f;

    private void CheckBoundaryWarning()
    {
        float dist = transform.position.magnitude;
        float warnThreshold = ScriptsReference.BoundaryLimit * 0.85f;
        if (dist > warnThreshold && Time.time - lastBoundaryWarnTime > 1.2f)
        {
            ScreenShake.Trigger(0.18f, 2f);
            lastBoundaryWarnTime = Time.time;
        }
    }
}
