using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-building circular radar shown at bottom-left.
/// Auto-creates itself in MainScene — no scene wiring required.
///
/// Map rotates around a fixed player triangle so world-north is always consistent.
/// Compass labels (N/E/S/W) are inside the rotating map and track with it.
/// A heading strip at the top of the screen shows the player's facing direction.
///
/// Enemies: red arrow (▲=above, ▼=below, ◆=same height) + distance.
/// Pickups: yellow dots (GameObjects tagged "Pickup").
/// Player : white ▲ at centre, always pointing up (map rotates around it).
/// </summary>
public class Radar : MonoBehaviour
{
    // ── Auto-creation ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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
        if (Object.FindAnyObjectByType<Radar>() != null) return;
        new GameObject("Radar").AddComponent<Radar>();
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private const float RadarDiameterPx  = 280f;
    private const float RadarRange       = 10000f;
    private const float MaxEnemiesShown  = 8;
    private const float HeightThreshold  = 10f;

    // Heading strip
    private const float StripWidth   = 420f;
    private const float StripHeight  = 28f;

    // ── Runtime UI ────────────────────────────────────────────────────────────

    private RectTransform  dotContainer;   // rotates with the map
    private RectTransform  playerArrowRt;  // stays fixed, always points up

    // Heading strip labels (N, E, S, W repeated for wrap)
    private Text[] headingLabels;
    private float[] headingAngles;
    private RectTransform headingMarker;

    private readonly List<RectTransform>  activeDots = new List<RectTransform>();
    private readonly Stack<RectTransform> dotPool    = new Stack<RectTransform>();

    private Texture2D bgTex;
    private Texture2D dotTex;
    private Font      uiFont;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        DestroyOldRadarUI();
    }

    private void Start()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bgTex  = MakeCircleTex(256, new Color(0f, 0f, 0f, 0.60f), 6, new Color(0.7f, 0.7f, 0.7f, 0.95f));
        dotTex = MakeCircleTex(32, Color.white, 0, Color.white);
        BuildUI();
    }

    private void OnDestroy()
    {
        if (bgTex  != null) Destroy(bgTex);
        if (dotTex != null) Destroy(dotTex);
    }

    private void Update()
    {
        if (Ship.PlayerShip != null) UpdateRadar();
    }

    // ── Old Radar Cleanup ─────────────────────────────────────────────────────

    private void DestroyOldRadarUI()
    {
        string[] suspects = {
            "RadarContainer", "RadarPanel", "RadarCanvas", "Radar Panel",
            "MiniMap", "Minimap", "RadarRoot",
            "MapBackground", "GreenPanel", "GreenSquare", "MapPanel",
            "RadarBackground", "OldRadar", "OldMiniMap"
        };
        foreach (string n in suspects)
        {
            var g = GameObject.Find(n);
            if (g != null && g != gameObject) Destroy(g);
        }

        // Also nuke any canvases parented to this object (old scene setup)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != "RadarCanvas" && child.name != "HeadingStripCanvas")
                Destroy(child.gameObject);
        }
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        BuildRadarCircle();
        // Heading strip moved to its own component — CompassBar.cs (top-centre).
    }

    private void BuildRadarCircle()
    {
        var canvasGo = new GameObject("RadarCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Root — anchored BOTTOM-RIGHT
        var rootGo = new GameObject("RadarRoot");
        rootGo.transform.SetParent(canvasGo.transform, false);
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.anchorMin        = new Vector2(1f, 0f);
        rootRt.anchorMax        = new Vector2(1f, 0f);
        rootRt.pivot            = new Vector2(1f, 0f);
        rootRt.anchoredPosition = new Vector2(-18f, 18f);
        rootRt.sizeDelta        = new Vector2(RadarDiameterPx + 20f, RadarDiameterPx + 20f);

        // Circular background
        var bgGo = new GameObject("BgCircle");
        bgGo.transform.SetParent(rootGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta        = new Vector2(RadarDiameterPx, RadarDiameterPx);
        bgRt.anchoredPosition = Vector2.zero;
        bgGo.AddComponent<RawImage>().texture = bgTex;

        // Dot / label container — this one ROTATES with the map
        var dcGo = new GameObject("DotContainer");
        dcGo.transform.SetParent(rootGo.transform, false);
        dotContainer = dcGo.AddComponent<RectTransform>();
        dotContainer.anchorMin = dotContainer.anchorMax = new Vector2(0.5f, 0.5f);
        dotContainer.sizeDelta        = new Vector2(RadarDiameterPx, RadarDiameterPx);
        dotContainer.anchoredPosition = Vector2.zero;

        // Compass labels inside the rotating container
        float labelRadius = RadarDiameterPx * 0.5f - 18f;
        AddCompassLabel(dcGo.transform, "N",  new Vector2(0f,  labelRadius));
        AddCompassLabel(dcGo.transform, "E",  new Vector2( labelRadius, 0f));
        AddCompassLabel(dcGo.transform, "S",  new Vector2(0f, -labelRadius));
        AddCompassLabel(dcGo.transform, "W",  new Vector2(-labelRadius, 0f));

        // Player arrow — fixed, always pointing up
        var arrowGo = new GameObject("PlayerArrow");
        arrowGo.transform.SetParent(rootGo.transform, false);
        playerArrowRt = arrowGo.AddComponent<RectTransform>();
        playerArrowRt.anchorMin = playerArrowRt.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrowRt.sizeDelta        = new Vector2(44f, 44f);
        playerArrowRt.anchoredPosition = Vector2.zero;
        playerArrowRt.localEulerAngles = Vector3.zero; // never rotated

        var arrowTxt = arrowGo.AddComponent<Text>();
        arrowTxt.text      = "▲";
        arrowTxt.font      = uiFont;
        arrowTxt.fontSize  = 36;
        arrowTxt.color     = Color.white;
        arrowTxt.fontStyle = FontStyle.Bold;
        arrowTxt.alignment = TextAnchor.MiddleCenter;
        arrowTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        arrowTxt.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    private void AddCompassLabel(Transform parent, string text, Vector2 pos)
    {
        var go = new GameObject("Compass_" + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(32f, 24f);
        rt.anchoredPosition = pos;

        var txt = go.AddComponent<Text>();
        txt.text      = text;
        txt.font      = uiFont;
        txt.fontSize  = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = new Color(1f, 1f, 1f, 0.95f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    private void BuildHeadingStrip()
    {
        var canvasGo = new GameObject("HeadingStripCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 121;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Strip background panel — top-center
        var stripGo = new GameObject("HeadingStrip");
        stripGo.transform.SetParent(canvasGo.transform, false);
        var stripRt = stripGo.AddComponent<RectTransform>();
        stripRt.anchorMin        = new Vector2(0.5f, 1f);
        stripRt.anchorMax        = new Vector2(0.5f, 1f);
        stripRt.pivot            = new Vector2(0.5f, 1f);
        stripRt.anchoredPosition = new Vector2(0f, -8f);
        stripRt.sizeDelta        = new Vector2(StripWidth, StripHeight);

        var stripBg = stripGo.AddComponent<Image>();
        stripBg.color = new Color(0f, 0f, 0f, 0.55f);

        // Mask the strip so labels clip at the edges
        stripGo.AddComponent<Mask>().showMaskGraphic = true;

        // Label container inside the strip (slides left/right as heading changes)
        var labelContainerGo = new GameObject("LabelContainer");
        labelContainerGo.transform.SetParent(stripGo.transform, false);
        var lcRt = labelContainerGo.AddComponent<RectTransform>();
        lcRt.anchorMin = lcRt.anchorMax = new Vector2(0.5f, 0.5f);
        lcRt.sizeDelta        = new Vector2(StripWidth * 3f, StripHeight); // wide for wrap
        lcRt.anchoredPosition = Vector2.zero;

        // N=0°, E=90°, S=180°, W=270° — repeated twice for seamless wrap
        string[] names   = { "N", "E", "S", "W", "N", "E", "S", "W" };
        float[]  angles  = { 0f, 90f, 180f, 270f, 360f, 450f, 540f, 630f };

        headingLabels = new Text[names.Length];
        headingAngles = angles;

        for (int i = 0; i < names.Length; i++)
        {
            var lgo = new GameObject("H_" + names[i] + i);
            lgo.transform.SetParent(labelContainerGo.transform, false);
            var lrt = lgo.AddComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta        = new Vector2(28f, StripHeight);
            lrt.anchoredPosition = Vector2.zero; // set each frame

            var txt = lgo.AddComponent<Text>();
            txt.text      = names[i];
            txt.font      = uiFont;
            txt.fontSize  = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = names[i] == "N"
                ? new Color(1f, 0.4f, 0.4f, 1f)   // red N for easy spotting
                : new Color(0.9f, 0.9f, 0.9f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;

            headingLabels[i] = txt;
        }

        // Centre tick mark
        var tickGo = new GameObject("CentreTick");
        tickGo.transform.SetParent(stripGo.transform, false);
        var tickRt = tickGo.AddComponent<RectTransform>();
        tickRt.anchorMin = tickRt.anchorMax = new Vector2(0.5f, 1f);
        tickRt.pivot            = new Vector2(0.5f, 1f);
        tickRt.anchoredPosition = Vector2.zero;
        tickRt.sizeDelta        = new Vector2(2f, 10f);
        tickGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
    }

    // ── Radar Update ──────────────────────────────────────────────────────────

    private void UpdateRadar()
    {
        ReturnAllDots();

        Vector3 playerPos = Ship.PlayerShip.transform.position;
        Vector3 fwd       = Ship.PlayerShip.transform.forward;

        // Player yaw: angle from world north (+Z)
        float angle = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;

        // Rotate the MAP (dotContainer) so world-north tracks correctly.
        // Player arrow stays fixed pointing up.
        if (dotContainer != null)
            dotContainer.localEulerAngles = new Vector3(0f, 0f, angle);

        // ── Enemies ───────────────────────────────────────────────────────────
        var enemies = new List<EnemyAI>(EnemyAI.allEnemies.Count);
        foreach (var e in EnemyAI.allEnemies)
            if (e != null) enemies.Add(e);

        enemies.Sort((a, b) =>
            Vector3.Distance(a.transform.position, playerPos)
                .CompareTo(Vector3.Distance(b.transform.position, playerPos)));

        int shown = Mathf.Min(enemies.Count, (int)MaxEnemiesShown);
        for (int i = 0; i < shown; i++)
        {
            Transform et   = enemies[i].transform;
            float     dist = Vector3.Distance(et.position, playerPos);
            float     dy   = et.position.y - playerPos.y;

            string symbol = dy > HeightThreshold ? "▲" : dy < -HeightThreshold ? "▼" : "◆";
            string label  = symbol + "\n" + Mathf.RoundToInt(dist / 100f);

            PlaceLabel(ToRadarPos(et.position, playerPos), label,
                       new Color(1f, 0.18f, 0.18f, 1f));
        }

        // ── Pickups ───────────────────────────────────────────────────────────
        var pickups = GameObject.FindGameObjectsWithTag("Pickup");
        foreach (var p in pickups)
        {
            if (p == null) continue;
            PlaceDot(ToRadarPos(p.transform.position, playerPos),
                     new Color(1f, 0.95f, 0.2f, 1f));
        }

        // Heading strip removed — CompassBar provides this now.
    }

    private void UpdateHeadingStrip(float playerYawDeg)
    {
        if (headingLabels == null) return;

        // px per degree along the strip
        float pxPerDeg = StripWidth / 90f; // 90° fills the full strip width

        for (int i = 0; i < headingLabels.Length; i++)
        {
            if (headingLabels[i] == null) continue;
            var rt = headingLabels[i].GetComponent<RectTransform>();

            // Angular offset of this label from player heading, wrapped to [-180, 180]
            float delta = Mathf.DeltaAngle(playerYawDeg, headingAngles[i]);
            float x     = delta * pxPerDeg;

            rt.anchoredPosition = new Vector2(x, 0f);
            // Fade out labels near the edges
            float alpha = Mathf.Clamp01(1f - Mathf.Abs(x) / (StripWidth * 0.5f + 10f));
            headingLabels[i].color = new Color(
                headingLabels[i].color.r,
                headingLabels[i].color.g,
                headingLabels[i].color.b,
                alpha);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 ToRadarPos(Vector3 worldPos, Vector3 playerPos)
    {
        Vector3 diff = worldPos - playerPos;
        float   half = RadarDiameterPx * 0.5f;
        var     pos  = new Vector2(diff.x / RadarRange * half,
                                   diff.z / RadarRange * half);
        float limit = half - 6f;
        if (pos.magnitude > limit) pos = pos.normalized * limit;
        return pos;
    }

    private void PlaceDot(Vector2 radarPos, Color colour)
    {
        var dot = AcquireDot();
        dot.GetComponent<RawImage>().color = colour;
        dot.anchoredPosition = radarPos;
        activeDots.Add(dot);
    }

    private void PlaceLabel(Vector2 radarPos, string text, Color colour)
    {
        RectTransform rt;
        Text          txt;

        if (dotPool.Count > 0)
        {
            rt  = dotPool.Pop();
            txt = rt.GetComponent<Text>();
            if (txt == null)
            {
                dotPool.Push(rt);
                rt = null; txt = null;
            }
        }
        else { rt = null; txt = null; }

        if (rt == null)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(dotContainer, false);
            rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 44f);
            txt = go.AddComponent<Text>();
            txt.font      = uiFont;
            txt.fontSize  = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
        }

        rt.gameObject.SetActive(true);
        txt.text  = text;
        txt.color = colour;
        rt.anchoredPosition = radarPos;
        activeDots.Add(rt);
    }

    private RectTransform AcquireDot()
    {
        while (dotPool.Count > 0)
        {
            var rt = dotPool.Pop();
            if (rt.GetComponent<RawImage>() != null)
            {
                rt.gameObject.SetActive(true);
                return rt;
            }
            dotPool.Push(rt); break;
        }

        var go = new GameObject("Dot");
        go.transform.SetParent(dotContainer, false);
        var r = go.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(14f, 14f);
        var img = go.AddComponent<RawImage>();
        img.texture = dotTex;
        return r;
    }

    private void ReturnAllDots()
    {
        foreach (var d in activeDots)
        {
            d.gameObject.SetActive(false);
            dotPool.Push(d);
        }
        activeDots.Clear();
    }

    // ── Texture Helpers ───────────────────────────────────────────────────────

    private static Texture2D MakeCircleTex(int size, Color fill, int borderPx, Color border)
    {
        var   tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float r   = size * 0.5f;
        float br  = r - borderPx;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            Color c  = d > r ? Color.clear : d > br ? border : fill;
            tex.SetPixel(x, y, c);
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}
