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
    // ── PlayerPrefs keys ──────────────────────────────────────────────────────

    private const string KeyControlScheme = "ControlScheme";
    private const string KeyInvertY       = "InvertY";
    private const string KeyVolMaster     = "VolMaster";
    private const string KeyVolMusic      = "VolMusic";
    private const string KeyVolSFX        = "VolSFX";

    // ── Selection state ───────────────────────────────────────────────────────

    private int selectedScheme;     // 0 = Keyboard Only, 1 = Mouse + Keyboard

    // ── Widget refs ───────────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] schemeButtons = new Button[2];
    [SerializeField] private GameObject invertYRow;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private GameObject invertPitchKbRow;
    [SerializeField] private Toggle invertPitchKbToggle;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;

    private Color[]  schemeColors = new[]
    {
        new Color(0.13f, 0.40f, 0.80f),
        new Color(0.55f, 0.30f, 0.75f),
    };

    private Font uiFont;

    // Sci-Fi UI Assets
    private Sprite panelSprite;
    private Sprite headerSprite;
    private Sprite btnLargeSprite;
    private Sprite btnSmallSprite;
    private Sprite checkboxSprite;
    private Sprite checkmarkSprite;
    private Sprite sliderTrackSprite;
    private Sprite sliderHandleSprite;
    private Sprite dividerSprite;

    // ── Static entry point ────────────────────────────────────────────────────

    private static SettingsMenu instance;

    public static void Show()
    {
        if (instance != null && instance.gameObject != null)
        {
            instance.gameObject.SetActive(true);
            return;
        }

        GameObject prefab = null;

        // Priority 1: MainMenu folder prefab
        #if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fight or Flight/Content/Scenes/MainMenu/SettingsMenu.prefab");
        #endif

        // Priority 2: Resources
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("RootResources/UI/SettingsMenu");
        }

        if (prefab != null)
        {
            var go = Instantiate(prefab);
            instance = go.GetComponent<SettingsMenu>();
        }
        else
        {
            // Fallback to procedural only if absolutely necessary
            var go = new GameObject("SettingsMenu");
            instance = go.AddComponent<SettingsMenu>();
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // If an instance already exists and it's not us, we might be a duplicate or 
        // a new one created while the old one is being destroyed.
        if (instance != null && instance != this)
        {
            // If the old one is valid, we don't need this one.
            if (instance.gameObject != null)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        instance = this;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        LoadAssets();
        LoadCurrent();
        EnsureEventSystem();
        
        // If we are instantiated from a prefab, UI components might already be assigned.
        // If not, we build it procedurally as a fallback.
        if (panel == null)
        {
            BuildUI();
        }
        else
        {
            WireListeners();
        }
        
        RefreshInvertYVisibility();
    }

    private void WireListeners()
    {
        if (schemeButtons[0] != null) schemeButtons[0].onClick.AddListener(() => SelectScheme(0));
        if (schemeButtons[1] != null) schemeButtons[1].onClick.AddListener(() => SelectScheme(1));
        if (applyButton != null) applyButton.onClick.AddListener(ApplySettings);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void LoadAssets()
    {
        panelSprite        = Resources.Load<Sprite>("RootResources/SciFiUI/panel_frame");
        headerSprite       = Resources.Load<Sprite>("RootResources/SciFiUI/header_bar");
        btnLargeSprite     = Resources.Load<Sprite>("RootResources/SciFiUI/button_large");
        btnSmallSprite     = Resources.Load<Sprite>("RootResources/SciFiUI/button_small");
        checkboxSprite     = Resources.Load<Sprite>("RootResources/SciFiUI/checkbox_bg");
        checkmarkSprite    = Resources.Load<Sprite>("RootResources/SciFiUI/checkmark");
        sliderTrackSprite  = Resources.Load<Sprite>("RootResources/SciFiUI/slider_track");
        sliderHandleSprite = Resources.Load<Sprite>("RootResources/SciFiUI/slider_handle");
        dividerSprite      = Resources.Load<Sprite>("RootResources/SciFiUI/divider");
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
        selectedScheme = PlayerPrefs.GetInt(KeyControlScheme, 1);
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // Dark overlay (full screen).
        MakeStretchedImage(gameObject, new Color(0f, 0f, 0f, 0.85f));

        var panel = MakePanel(new Vector2(0, 0), new Vector2(1100, 850));

        // ── Title ─────────────────────────────────────────────────────────────
        var titleGo = MakeLabel(panel, "SETTINGS", 56, new Color(0.3f, 1f, 1f), FontStyle.Bold,
                                new Vector2(0, 347), new Vector2(840, 90));
        titleGo.transform.localScale = new Vector3(0.779049993f, 0.779049993f, 0.779049993f);
        
        var topDivider = MakeDivider(panel, new Vector2(0, 315));

        // ── Type Header (Formerly Control Scheme) ─────────────────────────────
        var typeHeader = MakeHeaderBar(panel, "TYPE", 26, new Vector2(0, 265));
        typeHeader.transform.localScale = new Vector3(1f, 4.56069994f, 1f);
        foreach (Transform child in typeHeader.transform)
        {
            if (child.name == "Lbl") child.localScale = new Vector3(1f, 0.249009997f, 1f);
        }

        // Backing panel for buttons
        var btnBacking = new GameObject("ButtonPanel");
        btnBacking.transform.SetParent(panel.transform, false);
        var backingRt = btnBacking.AddComponent<RectTransform>();
        backingRt.anchoredPosition = new Vector2(0, 205);
        backingRt.sizeDelta = new Vector2(680, 80);
        btnBacking.transform.localScale = new Vector3(1f, 6.67498636f, 1f);
        var backingImg = btnBacking.AddComponent<Image>();
backingImg.sprite = headerSprite;
        backingImg.type = Image.Type.Sliced;
        backingImg.color = new Color(1, 1, 1, 0.3f);

        string[] schemeNames = { "Keyboard Only", "Mouse + Keyboard" };
        schemeColors = new[]
        {
            new Color(0.13f, 0.40f, 0.80f),
            new Color(0.55f, 0.30f, 0.75f),
        };
        for (int i = 0; i < 2; i++)
        {
            int idx = i;
            float xOff = (i == 0) ? -165f : 165f;
            var btn = MakeButton(panel, schemeNames[i], schemeColors[i],
                                 new Vector2(xOff, 205), new Vector2(300, 60),
                                 () => SelectScheme(idx), true);
            schemeButtons[i] = btn.GetComponent<Button>();
        }

        // ── Invert Y (only visible in Mouse + Keyboard) ───────────────────────
        invertYRow = new GameObject("InvertYRow");
        invertYRow.transform.SetParent(panel.transform, false);
        var rowRt = invertYRow.AddComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(41, 135);
        rowRt.sizeDelta = new Vector2(840, 50);

        MakeLabel(invertYRow, "INVERT Y-AXIS (MOUSE)", 24, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(-150, 0), new Vector2(500, 40));
        invertYToggle = MakeToggle(invertYRow, new Vector2(250, 0));
        invertYToggle.isOn = PlayerPrefs.GetInt(KeyInvertY, 0) == 1;

        // ── Invert Pitch (only visible in Keyboard Only) ──────────────────────
        invertPitchKbRow = new GameObject("InvertPitchKbRow");
        invertPitchKbRow.transform.SetParent(panel.transform, false);
        var ipRowRt = invertPitchKbRow.AddComponent<RectTransform>();
        ipRowRt.anchorMin = ipRowRt.anchorMax = new Vector2(0.5f, 0.5f);
        ipRowRt.anchoredPosition = new Vector2(41, 135);
        ipRowRt.sizeDelta = new Vector2(840, 50);

        MakeLabel(invertPitchKbRow, "INVERT PITCH (KEYBOARD)", 24, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(-150, 0), new Vector2(500, 40));
        invertPitchKbToggle = MakeToggle(invertPitchKbRow, new Vector2(250, 0));
        invertPitchKbToggle.isOn = PlayerPrefs.GetInt("InvertPitchKeyboard", 0) == 1;

        // ── Audio Settings Header ─────────────────────────────────────────────
        var audioHeader = MakeHeaderBar(panel, "AUDIO SETTINGS", 26, new Vector2(0, 60));
        audioHeader.transform.localScale = new Vector3(1f, 4.56069994f, 1f);
        foreach (Transform child in audioHeader.transform)
        {
            if (child.name == "Lbl") child.localScale = new Vector3(1f, 0.249009997f, 1f);
        }

        // ── Volume sliders ────────────────────────────────────────────────────
float labelX = -250f;
        float sliderX = 180f;
        float sliderWidth = 500f;

        MakeLabel(panel, "MASTER VOLUME", 22, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(labelX, 0), new Vector2(380, 40));
        masterSlider = MakeSlider(panel, new Vector2(sliderX, 0), sliderWidth, PlayerPrefs.GetFloat(KeyVolMaster, 1f));

        MakeLabel(panel, "MUSIC VOLUME", 22, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(labelX, -60), new Vector2(380, 40));
        musicSlider = MakeSlider(panel, new Vector2(sliderX, -60), sliderWidth, PlayerPrefs.GetFloat(KeyVolMusic, 0.8f));

        MakeLabel(panel, "SFX VOLUME", 22, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal,
                  new Vector2(labelX, -120), new Vector2(380, 40));
        sfxSlider = MakeSlider(panel, new Vector2(sliderX, -120), sliderWidth, PlayerPrefs.GetFloat(KeyVolSFX, 1f));

        // ── Apply / Back ──────────────────────────────────────────────────────
        var bottomDivider = MakeDivider(panel, new Vector2(0, -200));
        bottomDivider.transform.localScale = new Vector3(1f, 110.874985f, 1f);
        
        var applyBtn = MakeButton(panel, "APPLY", new Color(0.13f, 0.40f, 0.80f),
                                  new Vector2(5, -263), new Vector2(350, 70), ApplySettings, false);
        applyBtn.transform.localScale = new Vector3(1f, 1.89750004f, 1f);
        var applyLbl = applyBtn.transform.Find("Lbl");
        if (applyLbl != null) applyLbl.localScale = new Vector3(1f, 0.604030013f, 1f);

        var closeBtn = MakeButton(panel, "CLOSE", new Color(0.2980392f, 1f, 1f),
                                  new Vector2(11, -361), new Vector2(350, 70), Close, false);
        closeBtn.transform.localScale = new Vector3(1.44599998f, 1.40919995f, 1f);
        var closeLbl = closeBtn.transform.Find("Lbl");
        if (closeLbl != null) closeLbl.localScale = new Vector3(1f, 0.815180004f, 1f);

        // Highlight current selections.
        SelectScheme(selectedScheme);
    }

    private GameObject MakeHeaderBar(GameObject parent, string text, int size, Vector2 pos)
    {
        var go = new GameObject("HeaderBar");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(840, 60);
        var img = go.AddComponent<Image>();
        img.sprite = headerSprite;
        img.type = Image.Type.Sliced;
        MakeLabel(go, text, size, Color.white, FontStyle.Bold, Vector2.zero, new Vector2(840, 60));
        return go;
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
        img.sprite = panelSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        return go;
    }

    private GameObject MakeDivider(GameObject parent, Vector2 pos)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(820, 2);
        var img = go.AddComponent<Image>();
        img.sprite = dividerSprite;
        img.color = new Color(0.3f, 1f, 1f, 0.5f);
        return go;
    }

    private GameObject MakeLabel(GameObject parent, string text, int size, Color color,
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
        return go;
    }

    private GameObject MakeButton(GameObject parent, string label, Color bg,
                                  Vector2 pos, Vector2 size,
                                  UnityEngine.Events.UnityAction onClick, bool isSmall)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = isSmall ? btnSmallSprite : btnLargeSprite;
        img.type = Image.Type.Sliced;
        img.color = bg;
        img.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cols.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        cols.colorMultiplier  = 1f;
        btn.colors = cols;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        // Add Hitbox
        var hitboxGo = new GameObject("Hitbox");
        hitboxGo.transform.SetParent(go.transform, false);
        var hitboxRt = hitboxGo.AddComponent<RectTransform>();
        hitboxRt.anchorMin = hitboxRt.anchorMax = hitboxRt.pivot = new Vector2(0.5f, 0.5f);
        hitboxRt.anchoredPosition = Vector2.zero;
        hitboxRt.sizeDelta = new Vector2(size.x * 0.8f, size.y * 0.7f);
        var hitboxImg = hitboxGo.AddComponent<Image>();
        hitboxImg.color = new Color(0, 0, 0, 0);
        hitboxImg.raycastTarget = true;

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
        bgImg.sprite = checkboxSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        var check = new GameObject("Checkmark");
        check.transform.SetParent(bg.transform, false);
        var checkRt = check.AddComponent<RectTransform>();
        checkRt.anchorMin = checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.sizeDelta = new Vector2(36, 36);
        checkRt.anchoredPosition = Vector2.zero;
        var checkImg = check.AddComponent<Image>();
        checkImg.sprite = checkmarkSprite;
        checkImg.color = Color.white;

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        return toggle;
    }

    private Slider MakeSlider(GameObject parent, Vector2 pos, float width, float initialValue)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 40);

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(0, 8);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = sliderTrackSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        var fillAreaGo = new GameObject("FillArea");
        fillAreaGo.transform.SetParent(go.transform, false);
        var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0f);
        fillAreaRt.anchorMax = new Vector2(1f, 1f);
        fillAreaRt.offsetMin = new Vector2(5, 0); // Slight offset to stay inside frame
        fillAreaRt.offsetMax = new Vector2(-5, 0);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.sprite = sliderTrackSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.3f, 1f, 1f, 1f);

        var handleAreaGo = new GameObject("HandleArea");
        handleAreaGo.transform.SetParent(go.transform, false);
        var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0f, 0f);
        handleAreaRt.anchorMax = new Vector2(1f, 1f);
        handleAreaRt.offsetMin = new Vector2(10, 0);
        handleAreaRt.offsetMax = new Vector2(-10, 0);

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        var handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.sizeDelta = new Vector2(24, 50);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.sprite = sliderHandleSprite;
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
