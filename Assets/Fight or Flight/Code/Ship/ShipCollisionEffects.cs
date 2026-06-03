using UnityEngine;
using System.Collections;

public class ShipCollisionEffects : MonoBehaviour
{
    [SerializeField] public Transform visualsTransform;
    [SerializeField] private float _bumpIntensity = 5f;
    [SerializeField] private float _bumpDuration = 0.3f;

    private Vector3 _originalLocalPos;
    private bool _isBumping = false;

    private void Start()
    {
        if (visualsTransform == null)
        {
            visualsTransform = transform.Find("Ship_Body_Spaceship_1_0");
        }

        if (visualsTransform != null)
            _originalLocalPos = visualsTransform.localPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        string objName = collision.gameObject.name.ToLower();
        if ((objName.Contains("asteroid") || objName.Contains("rock")) && !_isBumping)
        {
            StartCoroutine(BumpCoroutine());
        }
    }

    private IEnumerator BumpCoroutine()
    {
        if (visualsTransform == null) yield break;

        _isBumping = true;
        float elapsed = 0f;

        while (elapsed < _bumpDuration)
        {
            elapsed += Time.deltaTime;
            float strength = 1f - (elapsed / _bumpDuration);

            visualsTransform.localPosition = _originalLocalPos + Random.insideUnitSphere * _bumpIntensity * strength;

            yield return null;
        }

        visualsTransform.localPosition = _originalLocalPos;
        _isBumping = false;
    }
}
