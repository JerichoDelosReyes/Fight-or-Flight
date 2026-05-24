using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds and shows the defeat overlay entirely in code — no prefab required.
/// Called statically by ShipHealth when the player dies.
/// </summary>
public class DefeatScreen : MonoBehaviour
{
    private static DefeatScreen instance;

    public static void Show(int score, int kills)
    {
        if (instance != null) return;
        var go = new GameObject("DefeatScreen");
        instance = go.AddComponent<DefeatScreen>();
        instance.Init(score, kills);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private Font uiFont;

    // Red / orange palette matching the reference
    private static readonly Color Red    = new Color(0.90f, 0.22f, 0.04f, 1f);
    private static readonly Color RedDim = new Color(0.90f, 0.22f, 0.04f, 0.38f);

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Init(int score, int kills)
    {
        BuildUI();
    }

    private void OnDestroy() { instance = null; }

    // ─── UI construction ──────────────────────────────────────────────────────

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

        // Full-screen dark dimmer
        var dimmerGo = new GameObject("Dimmer");
        dimmerGo.transform.SetParent(transform, false);
        var drt = dimmerGo.AddComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;
        dimmerGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        // Panel
        const float PW = 480f, PH = 560f;
        var panelRt = NewRt("Panel", transform);
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(PW, PH);

        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.sprite = Resources.Load<Sprite>("UI/Sprites/panel_background");
        panelImg.type   = Image.Type.Sliced;
        // Red tint over the panel sprite
        if (panelImg.sprite == null) panelImg.color = new Color(0.12f, 0.03f, 0.02f, 0.97f);
        else                         panelImg.color = new Color(1f, 0.35f, 0.2f, 1f);

        // "MISSION FAILED" header — no border box, just bold text
        var headerRt = NewRt("Header", panelRt.transform);
        headerRt.anchorMin = headerRt.anchorMax = headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -28f);
        headerRt.sizeDelta = new Vector2(PW - 30f, 60f);
        var ht = headerRt.gameObject.AddComponent<Text>();
        ht.font = uiFont; ht.fontSize = 40; ht.fontStyle = FontStyle.Bold;
        ht.color = Red; ht.alignment = TextAnchor.MiddleCenter;
        ht.text = "MISSION FAILED";
        ht.horizontalOverflow = HorizontalWrapMode.Overflow;

        // Divider below header
        var divTopRt = NewRt("DivTop", panelRt.transform);
        divTopRt.anchorMin = divTopRt.anchorMax = divTopRt.pivot = new Vector2(0.5f, 1f);
        divTopRt.anchoredPosition = new Vector2(0f, -100f);
        divTopRt.sizeDelta = new Vector2(PW - 40f, 2f);
        divTopRt.gameObject.AddComponent<Image>().color = RedDim;

        // Helmet icon — centered, large
        var helmetRt = NewRt("Helmet", panelRt.transform);
        helmetRt.anchorMin = helmetRt.anchorMax = helmetRt.pivot = new Vector2(0.5f, 1f);
        helmetRt.anchoredPosition = new Vector2(0f, -118f);
        helmetRt.sizeDelta = new Vector2(180f, 180f); // Larger focus
        var helmetImg = helmetRt.gameObject.AddComponent<Image>();
        helmetImg.sprite = Resources.Load<Sprite>("UI/Sprites/defeat_helmet_new");
        helmetImg.preserveAspect = true;
        helmetImg.raycastTarget  = false;
        helmetImg.color          = Color.white; // The sprite itself is red/glowy now

        // Buttons — vertically centered in space below the helmet
        const float BW = PW - 44f, BH = 60f;
        const float BY = -313f, STEP = -74f;

        var retryBtn   = AddButton(panelRt.transform, "RETRY WAVE",     BY,        true,  BW, BH, null);
        var quitBtn    = AddButton(panelRt.transform, "QUIT TO MENU",   BY + STEP, false, BW, BH, null);
        var replayBtn  = AddButton(panelRt.transform, "REPLAY MISSION", BY+STEP*2, false, BW, BH, null);

        retryBtn.onClick.AddListener(OnRetry);
        quitBtn.onClick.AddListener(OnQuit);
        replayBtn.onClick.AddListener(OnReplay);
    }

    // ─── Button actions ───────────────────────────────────────────────────────

    private void OnRetry()   => StartCoroutine(LoadScene("MainScene"));
    private void OnQuit()    => StartCoroutine(LoadScene("MainMenu"));
    private void OnReplay()  => StartCoroutine(LoadScene("MainScene"));

    private IEnumerator LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        yield return null;
        SceneManager.LoadScene(sceneName);
    }

    // ─── Button builder ───────────────────────────────────────────────────────

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
        // Red tint over the button sprites
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

        // Text
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

        // Icon
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
}
