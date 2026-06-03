using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CompassBar : MonoBehaviour
{

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


    private const float BarWidth  = 920f;
    private const float BarHeight = 52f;
    private const float VisibleSpanDeg = 110f;
    private const int   TickStepDeg    = 5;
    private const int   DegLabelStepDeg = 30;


    private RectTransform _labelContainer;
    private Text          _headingNum;

    private struct Tick
    {
        public RectTransform rt;
        public float angle;
        public bool   isCardinal;
    }
    private readonly List<Tick> _ticks = new List<Tick>();


    private void Start()
    {
        Build();
    }

    private void Update()
    {
        if (Ship.PlayerShip == null || _labelContainer == null) return;

        Vector3 fwd = Ship.PlayerShip.transform.forward;
        float yaw = Mathf.Repeat(Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg, 360f);

        float pxPerDeg = BarWidth / VisibleSpanDeg;
        float halfSpan = VisibleSpanDeg * 0.5f;

        for (int i = 0; i < _ticks.Count; i++)
        {
            var t = _ticks[i];
            float delta = Mathf.DeltaAngle(yaw, t.angle);
            if (Mathf.Abs(delta) > halfSpan + 6f)
            {
                t.rt.gameObject.SetActive(false);
                continue;
            }
            t.rt.gameObject.SetActive(true);
            t.rt.anchoredPosition = new Vector2(delta * pxPerDeg, 0f);

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

        var barGo = new GameObject("CompassBarBG");
        barGo.transform.SetParent(canvasGo.transform, false);
        var barRt = barGo.AddComponent<RectTransform>();
        barRt.anchorMin        = new Vector2(0.5f, 1f);
        barRt.anchorMax        = new Vector2(0.5f, 1f);
        barRt.pivot            = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -10f);
        barRt.sizeDelta        = new Vector2(BarWidth, BarHeight);
        var barImg = barGo.AddComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0f);
        barGo.AddComponent<RectMask2D>();

var lc = new GameObject("LabelContainer");
        lc.transform.SetParent(barGo.transform, false);
        _labelContainer = lc.AddComponent<RectTransform>();
        _labelContainer.anchorMin = _labelContainer.anchorMax = new Vector2(0.5f, 0.5f);
        _labelContainer.anchoredPosition = Vector2.zero;
        _labelContainer.sizeDelta        = new Vector2(BarWidth, BarHeight);

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
                    ? new Color(1f, 0.45f, 0.45f, 1f)
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
            else
            {
                AddMinorTick(deg);
                continue;
            }

            AddTextTick(deg, text, fontSize, col, isCardinal);
        }

        var ctickGo = new GameObject("CentreTick");
        ctickGo.transform.SetParent(barGo.transform, false);
        var ctickRt = ctickGo.AddComponent<RectTransform>();
        ctickRt.anchorMin = ctickRt.anchorMax = new Vector2(0.5f, 0.5f);
        ctickRt.pivot            = new Vector2(0.5f, 0.5f);
        ctickRt.anchoredPosition = Vector2.zero;
        ctickRt.sizeDelta        = new Vector2(2f, BarHeight);
        ctickGo.AddComponent<Image>().color = new Color(0.6f, 0.85f, 1f, 0.9f);

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
        _headingNum.color     = new Color(0.6f, 0.85f, 1f, 1f);
        _headingNum.alignment = TextAnchor.MiddleCenter;
        _headingNum.text      = "000°";
_headingNum.horizontalOverflow = HorizontalWrapMode.Overflow;
        _headingNum.verticalOverflow   = VerticalWrapMode.Overflow;

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
