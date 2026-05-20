using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Full-screen settings overlay built entirely in code — no prefab or scene setup required.
/// Opened by MainMenuController.OpenSettings(). All values persist via PlayerPrefs and
/// are committed only when the player clicks APPLY.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    // ── Static entry point ────────────────────────────────────────────────────

    private static SettingsMenu instance;

    public static void Show()
    {
        if (instance != null) { instance.gameObject.SetActive(true); return; }
        var go = new GameObject("SettingsMenu");
        instance = go.AddComponent<SettingsMenu>();
    }

    // ── PlayerPrefs keys ──────────────────────────────────────────────────────

    private const string KeyControlScheme = "ControlScheme";
    private const string KeyInvertY       = "InvertY";
    private const string KeyVolMaster     = "VolMaster";
    private const string KeyVolMusic      = "VolMusic";
    private const string KeyVolSFX        = "VolSFX";

    // ── Selection state ───────────────────────────────────────────────────────

    private int selectedScheme;     // 0 = Keyboard Only, 1 = Mouse + Keyboard

    // ── Widget refs ───────────────────────────────────────────────────────────

    private Button[] schemeButtons = new Button[2];
    private Color[]  schemeColors;

    private GameObject invertYRow;
    private Toggle invertYToggle;
    private GameObject invertPitchKbRow;
    private Toggle invertPitchKbToggle;
    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;

    private Font uiFont;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        instance = this;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        LoadCurrent();
        EnsureEventSystem();
        BuildUI();
        RefreshInvertYVisibility();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ── Load saved values ─────────────────────────────────────────────────────

    private void LoadCurrent()
    {
        selectedScheme = PlayerPrefs.GetInt(KeyControlScheme, 0);
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Dark overlay (full screen).
        MakeStretchedImage(gameObject, new Color(0f, 0f, 0f, 0.85f));

        // Centre panel — shrunk vertically since the Difficulty section was removed.
        var panel = MakePanel(new Vector2(0, 0), new Vector2(900, 830));

        // ── Title ─────────────────────────────────────────────────────────────
        MakeLabel(panel, "SETTINGS", 72, new Color(0.9f, 0.9f, 1f), FontStyle.Bold,
                  new Vector2(0, 350), new Vector2(840, 90));
        MakeDivider(panel, new Vector2(0, 295));

        // ── Control Scheme ────────────────────────────────────────────────────
        MakeLabel(panel, "CONTROL SCHEME", 30, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(0, 245), new Vector2(840, 40));

        string[] schemeNames = { "Keyboard Only", "Mouse + Keyboard" };
        schemeColors = new[]
        {
            new Color(0.13f, 0.40f, 0.80f),
            new Color(0.55f, 0.30f, 0.75f),
        };
        for (int i = 0; i < 2; i++)
        {
            int idx = i;
            float xOff = (i == 0) ? -160f : 160f;
            var btn = MakeButton(panel, schemeNames[i], schemeColors[i],
                                 new Vector2(xOff, 190), new Vector2(290, 60),
                                 () => SelectScheme(idx));
            schemeButtons[i] = btn.GetComponent<Button>();
        }

        // ── Invert Y (only visible in Mouse + Keyboard) ───────────────────────
        invertYRow = new GameObject("InvertYRow");
        invertYRow.transform.SetParent(panel.transform, false);
        var rowRt = invertYRow.AddComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(0, 120);
        rowRt.sizeDelta = new Vector2(840, 50);

        MakeLabel(invertYRow, "INVERT Y-AXIS (MOUSE)", 28, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(-120, 0), new Vector2(500, 40));
        invertYToggle = MakeToggle(invertYRow, new Vector2(210, 0));
        invertYToggle.isOn = PlayerPrefs.GetInt(KeyInvertY, 0) == 1;

        // ── Invert Pitch (only visible in Keyboard Only) ──────────────────────
        invertPitchKbRow = new GameObject("InvertPitchKbRow");
        invertPitchKbRow.transform.SetParent(panel.transform, false);
        var ipRowRt = invertPitchKbRow.AddComponent<RectTransform>();
        ipRowRt.anchorMin = ipRowRt.anchorMax = new Vector2(0.5f, 0.5f);
        ipRowRt.anchoredPosition = new Vector2(0, 120);
        ipRowRt.sizeDelta = new Vector2(840, 50);

        MakeLabel(invertPitchKbRow, "INVERT PITCH (KEYBOARD)", 28, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(-120, 0), new Vector2(500, 40));
        invertPitchKbToggle = MakeToggle(invertPitchKbRow, new Vector2(210, 0));
        invertPitchKbToggle.isOn = PlayerPrefs.GetInt("InvertPitchKeyboard", 0) == 1;

        // ── Volume sliders ────────────────────────────────────────────────────
        MakeLabel(panel, "MASTER VOLUME", 28, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(-170, 55), new Vector2(380, 40));
        masterSlider = MakeSlider(panel, new Vector2(200, 55), PlayerPrefs.GetFloat(KeyVolMaster, 1f));

        MakeLabel(panel, "MUSIC VOLUME", 28, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(-170, -5), new Vector2(380, 40));
        musicSlider = MakeSlider(panel, new Vector2(200, -5), PlayerPrefs.GetFloat(KeyVolMusic, 0.8f));

        MakeLabel(panel, "SFX VOLUME", 28, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(-170, -65), new Vector2(380, 40));
        sfxSlider = MakeSlider(panel, new Vector2(200, -65), PlayerPrefs.GetFloat(KeyVolSFX, 1f));

        // ── Apply / Back ──────────────────────────────────────────────────────
        MakeDivider(panel, new Vector2(0, -150));
        MakeButton(panel, "APPLY", new Color(0.13f, 0.40f, 0.80f),
                   new Vector2(-180, -225), new Vector2(320, 70), ApplySettings);
        MakeButton(panel, "BACK", new Color(0.25f, 0.25f, 0.25f),
                   new Vector2( 180, -225), new Vector2(320, 70), Close);

        // Highlight current selections.
        SelectScheme(selectedScheme);
    }

    // ── Selection actions (visual highlight + state) ──────────────────────────

    private void SelectScheme(int idx)
    {
        selectedScheme = idx;
        ApplyButtonHighlight(schemeButtons, schemeColors, idx);
        RefreshInvertYVisibility();
    }

    private void ApplyButtonHighlight(Button[] buttons, Color[] baseColors, int selectedIdx)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var img = buttons[i].GetComponent<Image>();
            if (img == null) continue;
            Color c = baseColors[i];
            img.color = (i == selectedIdx)
                ? c
                : new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 0.75f);
        }
    }

    private void RefreshInvertYVisibility()
    {
        bool mouseMode = selectedScheme == 1;
        if (invertYRow != null) invertYRow.SetActive(mouseMode);
        if (invertPitchKbRow != null) invertPitchKbRow.SetActive(!mouseMode);
    }

    // ── Apply / Close ─────────────────────────────────────────────────────────

    private void ApplySettings()
    {
        // Control scheme + invert toggles → routed through ControlSchemeManager so
        // the static caches stay in sync with PlayerPrefs without a scene reload.
        ControlSchemeManager.SetScheme((ControlSchemeManager.Scheme)selectedScheme);
        ControlSchemeManager.SetInvertY(invertYToggle.isOn);
        ControlSchemeManager.SetInvertPitchKeyboard(invertPitchKbToggle.isOn);

        // Volume — master applies live via AudioListener; music/SFX are persisted
        // for AudioSource components / future mixers to read.
        PlayerPrefs.SetFloat(KeyVolMaster, masterSlider.value);
        PlayerPrefs.SetFloat(KeyVolMusic,  musicSlider.value);
        PlayerPrefs.SetFloat(KeyVolSFX,    sfxSlider.value);
        AudioListener.volume = masterSlider.value;

        PlayerPrefs.Save();
        Close();
    }

    private void Close()
    {
        Destroy(gameObject);
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private void MakeStretchedImage(GameObject parent, Color color)
    {
        var go = new GameObject("Overlay");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
    }

    private GameObject MakePanel(Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(gameObject.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.07f, 0.07f, 0.12f, 0.97f);
        return go;
    }

    private void MakeDivider(GameObject parent, Vector2 pos)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(820, 2);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
    }

    private void MakeLabel(GameObject parent, string text, int size, Color color,
                           FontStyle style, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = uiFont;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private GameObject MakeButton(GameObject parent, string label, Color bg,
                                  Vector2 pos, Vector2 size,
                                  UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cols.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        cols.colorMultiplier  = 1f;
        btn.colors = cols;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        MakeLabel(go, label, 26, Color.white, FontStyle.Bold, Vector2.zero, size);
        return go;
    }

    private Toggle MakeToggle(GameObject parent, Vector2 pos)
    {
        var go = new GameObject("Toggle");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(50, 50);

        var bg = new GameObject("BG");
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(50, 50);
        bgRt.anchoredPosition = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        var check = new GameObject("Checkmark");
        check.transform.SetParent(bg.transform, false);
        var checkRt = check.AddComponent<RectTransform>();
        checkRt.anchorMin = checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.sizeDelta = new Vector2(36, 36);
        checkRt.anchoredPosition = Vector2.zero;
        var checkImg = check.AddComponent<Image>();
        checkImg.color = new Color(0.3f, 0.8f, 0.3f, 1f);

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        return toggle;
    }

    private Slider MakeSlider(GameObject parent, Vector2 pos, float initialValue)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(340, 40);

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        var fillAreaGo = new GameObject("FillArea");
        fillAreaGo.transform.SetParent(go.transform, false);
        var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = fillAreaRt.offsetMax = Vector2.zero;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.6f, 1f, 1f);

        var handleAreaGo = new GameObject("HandleArea");
        handleAreaGo.transform.SetParent(go.transform, false);
        var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0f, 0f);
        handleAreaRt.anchorMax = new Vector2(1f, 1f);
        handleAreaRt.offsetMin = handleAreaRt.offsetMax = Vector2.zero;

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        var handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.sizeDelta = new Vector2(24, 24);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;

        return slider;
    }
}
