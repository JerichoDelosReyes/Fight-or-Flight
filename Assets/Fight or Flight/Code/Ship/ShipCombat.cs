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

        bool fireInput = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");

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
        if (laserPrefab == null || firePoints == null || firePoints.Length == 0) return;

        foreach (Transform point in firePoints)
        {
            if (point == null) continue;
            GameObject laser = Instantiate(laserPrefab, point.position, point.rotation);
            var script = laser.GetComponent<ShipLaserProjectile>();
            if (script != null)
            {
                script.targetTag = "Enemy";
                script.Initialize(Vector3.zero);
            }
        }
    }
}