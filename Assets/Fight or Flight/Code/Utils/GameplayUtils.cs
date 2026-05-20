using UnityEngine;
using System.Collections.Generic;

public class ScreenShake : MonoBehaviour
{
    private static ScreenShake instance;
    private float shakeTime;
    private float shakeIntensity;
    private Vector3 originalPos;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        originalPos = transform.localPosition;
    }

    public static void Trigger(float duration, float intensity)
    {
        if (instance != null)
        {
            instance.shakeTime = duration;
            instance.shakeIntensity = intensity;
        }
    }

    private void Update()
    {
        if (shakeTime > 0)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * shakeIntensity;
            shakeTime -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }
}

public class ScoreManager : MonoBehaviour
{
    public static int Score { get; private set; }
    public static int Kills { get; private set; }

    private void OnEnable()
    {
        GameEventManager.OnIncrementScore += AddScore;
        GameEventManager.OnStartGame += ResetScore;
    }

    private void OnDisable()
    {
        GameEventManager.OnIncrementScore -= AddScore;
        GameEventManager.OnStartGame -= ResetScore;
    }

    /// <summary>Add points only — does NOT increment the kill counter. Use for pickups.</summary>
    public static void AddScore(int amount)
    {
        Score += amount;
    }

    /// <summary>Add points AND increment the kill counter. Use when an enemy is destroyed.</summary>
    public static void AddKillScore(int amount)
    {
        Score += amount;
        Kills++;
    }

    public static void ResetScore()
    {
        Score = 0;
        Kills = 0;
    }
}
