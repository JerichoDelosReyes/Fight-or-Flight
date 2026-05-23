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

    private const float PanelW  = 400f;
    private const float PanelH  = 280f;
    private const float MarginX = 30f;
    // Anchored to the bottom-left corner.
    private const float MarginY = -66f;

    private const float BarW    = 350f;
    private const float BarH    = 18f;
    private const float RowGap  = 60f;
    private const float IconSize = 40f;
    private const float IconGap  = 12f;

    private const string HealthIconPath = "Assets/Fight or Flight/Content/Materials/health.png";
    private const string ShieldIconPath = "Assets/Fight or Flight/Content/Materials/shield.png";
    private const string HeatIconPath   = "Assets/Fight or Flight/Content/Materials/heat.png";

    private static readonly Color PanelBG     = new Color(0f, 0f, 0f, 0f);
    private static readonly Color HealthFull  = new Color(0.15f, 0.85f, 0.15f, 1f);
    private static readonly Color HealthEmpty = new Color(0.85f, 0.10f, 0.10f, 1f);
    private static readonly Color HealthLow   = new Color(0.95f, 0.05f, 0.05f, 1f);
    private static readonly Color ShieldFull  = new Color(0f,    0.75f, 1f,    1f);
    private static readonly Color ShieldEmpty = new Color(0f,    0.20f, 0.55f, 1f);
    private static readonly Color HeatCool    = new Color(1f,    0.6f,  0.6f,  1f);
    private static readonly Color HeatHot     = new Color(1f,    0.0f,  0.0f,  1f);
    private static readonly Color BarBG       = new Color(0.15f, 0.15f, 0.15f, 0.8f);

    // ── Runtime refs ──────────────────────────────────────────────────────────

    private Image        _healthFill,  _shieldFill,  _heatFill;
    private Image        _panelBg;
    private Image        _heatBarBg;

    private ShipHealth   _health;
    private ShipCombat   _combat;

    // Damage / regen / low-ammo flash state
    private float _lastHealth = -1f;
    private float _lastShield = -1f;
    private float _damageFlashUntil;
    private float _shieldFullFlashUntil;
    private Vector2 _healthRowBaseAnchor;
    private RectTransform _healthRowRt;
    private RectTransform _shieldRowRt;
    private RectTransform _heatRowRt;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (transform.Find("PlayerHUDCanvas") != null)
        {
            FindReferencesInHierarchy();
        }
        else
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildHUD(font);
        }
    }

    private void FindReferencesInHierarchy()
    {
        Transform canvas = transform.Find("PlayerHUDCanvas");
        if (canvas == null) return;
        Transform panel = canvas.Find("HUDPanel");
        if (panel == null) return;
        _panelBg = panel.GetComponent<Image>();

        int barIndex = 0;
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (child.name == "BarBg")
            {
                Transform fillTrans = child.Find("Fill");
                Image fill = fillTrans != null ? fillTrans.GetComponent<Image>() : null;
                
                RectTransform rt = child.GetComponent<RectTransform>();

                if (barIndex == 0) { _healthFill = fill; _healthRowRt = rt; _healthRowBaseAnchor = rt.anchoredPosition; }
                else if (barIndex == 1) { _shieldFill = fill; _shieldRowRt = rt; }
                else if (barIndex == 2) { _heatFill = fill; _heatRowRt = rt; _heatBarBg = child.GetComponent<Image>(); }
                barIndex++;
            }
        }
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

            // Damage detection — set a brief flash window when health drops.
            if (_lastHealth >= 0f && _health.currentHealth < _lastHealth - 0.01f)
                _damageFlashUntil = Time.unscaledTime + 0.3f;
            _lastHealth = _health.currentHealth;

            // Shield-full flash — fires once on the frame shield refills to max.
            if (_lastShield >= 0f && _lastShield < _health.maxShield - 0.01f
                && _health.currentShield >= _health.maxShield - 0.01f)
            {
                _shieldFullFlashUntil = Time.unscaledTime + 0.5f;
            }
            _lastShield = _health.currentShield;

            bool damageFlash = Time.unscaledTime < _damageFlashUntil;

            // Low HP pulse: bar flashes between red and darker red
            Color hCol;
            if (damageFlash)
            {
                // Hard red flash — brighter than the regular pulse.
                hCol = new Color(1f, 0.15f, 0.15f, 1f);
            }
            else if (hFrac < 0.30f)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                hCol = Color.Lerp(HealthLow, new Color(0.5f, 0f, 0f, 1f), pulse);
            }
            else
            {
                hCol = Color.Lerp(HealthEmpty, HealthFull, hFrac);
            }

            // Shake the health row briefly on damage.
            if (_healthRowRt != null)
            {
                if (damageFlash)
                {
                    float remaining = (_damageFlashUntil - Time.unscaledTime) / 0.3f; // 0..1
                    Vector2 jitter = new Vector2(
                        (Random.value - 0.5f) * 6f * remaining,
                        (Random.value - 0.5f) * 6f * remaining);
                    _healthRowRt.anchoredPosition = _healthRowBaseAnchor + jitter;
                }
                else
                {
                    _healthRowRt.anchoredPosition = _healthRowBaseAnchor;
                }
            }

            if (_healthFill != null) { _healthFill.fillAmount = hFrac; _healthFill.color = hCol; }

            // Shield bar — normal interpolation + brief white/blue flash when fully restored.
            Color shieldCol = Color.Lerp(ShieldEmpty, ShieldFull, sFrac);
            if (Time.unscaledTime < _shieldFullFlashUntil)
            {
                float t = (_shieldFullFlashUntil - Time.unscaledTime) / 0.5f;
                shieldCol = Color.Lerp(shieldCol, Color.white, t);
            }
            if (_shieldFill != null) { _shieldFill.fillAmount = sFrac; _shieldFill.color = shieldCol; }

            // Pulse panel border red at low HP
            if (_panelBg != null)
            {
                if (hFrac < 0.30f)
                {
                    float p = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.25f;
                    _panelBg.color = new Color(p, 0f, 0f, 0.20f);
                }
                else
                {
                    _panelBg.color = PanelBG;
                }
            }
        }

        if (_combat != null)
        {
            float heatFrac = Mathf.Clamp01(_combat.heat / Mathf.Max(0.1f, _combat.overheatThreshold));

            Color heatCol = Color.Lerp(HeatCool, HeatHot, heatFrac);
            if (_combat.isOverheated)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f));
                heatCol = Color.Lerp(HeatHot, Color.white, pulse);
            }

            if (_heatFill != null) { _heatFill.fillAmount = heatFrac; _heatFill.color = heatCol; }

            if (_heatBarBg != null)
            {
                // It gets redder as the player heats up.
                // We transition from the default grey BarBG to a solid red.
                _heatBarBg.color = Color.Lerp(BarBG, new Color(1f, 0f, 0f, 0.8f), heatFrac);
            }
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

        float currentY = PanelH - 40f;

        // Row 0 — HEALTH
        (_healthFill, _healthRowRt) = AddBarRow(panelGo.transform, HealthIconPath, 0f, currentY, BarH, HealthFull);
        _healthRowBaseAnchor = _healthRowRt.anchoredPosition;

        // Row 1 — SHIELD
        currentY -= RowGap;
        (_shieldFill, _shieldRowRt) = AddBarRow(panelGo.transform, ShieldIconPath, 0f, currentY, BarH, ShieldFull);

        // Row 2 — HEAT
        currentY -= RowGap;
        (_heatFill, _heatRowRt) = AddBarRow(panelGo.transform, HeatIconPath, 0f, currentY, BarH, HeatHot);
        _heatBarBg = _heatRowRt.GetComponent<Image>();
    }

    private void AddLabel(Transform parent, Font font, string text, float x, float y, Color col)
    {
    }

    // Returns (fillImage, barBgRectTransform).
    private (Image, RectTransform) AddBarRow(Transform parent, string iconPath, float x, float y, float h, Color initialCol)
    {
        Sprite roundedSprite = RoundedRectSprite.Get();

        // Background (Created first so it's behind the icon)
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(parent, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0f, 0f);
        bgRt.pivot            = new Vector2(0f, 0.5f);
        // Overlap: start at the same X as the icon
        bgRt.anchoredPosition = new Vector2(x, y);
        bgRt.sizeDelta        = new Vector2(BarW, h);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = BarBG;
        bgImg.sprite = roundedSprite;
        bgImg.type = Image.Type.Sliced;
        
        // Add Mask for rounded clipping
        var mask = bgGo.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        // Padding: start fill after the icon's main area
        fillRt.offsetMin = new Vector2(IconSize - 5f, 0); 
        fillRt.offsetMax = Vector2.zero;
        var fill = fillGo.AddComponent<Image>();
        fill.type       = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        fill.color      = initialCol;

        // Icon (Created second so it's in front)
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(parent, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0f);
        iconRt.pivot            = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(x, y);
        iconRt.sizeDelta        = new Vector2(IconSize, IconSize);
        iconRt.localScale       = new Vector3(1.50000012f, 1.50000012f, 1.50000012f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
#if UNITY_EDITOR
        iconImg.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
#endif
        
        return (fill, bgRt);
    }
}
