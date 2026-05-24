#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor tool: "Fight or Flight → Setup Game Paused UI"
/// Builds the complete Game Paused Canvas hierarchy in the currently open scene
/// so you can visually inspect and tweak the layout without entering Play mode.
///
/// The runtime script (GamePausedUI.cs) auto-creates the same hierarchy when
/// MainScene starts, so this preview is optional — delete it before shipping.
/// </summary>
public static class GamePausedUISetup
{
    [MenuItem("Fight or Flight/Setup Game Paused UI")]
    private static void SetupGamePausedUI()
    {
        // Remove any existing preview from a prior run
        var existing = Object.FindFirstObjectByType<GamePausedUI>();
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Setup Game Paused UI",
                "A GamePausedUI preview already exists in the scene.\nReplace it?",
                "Replace", "Cancel");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // Root object
        var root = new GameObject("GamePausedUI");
        Undo.RegisterCreatedObjectUndo(root, "Setup Game Paused UI");

        // Canvas
        var canvasGo = new GameObject("PauseCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem (if absent)
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Setup Game Paused UI");
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildPreview(canvasGo.transform, font);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Setup Game Paused UI",
            "Preview created under 'GamePausedUI' in the scene.\n\n" +
            "• The PauseOverlay child is initially hidden — select it and tick Active to preview.\n" +
            "• At runtime, GamePausedUI.cs rebuilds this hierarchy automatically.\n" +
            "• Delete this preview object before shipping (it will be recreated at runtime).",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Preview builder — mirrors GamePausedUI.BuildUI() but runs in Edit mode.
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildPreview(Transform canvasT, Font font)
    {
        BuildPauseButton(canvasT, font);

        // Full-screen overlay
        var overlay = new GameObject("PauseOverlay");
        overlay.transform.SetParent(canvasT, false);
        var rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0.039f, 0.055f, 0.102f, 0.82f);
        overlay.SetActive(false); // hidden until designer toggles it

        const float PW = 370f, PH = 440f;

        var panelRoot = MakeCenteredGo("PanelRoot", overlay.transform, new Vector2(PW, PH));

        MakeCenteredImage(panelRoot.transform, "GlowOuter", new Vector2(PW+14,PH+14),
                          new Color(0f, 1f, 0.831f, 0.06f));
        MakeCenteredImage(panelRoot.transform, "GlowMid",   new Vector2(PW+7, PH+7),
                          new Color(0f, 1f, 0.831f, 0.15f));
        MakeCenteredImage(panelRoot.transform, "Border",    new Vector2(PW,   PH),
                          new Color(0f, 1f, 0.831f, 0.80f));
        var fill = MakeCenteredImage(panelRoot.transform, "Fill",
                                     new Vector2(PW-4, PH-4), SciFiUIStyle.PanelBg);

        BuildPanelContent(fill.gameObject.transform, font);
    }

    private static void BuildPauseButton(Transform canvasT, Font font)
    {
        var go = new GameObject("PauseBtn");
        go.transform.SetParent(canvasT, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(80f, 56f);
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 1f, 0.831f, 0.08f);

        var lblGo = new GameObject("Lbl");
        lblGo.transform.SetParent(go.transform, false);
        var lblRt = lblGo.AddComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
        var t = lblGo.AddComponent<Text>();
        t.font = font; t.fontSize = 28; t.fontStyle = FontStyle.Bold;
        t.color = new Color(0f, 1f, 0.831f, 0.85f);
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "II";
    }

    private static void BuildPanelContent(Transform fillT, Font font)
    {
        AddTopText(fillT, "GAME PAUSED", 46, SciFiUIStyle.Teal, FontStyle.Bold,
                   new Vector2(0f, -30f), new Vector2(340f, 50f), font);

        var lineGo = new GameObject("TitleLine");
        lineGo.transform.SetParent(fillT, false);
        var lineRt = lineGo.AddComponent<RectTransform>();
        lineRt.anchorMin = lineRt.anchorMax = lineRt.pivot = new Vector2(0.5f, 1f);
        lineRt.anchoredPosition = new Vector2(0f, -88f);
        lineRt.sizeDelta = new Vector2(330f, 2f);
        lineGo.AddComponent<Image>().color = new Color(0f, 1f, 0.831f, 0.38f);

        const float Y0 = -106f, Step = -76f;
        AddButton(fillT, "RESUME  >>",      Y0,          true,  font);
        AddButton(fillT, "SETTINGS  *",     Y0+Step,     false, font);
        AddButton(fillT, "RESTART WAVE",    Y0+Step*2f,  false, font);
        AddButton(fillT, "QUIT TO MENU  >", Y0+Step*3f,  false, font);
    }

    private static void AddButton(Transform parent, string label, float anchoredY,
                                  bool highlighted, Font font)
    {
        const float BW = 318f, BH = 58f;

        var cont = new GameObject("BtnCont_" + label.Split(' ')[0]);
        cont.transform.SetParent(parent, false);
        var contRt = cont.AddComponent<RectTransform>();
        contRt.anchorMin = contRt.anchorMax = contRt.pivot = new Vector2(0.5f, 1f);
        contRt.anchoredPosition = new Vector2(0f, anchoredY);
        contRt.sizeDelta = new Vector2(BW, BH);

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(cont.transform, false);
        var borderRt = borderGo.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-2f,-2f); borderRt.offsetMax = new Vector2(2f,2f);
        borderGo.AddComponent<Image>().color = highlighted
            ? new Color(SciFiUIStyle.GreenGlow.r, SciFiUIStyle.GreenGlow.g,
                        SciFiUIStyle.GreenGlow.b, 0.88f)
            : new Color(SciFiUIStyle.Teal.r, SciFiUIStyle.Teal.g, SciFiUIStyle.Teal.b, 0.30f);

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(cont.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bgGo.AddComponent<Image>().color = highlighted
            ? new Color(0.04f, 0.22f, 0.06f, 0.92f)
            : SciFiUIStyle.DimButtonBg;

        var lblGo = new GameObject("Lbl");
        lblGo.transform.SetParent(bgGo.transform, false);
        var lblRt = lblGo.AddComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
        var txt = lblGo.AddComponent<Text>();
        txt.font = font; txt.fontSize = highlighted ? 28 : 24;
        txt.fontStyle = FontStyle.Bold;
        txt.color = highlighted ? SciFiUIStyle.GreenGlow : SciFiUIStyle.DimText;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = label;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject MakeCenteredGo(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return go;
    }

    private static Image MakeCenteredImage(Transform parent, string name,
                                           Vector2 size, Color color)
    {
        var go = MakeCenteredGo(name, parent, size);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static void AddTopText(Transform parent, string content, int size, Color color,
                                   FontStyle style, Vector2 anchoredPos, Vector2 sizeDelta,
                                   Font font)
    {
        var go = new GameObject("Txt_" + content.Split(' ')[0]);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.color = color;
        t.fontStyle = style; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        t.text = content;
    }
}
#endif
