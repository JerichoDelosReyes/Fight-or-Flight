using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class TitlePulse : MonoBehaviour
{
    [Tooltip("Pulses per second (full bright + full glow cycle).")]
    public float pulseRate = 0.5f;

    [Tooltip("Range of brightness multiplier — 1.0 = base, 1.x = brighter peak.")]
    public float brightnessAmplitude = 0.1f;

    [Tooltip("Maximum outline alpha at the peak of the pulse.")]
    public float glowMaxAlpha = 0.2f;

    private Graphic _graphic;
    private Color _baseColor;
    private Outline _outline;

    private void Awake()
    {
        _graphic = GetComponent<Graphic>();
        if (_graphic != null) _baseColor = _graphic.color;

        _outline = GetComponent<Outline>();
        if (_outline == null) _outline = gameObject.AddComponent<Outline>();
        _outline.effectDistance = new Vector2(2f, -2f);
        _outline.effectColor    = new Color(0.4f, 0.7f, 1f, 0f);
    }

    private void Update()
    {
        if (_graphic == null) return;

        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseRate * Mathf.PI * 2f);

        float mul = 1f + (t - 0.5f) * brightnessAmplitude * 2f;
        Color c = _baseColor;
        c.r = Mathf.Clamp01(_baseColor.r * mul);
        c.g = Mathf.Clamp01(_baseColor.g * mul);
        c.b = Mathf.Clamp01(_baseColor.b * mul);
        _graphic.color = c;

        if (_outline != null)
        {
            var oc = _outline.effectColor;
            oc.a = t * glowMaxAlpha;
            _outline.effectColor = oc;
        }
    }
}
