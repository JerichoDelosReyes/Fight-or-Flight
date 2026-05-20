using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-building circular radar shown at bottom-right.
/// Auto-creates itself in MainScene — no scene wiring required.
///
/// Enemies: red arrow (▲ = above player, ▼ = below, ◆ = same height) + distance.
/// Pickups: yellow dots (GameObjects tagged "Pickup").
/// Player : white ▲ at center, rotates with horizontal heading.
///
/// On Awake the script destroys any old green-square radar canvas that was
/// wired into the scene before this rewrite.
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

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<Radar>() != null) return;
        new GameObject("Radar").AddComponent<Radar>();
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private const float RadarDiameterPx  = 200f;   // UI size at 1920×1080 reference
    private const float RadarRange       = 10000f;  // world units shown at radar edge
    private const float MaxEnemiesShown  = 8;       // cap on concurrent enemy blips
    private const float HeightThreshold  = 10f;     // ±Y units to be considered "same height"

    // ── Runtime UI ────────────────────────────────────────────────────────────

    private RectTransform  dotContainer;
    private RectTransform  playerArrowRt;

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
        dotTex = MakeCircleTex(32,  Color.white, 0, Color.white);
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
        // Destroy any scene-wired canvas or panel from the legacy green-square radar.
        string[] suspects = { "RadarContainer", "RadarPanel", "RadarCanvas", "Radar Panel",
                              "MiniMap", "Minimap", "RadarRoot" };
        foreach (string n in suspects)
        {
            var g = GameObject.Find(n);
            if (g != null && g != gameObject) Destroy(g);
        }

        // Also nuke any canvases parented to this object (old scene setup)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != "RadarCanvas")
                Destroy(child.gameObject);
        }
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGo = new GameObject("RadarCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Root — anchored bottom-right
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

        // Dot / label container (center-anchored, clips via position math — no Mask needed)
        var dcGo = new GameObject("DotContainer");
        dcGo.transform.SetParent(rootGo.transform, false);
        dotContainer = dcGo.AddComponent<RectTransform>();
        dotContainer.anchorMin = dotContainer.anchorMax = new Vector2(0.5f, 0.5f);
        dotContainer.sizeDelta        = new Vector2(RadarDiameterPx, RadarDiameterPx);
        dotContainer.anchoredPosition = Vector2.zero;

        // Player arrow
        var arrowGo = new GameObject("PlayerArrow");
        arrowGo.transform.SetParent(rootGo.transform, false);
        playerArrowRt = arrowGo.AddComponent<RectTransform>();
        playerArrowRt.anchorMin = playerArrowRt.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrowRt.sizeDelta        = new Vector2(20f, 20f);
        playerArrowRt.anchoredPosition = Vector2.zero;

        var arrowTxt = arrowGo.AddComponent<Text>();
        arrowTxt.text      = "▲";
        arrowTxt.font      = uiFont;
        arrowTxt.fontSize  = 16;
        arrowTxt.color     = Color.white;
        arrowTxt.fontStyle = FontStyle.Bold;
        arrowTxt.alignment = TextAnchor.MiddleCenter;
        arrowTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        arrowTxt.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    // ── Radar Update ──────────────────────────────────────────────────────────

    private void UpdateRadar()
    {
        ReturnAllDots();

        Vector3 playerPos = Ship.PlayerShip.transform.position;

        // Rotate player arrow (horizontal yaw only)
        Vector3 fwd   = Ship.PlayerShip.transform.forward;
        float   angle = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        playerArrowRt.localEulerAngles = new Vector3(0f, 0f, -angle);

        // ── Enemies ───────────────────────────────────────────────────────────
        // Collect, sort by distance, clamp to MaxEnemiesShown
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
                // Pool item was a dot (RawImage) — can't reuse as label; return it and get fresh
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
            rt.sizeDelta = new Vector2(50f, 28f);
            txt = go.AddComponent<Text>();
            txt.font      = uiFont;
            txt.fontSize  = 9;
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
        // Only reuse pool items that have RawImage (dot), not Text (label)
        while (dotPool.Count > 0)
        {
            var rt = dotPool.Pop();
            if (rt.GetComponent<RawImage>() != null)
            {
                rt.gameObject.SetActive(true);
                return rt;
            }
            dotPool.Push(rt); break; // leave mismatched item, fall through to create
        }

        var go = new GameObject("Dot");
        go.transform.SetParent(dotContainer, false);
        var r = go.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(8f, 8f);
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
