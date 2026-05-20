using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floats a score "+N" text upward and fades it out, then self-destructs.
/// Attached by ScoreHUD when the player's score increases.
/// </summary>
public class ScorePopupFloat : MonoBehaviour
{
    [Tooltip("Lifetime in seconds (uses unscaled time so it works under pause).")]
    public float lifetime = 1.2f;

    [Tooltip("Total vertical travel during lifetime, in reference-resolution pixels.")]
    public float riseDistance = 60f;

    private RectTransform _rt;
    private Text _txt;
    private Vector2 _basePos;
    private float _t;

    private void Awake()
    {
        _rt   = GetComponent<RectTransform>();
        _txt  = GetComponent<Text>();
        if (_rt != null) _basePos = _rt.anchoredPosition;
    }

    private void Update()
    {
        if (_rt == null || _txt == null) return;
        _t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_t / lifetime);

        // Top-right anchor + top-right pivot → positive Y is upward on screen.
        _rt.anchoredPosition = _basePos + new Vector2(0f, riseDistance * k);

        Color c = _txt.color;
        c.a = 1f - k;
        _txt.color = c;

        if (k >= 1f) Destroy(gameObject);
    }
}
