using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-building circular radar shown at bottom-right.
/// Enemies: red dots. Pickups: yellow dots (if any exist with tag "Pickup").
/// Player: white triangle at center that rotates with horizontal facing.
/// Auto-creates itself in MainScene — no scene wiring required.
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

    private const float RadarDiameterPx = 200f; // UI size at 1920×1080
    private const float RadarRange      = 10000f; // world units → radar edge
    private const float DotSizePx       = 10f;
    private const float PlayerArrowSize = 18f;

    // ── Runtime UI ────────────────────────────────────────────────────────────

    private RectTransform  dotContainer;
    private RectTransform  playerArrowRt;
    private readonly List<RectTransform> activeDots = new List<RectTransform>();
    private readonly Stack<RectTransform> dotPool   = new Stack<RectTransform>();

    // Cached textures — created once, reused.
    private Texture2D bgTex;
    private Texture2D dotTex;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        bgTex  = MakeCircleTex(256, new Color(0f, 0f, 0f, 0.55f),
                               8, new Color(0.6f, 0.6f, 0.6f, 0.9f));
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
        if (Ship.PlayerShip == null) return;
        UpdateRadar();
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas — sits above HUD elements
        var canvasGo = new GameObject("RadarCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        float half = RadarDiameterPx * 0.5f;

        // Root container — anchored bottom-right
        var rootGo = new GameObject("RadarRoot");
        rootGo.transform.SetParent(canvasGo.transform, false);
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.anchorMin        = new Vector2(1f, 0f);
        rootRt.anchorMax        = new Vector2(1f, 0f);
        rootRt.pivot            = new Vector2(1f, 0f);
        rootRt.anchoredPosition = new Vector2(-18f, 18f);
        rootRt.sizeDelta        = new Vector2(RadarDiameterPx + 20f, RadarDiameterPx + 20f);

        // Background circle
        var bgGo = new GameObject("BgCircle");
        bgGo.transform.SetParent(rootGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta        = new Vector2(RadarDiameterPx, RadarDiameterPx);
        bgRt.anchoredPosition = Vector2.zero;
        var bgImg = bgGo.AddComponent<RawImage>();
        bgImg.texture = bgTex;

        // Dot container (centered, same size as the circle — no mask needed; dots clamp to circle)
        var dotContGo = new GameObject("DotContainer");
        dotContGo.transform.SetParent(rootGo.transform, false);
        dotContainer = dotContGo.AddComponent<RectTransform>();
        dotContainer.anchorMin = dotContainer.anchorMax = new Vector2(0.5f, 0.5f);
        dotContainer.sizeDelta        = new Vector2(RadarDiameterPx, RadarDiameterPx);
        dotContainer.anchoredPosition = Vector2.zero;

        // Player arrow ("▲" text rotates with ship's horizontal heading)
        var arrowGo = new GameObject("PlayerArrow");
        arrowGo.transform.SetParent(rootGo.transform, false);
        playerArrowRt = arrowGo.AddComponent<RectTransform>();
        playerArrowRt.anchorMin = playerArrowRt.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrowRt.sizeDelta        = new Vector2(PlayerArrowSize * 2f, PlayerArrowSize * 2f);
        playerArrowRt.anchoredPosition = Vector2.zero;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var arrowText = arrowGo.AddComponent<Text>();
        arrowText.text      = "▲";
        arrowText.font      = font;
        arrowText.fontSize  = (int)PlayerArrowSize;
        arrowText.color     = Color.white;
        arrowText.fontStyle = FontStyle.Bold;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.horizontalOverflow = HorizontalWrapMode.Overflow;
        arrowText.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    // ── Radar Update ──────────────────────────────────────────────────────────

    private void UpdateRadar()
    {
        ReturnAllDots();

        Vector3 playerPos = Ship.PlayerShip.transform.position;

        // Rotate player arrow to match horizontal facing (yaw only)
        Vector3 fwd   = Ship.PlayerShip.transform.forward;
        float   angle = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        playerArrowRt.localEulerAngles = new Vector3(0f, 0f, -angle);

        // Enemy dots (red)
        foreach (var enemy in EnemyAI.allEnemies)
        {
            if (enemy == null) continue;
            PlaceDot(ToRadarPos(enemy.transform.position, playerPos),
                     new Color(1f, 0.18f, 0.18f, 1f));
        }

        // Pickup dots — scan for any GameObjects tagged "Pickup"
        // (cheap enough at low pickup counts; can be replaced with a static list if needed)
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
        var     pos  = new Vector2(diff.x / RadarRange * half, diff.z / RadarRange * half);

        // Clamp to the radar circle edge so off-range objects sit on the perimeter.
        float limit = half - DotSizePx * 0.5f - 1f;
        if (pos.magnitude > limit)
            pos = pos.normalized * limit;

        return pos;
    }

    private void PlaceDot(Vector2 radarPos, Color colour)
    {
        RectTransform dot;
        if (dotPool.Count > 0)
        {
            dot = dotPool.Pop();
            dot.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(dotContainer, false);
            dot = go.AddComponent<RectTransform>();
            dot.sizeDelta = new Vector2(DotSizePx, DotSizePx);
            var img = go.AddComponent<RawImage>();
            img.texture = dotTex;
        }

        dot.GetComponent<RawImage>().color = colour;
        dot.anchoredPosition = radarPos;
        activeDots.Add(dot);
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

            Color c;
            if      (d > r)  c = Color.clear;
            else if (d > br) c = border;
            else             c = fill;

            tex.SetPixel(x, y, c);
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}
