using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Explosion))]
public class Asteroid : MonoBehaviour
{
    public static List<Asteroid> AllAsteroids = new List<Asteroid>();

    [SerializeField] private float _minScale = 0.8f;
    [SerializeField] private float _maxScale = 1.2f;

    public static float destructionDelay = 1f;

    private void OnEnable()
    {
        AllAsteroids.Add(this);
    }

    private void OnDisable()
    {
        AllAsteroids.Remove(this);
    }

    private void Start()
{
        transform.localScale = GetRandomVector3(_minScale, _maxScale);
    }

    private Vector3 GetRandomVector3(float minRange, float maxRange)
    {
        var returnValue = Vector3.zero;
        returnValue.x = Random.Range(minRange, maxRange);
        returnValue.y = Random.Range(minRange, maxRange);
        returnValue.z = Random.Range(minRange, maxRange);
        return returnValue;
    }

    public void SelfDestruct()
    {
        var timer = Random.Range(0, destructionDelay);
        Invoke("BlowUp", timer);
    }

    private void BlowUp()
    {
        GetComponent<Explosion>().BlowUp();
    }

}