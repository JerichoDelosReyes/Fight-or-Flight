using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-building sci-fi HUD panel at the bottom-left of the screen.
/// Shows health bar, shield bar, and ammo counter with icons and exact values.
/// Auto-creates itself in MainScene — no scene wiring required.
/// Sits above the Radar circle.
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

    // ── Config ────────────────────────────────────────────────────────────────

    private const float PanelW       = 300f;
    private const float PanelH       = 130f;
    private const float PanelMarginX = 18f;
    // Sits above the radar (200px diameter + 20px padding + 18px bottom margin + 8px gap)
    private const float PanelBottomY = 200f + 20f + 18f + 8f;

    private static readonly Color PanelBG     = new Color(0f,    0f,    0f,    0.65f);
    private static readonly Color HealthFull  = new Color(0.2f,  0.9f,  0.2f,  1f);
    private static readonly Color HealthEmpty = new Color(0.9f,  0.1f,  0.1f,  1f);
    private static readonly Color ShieldColor = new Color(0f,    0.75f, 1f,    1f);
    private static readonly Color ShieldEmpty = new Color(0f,    0.25f, 0.6f,  1f);
    private static readonly Color AmmoColor   = new Color(1f,    0.9f,  0.15f, 1f);
    private static readonly Color AmmoEmpty   = new Color(0.5f,  0.35f, 0f,    1f);
    private static readonly Color DarkBar     = new Color(0.08f, 0.08f, 0.08f, 0.9f);

    // ── Runtime refs ──────────────────────────────────────────────────────────

    private Image  _healthFill,  _shieldFill,  _ammoFill;
    private Text   _healthText,  _shieldText,  _ammoText;
    private Text   _reloadText;

    private ShipHealth  _health;
    private ShipCombat  _combat;

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
            SetBar(_healthFill, _healthText,
                   _health.currentHealth, _health.maxHealth,
                   HealthFull, HealthEmpty,
                   (int)_health.currentHealth + " / " + (int)_health.maxHealth);

            SetBar(_shieldFill, _shieldText,
                   _health.currentShield, _health.maxShield,
                   ShieldColor, ShieldEmpty,
                   (int)_health.currentShield + " / " + (int)_health.maxShield);
        }

        if (_combat != null)
        {
            SetBar(_ammoFill, _ammoText,
                   _combat.ammoCount, _combat.maxAmmo,
                   AmmoColor, AmmoEmpty,
                   _combat.ammoCount + " / " + _combat.maxAmmo);

            if (_reloadText != null)
                _reloadText.gameObject.SetActive(_combat.isReloading);
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

        // Root panel
        var panelGo = new GameObject("HUDPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(0f, 0f);
        panelRt.pivot            = new Vector2(0f, 0f);
        panelRt.anchoredPosition = new Vector2(PanelMarginX, PanelBottomY);
        panelRt.sizeDelta        = new Vector2(PanelW, PanelH);
        panelGo.AddComponent<Image>().color = PanelBG;

        float rowH   = 30f;
        float barW   = 200f;
        float barH   = 12f;
        float iconX  = 12f;
        float barX   = 70f;
        float valX   = barX + barW + 6f;

        // Row 0: Health
        float y0 = PanelH - 30f;
        AddLabel(panelGo.transform, font, "HP", iconX, y0, HealthFull);
        var (hFill, hText) = AddBar(panelGo.transform, font, barX, y0, barW, barH);
        _healthFill = hFill;
        _healthText = hText;

        // Row 1: Shield
        float y1 = y0 - rowH - 4f;
        AddLabel(panelGo.transform, font, "SH", iconX, y1, ShieldColor);
        var (sFill, sText) = AddBar(panelGo.transform, font, barX, y1, barW, barH);
        _shieldFill = sFill;
        _shieldText = sText;

        // Row 2: Ammo
        float y2 = y1 - rowH - 4f;
        AddLabel(panelGo.transform, font, "AMO", iconX, y2, AmmoColor);
        var (aFill, aText) = AddBar(panelGo.transform, font, barX, y2, barW, barH);
        _ammoFill = aFill;
        _ammoText = aText;

        // Reload indicator (yellow, right side of ammo row)
        var reloadGo = new GameObject("ReloadText");
        reloadGo.transform.SetParent(panelGo.transform, false);
        var reloadRt = reloadGo.AddComponent<RectTransform>();
        reloadRt.anchorMin = reloadRt.anchorMax = new Vector2(0f, 0f);
        reloadRt.pivot            = new Vector2(0f, 0.5f);
        reloadRt.anchoredPosition = new Vector2(barX + barW + 8f, y2 + barH * 0.5f);
        reloadRt.sizeDelta        = new Vector2(80f, 18f);
        _reloadText = reloadGo.AddComponent<Text>();
        _reloadText.text      = "RELOAD";
        _reloadText.font      = font;
        _reloadText.fontSize  = 11;
        _reloadText.fontStyle = FontStyle.Bold;
        _reloadText.color     = new Color(1f, 0.85f, 0.1f, 1f);
        _reloadText.alignment = TextAnchor.MiddleLeft;
        _reloadText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _reloadText.verticalOverflow   = VerticalWrapMode.Overflow;
        reloadGo.SetActive(false);
    }

    private void AddLabel(Transform parent, Font font, string text, float x, float y, Color colour)
    {
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y + 6f);
        rt.sizeDelta        = new Vector2(52f, 18f);

        var txt = go.AddComponent<Text>();
        txt.text      = text;
        txt.font      = font;
        txt.fontSize  = 12;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = colour;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    // Returns (fillImage, valueText)
    private (Image fill, Text valueText) AddBar(Transform parent, Font font,
                                                 float x, float y, float w, float h)
    {
        // Background
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(parent, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0f, 0f);
        bgRt.pivot            = new Vector2(0f, 0f);
        bgRt.anchoredPosition = new Vector2(x, y);
        bgRt.sizeDelta        = new Vector2(w, h);
        bgGo.AddComponent<Image>().color = DarkBar;

        // Fill (Image.Type.Filled, Horizontal)
        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        fillImg.color      = HealthFull; // updated each frame

        // Value text to the right
        var txtGo = new GameObject("BarValue");
        txtGo.transform.SetParent(parent, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = txtRt.anchorMax = new Vector2(0f, 0f);
        txtRt.pivot            = new Vector2(0f, 0.5f);
        txtRt.anchoredPosition = new Vector2(x + w + 6f, y + h * 0.5f);
        txtRt.sizeDelta        = new Vector2(80f, 16f);
        var txt = txtGo.AddComponent<Text>();
        txt.text      = "—";
        txt.font      = font;
        txt.fontSize  = 11;
        txt.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;

        return (fillImg, txt);
    }

    private static void SetBar(Image fill, Text label, float cur, float max,
                                Color fullCol, Color emptyCol, string valueStr)
    {
        if (fill == null) return;
        float frac = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
        fill.fillAmount = frac;
        fill.color      = Color.Lerp(emptyCol, fullCol, frac);
        if (label != null) label.text = valueStr;
    }
}
