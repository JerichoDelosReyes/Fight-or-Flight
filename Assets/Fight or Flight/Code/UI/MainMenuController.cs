using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenuController : MonoBehaviour
{
    public string startSceneName = "MainScene";
    public Sprite buttonFrameSprite;
    public Font menuFont;

    [Header("UI Prefabs")]
    public GameObject instructionsPrefab;
    public GameObject settingsPrefab;

    [Header("Sci-Fi UI Assets")]
    public Sprite panelFrameSprite;
    public Sprite headerBarSprite;
    public Sprite buttonLargeSprite;
    public Sprite dividerSprite;

    private GameObject instrOverlay;
    private GameObject modeOverlay;

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
            buttonFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Fight or Flight/Content/Sprites/UI/SciFiButtonFrame.png");
            #endif
        }

        // Fallback for font if not assigned
        if (menuFont == null)
        {
            #if UNITY_EDITOR
            menuFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fight or Flight/Content/Models/Inter-VariableFont_opsz,wght.ttf");
            #endif
        }

        // Load new Sci-Fi assets from Resources
        if (panelFrameSprite == null)  panelFrameSprite  = Resources.Load<Sprite>("RootResources/SciFiUI/panel_frame");
        if (headerBarSprite == null)   headerBarSprite   = Resources.Load<Sprite>("RootResources/SciFiUI/header_bar");
        if (buttonLargeSprite == null) buttonLargeSprite = Resources.Load<Sprite>("RootResources/SciFiUI/button_large");
        if (dividerSprite == null)     dividerSprite     = Resources.Load<Sprite>("RootResources/SciFiUI/divider");
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

    // Start Game now opens the mode picker instead of loading the scene directly.
    // The chosen mode's button is what actually loads MainScene.
    public void StartGame() { OpenModeSelect(); }

    public void OpenModeSelect()
    {
        if (modeOverlay != null) return;
        modeOverlay = BuildModeSelectOverlay();
    }

    private void LaunchMode(GameModeManager.Mode mode)
    {
        GameModeManager.Select(mode);
        if (modeOverlay != null) { Destroy(modeOverlay); modeOverlay = null; }
        SceneManager.LoadScene(startSceneName);
    }

    private GameObject BuildModeSelectOverlay()
    {
        var root = new GameObject("ModeSelectOverlay");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;
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
        panelRt.sizeDelta = new Vector2(980f, 800f);
        var panelImg = panelGo.AddComponent<UnityEngine.UI.Image>();
        panelImg.sprite = panelFrameSprite;
        panelImg.type = UnityEngine.UI.Image.Type.Sliced;
        panelImg.color = Color.white;

        AddLabel(panelGo.transform, font, "SELECT MODE", 50, new Color(0.3f, 1f, 1f), FontStyle.Bold,
                 new Vector2(0f, 323f), new Vector2(750f, 65f));

        // CAMPAIGN — always available.
        var campaign = AddModeCard(panelGo.transform, font,
            "CAMPAIGN", "Be the last ship standing.",
            "Fight through 5 increasingly difficult waves of enemy ships.\nClear all waves to complete the run and unlock Survival Mode.",
            new Vector2(0f, 138f), new Color(0f, 0.88f, 1f, 1f), true);
        campaign.onClick.AddListener(() => LaunchMode(GameModeManager.Mode.Campaign));

        // SURVIVAL — locked until Campaign is cleared.
        bool unlocked = GameModeManager.SurvivalUnlocked;
        string survTag  = unlocked ? "Survive the Onslaught"  : "LOCKED - COMPLETE CAMPAIGN FIRST";
        string survDesc = unlocked
            ? "Face never-ending waves of enemies. No lives, no limits - survive\nas long as you can and chase the highest score."
            : "Prove yourself in Campaign first.\nClear all 5 waves to unlock this mode and face the endless onslaught.";
        Color survAccent = unlocked ? new Color(0f, 1f, 0.65f, 1f) : new Color(0.45f, 0.45f, 0.45f, 1f);
        var survival = AddModeCard(panelGo.transform, font,
            "SURVIVAL", survTag, survDesc,
            new Vector2(0f, -138f), survAccent, unlocked);
        if (unlocked)
            survival.onClick.AddListener(() => LaunchMode(GameModeManager.Mode.Survival));

        // CLOSE — matches SettingsMenu close button style exactly
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(panelGo.transform, false);
        var closeRt = closeGo.AddComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(0.5f, 0.5f);
        closeRt.anchoredPosition = new Vector2(0f, -335f);
        closeRt.sizeDelta = new Vector2(350f, 70f);
        var closeImg = closeGo.AddComponent<UnityEngine.UI.Image>();
        closeImg.sprite = buttonLargeSprite;
        closeImg.type = UnityEngine.UI.Image.Type.Sliced;
        closeImg.color = new Color(0.2980392f, 1f, 1f, 1f);
        closeImg.raycastTarget = false;
        var closeBtn = closeGo.AddComponent<UnityEngine.UI.Button>();
        closeBtn.targetGraphic = closeImg;
        var cc2 = closeBtn.colors;
        cc2.normalColor      = Color.white;
        cc2.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cc2.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        cc2.colorMultiplier  = 1f;
        closeBtn.colors = cc2;
        var closeHitGo = new GameObject("Hitbox");
        closeHitGo.transform.SetParent(closeGo.transform, false);
        var closeHitRt = closeHitGo.AddComponent<RectTransform>();
        closeHitRt.anchorMin = Vector2.zero; closeHitRt.anchorMax = Vector2.one;
        closeHitRt.offsetMin = closeHitRt.offsetMax = Vector2.zero;
        var closeHitImg = closeHitGo.AddComponent<UnityEngine.UI.Image>();
        closeHitImg.color = new Color(0, 0, 0, 0);
        closeHitImg.raycastTarget = true;
        AddLabel(closeGo.transform, font, "CLOSE", 28, Color.white, FontStyle.Bold,
                 Vector2.zero, new Vector2(310f, 52f));
        closeBtn.onClick.AddListener(() => { Destroy(root); modeOverlay = null; });

        return root;
    }

    // Builds a large overlay button with an optional sub-label and a transparent
    // hitbox child (matching the menu's other buttons). Returns the Button.
    private UnityEngine.UI.Button AddModeButton(Transform parent, Font font, string label, string subLabel,
                                                Vector2 pos, Color colour, bool interactable)
    {
        // Bar sprite renders at a fixed visual height (~62px). When a subLabel is present,
        // wrap the bar + subtext in an invisible container so both sit inside a clear layout
        // rather than cramming both into the bar's height.
        const float BarH  = 92f;
        const float SubH  = 32f;
        const float Gap   = 10f;

        Transform btnParent;
        Vector2   btnLocalPos;

        if (subLabel != null)
        {
            var wrapGo = new GameObject(label + "Wrap");
            wrapGo.transform.SetParent(parent, false);
            var wrapRt = wrapGo.AddComponent<RectTransform>();
            wrapRt.anchorMin = wrapRt.anchorMax = wrapRt.pivot = new Vector2(0.5f, 0.5f);
            wrapRt.anchoredPosition = pos;
            wrapRt.sizeDelta = new Vector2(700f, BarH + Gap + SubH);

            // Bar sits in the top portion; subtext in the bottom portion.
            btnParent   = wrapGo.transform;
            btnLocalPos = new Vector2(0f, (SubH + Gap) * 0.5f);

            float subY = -(BarH + Gap) * 0.5f;
            AddLabel(wrapGo.transform, font, subLabel, 19,
                     new Color(1f, 1f, 1f, 0.85f), FontStyle.Normal,
                     new Vector2(0f, subY), new Vector2(660f, SubH));
        }
        else
        {
            btnParent   = parent;
            btnLocalPos = pos;
        }

        var btnGo = new GameObject(label + "Btn");
        btnGo.transform.SetParent(btnParent, false);
        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = btnLocalPos;
        rt.sizeDelta = new Vector2(700f, BarH);

        var img = btnGo.AddComponent<UnityEngine.UI.Image>();
        img.sprite = buttonLargeSprite;
        img.type = UnityEngine.UI.Image.Type.Simple;   // Simple stretches to full BarH; Sliced clips to sprite borders
        img.preserveAspect = false;
        img.color = colour;
        img.raycastTarget = false;

        var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.interactable = interactable;
        var c = btn.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        c.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        c.disabledColor = new Color(1f, 1f, 1f, 1f);
        btn.colors = c;

        // Hitbox child carries the raycast so clicks register reliably.
        var hitGo = new GameObject("Hitbox");
        hitGo.transform.SetParent(btnGo.transform, false);
        var hitRt = hitGo.AddComponent<RectTransform>();
        hitRt.anchorMin = Vector2.zero; hitRt.anchorMax = Vector2.one;
        hitRt.offsetMin = hitRt.offsetMax = Vector2.zero;
        var hitImg = hitGo.AddComponent<UnityEngine.UI.Image>();
        hitImg.color = new Color(0, 0, 0, 0);
        hitImg.raycastTarget = true;

        // Main label: centered inside the bar sprite.
        int mainSize = subLabel != null ? 34 : 30;
        AddLabel(btnGo.transform, font, label, mainSize, Color.white, FontStyle.Bold,
                 Vector2.zero, new Vector2(660f, BarH - 10f));

        return btn;
    }

    // Rich mode-selection card: dark background, accent bar, title, tagline, description.
    private UnityEngine.UI.Button AddModeCard(Transform parent, Font font,
        string title, string tagline, string description,
        Vector2 pos, Color accent, bool interactable)
    {
        var cardGo = new GameObject(title + "Card");
        cardGo.transform.SetParent(parent, false);
        var rt = cardGo.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(760f, 235f);

        // RectMask2D container — clips the tall sprite to the card's visible bounds
        var maskGo = new GameObject("Mask");
        maskGo.transform.SetParent(cardGo.transform, false);
        var maskRt = maskGo.AddComponent<RectTransform>();
        maskRt.anchorMin = Vector2.zero; maskRt.anchorMax = Vector2.one;
        maskRt.offsetMin = maskRt.offsetMax = Vector2.zero;
        maskGo.AddComponent<RectMask2D>();

        // Sprite stretched tall (920px) so its bar portion (~21% = 193px) fills the 205px card
        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(maskGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(760f, 1060f);
        var bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgImg.sprite = buttonLargeSprite;
        bgImg.type = UnityEngine.UI.Image.Type.Simple;
        bgImg.preserveAspect = false;
        bgImg.color = accent;
        bgImg.raycastTarget = false;

        // Title — horizontally + vertically centered
        AddLabel(cardGo.transform, font, title, 38, Color.white, FontStyle.Bold,
                 new Vector2(0f, 57f), new Vector2(720f, 48f));

        // Tagline — centered, accent color for visual importance
        AddLabel(cardGo.transform, font, tagline, 22, accent, FontStyle.Bold,
                 new Vector2(0f, 24f), new Vector2(720f, 30f));

        // Description — centered, white, wrapping
        var descGo = new GameObject("Desc");
        descGo.transform.SetParent(cardGo.transform, false);
        var descRt = descGo.AddComponent<RectTransform>();
        descRt.anchorMin = descRt.anchorMax = descRt.pivot = new Vector2(0.5f, 0.5f);
        descRt.anchoredPosition = new Vector2(0f, -46f);
        descRt.sizeDelta = new Vector2(710f, 78f);
        var descTxt = descGo.AddComponent<UnityEngine.UI.Text>();
        descTxt.text = description;
        descTxt.font = font;
        descTxt.fontSize = 19;
        descTxt.color = Color.white;
        descTxt.fontStyle = FontStyle.Normal;
        descTxt.alignment = TextAnchor.MiddleCenter;
        descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        descTxt.verticalOverflow = VerticalWrapMode.Overflow;

        // Button + colours
        var btn = cardGo.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = bgImg;
        btn.interactable = interactable;
        var c = btn.colors;
        c.normalColor    = Color.white;
        c.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        c.pressedColor   = new Color(0.78f, 0.78f, 0.78f, 1f);
        c.disabledColor  = new Color(0.55f, 0.55f, 0.55f, 1f);
        btn.colors = c;

        // Hitbox
        var hitGo = new GameObject("Hitbox");
        hitGo.transform.SetParent(cardGo.transform, false);
        var hitRt = hitGo.AddComponent<RectTransform>();
        hitRt.anchorMin = Vector2.zero; hitRt.anchorMax = Vector2.one;
        hitRt.offsetMin = hitRt.offsetMax = Vector2.zero;
        var hitImg = hitGo.AddComponent<UnityEngine.UI.Image>();
        hitImg.color = new Color(0, 0, 0, 0);
        hitImg.raycastTarget = true;

        return btn;
    }

    public void OpenInstructions() 
    { 
        if (instrOverlay != null) return;

        if (instructionsPrefab != null)
        {
            instrOverlay = Instantiate(instructionsPrefab);
            
            var closeBtn = instrOverlay.GetComponentInChildren<UnityEngine.UI.Button>(true);
            var buttons = instrOverlay.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var b in buttons)
            {
                if (b.name.Contains("Close", System.StringComparison.OrdinalIgnoreCase))
                {
                    closeBtn = b;
                    break;
                }
            }

            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => {
                    Destroy(instrOverlay);
                    instrOverlay = null;
                });
            }
        }
        else
        {
            Debug.LogWarning("Instructions Prefab not assigned to MainMenuController.");
        }
    }

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
        panelRt.sizeDelta = new Vector2(1000f, 800f);
        var panelImg = panelGo.AddComponent<UnityEngine.UI.Image>();
        panelImg.sprite = panelFrameSprite;
        panelImg.type = UnityEngine.UI.Image.Type.Sliced;
        panelImg.color = Color.white;

        AddLabel(panelGo.transform, font, "CONTROLS", 52, new Color(0.3f, 1f, 1f), FontStyle.Bold, new Vector2(0f, 320f), new Vector2(860f, 60f));

        float y = 210f;
        float spacing = 45f;
        
        // Headers with background bars
        AddHeaderBar(panelGo.transform, font, "MOUSE + KEYBOARD MODE", 26, Color.white, new Vector2(0, y));
        y -= spacing * 1.5f;
        
        AddLabel(panelGo.transform, font, "W / S - THRUST & BRAKE", 20, Color.white, FontStyle.Normal, new Vector2(0, y), new Vector2(860, 30));
        AddDivider(panelGo.transform, new Vector2(0, y - spacing * 0.5f));
        y -= spacing;
        AddLabel(panelGo.transform, font, "A / D - STRAFE LEFT / RIGHT", 20, Color.white, FontStyle.Normal, new Vector2(0, y), new Vector2(860, 30));
        AddDivider(panelGo.transform, new Vector2(0, y - spacing * 0.5f));
        y -= spacing;
        AddLabel(panelGo.transform, font, "MOUSE - PITCH & YAW", 20, Color.white, FontStyle.Normal, new Vector2(0, y), new Vector2(860, 30));
        AddDivider(panelGo.transform, new Vector2(0, y - spacing * 0.5f));
        y -= spacing;
        AddLabel(panelGo.transform, font, "Q / E - ROLL        L-SHIFT - BOOST", 20, Color.white, FontStyle.Normal, new Vector2(0, y), new Vector2(860, 30));

        y -= spacing * 2f;
        AddHeaderBar(panelGo.transform, font, "KEYBOARD ONLY MODE", 26, Color.white, new Vector2(0, y));
        y -= spacing * 1.5f;

        AddLabel(panelGo.transform, font, "W / S - PITCH UP / DOWN", 20, Color.white, FontStyle.Normal, new Vector2(0, y), new Vector2(860, 30));
        AddDivider(panelGo.transform, new Vector2(0, y - spacing * 0.5f));
        y -= spacing;
        AddLabel(panelGo.transform, font, "A / D - YAW LEFT / RIGHT", 20, Color.white, FontStyle.Normal, new Vector2(0, y), new Vector2(860, 30));
        AddDivider(panelGo.transform, new Vector2(0, y - spacing * 0.5f));
        y -= spacing;
        AddLabel(panelGo.transform, font, "Q / E - ROLL        L-SHIFT - THRUST", 20, Color.white, FontStyle.Normal, new Vector2(0, y), new Vector2(860, 30));

        var closeBtnGo = new GameObject("CloseBtn");
        closeBtnGo.transform.SetParent(panelGo.transform, false);
        var closeBtnRt = closeBtnGo.AddComponent<RectTransform>();
        closeBtnRt.anchorMin = closeBtnRt.anchorMax = closeBtnRt.pivot = new Vector2(0.5f, 0.5f);
        closeBtnRt.anchoredPosition = new Vector2(0f, -345f);
        closeBtnRt.sizeDelta = new Vector2(280f, 70f);
        var closeBtnImg = closeBtnGo.AddComponent<UnityEngine.UI.Image>();
        closeBtnImg.sprite = buttonLargeSprite;
        closeBtnImg.type = UnityEngine.UI.Image.Type.Sliced;
        closeBtnImg.color = new Color(1.0f, 0.31f, 0.31f, 1.0f); // Match QuitButton color
        var closeBtn = closeBtnGo.AddComponent<UnityEngine.UI.Button>();
        closeBtn.targetGraphic = closeBtnImg;
        var cc = closeBtn.colors; 
        cc.normalColor = Color.white;
        cc.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f); 
        cc.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        closeBtn.colors = cc;
        closeBtn.onClick.AddListener(() => { DestroyImmediate(root); instrOverlay = null; });
        
        // Add Hitbox for consistency
        var hitboxGo = new GameObject("Hitbox");
        hitboxGo.transform.SetParent(closeBtnGo.transform, false);
        var hitboxRt = hitboxGo.AddComponent<RectTransform>();
        hitboxRt.anchorMin = hitboxRt.anchorMax = hitboxRt.pivot = new Vector2(0.5f, 0.5f);
        hitboxRt.anchoredPosition = Vector2.zero;
        hitboxRt.sizeDelta = new Vector2(280f * 0.8f, 70f * 0.7f);
        var hitboxImg = hitboxGo.AddComponent<UnityEngine.UI.Image>();
        hitboxImg.color = new Color(0, 0, 0, 0);
        hitboxImg.raycastTarget = true;
        closeBtnImg.raycastTarget = false;

        AddLabel(closeBtnGo.transform, font, "CLOSE", 28, Color.white, FontStyle.Bold, Vector2.zero, new Vector2(200f, 52f));

        return root;
    }

    private void AddHeaderBar(Transform parent, Font font, string text, int size, Color colour, Vector2 pos)
    {
        var go = new GameObject("HeaderBar");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(700f, 60f);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.sprite = headerBarSprite;
        img.type = UnityEngine.UI.Image.Type.Sliced;
        
        AddLabel(go.transform, font, text, size, colour, FontStyle.Bold, Vector2.zero, new Vector2(700, 60));
    }

    private void AddDivider(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600f, 2f);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.sprite = dividerSprite;
        img.color = new Color(0.3f, 1f, 1f, 0.5f);
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
