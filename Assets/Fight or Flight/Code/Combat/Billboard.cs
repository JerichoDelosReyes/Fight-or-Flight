using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _camTransform;

    private void Start()
    {
        if (Camera.main != null)
            _camTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (_camTransform != null)
        {
            transform.LookAt(transform.position + _camTransform.forward);
        }
    }
}
