using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PUBG / Warzone style compass bar.
/// Thin horizontal strip across the top-centre of the screen.
/// Shows tick marks every 5°, degree numbers every 30°, cardinals (N/E/S/W)
/// and intercardinals (NE/NW/SE/SW) with larger text. The labels slide left
/// and right as the player rotates so the player's current heading is always
/// at the centre. The current heading (e.g. "127°") is displayed under the
/// centre tick.
///
/// Auto-creates itself in MainScene — no scene wiring required.
/// </summary>
public class CompassBar : MonoBehaviour
{
    // ── Auto-creation ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode) => TryCreate(scene);

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<CompassBar>() != null) return;
        new GameObject("CompassBar").AddComponent<CompassBar>();
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float BarWidth  = 920f;
    private const float BarHeight = 52f;
    private const float VisibleSpanDeg = 110f;   // total degrees visible across the bar
    private const int   TickStepDeg    = 5;      // tick mark every N degrees
    private const int   DegLabelStepDeg = 30;    // numeric label every N degrees

    // ── Runtime ───────────────────────────────────────────────────────────────

    private RectTransform _labelContainer;
    private Text          _headingNum;

    // Cached label entries — one per angle, kept reusable for cheap per-frame
    // repositioning rather than rebuilding text every frame.
    private struct Tick
    {
        public RectTransform rt;
        public float angle;       // degrees, 0..360
        public bool   isCardinal; // larger fade-out region
    }
    private readonly List<Tick> _ticks = new List<Tick>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        Build();
    }

    private void Update()
    {
        if (Ship.PlayerShip == null || _labelContainer == null) return;

        Vector3 fwd = Ship.PlayerShip.transform.forward;
        // Heading: 0° = +Z (north), increases clockwise to 360°.
        float yaw = Mathf.Repeat(Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg, 360f);

        float pxPerDeg = BarWidth / VisibleSpanDeg;
        float halfSpan = VisibleSpanDeg * 0.5f;

        for (int i = 0; i < _ticks.Count; i++)
        {
            var t = _ticks[i];
            float delta = Mathf.DeltaAngle(yaw, t.angle); // (-180, 180]
            if (Mathf.Abs(delta) > halfSpan + 6f)
            {
                t.rt.gameObject.SetActive(false);
                continue;
            }
            t.rt.gameObject.SetActive(true);
            t.rt.anchoredPosition = new Vector2(delta * pxPerDeg, 0f);

            // Fade near the edges
            var txt = t.rt.GetComponent<Text>();
            if (txt != null)
            {
                float a = Mathf.Clamp01(1f - Mathf.Abs(delta) / halfSpan);
                Color c = txt.color; c.a = a; txt.color = c;
            }
        }

        if (_headingNum != null)
            _headingNum.text = Mathf.RoundToInt(yaw).ToString("000") + "°";
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void Build()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("CompassBarCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 118;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Bar background — top-centre
        var barGo = new GameObject("CompassBarBG");
        barGo.transform.SetParent(canvasGo.transform, false);
        var barRt = barGo.AddComponent<RectTransform>();
        barRt.anchorMin        = new Vector2(0.5f, 1f);
        barRt.anchorMax        = new Vector2(0.5f, 1f);
        barRt.pivot            = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -10f);
        barRt.sizeDelta        = new Vector2(BarWidth, BarHeight);
        var barImg = barGo.AddComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0.55f);
        barGo.AddComponent<Mask>().showMaskGraphic = true;

        // Sliding label container
        var lc = new GameObject("LabelContainer");
        lc.transform.SetParent(barGo.transform, false);
        _labelContainer = lc.AddComponent<RectTransform>();
        _labelContainer.anchorMin = _labelContainer.anchorMax = new Vector2(0.5f, 0.5f);
        _labelContainer.anchoredPosition = Vector2.zero;
        _labelContainer.sizeDelta        = new Vector2(BarWidth, BarHeight);

        // Generate ticks every TickStepDeg degrees, full 360°
        for (int deg = 0; deg < 360; deg += TickStepDeg)
        {
            bool isCardinal      = (deg % 90 == 0);
            bool isInterCardinal = (deg % 45 == 0) && !isCardinal;
            bool isDegLabel      = (deg % DegLabelStepDeg == 0) && !isCardinal && !isInterCardinal;
            bool isMinorTick     = !isCardinal && !isInterCardinal && !isDegLabel;

            string text;
            int    fontSize;
            Color  col;

            if (isCardinal)
            {
                text     = CardinalLetter(deg);
                fontSize = 30;
                col      = (deg == 0)
                    ? new Color(1f, 0.45f, 0.45f, 1f)   // red North
                    : new Color(1f, 1f, 1f, 1f);
            }
            else if (isInterCardinal)
            {
                text     = InterCardinalLetter(deg);
                fontSize = 22;
                col      = new Color(0.85f, 0.85f, 0.85f, 1f);
            }
            else if (isDegLabel)
            {
                text     = deg.ToString();
                fontSize = 16;
                col      = new Color(0.75f, 0.75f, 0.75f, 1f);
            }
            else // minor tick — vertical line, no text
            {
                AddMinorTick(deg);
                continue;
            }

            AddTextTick(deg, text, fontSize, col, isCardinal);
        }

        // Centre tick mark — vertical white line at exact centre
        var ctickGo = new GameObject("CentreTick");
        ctickGo.transform.SetParent(barGo.transform, false);
        var ctickRt = ctickGo.AddComponent<RectTransform>();
        ctickRt.anchorMin = ctickRt.anchorMax = new Vector2(0.5f, 0.5f);
        ctickRt.pivot            = new Vector2(0.5f, 0.5f);
        ctickRt.anchoredPosition = Vector2.zero;
        ctickRt.sizeDelta        = new Vector2(2f, BarHeight);
        ctickGo.AddComponent<Image>().color = new Color(1f, 0.85f, 0.2f, 0.9f);

        // Heading number under the centre tick
        var hnGo = new GameObject("HeadingNum");
        hnGo.transform.SetParent(canvasGo.transform, false);
        var hnRt = hnGo.AddComponent<RectTransform>();
        hnRt.anchorMin        = new Vector2(0.5f, 1f);
        hnRt.anchorMax        = new Vector2(0.5f, 1f);
        hnRt.pivot            = new Vector2(0.5f, 1f);
        hnRt.anchoredPosition = new Vector2(0f, -10f - BarHeight - 2f);
        hnRt.sizeDelta        = new Vector2(160f, 32f);
        _headingNum = hnGo.AddComponent<Text>();
        _headingNum.font      = font;
        _headingNum.fontSize  = 22;
        _headingNum.fontStyle = FontStyle.Bold;
        _headingNum.color     = new Color(1f, 0.85f, 0.2f, 1f);
        _headingNum.alignment = TextAnchor.MiddleCenter;
        _headingNum.text      = "000°";
        _headingNum.horizontalOverflow = HorizontalWrapMode.Overflow;
        _headingNum.verticalOverflow   = VerticalWrapMode.Overflow;

        // Build text ticks
        void AddTextTick(int deg, string text, int size, Color col, bool cardinal)
        {
            var go = new GameObject("T_" + deg);
            go.transform.SetParent(_labelContainer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(56f, BarHeight);
            rt.anchoredPosition = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.text      = text;
            txt.font      = font;
            txt.fontSize  = size;
            txt.fontStyle = cardinal ? FontStyle.Bold : FontStyle.Normal;
            txt.color     = col;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;

            _ticks.Add(new Tick { rt = rt, angle = deg, isCardinal = cardinal });
        }

        void AddMinorTick(int deg)
        {
            var go = new GameObject("Tick_" + deg);
            go.transform.SetParent(_labelContainer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(1f, BarHeight * 0.30f);
            rt.anchoredPosition = Vector2.zero;
            go.AddComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f, 0.7f);

            _ticks.Add(new Tick { rt = rt, angle = deg, isCardinal = false });
        }
    }

    private static string CardinalLetter(int deg)
    {
        switch (deg)
        {
            case 0:   return "N";
            case 90:  return "E";
            case 180: return "S";
            case 270: return "W";
            default:  return deg.ToString();
        }
    }

    private static string InterCardinalLetter(int deg)
    {
        switch (deg)
        {
            case 45:  return "NE";
            case 135: return "SE";
            case 225: return "SW";
            case 315: return "NW";
            default:  return deg.ToString();
        }
    }
}
