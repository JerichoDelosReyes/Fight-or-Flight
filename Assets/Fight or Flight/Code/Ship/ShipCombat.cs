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
                AudioSource.PlayClipAtPoint(laserShotSound, transform.position, 0.5f);

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

        // Calculate aim target based on screen center (the crosshair).
        Vector3 aimTarget;
        if (Camera.main != null)
        {
            Ray camRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            // Converge at a distance (e.g., 2000 units) if nothing is hit, or hit something.
            if (Physics.Raycast(camRay, out RaycastHit hit, 15000f))
                aimTarget = hit.point;
            else
                aimTarget = camRay.origin + camRay.direction * 15000f;
        }
        else
        {
            aimTarget = transform.position + transform.forward * 15000f;
        }

        foreach (Transform point in firePoints)
        {
            if (point == null) continue;

            // Fired towards the aim target so it actually hits the crosshair.
            Vector3 shotDirection = (aimTarget - point.position).normalized;
            Quaternion shotRotation = Quaternion.LookRotation(shotDirection);

            GameObject laser = Instantiate(laserPrefab, point.position, shotRotation);
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