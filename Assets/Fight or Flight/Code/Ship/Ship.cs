//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;

/// <summary>
/// Ties all the primary ship components together.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShipPhysics))]
[RequireComponent(typeof(ShipInput))]
public class Ship : MonoBehaviour
{    
    public bool isPlayer = false;

    private ShipInput input;
    private ShipPhysics physics;    

    // Keep a static reference for whether or not this is the player ship. It can be used
    // by various gameplay mechanics. Returns the player ship if possible, otherwise null.
    public static Ship PlayerShip { get { return playerShip; } }
    private static Ship playerShip;

    // Getters for external objects to reference things like input.
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
        // Pass the input to the physics to move the ship.
        physics.SetPhysicsInput(new Vector3(input.strafe, 0.0f, input.throttle), new Vector3(input.pitch, input.yaw, input.roll));

        // Adjust rumble pitch/volume based on throttle
        if (rumbleSource != null)
        {
            float absThrottle = Mathf.Abs(input.throttle);
            
            // If throttle is pressed, ensure the sound is playing
            if (absThrottle > 0.01f && !rumbleSource.isPlaying)
            {
                rumbleSource.Play();
            }

            rumbleSource.pitch = 0.5f + absThrottle * 0.7f;
            rumbleSource.volume = absThrottle * 0.6f;
            }

        // If this is the player ship, then set the static reference.If more than one ship
// is set to player, then whatever happens to be the last ship to be updated will be
        // considered the player. Don't let this happen.
        if (isPlayer)
            playerShip = this;
    }
}
