using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

/// <summary>
/// Full-screen colour flash overlay. Self-creates on first Trigger call.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    private static ScreenFlash _instance;
    private Image     _overlay;
    private Coroutine _active;

    public static void Trigger(Color colour, float duration)
    {
        if (_instance == null)
        {
            var go = new GameObject("ScreenFlash");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ScreenFlash>();
            _instance.BuildOverlay();
        }
        if (_instance._active != null)
            _instance.StopCoroutine(_instance._active);
        _instance._active = _instance.StartCoroutine(_instance.DoFlash(colour, duration));
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    private void BuildOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("FlashOverlay");
        imgGo.transform.SetParent(transform, false);
        _overlay = imgGo.AddComponent<Image>();
        var rt = _overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _overlay.color = new Color(1f, 1f, 1f, 0f);
        _overlay.raycastTarget = false;
    }

    public static void Clear()
    {
        if (_instance != null && _instance._overlay != null)
            _instance._overlay.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator DoFlash(Color colour, float duration)
    {
        if (duration <= 0f) { _overlay.color = new Color(1f, 1f, 1f, 0f); yield break; }
        colour.a = 0.45f;
        _overlay.color = colour;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // works even when time is paused
            Color c = _overlay.color;
            c.a = Mathf.Lerp(0.45f, 0f, elapsed / duration);
            _overlay.color = c;
            yield return null;
        }
        _overlay.color = new Color(1f, 1f, 1f, 0f);
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
