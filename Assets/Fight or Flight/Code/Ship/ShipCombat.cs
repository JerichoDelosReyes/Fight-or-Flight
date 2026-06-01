using System.Collections;
using UnityEngine;

public class ShipCombat : MonoBehaviour
{
    public GameObject laserPrefab;
    public float fireRate = 0.15f;
    public Transform[] firePoints;

    [Header("Heat System")]
    public float heat = 0f;
    public float heatPerShot = 0.2f;
    public float coolingRate = 0.5f;
    public bool isOverheated = false;
    public float overheatThreshold = 1.0f;

    [Header("Audio")]
    public AudioClip laserShotSound;
    public float laserShotVolume = 1f;

    private float _lastFireTime;
    private float _nextFireTime;

    private void Update()
    {
        if (heat > 0)
        {
            heat -= coolingRate * Time.deltaTime;
            if (heat < 0) heat = 0;
        }

        if (isOverheated && heat <= 0) isOverheated = false;

        bool fireInput = Input.GetKey(KeyCode.Space);
        if (ControlSchemeManager.IsMouseKeyboard)
            fireInput |= Input.GetMouseButton(0);

        if (fireInput && Time.time >= _nextFireTime && !isOverheated)
        {
            FireLasers();
            _nextFireTime = Time.time + fireRate;
            _lastFireTime = Time.time;

            if (laserShotSound != null)
                AudioSource.PlayClipAtPoint(laserShotSound, transform.position, laserShotVolume);

            // Tiny camera shake — adds weight to firing without being distracting.
            ScreenShake.Trigger(0.05f, 0.4f);

            heat += heatPerShot;
            if (heat >= overheatThreshold)
            {
                isOverheated = true;
                heat = overheatThreshold;
            }
        }
    }

    private void FireLasers()
    {
        if (laserPrefab == null)
        {
            Debug.LogWarning("ShipCombat: Laser Prefab is not assigned!");
            return;
        }

        if (firePoints == null || firePoints.Length == 0)
        {
            // Try to find any child transforms whose name contains "FirePoint" (case-insensitive)
            var children = GetComponentsInChildren<Transform>(true);
            var found = new System.Collections.Generic.List<Transform>();
            foreach (var t in children)
            {
                if (t == this.transform) continue;
                if (t.name.ToLower().Contains("firepoint"))
                    found.Add(t);
            }

            if (found.Count > 0)
            {
                firePoints = found.ToArray();
            }
            else
            {
                Debug.LogWarning("ShipCombat: No fire points assigned and couldn't find children named like 'FirePoint' (e.g. FirePoint_L). ");
                return;
            }
        }

        // Use the camera's forward direction for ALL lasers.
        // This ensures they all fly in a perfectly straight, parallel stream.
        Vector3 shotDirection = transform.forward;
        if (Camera.main != null)
        {
            shotDirection = Camera.main.transform.forward;
        }
        
        Quaternion shotRotation = Quaternion.LookRotation(shotDirection);

        foreach (Transform point in firePoints)
        {
            if (point == null) continue;

            // Instantiate at the wing, but fire in the universal forward direction.
            GameObject laser = Instantiate(laserPrefab, point.position, shotRotation);
            laser.transform.SetParent(null, true);

            ShipLaserProjectile script = laser.GetComponent<ShipLaserProjectile>();
            if (script != null)
            {
                script.targetTag = "Enemy";
                script.Initialize(shotDirection);
            }
        }
    }
}