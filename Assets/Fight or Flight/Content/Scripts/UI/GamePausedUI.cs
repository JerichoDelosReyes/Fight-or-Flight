using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sci-fi "GAME PAUSED" panel. Auto-creates in MainScene via RuntimeInitializeOnLoadMethod.
/// Escape or TogglePause() shows/hides the panel.
/// </summary>
public class GamePausedUI : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    private static GamePausedUI instance;

    private GameObject pauseOverlay;
    private Font       uiFont;

    // Teal and green color constants matching the reference
    private static readonly Color Teal      = new Color(0f,    1f,    0.831f, 1f);
    private static readonly Color TealDim   = new Color(0f,    1f,    0.831f, 0.38f);
    private static readonly Color GreenText = new Color(0.72f, 1f,    0.55f,  1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsPaused = false;
        Time.timeScale = 1f;
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<GamePausedUI>() != null) return;
        new GameObject("GamePausedUI").AddComponent<GamePausedUI>();
    }

    private void Awake()
    {
        instance = this;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildUI();
    }

    private void OnEnable()  => GameEventManager.OnPlayerDestroyed += OnPlayerDied;
    private void OnDisable() => GameEventManager.OnPlayerDestroyed -= OnPlayerDied;

    private void OnPlayerDied() { if (IsPaused) DoResume(); }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
            MissionCompleteScreen.Show(5000, 525f, 25, 5, 5);
#endif
    }

    public static void TogglePause()
    {
        if (instance == null) return;
        if (IsPaused) instance.DoResume();
        else          instance.DoPause();
    }

    private void DoPause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        pauseOverlay?.SetActive(true);
        ShipInput.ReleaseCursor();
    }

    private void DoResume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pauseOverlay?.SetActive(false);
        var si = Object.FindAnyObjectByType<ShipInput>();
        if (si != null) si.ApplyCursorState();
        else if (ControlSchemeManager.IsMouseKeyboard) ShipInput.LockCursor();
    }

    private void DoSettings() { Debug.Log("Settings (Stub)"); }

    private void DoRestartWave()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        ShipInput.ReleaseCursor();
        SceneManager.LoadScene("MainScene");
    }

    private void DoQuitToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        ShipInput.ReleaseCursor();
        SceneManager.LoadScene("MainMenu");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvasGo = new GameObject("PauseCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        BuildPauseButton(canvasGo.transform);
        BuildPauseOverlay(canvasGo.transform);
    }

    private void BuildPauseButton(Transform canvasT)
    {
        var go = new GameObject("PauseBtn");
        go.transform.SetParent(canvasT, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f, -30f);
        rt.sizeDelta = new Vector2(80f, 80f);

        var img = go.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>("UI/Sprites/button_base");
        img.type = Image.Type.Sliced;
        if (img.sprite == null) img.color = new Color(0f, 1f, 0.831f, 0.2f);
        else img.color = Color.white;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(TogglePause);

        var lblRt = NewRt("Lbl", go.transform);
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
        var t = lblRt.gameObject.AddComponent<Text>();
        t.font = uiFont; t.fontSize = 32; t.fontStyle = FontStyle.Bold;
        t.color = Teal; t.alignment = TextAnchor.MiddleCenter;
        t.text = "II";
    }

    private void BuildPauseOverlay(Transform canvasT)
    {
        // Full-screen dimmer
        pauseOverlay = new GameObject("PauseOverlay");
        pauseOverlay.transform.SetParent(canvasT, false);
        var overlayRt = pauseOverlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
        pauseOverlay.AddComponent<Image>().color = new Color(0f, 0.02f, 0.06f, 0.78f);

        // Panel
        const float PW = 560f, PH = 580f;
        var panelRt = NewRt("Panel", pauseOverlay.transform);
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(PW, PH);

        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.sprite = Resources.Load<Sprite>("UI/Sprites/panel_background");
        panelImg.type = Image.Type.Sliced;
        if (panelImg.sprite == null) panelImg.color = new Color(0.04f, 0.12f, 0.14f, 0.97f);
        else panelImg.color = Color.white;

        // "GAME PAUSED" — plain text, extra top padding
        var headerRt = NewRt("Header", panelRt.transform);
        headerRt.anchorMin = headerRt.anchorMax = headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -32f);
        headerRt.sizeDelta = new Vector2(PW - 30f, 64f);

        var ht = headerRt.gameObject.AddComponent<Text>();
        ht.font = uiFont; ht.fontSize = 40; ht.fontStyle = FontStyle.Bold;
        ht.color = Teal; ht.alignment = TextAnchor.MiddleCenter;
        ht.text = "GAME PAUSED";
        ht.horizontalOverflow = HorizontalWrapMode.Overflow;

        // Divider line below header
        var divRt = NewRt("Divider", panelRt.transform);
        divRt.anchorMin = divRt.anchorMax = divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -96f);
        divRt.sizeDelta = new Vector2(PW - 40f, 2f);
        divRt.gameObject.AddComponent<Image>().color = TealDim;

        // Buttons — vertically centered in the space below the divider.
        // Group height = 3×STEP_MAG + BH = 288+70 = 358.
        const float BW = PW - 44f, BH = 70f;
        const float BY = -136f, STEP = -96f;

        var resumeBtn   = AddButton(panelRt.transform, "RESUME",       BY,          true,  BW, BH, "Boxy/Icons/resume");
        var settingsBtn = AddButton(panelRt.transform, "SETTINGS",     BY + STEP,   false, BW, BH, "Boxy/Icons/settings");
        var restartBtn  = AddButton(panelRt.transform, "RESTART WAVE", BY + STEP*2, false, BW, BH, "Boxy/Icons/restart");
        var quitBtn     = AddButton(panelRt.transform, "QUIT TO MENU", BY + STEP*3, false, BW, BH, "Boxy/Icons/quit");

        resumeBtn.onClick.AddListener(DoResume);
        settingsBtn.onClick.AddListener(DoSettings);
        restartBtn.onClick.AddListener(DoRestartWave);
        quitBtn.onClick.AddListener(DoQuitToMenu);

        pauseOverlay.SetActive(false);
    }

    // highlighted = true  → always-visible green glow (RESUME)
    // highlighted = false → transparent normally, subtle teal border on hover
    // iconSpriteName = null → no icon
    private Button AddButton(Transform parent, string label, float anchoredY, bool highlighted,
                             float bw, float bh, string iconSpriteName)
    {
        var root = NewRt(label, parent);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, anchoredY);
        root.sizeDelta = new Vector2(bw, bh);

        var img = root.gameObject.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>(highlighted ? "UI/Sprites/button_highlighted" : "UI/Sprites/button_base");
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 3f;   // compresses 9-slice borders → smaller corner radius
        img.color = Color.white;

        var btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        // All buttons transparent at rest. On hover:
        //   RESUME → full-opacity green glow (button_highlighted sprite)
        //   others → ~55% teal outline (button_base sprite)
        var colors = btn.colors;
        colors.normalColor      = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = highlighted ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        colors.pressedColor     = highlighted ? new Color(0.6f, 0.9f, 0.35f, 1f) : new Color(1f, 1f, 1f, 0.75f);
        colors.selectedColor    = new Color(1f, 1f, 1f, 0f);
        colors.colorMultiplier  = 1f;
        btn.colors = colors;

        // Text — centered; reserve right padding only when an icon is present
        float rightPad = iconSpriteName != null ? -80f : -10f;
        var txtRt = NewRt("Txt", root.transform);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(10f, 0f);
        txtRt.offsetMax = new Vector2(rightPad, 0f);
        var t = txtRt.gameObject.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = 36;
        t.fontStyle = FontStyle.Bold;
        t.color = Teal;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        // Icon anchored to right edge (skipped when iconSpriteName is null)
        if (iconSpriteName != null)
        {
            var iconRt = NewRt("Icon", root.transform);
            iconRt.anchorMin = new Vector2(1f, 0.5f);
            iconRt.anchorMax = new Vector2(1f, 0.5f);
            iconRt.pivot     = new Vector2(1f, 0.5f);
            iconRt.anchoredPosition = new Vector2(-20f, 0f);
            iconRt.sizeDelta = new Vector2(56f, 56f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = Resources.Load<Sprite>("UI/Sprites/" + iconSpriteName);
            // Bright teal tint so the icon pops against the dark panel background
            iconImg.color = new Color(0f, 1f, 0.831f, 1f);
            iconImg.raycastTarget = false;
            if (iconImg.sprite == null) iconImg.enabled = false;
        }

        return btn;
    }

    private static RectTransform NewRt(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
