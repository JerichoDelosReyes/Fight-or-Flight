using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string startSceneName = "MainScene";

    private GameObject instrOverlay;

    private void Start()
    {
        EnsureSettingsButton();
        ApplyMenuPolish();
    }

    // ── Polish pass ───────────────────────────────────────────────────────────
    //
    // The scene-baked main menu has flat un-styled buttons and a plain title.
    // We patch it at runtime: resize buttons (Start bigger than the rest),
    // tweak hover colors, add a subtle dark gradient at screen edges, a
    // pulsing glow on the FIGHT OR FLIGHT title, and a "v1.0" tag bottom-right.

    private const string Version = "v1.0";

    private void ApplyMenuPolish()
    {
        // 1. Background gradient removed as per user request.

        // 2. Resize + restyle the buttons. All buttons are now unified in size.
        Vector2 unifiedSize = new Vector2(560f, 96f);
        int unifiedFontSize = 38;

        StyleButton("StartButton",        unifiedSize, unifiedFontSize, new Color(0.16f, 0.50f, 0.95f), new Color(0.30f, 0.65f, 1.00f));
        StyleButton("InstructionsButton", unifiedSize, unifiedFontSize, new Color(0.20f, 0.20f, 0.30f), new Color(0.38f, 0.38f, 0.55f));
        StyleButton("SettingsButton",     unifiedSize, unifiedFontSize, new Color(0.20f, 0.20f, 0.30f), new Color(0.38f, 0.38f, 0.55f));
        StyleButton("QuitButton",         unifiedSize, unifiedFontSize, new Color(0.35f, 0.15f, 0.18f), new Color(0.55f, 0.25f, 0.30f));

        RepositionButtons();

        // 3. Title pulse (search for the FIGHT title text anywhere in the scene)
        TryAddTitlePulse();

        // 4. Version tag bottom-right
        AddVersionLabel();
    }

    private void RepositionButtons()
    {
        // Vertical layout: Lowered position with equal spacing between all 4 buttons.
        // Screen center is 0, bottom is -540.
        // Using startY=-130 and spacing=120 ensures the bottom button (Quit) 
        // stays just above the screen edge.
        float startY = -30f;
        float spacing = 130f;

        SetButtonY("StartButton",        startY);
        SetButtonY("InstructionsButton", startY - spacing);
        SetButtonY("SettingsButton",     startY - spacing * 2);
        SetButtonY("QuitButton",         startY - spacing * 3);
    }

    private void SetButtonY(string goName, float y)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(0, y);
    }

    private static void StyleButton(string goName, Vector2 size, int fontSize,
                                    Color normal, Color highlighted)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;

        // Resize
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = size;

        // Bigger, rounder visual via a procedurally rounded sprite + recolour
        var img = go.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = RoundedRectSprite.Get();
            img.type   = Image.Type.Sliced;
            img.color  = normal;
        }

        // Hover / pressed colors
        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            var cols = btn.colors;
            cols.normalColor      = Color.white;
            cols.highlightedColor = new Color(highlighted.r / normal.r, highlighted.g / normal.g, highlighted.b / normal.b, 1f);
            cols.pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f);
            cols.colorMultiplier  = 1f;
            cols.fadeDuration     = 0.1f;
            btn.colors = cols;
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
        }

        // Make the child Text reflect the requested font size
        foreach (var t in go.GetComponentsInChildren<Text>(true))
        {
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
        }
    }

    private void TryAddTitlePulse()
    {
        // 1. Check for the specific V2 image title
        var v2Title = GameObject.Find("TitleFightOrFlight_V2");
        if (v2Title != null && v2Title.GetComponent<TitlePulse>() == null)
        {
            v2Title.AddComponent<TitlePulse>();
        }

        // 2. Fallback for any other text titles containing FIGHT/FLIGHT
        foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
        {
            if (t == null) continue;
            string up = (t.text ?? "").ToUpperInvariant();
            if (up.Contains("FIGHT") || up.Contains("FLIGHT"))
            {
                if (t.GetComponent<TitlePulse>() == null)
                    t.gameObject.AddComponent<TitlePulse>();
            }
        }
    }

    private void AddVersionLabel()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Don't double-add on scene reload.
        if (GameObject.Find("VersionLabel") != null) return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("VersionLabel");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-22f, 18f);
        rt.sizeDelta = new Vector2(120f, 26f);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = 18;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(1f, 1f, 1f, 0.55f);
        t.alignment = TextAnchor.MiddleRight;
        t.text = Version;
        t.raycastTarget = false;
    }

    public void StartGame()
    {
        SceneManager.LoadScene(startSceneName);
    }

    public void OpenInstructions()
    {
        if (instrOverlay != null) return;
        instrOverlay = BuildInstructionsOverlay();
    }

    public void OpenSettings()
    {
        SettingsMenu.Show();
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // ── Instructions overlay ──────────────────────────────────────────────────

    private GameObject BuildInstructionsOverlay()
    {
        // Root canvas
        var root = new GameObject("InstructionsOverlay");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Dim background
        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(root.transform, false);
        var dimRt = dimGo.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
        dimGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // Panel
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(root.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(900f, 660f);
        panelGo.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.97f);

        // Title
        AddLabel(panelGo.transform, font, "CONTROLS", 52, new Color(1f, 0.9f, 0.3f),
                 FontStyle.Bold, new Vector2(0f, 290f), new Vector2(860f, 60f));

        // Divider
        var divGo = new GameObject("Div");
        divGo.transform.SetParent(panelGo.transform, false);
        var divRt = divGo.AddComponent<RectTransform>();
        divRt.anchorMin = divRt.anchorMax = new Vector2(0.5f, 0.5f);
        divRt.anchoredPosition = new Vector2(0f, 255f);
        divRt.sizeDelta = new Vector2(800f, 2f);
        divGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);

        // Two columns of control text
        string leftHeader  = "KEYBOARD ONLY MODE";
        string leftBody    =
            "W / S            Pitch (nose up / down)\n" +
            "A / D             Yaw (turn left / right)\n" +
            "Q / E             Roll left / right\n" +
            "Left Shift       Thrust forward\n" +
            "Space            Fire lasers";

        string rightHeader = "MOUSE + KEYBOARD MODE";
        string rightBody   =
            "W / S            Throttle forward / back\n" +
            "A / D             Strafe left / right\n" +
            "Mouse            Aim (FPS-style)\n" +
            "Q / E             Roll left / right\n" +
            "Left Shift       Boost\n" +
            "LMB / Space    Fire lasers";

        string generalHeader = "GENERAL";
        string generalBody   =
            "Escape / Pause button    Pause game\n" +
            "Volume and control options can be changed in Settings from the main menu.";

        // Left column
        AddLabel(panelGo.transform, font, leftHeader, 26, new Color(0.5f, 0.8f, 1f),
                 FontStyle.Bold, new Vector2(-220f, 185f), new Vector2(380f, 36f));
        AddLabel(panelGo.transform, font, leftBody, 22, Color.white,
                 FontStyle.Normal, new Vector2(-220f, 50f), new Vector2(400f, 260f));

        // Right column
        AddLabel(panelGo.transform, font, rightHeader, 26, new Color(0.7f, 0.5f, 1f),
                 FontStyle.Bold, new Vector2(220f, 185f), new Vector2(400f, 36f));
        AddLabel(panelGo.transform, font, rightBody, 22, Color.white,
                 FontStyle.Normal, new Vector2(220f, 40f), new Vector2(420f, 300f));

        // General section
        AddLabel(panelGo.transform, font, generalHeader, 26, new Color(1f, 0.75f, 0.4f),
                 FontStyle.Bold, new Vector2(0f, -185f), new Vector2(860f, 36f));
        AddLabel(panelGo.transform, font, generalBody, 21, new Color(0.85f, 0.85f, 0.85f),
                 FontStyle.Normal, new Vector2(0f, -240f), new Vector2(820f, 56f));

        // Close button
        var closeBtnGo = new GameObject("CloseBtn");
        closeBtnGo.transform.SetParent(panelGo.transform, false);
        var closeBtnRt = closeBtnGo.AddComponent<RectTransform>();
        closeBtnRt.anchorMin = closeBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
        closeBtnRt.pivot = new Vector2(0.5f, 0.5f);
        closeBtnRt.anchoredPosition = new Vector2(0f, -295f);
        closeBtnRt.sizeDelta = new Vector2(200f, 52f);
        var closeBtnImg = closeBtnGo.AddComponent<Image>();
        closeBtnImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        var cc = closeBtn.colors;
        cc.highlightedColor = new Color(0.4f, 0.4f, 0.4f); closeBtn.colors = cc;
        closeBtn.onClick.AddListener(() => { Destroy(root); instrOverlay = null; });
        AddLabel(closeBtnGo.transform, font, "CLOSE", 28, Color.white,
                 FontStyle.Bold, Vector2.zero, new Vector2(200f, 52f));

        return root;
    }

    private static void AddLabel(Transform parent, Font font, string text, int size,
                                  Color colour, FontStyle style, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.text = text; t.font = font; t.fontSize = size;
        t.color = colour; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // ── Settings button injection ─────────────────────────────────────────────
    // The MainMenu scene was authored with Play / Instructions / Quit buttons
    // but no Settings button. At runtime we clone the Instructions button,
    // move Quit to the bottom slot, and place Settings above it.
    private void EnsureSettingsButton()
    {
        var existing = GameObject.Find("SettingsButton");
        if (existing != null) return;

        var instructions = GameObject.Find("InstructionsButton");
        if (instructions == null) return;

        // Push Quit to the bottom slot so Settings can sit above it.
        // Scene order: Play=-40, Instructions=-160, Quit=-280 → Quit moves to -400.
        var quit = GameObject.Find("QuitButton");
        if (quit != null)
        {
            var qrt = quit.GetComponent<RectTransform>();
            if (qrt != null) qrt.anchoredPosition = new Vector2(0, -400);
        }

        var clone = Instantiate(instructions, instructions.transform.parent);
        clone.name = "SettingsButton";

        // Settings sits where Quit used to be (y=-280), above the new Quit (y=-400).
        var rt = clone.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(0, -280);

        foreach (var text in clone.GetComponentsInChildren<Text>(true))
            text.text = "SETTINGS";

        // Strip the old Button (retains serialized persistent listeners from the scene)
        // and add a fresh one wired to OpenSettings.
        var oldBtn = clone.GetComponent<Button>();
        if (oldBtn != null)
        {
            var colors          = oldBtn.colors;
            var transition      = oldBtn.transition;
            var navigation      = oldBtn.navigation;
            var targetGraphic   = oldBtn.targetGraphic;
            var spriteState     = oldBtn.spriteState;
            var animationTriggers = oldBtn.animationTriggers;

            DestroyImmediate(oldBtn);

            var btn = clone.AddComponent<Button>();
            btn.colors = colors;
            btn.transition = transition;
            btn.navigation = navigation;
            btn.targetGraphic = targetGraphic;
            btn.spriteState = spriteState;
            btn.animationTriggers = animationTriggers;
            btn.onClick.AddListener(OpenSettings);
        }
    }
}
