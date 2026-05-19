using UnityEngine;

public class ShipCombat : MonoBehaviour
{
    [SerializeField] private GameObject _laserPrefab;
    [SerializeField] private float _fireRate = 0.2f;
    [SerializeField] private Vector3 _leftFireOffset = new Vector3(-50f, 0, 2f);
    [SerializeField] private Vector3 _rightFireOffset = new Vector3(50f, 0, 2f);

    private float _nextFireTime;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && Time.time >= _nextFireTime)
        {
            FireLasers();
            _nextFireTime = Time.time + _fireRate;
        }
    }

    private void FireLasers()
    {
        if (_laserPrefab == null) return;

        // Fire Left
        Vector3 leftPos = transform.TransformPoint(_leftFireOffset);
        Instantiate(_laserPrefab, leftPos, transform.rotation);

        // Fire Right
        Vector3 rightPos = transform.TransformPoint(_rightFireOffset);
        Instantiate(_laserPrefab, rightPos, transform.rotation);
    }
}
