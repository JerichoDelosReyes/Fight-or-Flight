using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Auto-creates a pause button (top-left) and pause overlay in MainScene.
/// Escape or the pause button toggles pause. Shows Main Menu option while paused.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    private static PauseManager instance;

    private GameObject pauseOverlay;

    // ── Auto-creation ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Always clean up pause state when a new scene loads.
        IsPaused = false;
        Time.timeScale = 1f;
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<PauseManager>() != null) return;
        new GameObject("PauseManager").AddComponent<PauseManager>();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildUI();
    }

    private void OnEnable()
    {
        GameEventManager.OnPlayerDestroyed += OnPlayerDied;
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerDestroyed -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        if (IsPaused) Resume();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static void TogglePause()
    {
        if (instance == null) return;
        if (IsPaused) instance.Resume();
        else instance.Pause();
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    private void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseOverlay != null) pauseOverlay.SetActive(true);
        ShipInput.ReleaseCursor();
    }

    private void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseOverlay != null) pauseOverlay.SetActive(false);
        // Re-lock cursor only if Mouse+Keyboard mode is active.
        var shipInput = Object.FindAnyObjectByType<ShipInput>();
        if (shipInput != null) shipInput.ApplyCursorState();
        else if (ControlSchemeManager.IsMouseKeyboard) ShipInput.LockCursor();
    }

    private void GoToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        ShipInput.ReleaseCursor();
        SceneManager.LoadScene("MainMenu");
    }

    // ── UI construction ───────────────────────────────────────────────────────

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

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildPauseButton(canvasGo.transform, font);
        BuildPauseOverlay(canvasGo.transform, font);
    }

    private void BuildPauseButton(Transform canvasT, Font font)
    {
        var go = new GameObject("PauseButton");
        go.transform.SetParent(canvasT, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(90f, 70f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        btn.colors = colors;
        btn.onClick.AddListener(TogglePause);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        var labelText = labelGo.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = 36;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.text = "II";
    }

    private void BuildPauseOverlay(Transform canvasT, Font font)
    {
        pauseOverlay = new GameObject("PauseOverlay");
        pauseOverlay.transform.SetParent(canvasT, false);

        var overlayRt = pauseOverlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;

        var dimmer = pauseOverlay.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.65f);

        // ── Panel ─────────────────────────────────────────────────────────────
        var panel = new GameObject("Panel");
        panel.transform.SetParent(pauseOverlay.transform, false);

        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(380f, 280f);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.07f, 0.07f, 0.14f, 0.96f);

        // ── Title ─────────────────────────────────────────────────────────────
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);

        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -28f);
        titleRt.sizeDelta = new Vector2(0f, 56f);

        var titleText = titleGo.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 46;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.95f, 0.5f, 1f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "PAUSED";

        // ── Buttons ───────────────────────────────────────────────────────────
        MakeButton(panel.transform, font, "RESUME",    new Vector2(0f, -120f), Resume);
        MakeButton(panel.transform, font, "MAIN MENU", new Vector2(0f, -200f), GoToMainMenu);

        pauseOverlay.SetActive(false);
    }

    private static void MakeButton(Transform parent, Font font, string label, Vector2 pos,
                                   UnityEngine.Events.UnityAction callback)
    {
        var go = new GameObject(label.Replace(" ", "") + "Btn");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(280f, 60f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.14f, 0.24f, 0.44f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.24f, 0.44f, 0.72f, 1f);
        colors.pressedColor = new Color(0.08f, 0.14f, 0.28f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(callback);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.font = font;
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }
    }
}
