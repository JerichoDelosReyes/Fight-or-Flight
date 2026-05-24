using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds and shows the mission-complete overlay entirely in code — no prefab required.
/// Called statically by WaveManager (or similar) when the player clears the final wave.
/// </summary>
public class MissionCompleteScreen : MonoBehaviour
{
    private static MissionCompleteScreen instance;

    public static void Show(int score, float timeSeconds, int kills, int wavesCompleted, int totalWaves = 5)
    {
        if (instance != null) return;
        var go = new GameObject("MissionCompleteScreen");
        instance = go.AddComponent<MissionCompleteScreen>();
        instance.Init(score, timeSeconds, kills, wavesCompleted, totalWaves);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private Font uiFont;

    private static readonly Color Teal    = new Color(0f,    1f,    0.831f, 1f);
    private static readonly Color TealDim = new Color(0f,    1f,    0.831f, 0.38f);
    private static readonly Color TealDim2= new Color(0f,    1f,    0.831f, 0.12f);

    private int   _score;
    private float _time;
    private int   _kills;
    private int   _waves;
    private int   _totalWaves;

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Init(int score, float time, int kills, int waves, int totalWaves)
    {
        _score = score; _time = time; _kills = kills; _waves = waves; _totalWaves = totalWaves;
        BuildUI();
    }

    private void OnDestroy() { instance = null; }

    // ─── UI construction ──────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Full-screen dimmer
        var dimmerGo = new GameObject("Dimmer");
        dimmerGo.transform.SetParent(transform, false);
        var drt = dimmerGo.AddComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;
        dimmerGo.AddComponent<Image>().color = new Color(0f, 0.02f, 0.06f, 0.82f);

        // Panel
        const float PW = 640f, PH = 880f; 
        var panelRt = NewRt("Panel", transform);
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(PW, PH);

        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.sprite = Resources.Load<Sprite>("UI/Sprites/panel_background");
        panelImg.type   = Image.Type.Sliced;
        if (panelImg.sprite == null) panelImg.color = new Color(0.04f, 0.12f, 0.14f, 0.97f);
        else                         panelImg.color = Color.white;

        // --- Scrollable setup ---
        var scrollGo = new GameObject("ScrollArea");
        scrollGo.transform.SetParent(panelRt, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(10, 20); 
        scrollRt.offsetMax = new Vector2(-10, -10);

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Viewport (masking)
        var viewGo = new GameObject("Viewport");
        viewGo.transform.SetParent(scrollRt, false);
        var viewRt = viewGo.AddComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = viewRt.offsetMax = Vector2.zero;
        viewGo.AddComponent<RectMask2D>(); 

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewRt, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 780f);
        scrollRect.content = contentRt;

        // "MISSION COMPLETE" header
        var headerRt = NewRt("Header", contentRt);
        headerRt.anchorMin = headerRt.anchorMax = headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -32f);
        headerRt.sizeDelta = new Vector2(PW - 60f, 64f);
        var ht = headerRt.gameObject.AddComponent<Text>();
        ht.font = uiFont; ht.fontSize = 44; ht.fontStyle = FontStyle.Bold;
        ht.color = Teal; ht.alignment = TextAnchor.MiddleCenter;
        ht.text = "MISSION COMPLETE";
        ht.horizontalOverflow = HorizontalWrapMode.Overflow;

        // Divider below header
        var divTopRt = NewRt("DivTop", contentRt);
        divTopRt.anchorMin = divTopRt.anchorMax = divTopRt.pivot = new Vector2(0.5f, 1f);
        divTopRt.anchoredPosition = new Vector2(0f, -96f);
        divTopRt.sizeDelta = new Vector2(PW - 100f, 2f);
        divTopRt.gameObject.AddComponent<Image>().color = TealDim;

        // Icon — centered
        var iconRt = NewRt("Icon", contentRt);
        iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.anchoredPosition = new Vector2(0f, -126f);
        iconRt.sizeDelta = new Vector2(180f, 180f);
        var iconImg = iconRt.gameObject.AddComponent<Image>();
        iconImg.sprite = Resources.Load<Sprite>("UI/Sprites/mission_comp");
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.color          = Color.white;

        // Stats block
        BuildStats(contentRt, PW);

        // Buttons
        const float BW = PW - 100f, BH = 70f;
        const float BY = -554f, STEP = -82f;

        var playBtn = AddButton(contentRt, "PLAY AGAIN",      BY,        true,  BW, BH);
        var quitBtn = AddButton(contentRt, "QUIT TO MAIN MENU", BY + STEP, false, BW, BH);

        playBtn.onClick.AddListener(OnNextLevel);
        quitBtn.onClick.AddListener(OnQuit);
    }

    private void BuildStats(Transform contentT, float PW)
    {
        // "MISSION SUCCESSFUL!" subtitle label
        var subtitleRt = NewRt("Subtitle", contentT);
        subtitleRt.anchorMin = subtitleRt.anchorMax = subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.anchoredPosition = new Vector2(0f, -302f);
        subtitleRt.sizeDelta = new Vector2(PW - 80f, 42f);
        var st = subtitleRt.gameObject.AddComponent<Text>();
        st.font = uiFont; st.fontSize = 32; st.fontStyle = FontStyle.Bold;
        st.color = new Color(0f, 1f, 0.831f, 0.9f);
        st.alignment = TextAnchor.MiddleCenter;
        st.text = "MISSION SUCCESSFUL!";

        // Stat rows
        var rows = new (string label, string value)[]
        {
            ("SCORE:",            _score.ToString("N0")),
            ("TIME:",             FormatTime(_time)),
            ("KILLS:",            _kills.ToString()),
            ("WAVES COMPLETED:",  $"{_waves} OF {_totalWaves}"),
        };

        float rowY = -358f;
        const float ROW_H = 40f, ROW_STEP = -44f;
        float sideMargin = 60f;

        foreach (var (label, value) in rows)
        {
            // Label (left)
            var lblRt = NewRt(label + "_Lbl", contentT);
            lblRt.anchorMin = lblRt.anchorMax = lblRt.pivot = new Vector2(0f, 1f); // Anchor to left
            lblRt.anchoredPosition = new Vector2(sideMargin, rowY);
            lblRt.sizeDelta = new Vector2(PW * 0.5f, ROW_H);
            var lt = lblRt.gameObject.AddComponent<Text>();
            lt.font = uiFont; lt.fontSize = 26; lt.fontStyle = FontStyle.Bold;
            lt.color = Color.white;
            lt.alignment = TextAnchor.MiddleLeft;
            lt.text = label;

            // Value (right)
            var valRt = NewRt(label + "_Val", contentT);
            valRt.anchorMin = valRt.anchorMax = valRt.pivot = new Vector2(1f, 1f);
            valRt.anchoredPosition = new Vector2(-sideMargin, rowY);
            valRt.sizeDelta = new Vector2(PW * 0.5f, ROW_H);
            var vt = valRt.gameObject.AddComponent<Text>();
            vt.font = uiFont; vt.fontSize = 26; vt.fontStyle = FontStyle.Bold;
            vt.color = new Color(1f, 0.92f, 0.02f, 1f);
            vt.alignment = TextAnchor.MiddleRight;
            vt.text = value;

            rowY += ROW_STEP;
        }
    }

    // ─── Button actions ───────────────────────────────────────────────────────

    private void OnNextLevel() => StartCoroutine(LoadScene("MainScene"));
    private void OnReplay()    => StartCoroutine(LoadScene("MainScene"));
    private void OnQuit()      => StartCoroutine(LoadScene("MainMenu"));

    private IEnumerator LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        yield return null;
        SceneManager.LoadScene(sceneName);
    }

    // ─── Button builder ───────────────────────────────────────────────────────

    private Button AddButton(Transform parent, string label, float anchoredY, bool highlighted, float bw, float bh)
    {
        var root = NewRt(label, parent);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, anchoredY);
        root.sizeDelta = new Vector2(bw, bh);

        var img = root.gameObject.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>(highlighted ? "UI/Sprites/button_highlighted" : "UI/Sprites/button_base");
        img.type   = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 3f;
        img.color  = Color.white;

        var btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor      = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = highlighted ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        colors.pressedColor     = highlighted ? new Color(0.6f, 0.9f, 0.35f, 1f) : new Color(1f, 1f, 1f, 0.75f);
        colors.selectedColor    = new Color(1f, 1f, 1f, 0f);
        colors.colorMultiplier  = 1f;
        btn.colors = colors;

        var txtRt = NewRt("Txt", root.transform);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(10f, 0f);
        txtRt.offsetMax = new Vector2(-10f, 0f);
        var t = txtRt.gameObject.AddComponent<Text>();
        t.font = uiFont; t.fontSize = 32; t.fontStyle = FontStyle.Bold;
        t.color = Teal;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        return btn;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static RectTransform NewRt(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:D2}";
    }
}
