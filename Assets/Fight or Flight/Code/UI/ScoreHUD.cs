using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-building score / kill counter panel at the top-right of the screen.
/// Auto-creates itself in MainScene — no scene wiring required.
/// Replaces the scene-wired scoreText / killText in HUDManager with a styled panel.
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    // ── Auto-creation ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode) => TryCreate(scene);

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<ScoreHUD>() != null) return;
        new GameObject("ScoreHUD").AddComponent<ScoreHUD>();
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private static readonly Color PanelBG    = new Color(0f,    0f,    0f,    0.65f);
    private static readonly Color ScoreColor = new Color(1f,    0.92f, 0.3f,  1f);
    private static readonly Color KillColor  = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color WaveColor  = new Color(0.6f,  0.85f, 1f,    1f);

    // ── Runtime refs ──────────────────────────────────────────────────────────

    private Text _scoreText;
    private Text _killText;
    private Text _waveText;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildHUD(font);
    }

    private void Update()
    {
        if (_scoreText != null)
            _scoreText.text = string.Format("SCORE  {0:D6}", ScoreManager.Score);
        if (_killText != null)
            _killText.text  = string.Format("KILLS  {0}", ScoreManager.Kills);
        if (_waveText != null)
            _waveText.text  = string.Format("WAVE  {0}", WaveManager.CurrentWave);
    }

    // ── HUD Construction ──────────────────────────────────────────────────────

    private void BuildHUD(Font font)
    {
        var canvasGo = new GameObject("ScoreHUDCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 116;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel — top-right
        var panelGo = new GameObject("ScorePanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(1f, 1f);
        panelRt.anchorMax        = new Vector2(1f, 1f);
        panelRt.pivot            = new Vector2(1f, 1f);
        panelRt.anchoredPosition = new Vector2(-18f, -18f);
        panelRt.sizeDelta        = new Vector2(260f, 105f);
        panelGo.AddComponent<Image>().color = PanelBG;

        // Score
        _scoreText = AddLine(panelGo.transform, font, "SCORE  000000",
                             ScoreColor, 22, FontStyle.Bold, -20f);
        // Kill counter
        _killText  = AddLine(panelGo.transform, font, "KILLS  0",
                             KillColor,  17, FontStyle.Normal, -52f);
        // Wave
        _waveText  = AddLine(panelGo.transform, font, "WAVE  1",
                             WaveColor,  17, FontStyle.Normal, -78f);
    }

    private Text AddLine(Transform parent, Font font, string initial,
                         Color col, int size, FontStyle style, float yOffset)
    {
        var go = new GameObject("Line_" + initial.Split(' ')[0]);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, yOffset);
        rt.sizeDelta        = new Vector2(200f, 22f);

        var txt = go.AddComponent<Text>();
        txt.text      = initial;
        txt.font      = font;
        txt.fontSize  = size;
        txt.fontStyle = style;
        txt.color     = col;
        txt.alignment = TextAnchor.MiddleRight;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        return txt;
    }
}
