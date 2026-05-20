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

    [Header("Ammo")]
    public int  ammoCount  = 30;
    public int  maxAmmo    = 30;
    public bool isReloading = false;
    private float _lastFireTime;
    private const float ReloadDelay    = 2f;
    private const float ReloadDuration = 1f;

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

        // Auto-reload after ReloadDelay seconds of not firing
        if (!isReloading && ammoCount < maxAmmo && Time.time - _lastFireTime >= ReloadDelay)
            StartCoroutine(AutoReload());

        bool fireInput = Input.GetKey(KeyCode.Space);
        if (ControlSchemeManager.IsMouseKeyboard)
            fireInput |= Input.GetMouseButton(0);

        if (fireInput && Time.time >= _nextFireTime && !isOverheated && ammoCount > 0 && !isReloading)
        {
            FireLasers();
            _nextFireTime = Time.time + fireRate;
            _lastFireTime = Time.time;
            ammoCount--;

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

    private IEnumerator AutoReload()
    {
        isReloading = true;
        int startAmmo = ammoCount;
        float elapsed = 0f;
        while (elapsed < ReloadDuration)
        {
            elapsed    += Time.deltaTime;
            ammoCount   = startAmmo + Mathf.FloorToInt((maxAmmo - startAmmo) * (elapsed / ReloadDuration));
            yield return null;
        }
        ammoCount   = maxAmmo;
        isReloading = false;
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

        // Aim target derived from the camera's view — independent of fire-point rotation,
        // which may be incorrectly set up in the prefab.
        //
        // Mouse+KB: camera is locked at screen center = crosshair; cast from camera forward.
        // Keyboard:  use Ship.PlayerShip.transform.forward (same axis HUDController uses for
        //            the projected crosshair), not this.transform.forward which may differ.
        Vector3 aimTarget;
        if (ControlSchemeManager.IsMouseKeyboard && Camera.main != null)
        {
            Ray camRay = Camera.main.ScreenPointToRay(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            aimTarget = camRay.origin + camRay.direction * 15000f;
        }
        else
        {
            Ship ps = Ship.PlayerShip;
            Vector3 origin = ps != null ? ps.transform.position : transform.position;
            Vector3 fwd    = ps != null ? ps.transform.forward  : transform.forward;
            aimTarget = origin + fwd * 15000f;
        }

        foreach (Transform point in firePoints)
        {
            if (point == null) continue;

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