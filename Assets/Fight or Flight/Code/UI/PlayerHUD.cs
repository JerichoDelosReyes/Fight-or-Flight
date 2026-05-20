using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-building HUD panel at the bottom-left of the screen.
/// Shows HEALTH, SHIELD, and AMMO bars with exact values displayed inside each bar.
/// Low-health warning: health bar pulses red when below 30%.
/// Auto-creates itself in MainScene — no scene wiring required.
/// </summary>
public class PlayerHUD : MonoBehaviour
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
        if (Object.FindAnyObjectByType<PlayerHUD>() != null) return;
        new GameObject("PlayerHUD").AddComponent<PlayerHUD>();
    }

    // ── Layout config ─────────────────────────────────────────────────────────

    private const float PanelW  = 320f;
    private const float PanelH  = 160f;
    private const float MarginX = 18f;
    // Anchored to the bottom-left corner (radar now lives at bottom-right).
    private const float MarginY = 18f;

    private const float BarW    = 230f;
    private const float BarH    = 22f;
    private const float LabelW  = 68f;
    private const float RowGap  = 36f;

    private static readonly Color PanelBG     = new Color(0f,    0f,    0f,    0.70f);
    private static readonly Color HealthFull  = new Color(0.15f, 0.85f, 0.15f, 1f);
    private static readonly Color HealthEmpty = new Color(0.85f, 0.10f, 0.10f, 1f);
    private static readonly Color HealthLow   = new Color(0.95f, 0.05f, 0.05f, 1f);
    private static readonly Color ShieldFull  = new Color(0f,    0.75f, 1f,    1f);
    private static readonly Color ShieldEmpty = new Color(0f,    0.20f, 0.55f, 1f);
    private static readonly Color AmmoFull    = new Color(1f,    0.88f, 0.10f, 1f);
    private static readonly Color AmmoEmpty   = new Color(0.45f, 0.30f, 0f,    1f);
    private static readonly Color BarBG       = new Color(0.08f, 0.08f, 0.08f, 0.92f);

    // ── Runtime refs ──────────────────────────────────────────────────────────

    private Image        _healthFill,  _shieldFill,  _ammoFill;
    private Text         _healthVal,   _shieldVal,   _ammoVal;
    private Text         _reloadText;
    private Image        _panelBg;

    private ShipHealth   _health;
    private ShipCombat   _combat;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildHUD(font);
    }

    private void Update()
    {
        if (Ship.PlayerShip == null) return;

        if (_health == null) _health = Ship.PlayerShip.GetComponent<ShipHealth>();
        if (_combat == null) _combat = Ship.PlayerShip.GetComponent<ShipCombat>();

        if (_health != null)
        {
            float hFrac = Mathf.Clamp01(_health.currentHealth / Mathf.Max(1f, _health.maxHealth));
            float sFrac = Mathf.Clamp01(_health.currentShield  / Mathf.Max(1f, _health.maxShield));

            // Low HP pulse: bar flashes between red and darker red
            Color hCol;
            if (hFrac < 0.30f)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                hCol = Color.Lerp(HealthLow, new Color(0.5f, 0f, 0f, 1f), pulse);
            }
            else
            {
                hCol = Color.Lerp(HealthEmpty, HealthFull, hFrac);
            }

            if (_healthFill != null) { _healthFill.fillAmount = hFrac; _healthFill.color = hCol; }
            if (_healthVal  != null) _healthVal.text = (int)_health.currentHealth + " / " + (int)_health.maxHealth;

            if (_shieldFill != null) { _shieldFill.fillAmount = sFrac; _shieldFill.color = Color.Lerp(ShieldEmpty, ShieldFull, sFrac); }
            if (_shieldVal  != null) _shieldVal.text = (int)_health.currentShield + " / " + (int)_health.maxShield;

            // Pulse panel border red at low HP
            if (_panelBg != null)
            {
                if (hFrac < 0.30f)
                {
                    float p = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.25f;
                    _panelBg.color = new Color(p, 0f, 0f, 0.70f);
                }
                else
                {
                    _panelBg.color = PanelBG;
                }
            }
        }

        if (_combat != null)
        {
            float aFrac = _combat.maxAmmo > 0 ? Mathf.Clamp01((float)_combat.ammoCount / _combat.maxAmmo) : 0f;
            if (_ammoFill != null) { _ammoFill.fillAmount = aFrac; _ammoFill.color = Color.Lerp(AmmoEmpty, AmmoFull, aFrac); }
            if (_ammoVal  != null) _ammoVal.text = _combat.isReloading ? "RELOADING..." : _combat.ammoCount + " / " + _combat.maxAmmo;
        }
    }

    // ── HUD Construction ──────────────────────────────────────────────────────

    private void BuildHUD(Font font)
    {
        var canvasGo = new GameObject("PlayerHUDCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 115;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel — bottom-left, sits at the very corner
        var panelGo = new GameObject("HUDPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(0f, 0f);
        panelRt.pivot            = new Vector2(0f, 0f);
        panelRt.anchoredPosition = new Vector2(MarginX, MarginY);
        panelRt.sizeDelta        = new Vector2(PanelW, PanelH);
        _panelBg = panelGo.AddComponent<Image>();
        _panelBg.color = PanelBG;

        float firstRowY = PanelH - 44f;  // top row y-offset from panel bottom

        // Row 0 — HEALTH
        AddLabel(panelGo.transform, font, "HEALTH", 10f, firstRowY, HealthFull);
        (_healthFill, _healthVal) = AddBarRow(panelGo.transform, font, firstRowY, BarH, HealthFull);

        // Row 1 — SHIELD
        float r1y = firstRowY - RowGap;
        AddLabel(panelGo.transform, font, "SHIELD", 10f, r1y, ShieldFull);
        (_shieldFill, _shieldVal) = AddBarRow(panelGo.transform, font, r1y, BarH - 4f, ShieldFull);

        // Row 2 — AMMO
        float r2y = r1y - RowGap;
        AddLabel(panelGo.transform, font, "AMMO", 10f, r2y, AmmoFull);
        (_ammoFill, _ammoVal) = AddBarRow(panelGo.transform, font, r2y, BarH, AmmoFull);

        // Reload indicator — reuses the ammo value text (text changes to "RELOADING...")
        _reloadText = _ammoVal; // same text object, just changes content
    }

    private void AddLabel(Transform parent, Font font, string text, float x, float y, Color col)
    {
        var go = new GameObject("Lbl_" + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y + BarH * 0.5f);
        rt.sizeDelta        = new Vector2(LabelW, 20f);
        var t = go.AddComponent<Text>();
        t.text      = text;
        t.font      = font;
        t.fontSize  = 14;
        t.fontStyle = FontStyle.Bold;
        t.color     = col;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    // Returns (fillImage, valueText-inside-bar)
    private (Image, Text) AddBarRow(Transform parent, Font font, float yBase, float h, Color initialCol)
    {
        float barX = LabelW + 14f; // starts right after the label

        // Background
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(parent, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0f, 0f);
        bgRt.pivot            = new Vector2(0f, 0f);
        bgRt.anchoredPosition = new Vector2(barX, yBase);
        bgRt.sizeDelta        = new Vector2(BarW, h);
        bgGo.AddComponent<Image>().color = BarBG;

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        var fill = fillGo.AddComponent<Image>();
        fill.type       = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        fill.color      = initialCol;

        // Value text INSIDE the bar (centred)
        var valGo = new GameObject("Val");
        valGo.transform.SetParent(bgGo.transform, false);
        var valRt = valGo.AddComponent<RectTransform>();
        valRt.anchorMin = Vector2.zero;
        valRt.anchorMax = Vector2.one;
        valRt.offsetMin = valRt.offsetMax = Vector2.zero;
        var val = valGo.AddComponent<Text>();
        val.text      = "—";
        val.font      = font;
        val.fontSize  = 13;
        val.fontStyle = FontStyle.Bold;
        val.color     = Color.white;
        val.alignment = TextAnchor.MiddleCenter;
        val.horizontalOverflow = HorizontalWrapMode.Overflow;
        val.verticalOverflow   = VerticalWrapMode.Overflow;

        return (fill, val);
    }
}
