using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DefeatScreen : MonoBehaviour
{
    private static DefeatScreen instance;

    public static void Show(int score, int kills, float time, int waves, int totalWaves, bool survival)
    {
        if (instance != null) return;
        var go = new GameObject("DefeatScreen");
        instance = go.AddComponent<DefeatScreen>();
        instance.Init(score, kills, time, waves, totalWaves, survival);
    }


    private Font uiFont;

    private static readonly Color Red    = new Color(0.90f, 0.22f, 0.04f, 1f);
    private static readonly Color RedDim = new Color(0.90f, 0.22f, 0.04f, 0.38f);

    private int   _score;
    private int   _kills;
    private float _time;
    private int   _waves;
    private int   _totalWaves;
    private bool  _survival;

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Init(int score, int kills, float time, int waves, int totalWaves, bool survival)
    {
        _score = score; _kills = kills; _time = time;
        _waves = waves; _totalWaves = totalWaves; _survival = survival;
        BuildUI();
    }

    private void OnDestroy() { instance = null; }


    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        var dimmerGo = new GameObject("Dimmer");
        dimmerGo.transform.SetParent(transform, false);
        var drt = dimmerGo.AddComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;
        dimmerGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        const float PW = 520f, PH = 660f;
        var panelRt = NewRt("Panel", transform);
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(PW, PH);

        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.sprite = Resources.Load<Sprite>("UI/Sprites/panel_background");
        panelImg.type   = Image.Type.Sliced;
        if (panelImg.sprite == null) panelImg.color = new Color(0.12f, 0.03f, 0.02f, 0.97f);
        else                         panelImg.color = new Color(1f, 0.35f, 0.2f, 1f);

        var headerRt = NewRt("Header", panelRt.transform);
        headerRt.anchorMin = headerRt.anchorMax = headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -34f);
        headerRt.sizeDelta = new Vector2(PW - 30f, 60f);
        var ht = headerRt.gameObject.AddComponent<Text>();
        ht.font = uiFont; ht.fontSize = 40; ht.fontStyle = FontStyle.Bold;
        ht.color = Red; ht.alignment = TextAnchor.MiddleCenter;
        ht.text = "MISSION FAILED";
        ht.horizontalOverflow = HorizontalWrapMode.Overflow;

        var divTopRt = NewRt("DivTop", panelRt.transform);
        divTopRt.anchorMin = divTopRt.anchorMax = divTopRt.pivot = new Vector2(0.5f, 1f);
        divTopRt.anchoredPosition = new Vector2(0f, -100f);
        divTopRt.sizeDelta = new Vector2(PW - 40f, 2f);
        divTopRt.gameObject.AddComponent<Image>().color = RedDim;

        var helmetRt = NewRt("Helmet", panelRt.transform);
        helmetRt.anchorMin = helmetRt.anchorMax = helmetRt.pivot = new Vector2(0.5f, 1f);
        helmetRt.anchoredPosition = new Vector2(0f, -128f);
        helmetRt.sizeDelta = new Vector2(122f, 122f);
        var helmetImg = helmetRt.gameObject.AddComponent<Image>();
        helmetImg.sprite = Resources.Load<Sprite>("UI/Sprites/defeat_helmet_new");
        helmetImg.preserveAspect = true;
        helmetImg.raycastTarget  = false;
        helmetImg.color          = Color.white;

        BuildStats(panelRt.transform, PW);

        const float BW = PW - 44f, BH = 60f;
        const float BY = -476f, STEP = -74f;

        var retryBtn = AddButton(panelRt.transform, "RETRY MISSION", BY,        true,  BW, BH, null);
        var quitBtn  = AddButton(panelRt.transform, "QUIT TO MENU",  BY + STEP, false, BW, BH, null);

        retryBtn.onClick.AddListener(OnRetry);
        quitBtn.onClick.AddListener(OnQuit);
    }


    private void BuildStats(Transform panelT, float PW)
    {
        string wavesLabel = _survival ? "WAVES SURVIVED:" : "WAVES COMPLETED:";
        string wavesValue = _survival ? _waves.ToString() : $"{_waves} OF {_totalWaves}";

        var rows = new (string label, string value)[]
        {
            ("SCORE:",     _score.ToString("N0")),
            ("TIME:",      FormatTime(_time)),
            ("KILLS:",     _kills.ToString()),
            (wavesLabel,   wavesValue),
        };

        float rowY = -262f;
        const float ROW_H = 40f, ROW_STEP = -46f;
        const float sideMargin = 56f;

        foreach (var (label, value) in rows)
        {
            var lblRt = NewRt(label + "_Lbl", panelT);
            lblRt.anchorMin = lblRt.anchorMax = lblRt.pivot = new Vector2(0f, 1f);
            lblRt.anchoredPosition = new Vector2(sideMargin, rowY);
            lblRt.sizeDelta = new Vector2(PW * 0.5f, ROW_H);
            var lt = lblRt.gameObject.AddComponent<Text>();
            lt.font = uiFont; lt.fontSize = 26; lt.fontStyle = FontStyle.Bold;
            lt.color = Color.white;
            lt.alignment = TextAnchor.MiddleLeft;
            lt.text = label;

            var valRt = NewRt(label + "_Val", panelT);
            valRt.anchorMin = valRt.anchorMax = valRt.pivot = new Vector2(1f, 1f);
            valRt.anchoredPosition = new Vector2(-sideMargin, rowY);
            valRt.sizeDelta = new Vector2(PW * 0.5f, ROW_H);
            var vt = valRt.gameObject.AddComponent<Text>();
            vt.font = uiFont; vt.fontSize = 26; vt.fontStyle = FontStyle.Bold;
            vt.color = new Color(1f, 0.78f, 0.2f, 1f);
            vt.alignment = TextAnchor.MiddleRight;
            vt.text = value;

            rowY += ROW_STEP;
        }
    }

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:D2}";
    }


    private void OnRetry() => StartCoroutine(LoadScene("MainScene"));
    private void OnQuit()  => StartCoroutine(LoadScene("MainMenu"));

    private IEnumerator LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        yield return null;
        SceneManager.LoadScene(sceneName);
    }


    private Button AddButton(Transform parent, string label, float anchoredY, bool highlighted,
                             float bw, float bh, string iconSpriteName)
    {
        var root = NewRt(label, parent);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, anchoredY);
        root.sizeDelta = new Vector2(bw, bh);

        var img = root.gameObject.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>(highlighted ? "UI/Sprites/button_highlighted" : "UI/Sprites/button_base");
        img.type   = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 3f;
        img.color = highlighted ? new Color(1f, 0.28f, 0.08f, 1f) : new Color(1f, 0.28f, 0.08f, 1f);

        var btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor      = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = highlighted ? new Color(1f, 0.35f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.55f);
        colors.pressedColor     = highlighted ? new Color(0.7f, 0.15f, 0.02f, 1f) : new Color(1f, 1f, 1f, 0.75f);
        colors.selectedColor    = new Color(1f, 1f, 1f, 0f);
        colors.colorMultiplier  = 1f;
        btn.colors = colors;

        float rightPad = iconSpriteName != null ? -80f : -10f;
        var txtRt = NewRt("Txt", root.transform);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(10f, 0f);
        txtRt.offsetMax = new Vector2(rightPad, 0f);
        var t = txtRt.gameObject.AddComponent<Text>();
        t.font = uiFont; t.fontSize = 30; t.fontStyle = FontStyle.Bold;
        t.color = Red; t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        if (iconSpriteName != null)
        {
            var iconRt = NewRt("Icon", root.transform);
            iconRt.anchorMin = new Vector2(1f, 0.5f);
            iconRt.anchorMax = new Vector2(1f, 0.5f);
            iconRt.pivot     = new Vector2(1f, 0.5f);
            iconRt.anchoredPosition = new Vector2(-20f, 0f);
            iconRt.sizeDelta = new Vector2(44f, 44f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = Resources.Load<Sprite>("UI/Sprites/" + iconSpriteName);
            iconImg.color  = new Color(0.9f, 0.22f, 0.04f, 1f);
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
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}
