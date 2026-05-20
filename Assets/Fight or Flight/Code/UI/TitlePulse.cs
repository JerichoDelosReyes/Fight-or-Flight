using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pulses the brightness of a UI Text and adds a soft outline that fades in
/// and out on the same beat. Used by the main-menu "FIGHT OR FLIGHT" title.
///
/// Pulsing uses unscaled time so it keeps animating even on pause / menus.
/// </summary>
[RequireComponent(typeof(Text))]
public class TitlePulse : MonoBehaviour
{
    [Tooltip("Pulses per second (full bright + full glow cycle).")]
    public float pulseRate = 1.3f;

    [Tooltip("Range of brightness multiplier — 1.0 = base, 1.x = brighter peak.")]
    public float brightnessAmplitude = 0.20f;

    [Tooltip("Maximum outline alpha at the peak of the pulse.")]
    public float glowMaxAlpha = 0.70f;

    private Text _txt;
    private Color _baseColor;
    private Outline _outline;

    private void Awake()
    {
        _txt = GetComponent<Text>();
        if (_txt != null) _baseColor = _txt.color;

        _outline = GetComponent<Outline>();
        if (_outline == null) _outline = gameObject.AddComponent<Outline>();
        _outline.effectDistance = new Vector2(3f, -3f);
        _outline.effectColor    = new Color(0.4f, 0.7f, 1f, 0f);
    }

    private void Update()
    {
        if (_txt == null) return;

        // Smooth 0..1 oscillation.
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseRate * Mathf.PI * 2f);

        // Brightness pulse on the text itself.
        float mul = 1f + (t - 0.5f) * brightnessAmplitude * 2f;
        Color c = _baseColor;
        c.r = Mathf.Clamp01(_baseColor.r * mul);
        c.g = Mathf.Clamp01(_baseColor.g * mul);
        c.b = Mathf.Clamp01(_baseColor.b * mul);
        _txt.color = c;

        // Outline alpha follows the same pulse.
        if (_outline != null)
        {
            var oc = _outline.effectColor;
            oc.a = t * glowMaxAlpha;
            _outline.effectColor = oc;
        }
    }
}
