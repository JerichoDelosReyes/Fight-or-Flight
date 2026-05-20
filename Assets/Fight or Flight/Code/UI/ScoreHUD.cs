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
    private Transform _popupParent;
    private Font _font;
    private int _lastScore = -1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildHUD(_font);
    }

    private void Update()
    {
        if (_scoreText != null)
            _scoreText.text = string.Format("SCORE  {0:D6}", ScoreManager.Score);
        if (_killText != null)
            _killText.text  = string.Format("KILLS  {0}", ScoreManager.Kills);
        if (_waveText != null)
            _waveText.text  = string.Format("WAVE  {0}", WaveManager.CurrentWave);

        // Score popup: spawn a floating "+N" whenever score increases.
        if (_lastScore < 0) _lastScore = ScoreManager.Score; // initialise without popup
        int delta = ScoreManager.Score - _lastScore;
        if (delta > 0)
        {
            SpawnScorePopup(delta);
            _lastScore = ScoreManager.Score;
        }
        else if (delta < 0)
        {
            // Score reset (e.g. retry) — re-sync without popup.
            _lastScore = ScoreManager.Score;
        }
    }

    private void SpawnScorePopup(int amount)
    {
        if (_popupParent == null || _font == null) return;

        var go = new GameObject("ScorePopup");
        go.transform.SetParent(_popupParent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        // Just below the score panel (which spans y=[-168, -18]); the popup
        // rises 60px upward into the panel-side area and fades out.
        rt.anchoredPosition = new Vector2(-22f, -185f);
        rt.sizeDelta = new Vector2(220f, 40f);

        var txt = go.AddComponent<Text>();
        txt.font      = _font;
        txt.fontSize  = 26;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = new Color(1f, 0.95f, 0.35f, 1f);
        txt.alignment = TextAnchor.MiddleRight;
        txt.text      = "+" + amount;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;

        go.AddComponent<ScorePopupFloat>();
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
        panelRt.sizeDelta        = new Vector2(340f, 150f);
        panelGo.AddComponent<Image>().color = PanelBG;
        // Score popups float upward beneath the panel — siblings of the panel
        // on the same canvas so they share the screen-space layer.
        _popupParent = canvasGo.transform;

        // Score
        _scoreText = AddLine(panelGo.transform, font, "SCORE  000000",
                             ScoreColor, 32, FontStyle.Bold, -28f);
        // Kill counter
        _killText  = AddLine(panelGo.transform, font, "KILLS  0",
                             KillColor,  24, FontStyle.Bold, -74f);
        // Wave
        _waveText  = AddLine(panelGo.transform, font, "WAVE  1",
                             WaveColor,  24, FontStyle.Bold, -110f);
    }

    private Text AddLine(Transform parent, Font font, string initial,
                         Color col, int size, FontStyle style, float yOffset)
    {
        var go = new GameObject("Line_" + initial.Split(' ')[0]);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-14f, yOffset);
        rt.sizeDelta        = new Vector2(280f, 36f);

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
