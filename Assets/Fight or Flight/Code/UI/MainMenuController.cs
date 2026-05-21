using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenuController : MonoBehaviour
{
    public string startSceneName = "MainScene";
    public Sprite buttonFrameSprite;
    public Font menuFont;

    private GameObject instrOverlay;

    private void Start()
    {
        InitializeAssets();
        EnsureSettingsButton();
        ApplyMenuPolish();
    }

    private void Update()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            InitializeAssets();
            EnsureSettingsButton();
            ApplyMenuPolish();
        }
        #endif
    }

    private void InitializeAssets()
    {
        // Fallback for sprite if not assigned
        if (buttonFrameSprite == null)
        {
            #if UNITY_EDITOR
            buttonFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Fight or Flight/Content/UI/Sprites/SciFiButtonFrame.png");
            #endif
        }

        // Fallback for font if not assigned
        if (menuFont == null)
        {
            #if UNITY_EDITOR
            menuFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fight or Flight/Content/GLB/Inter-VariableFont_opsz,wght.ttf");
            #endif
        }
    }

    // ── Polish pass ───────────────────────────────────────────────────────────
    private const string Version = "v1.0";

    private void ApplyMenuPolish()
    {
        Vector2 unifiedSize = new Vector2(560f, 96f);
        int unifiedFontSize = 38;

        Color startColor = new Color(0.0f, 1.0f, 1.0f, 1.0f); // Cyan
        Color otherColor = new Color(0.63f, 0.63f, 1.0f, 1.0f); // Blue/Purple
        Color quitColor = new Color(1.0f, 0.31f, 0.31f, 1.0f); // Pink/Red

        StyleButton("StartButton",        unifiedSize, unifiedFontSize, startColor, startColor * 1.2f);
        StyleButton("InstructionsButton", unifiedSize, unifiedFontSize, otherColor, otherColor * 1.2f);
        StyleButton("SettingsButton",     unifiedSize, unifiedFontSize, otherColor, otherColor * 1.2f);
        StyleButton("QuitButton",         unifiedSize, unifiedFontSize, quitColor, quitColor * 1.2f);

        RepositionButtons();

        if (Application.isPlaying)
        {
            TryAddTitlePulse();
            AddVersionLabel();
        }
    }

    private void RepositionButtons()
    {
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

    private void StyleButton(string goName, Vector2 size, int fontSize, Color normal, Color highlighted)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = size;

        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            if (buttonFrameSprite != null)
            {
                img.sprite = buttonFrameSprite;
                img.type = UnityEngine.UI.Image.Type.Sliced;
            }
            img.color = normal;
            img.raycastTarget = false;
        }

        // Hitbox implementation
        GameObject hitboxGo = null;
        Transform hitboxTransform = go.transform.Find("Hitbox");
        if (hitboxTransform != null) hitboxGo = hitboxTransform.gameObject;
        else
        {
            hitboxGo = new GameObject("Hitbox");
            hitboxGo.transform.SetParent(go.transform, false);
        }

        var hitboxRt = hitboxGo.GetComponent<RectTransform>();
        if (hitboxRt == null) hitboxRt = hitboxGo.AddComponent<RectTransform>();
        hitboxRt.anchorMin = hitboxRt.anchorMax = hitboxRt.pivot = new Vector2(0.5f, 0.5f);
        hitboxRt.anchoredPosition = Vector2.zero;
        hitboxRt.sizeDelta = new Vector2(size.x * 0.8f, size.y * 0.7f);

        var hitboxImg = hitboxGo.GetComponent<UnityEngine.UI.Image>();
        if (hitboxImg == null) hitboxImg = hitboxGo.AddComponent<UnityEngine.UI.Image>();
        hitboxImg.color = new Color(0, 0, 0, 0);
        hitboxImg.raycastTarget = true;

        var btn = go.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(highlighted.r / normal.r, highlighted.g / normal.g, highlighted.b / normal.b, 1f);
            cols.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cols.colorMultiplier = 1f;
            cols.fadeDuration = 0.1f;
            btn.colors = cols;
            btn.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
        }

        foreach (var t in go.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            if (t.gameObject.name == "Hitbox") continue;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            if (menuFont != null) t.font = menuFont;
            t.raycastTarget = false;
        }
    }

    private void TryAddTitlePulse()
    {
        var v2Title = GameObject.Find("TitleFightOrFlight_V2");
        if (v2Title != null && v2Title.GetComponent<TitlePulse>() == null) v2Title.AddComponent<TitlePulse>();

        foreach (var t in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include))
        {
            if (t == null) continue;
            string up = (t.text ?? "").ToUpperInvariant();
            if (up.Contains("FIGHT") || up.Contains("FLIGHT"))
            {
                if (t.GetComponent<TitlePulse>() == null) t.gameObject.AddComponent<TitlePulse>();
            }
        }
    }

    private void AddVersionLabel()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        if (GameObject.Find("VersionLabel") != null) return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("VersionLabel");
        go.transform.SetParent(canvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-22f, 18f);
        rt.sizeDelta = new Vector2(120f, 26f);
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.font = font; t.fontSize = 18; t.fontStyle = FontStyle.Bold;
        t.color = new Color(1f, 1f, 1f, 0.55f);
        t.alignment = TextAnchor.MiddleRight;
        t.text = Version;
        t.raycastTarget = false;
    }

    public void StartGame() { SceneManager.LoadScene(startSceneName); }
    public void OpenInstructions() { if (instrOverlay == null) instrOverlay = BuildInstructionsOverlay(); }
    public void OpenSettings() { SettingsMenu.Show(); }
    public void QuitGame() { 
        #if UNITY_EDITOR 
        UnityEditor.EditorApplication.isPlaying = false; 
        #else 
        Application.Quit(); 
        #endif 
    }

    private GameObject BuildInstructionsOverlay()
    {
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
        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(root.transform, false);
        var dimRt = dimGo.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
        dimGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.75f);

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(root.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(900f, 660f);
        panelGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.97f);

        AddLabel(panelGo.transform, font, "CONTROLS", 52, new Color(1f, 0.9f, 0.3f), FontStyle.Bold, new Vector2(0f, 290f), new Vector2(860f, 60f));

        var closeBtnGo = new GameObject("CloseBtn");
        closeBtnGo.transform.SetParent(panelGo.transform, false);
        var closeBtnRt = closeBtnGo.AddComponent<RectTransform>();
        closeBtnRt.anchorMin = closeBtnRt.anchorMax = closeBtnRt.pivot = new Vector2(0.5f, 0.5f);
        closeBtnRt.anchoredPosition = new Vector2(0f, -295f);
        closeBtnRt.sizeDelta = new Vector2(200f, 52f);
        var closeBtnImg = closeBtnGo.AddComponent<UnityEngine.UI.Image>();
        closeBtnImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var closeBtn = closeBtnGo.AddComponent<UnityEngine.UI.Button>();
        closeBtn.targetGraphic = closeBtnImg;
        var cc = closeBtn.colors; cc.highlightedColor = new Color(0.4f, 0.4f, 0.4f); closeBtn.colors = cc;
        closeBtn.onClick.AddListener(() => { DestroyImmediate(root); instrOverlay = null; });
        AddLabel(closeBtnGo.transform, font, "CLOSE", 28, Color.white, FontStyle.Bold, Vector2.zero, new Vector2(200f, 52f));

        return root;
    }

    private static void AddLabel(Transform parent, Font font, string text, int size, Color colour, FontStyle style, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.text = text; t.font = font; t.fontSize = size; t.color = colour; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void EnsureSettingsButton()
    {
        if (GameObject.Find("SettingsButton") != null) return;
        var instructions = GameObject.Find("InstructionsButton");
        if (instructions == null) return;

        var quit = GameObject.Find("QuitButton");
        if (quit != null)
        {
            var qrt = quit.GetComponent<RectTransform>();
            if (qrt != null) qrt.anchoredPosition = new Vector2(0, -400);
        }

        GameObject clone = Instantiate(instructions, instructions.transform.parent);
        clone.name = "SettingsButton";
        var rt = clone.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(0, -280);

        foreach (var text in clone.GetComponentsInChildren<UnityEngine.UI.Text>(true)) text.text = "SETTINGS";

        var btn = clone.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OpenSettings);
        }
    }
}
