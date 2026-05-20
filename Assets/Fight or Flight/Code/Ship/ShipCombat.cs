using UnityEngine;

public class ShipCombat : MonoBehaviour
{
    public GameObject laserPrefab;
    public float fireRate = 0.15f;
    public Transform[] firePoints; // Changed to public and renamed for immediate visibility

    [Header("Heat System")]
    public float heat = 0f;
    public float heatPerShot = 0.2f;
    public float coolingRate = 0.5f;
    public bool isOverheated = false;
    public float overheatThreshold = 1.0f;

    [Header("Audio")]
    public AudioClip laserShotSound;

    private float _nextFireTime;

    private void Update()
    {
        if (heat > 0)
        {
            heat -= coolingRate * Time.deltaTime;
            if (heat < 0) heat = 0;
        }

        if (isOverheated && heat <= 0) isOverheated = false;

        // Space always fires. Left mouse fires only in Mouse+Keyboard mode so a stray
        // click in keyboard-only mode (e.g. clicking off-screen UI) doesn't shoot.
        bool fireInput = Input.GetKey(KeyCode.Space);
        if (ControlSchemeManager.IsMouseKeyboard)
            fireInput |= Input.GetMouseButton(0);

        if (fireInput && Time.time >= _nextFireTime && !isOverheated)
        {
            FireLasers();
            _nextFireTime = Time.time + fireRate;

            if (laserShotSound != null)
AudioSource.PlayClipAtPoint(laserShotSound, transform.position, 0.5f);
            
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
                string names = "";
                for (int i = 0; i < found.Count; i++)
                {
                    if (i > 0) names += ", ";
                    names += found[i].name;
                }
                Debug.Log("ShipCombat: Automatically found and assigned fire points: " + names);
            }
            else
            {
                Debug.LogWarning("ShipCombat: No fire points assigned and couldn't find children named like 'FirePoint' (e.g. FirePoint_L). ");
                return;
            }
        }

        foreach (Transform point in firePoints)
        {
            if (point == null) continue;

            Vector3 shotDirection = point.forward;
            Quaternion shotRotation = point.rotation;

            GameObject laser = Instantiate(laserPrefab, point.position, shotRotation);
            // Ensure the instantiated prefab is not parented to anything (safety)
            laser.transform.SetParent(null);
            ShipLaserProjectile script = laser.GetComponent<ShipLaserProjectile>();
            if (script != null)
            {
                script.targetTag = "Enemy";
                script.Initialize(shotDirection);
            }
        }
    }
}