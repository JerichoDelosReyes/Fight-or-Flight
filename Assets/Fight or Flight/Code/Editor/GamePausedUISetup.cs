#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor tool: "Fight or Flight → Setup Game Paused UI"
/// Builds a live-editable preview of the pause panel in the currently open scene.
/// The preview mirrors GamePausedUI.cs exactly — sprites, sizes, and layout.
///
/// HOW TO USE:
///   1. Click  Fight or Flight → Setup Game Paused UI
///   2. In the Hierarchy find  GamePausedUI_Preview → PauseCanvas → PauseOverlay
///   3. Tick "Active" on PauseOverlay in the Inspector to make it visible
///   4. Select any child to move / resize / recolour it in the Scene view (just like Figma)
///   5. When you're happy, copy the numeric values back into GamePausedUI.cs
///   6. Delete the preview object before shipping
/// </summary>
public static class GamePausedUISetup
{
    [MenuItem("Fight or Flight/Setup Game Paused UI")]
    private static void SetupGamePausedUI()
    {
        var existing = GameObject.Find("GamePausedUI_Preview");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Setup Game Paused UI",
                "A preview already exists in the scene.\nReplace it?",
                "Replace", "Cancel");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        var root = new GameObject("GamePausedUI_Preview");
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

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Setup Game Paused UI");
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildPauseButtonPreview(canvasGo.transform, font);
        BuildPauseOverlayPreview(canvasGo.transform, font);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Setup Game Paused UI",
            "Preview created under 'GamePausedUI_Preview'.\n\n" +
            "• Find PauseCanvas → PauseOverlay in the Hierarchy\n" +
            "• Tick 'Active' in the Inspector to show the panel\n" +
            "• Select any child element to drag/resize it (like Figma)\n" +
            "• Copy changed values back into GamePausedUI.cs to make them permanent\n" +
            "• Delete this preview before shipping",
            "OK");
    }

    // ─── Pause button (top-left corner) ──────────────────────────────────────

    private static void BuildPauseButtonPreview(Transform canvasT, Font font)
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

        var lblRt = NewRt("Lbl", go.transform, font);
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
        var t = lblRt.gameObject.GetComponent<Text>();
        t.fontSize = 32; t.fontStyle = FontStyle.Bold;
        t.color = new Color(0f, 1f, 0.831f, 0.85f);
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "II";
    }

    // ─── Pause overlay panel ─────────────────────────────────────────────────

    private static void BuildPauseOverlayPreview(Transform canvasT, Font font)
    {
        // Full-screen dimmer — starts INACTIVE so it doesn't block the scene
        var overlay = new GameObject("PauseOverlay");
        overlay.transform.SetParent(canvasT, false);
        var ort = overlay.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0f, 0.02f, 0.06f, 0.78f);
        overlay.SetActive(false);

        // Panel
        const float PW = 560f, PH = 580f;
        var panelRt = NewRtBlank("Panel", overlay.transform);
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(PW, PH);

        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.sprite = Resources.Load<Sprite>("UI/Sprites/panel_background");
        panelImg.type = Image.Type.Sliced;
        if (panelImg.sprite == null) panelImg.color = new Color(0.04f, 0.12f, 0.14f, 0.97f);
        else panelImg.color = Color.white;

        // "GAME PAUSED" — plain text, extra top padding
        var headerRt = NewRt("Header", panelRt.transform, font);
        headerRt.anchorMin = headerRt.anchorMax = headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -32f);
        headerRt.sizeDelta = new Vector2(PW - 30f, 64f);
        var ht = headerRt.gameObject.GetComponent<Text>();
        ht.fontSize = 40; ht.fontStyle = FontStyle.Bold;
        ht.color = new Color(0f, 1f, 0.831f, 1f);
        ht.alignment = TextAnchor.MiddleCenter;
        ht.text = "GAME PAUSED";
        ht.horizontalOverflow = HorizontalWrapMode.Overflow;

        // Divider
        var divRt = NewRtBlank("Divider", panelRt.transform);
        divRt.anchorMin = divRt.anchorMax = divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -96f);
        divRt.sizeDelta = new Vector2(PW - 40f, 2f);
        divRt.gameObject.AddComponent<Image>().color = new Color(0f, 1f, 0.831f, 0.38f);

        // Buttons — vertically centered below divider
        const float BW = PW - 44f, BH = 70f;
        const float BY = -136f, STEP = -96f;

        AddButtonPreview(panelRt.transform, "RESUME",       BY,          true,  BW, BH, "resume_icon",   font);
        AddButtonPreview(panelRt.transform, "SETTINGS",     BY + STEP,   false, BW, BH, "settings_icon", font);
        AddButtonPreview(panelRt.transform, "RESTART WAVE", BY + STEP*2, false, BW, BH, "restart_icon",  font);
        AddButtonPreview(panelRt.transform, "QUIT TO MENU", BY + STEP*3, false, BW, BH, "quit_icon",     font);
    }

    private static void AddButtonPreview(Transform parent, string label, float anchoredY,
                                         bool highlighted, float bw, float bh,
                                         string iconSpriteName, Font font)
    {
        var root = NewRtBlank(label, parent);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, anchoredY);
        root.sizeDelta = new Vector2(bw, bh);

        var img = root.gameObject.AddComponent<Image>();
        img.sprite = Resources.Load<Sprite>(highlighted ? "UI/Sprites/button_highlighted" : "UI/Sprites/button_base");
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 3f;
        img.color = Color.white;

        var btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        // Mirror runtime ColorBlock: non-highlighted buttons are transparent in normal state
        // Preview at 40% so button bounds are visible in editor; runtime starts at 0%
        var colors = btn.colors;
        colors.normalColor      = new Color(1f, 1f, 1f, 0.4f);
        colors.highlightedColor = highlighted ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        colors.pressedColor     = highlighted ? new Color(0.6f, 0.9f, 0.35f, 1f) : new Color(1f, 1f, 1f, 0.75f);
        colors.colorMultiplier  = 1f;
        btn.colors = colors;

        // Text — centered
        float rightPad = iconSpriteName != null ? -80f : -10f;
        var txtRt = NewRt("Txt", root.transform, font);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(10f, 0f);
        txtRt.offsetMax = new Vector2(rightPad, 0f);
        var t = txtRt.gameObject.GetComponent<Text>();
        t.fontSize = 36;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(0f, 1f, 0.831f, 1f);
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        // Icon (skipped for RESTART WAVE which has no icon in the reference)
        if (iconSpriteName != null)
        {
            var iconRt = NewRtBlank("Icon", root.transform);
            iconRt.anchorMin = new Vector2(1f, 0.5f);
            iconRt.anchorMax = new Vector2(1f, 0.5f);
            iconRt.pivot     = new Vector2(1f, 0.5f);
            iconRt.anchoredPosition = new Vector2(-20f, 0f);
            iconRt.sizeDelta = new Vector2(56f, 56f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = Resources.Load<Sprite>("UI/Sprites/" + iconSpriteName);
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            if (iconImg.sprite == null) iconImg.enabled = false;
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    // Creates a RectTransform + Text component (Text is ready to configure after)
    private static RectTransform NewRt(string name, Transform parent, Font font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = font;
        return go.GetComponent<RectTransform>();
    }

    // Creates a bare RectTransform
    private static RectTransform NewRtBlank(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }
}
#endif
