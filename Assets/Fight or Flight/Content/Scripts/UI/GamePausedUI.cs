using System.Collections;
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
        BuildPauseOverlayFromPrefab(canvasGo.transform);
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
        if (img.sprite == null) img.color = new Color(0f, 1f, 0.831f, 0.2f);
        else img.color = Color.white;
        img.type = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(TogglePause);

        var lblGo = new GameObject("Lbl");
        lblGo.transform.SetParent(go.transform, false);
        var lblRt = lblGo.AddComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
        var t = lblGo.AddComponent<Text>();
        t.font = uiFont; t.fontSize = 32; t.fontStyle = FontStyle.Bold;
        t.color = new Color(0f, 1f, 0.831f, 0.85f);
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "II";
    }

    private void BuildPauseOverlayFromPrefab(Transform canvasT)
    {
        GameObject prefab = Resources.Load<GameObject>("UI/PauseOverlayPrefab");
        if (prefab != null)
        {
            pauseOverlay = Instantiate(prefab, canvasT, false);
            pauseOverlay.name = "PauseOverlay";

            // Map buttons
            Button resumeBtn = FindButton(pauseOverlay.transform, "RESUME");
            if (resumeBtn != null) resumeBtn.onClick.AddListener(DoResume);

            Button settingsBtn = FindButton(pauseOverlay.transform, "SETTINGS");
            if (settingsBtn != null) settingsBtn.onClick.AddListener(DoSettings);

            Button restartBtn = FindButton(pauseOverlay.transform, "RESTART WAVE");
            if (restartBtn != null) restartBtn.onClick.AddListener(DoRestartWave);

            Button quitBtn = FindButton(pauseOverlay.transform, "QUIT TO MENU");
            if (quitBtn != null) quitBtn.onClick.AddListener(DoQuitToMenu);
        }
        else
        {
            Debug.LogError("PauseOverlayPrefab not found in Resources/UI/");
        }

        if (pauseOverlay != null) pauseOverlay.SetActive(false);
    }

    private Button FindButton(Transform root, string name)
    {
        Transform t = root.Find("Panel/Buttons/" + name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
