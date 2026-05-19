using UnityEngine;

public class ShipCombat : MonoBehaviour
{
    [SerializeField] private GameObject _laserPrefab;
    [SerializeField] private float _fireRate = 0.15f;
    [SerializeField] private Vector3 _leftFireOffset = new Vector3(-15f, 0, 10f);
    [SerializeField] private Vector3 _rightFireOffset = new Vector3(15f, 0, 10f);

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
        // Cooling
        if (heat > 0)
        {
            heat -= coolingRate * Time.deltaTime;
            if (heat < 0) heat = 0;
        }

        if (isOverheated)
        {
            if (heat <= 0) isOverheated = false;
        }

        // Check for spacebar or Fire1
        bool fireInput = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");

        if (fireInput && Time.time >= _nextFireTime && !isOverheated)
        {
            FireLasers();
            _nextFireTime = Time.time + _fireRate;

            // Audio feedback
            if (laserShotSound != null)
            {
                AudioSource.PlayClipAtPoint(laserShotSound, transform.position, 0.5f);
            }
            
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
        if (_laserPrefab == null) return;

        // Calculate a target point in the distance based on where the ship is aiming
        float aimDistance = 1000f;
        Vector3 targetPoint = transform.position + transform.forward * aimDistance;

        // Try to find the HUD aim point (prioritize mouse crosshair for cursor aiming)
        HUDController hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            RectTransform targetCrosshair = hud.mouseCrosshair != null ? hud.mouseCrosshair : hud.fixedCrosshair;
            
            if (targetCrosshair != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(targetCrosshair.position);
                if (Physics.Raycast(ray, out RaycastHit hit, aimDistance))
                {
                    targetPoint = hit.point;
                }
                else
                {
                    targetPoint = ray.GetPoint(aimDistance);
                }
            }
        }

        // Fire Left
        SpawnLaser(_leftFireOffset, targetPoint);
        // Fire Right
        SpawnLaser(_rightFireOffset, targetPoint);
        }

        private void SpawnLaser(Vector3 offset, Vector3 targetPoint)
        {
        Vector3 pos = transform.TransformPoint(offset);
        // Rotate laser to point at targetPoint
        Quaternion rot = Quaternion.LookRotation(targetPoint - pos);
        
        GameObject laser = Instantiate(_laserPrefab, pos, rot);
        
        var script = laser.GetComponent<ShipLaserProjectile>();
        if (script != null)
        {
            script.targetTag = "Enemy";
            
            Rigidbody rb = GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                script.Initialize(rb.linearVelocity);
            }
        }
        }
}
