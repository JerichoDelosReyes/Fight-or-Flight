using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that floats above each enemy ship.
/// Added programmatically by EnemyAI.Start() — no prefab changes required.
/// Hidden while at full health; fades in on first hit.
/// Billboard: canvas faces the camera every frame.
/// </summary>
[RequireComponent(typeof(ShipHealth))]
public class EnemyHealthBar : MonoBehaviour
{
    // ── Config ────────────────────────────────────────────────────────────────

    private const float BarWidth     = 220f;   // world-unit width of the bar rect
    private const float BarHeight    = 18f;
    private const float YOffset      = 280f;   // height above the ship's transform origin
    private const float CanvasScale  = 0.5f;   // scales the canvas in world space

    // ── Runtime ───────────────────────────────────────────────────────────────

    private ShipHealth  health;
    private Canvas      canvas;
    private Transform   canvasRoot;
    private Image       fill;
    private CanvasGroup group;

    private float maxHealthAtSpawn;
    private bool  everDamaged;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        health          = GetComponent<ShipHealth>();
        maxHealthAtSpawn = health != null ? health.maxHealth : 100f;
        BuildUI();
    }

    private void LateUpdate()
    {
        if (health == null || canvasRoot == null) return;
        if (Camera.main == null) return;

        // Lazy-assign worldCamera so the canvas sorts correctly against 3D objects
        if (canvas != null && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        // Billboard: align canvas to camera's orientation
        canvasRoot.rotation = Camera.main.transform.rotation;

        float fraction = Mathf.Clamp01(health.currentHealth / Mathf.Max(1f, health.maxHealth));

        // Reveal on first damage
        if (!everDamaged && health.currentHealth < health.maxHealth)
            everDamaged = true;

        group.alpha = everDamaged ? 1f : 0f;

        // Scale fill bar horizontally
        fill.rectTransform.localScale = new Vector3(fraction, 1f, 1f);
        fill.color = Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(0.1f, 0.9f, 0.1f), fraction);
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // World-space canvas parented to this ship
        var canvasGo = new GameObject("HealthBarCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, YOffset, 0f);
        canvasGo.transform.localScale    = Vector3.one * CanvasScale;
        canvasRoot = canvasGo.transform;

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.WorldSpace;
        canvas.worldCamera    = Camera.main; // assign now; re-assigned lazily in LateUpdate if null
        canvas.overrideSorting = true;
        canvas.sortingOrder   = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        group       = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        // Root rect — centred
        var rootRt = canvasGo.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(BarWidth + 20f, BarHeight + 30f);

        // "DRONE" label above the bar
        var labelGo = new GameObject("TypeLabel");
        labelGo.transform.SetParent(canvasGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0f, BarHeight * 0.5f + 12f);
        labelRt.sizeDelta        = new Vector2(BarWidth, 20f);

        var labelTxt = labelGo.AddComponent<Text>();
        labelTxt.text      = "DRONE";
        labelTxt.font      = font;
        labelTxt.fontSize  = 14;
        labelTxt.color     = new Color(0.85f, 0.85f, 0.85f);
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelTxt.verticalOverflow   = VerticalWrapMode.Overflow;

        // Bar background (dark)
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot             = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition  = new Vector2(0f, -4f);
        bgRt.sizeDelta         = new Vector2(BarWidth, BarHeight);
        bgGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        // Fill bar — left-pivoted so it shrinks from the right
        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(canvasGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = fillRt.anchorMax = new Vector2(0.5f, 0.5f);
        fillRt.pivot              = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition   = new Vector2(-BarWidth * 0.5f, -4f);
        fillRt.sizeDelta          = new Vector2(BarWidth, BarHeight - 2f);

        fill       = fillGo.AddComponent<Image>();
        fill.color = new Color(0.1f, 0.9f, 0.1f);
    }
}
