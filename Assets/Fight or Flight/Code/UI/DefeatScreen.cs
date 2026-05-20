using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds and shows the Game Over / Defeat overlay entirely in code — no prefab or scene
/// setup required. Called statically by ShipHealth when the player dies.
/// </summary>
public class DefeatScreen : MonoBehaviour
{
    // ── Static entry point ────────────────────────────────────────────────────

    private static DefeatScreen instance;

    public static void Show(int score, int kills)
    {
        if (instance != null) return; // already showing

        var go = new GameObject("DefeatScreen");
        instance = go.AddComponent<DefeatScreen>();
        instance.score = score;
        instance.kills = kills;
    }

    // ── Instance state ────────────────────────────────────────────────────────

    private int score;
    private int kills;

    private Text scoreText;
    private Text killText;

    private Font uiFont;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUI();
        Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        instance = null;
    }

    // The defeat screen survives scene reload via its own coroutine, so it
    // owns the timeScale reset to make sure gameplay resumes in MainScene.
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Root canvas — renders on top of everything.
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Semi-transparent dark overlay covering the whole screen.
        MakeImage(gameObject, new Color(0f, 0f, 0f, 0.78f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Centred panel.
        var panel = MakePanel(new Vector2(0, 40), new Vector2(860, 600));

        // "GAME OVER" big red header.
        MakeText(panel, "GAME OVER", 110, new Color(0.95f, 0.1f, 0.1f), FontStyle.Bold,
                 new Vector2(0, 200), new Vector2(800, 130));

        // "DEFEATED" subtitle.
        MakeText(panel, "DEFEATED", 56, new Color(1f, 0.8f, 0.2f), FontStyle.Bold,
                 new Vector2(0, 110), new Vector2(700, 70));

        // Divider line.
        var line = MakeImage(panel, new Color(1f, 1f, 1f, 0.25f));
        SetRect(line, new Vector2(0, 55), new Vector2(700, 3));

        // Score and kill readouts — updated with real values below.
        scoreText = MakeText(panel, "", 46, Color.white, FontStyle.Normal,
                             new Vector2(0, 0), new Vector2(700, 60));
        killText  = MakeText(panel, "", 42, new Color(0.8f, 0.8f, 0.8f), FontStyle.Normal,
                             new Vector2(0, -60), new Vector2(700, 55));

        scoreText.text = string.Format("FINAL SCORE:  {0:D6}", score);
        killText.text  = string.Format("ENEMIES KILLED:  {0}", kills);

        // Buttons.
        MakeButton(panel, "TRY AGAIN",  new Color(0.13f, 0.40f, 0.80f),
                   new Vector2(-170, -195), new Vector2(300, 70),
                   () => StartCoroutine(LoadScene("MainScene")));

        MakeButton(panel, "MAIN MENU", new Color(0.25f, 0.25f, 0.25f),
                   new Vector2( 170, -195), new Vector2(300, 70),
                   () => StartCoroutine(LoadScene("MainMenu")));
    }

    private IEnumerator LoadScene(string name)
    {
        Time.timeScale = 1f;
        yield return null;
        SceneManager.LoadScene(name);
    }

    // ── UI helper methods ─────────────────────────────────────────────────────

    private Image MakeImage(GameObject parent, Color colour,
                            Vector2 anchorMin, Vector2 anchorMax,
                            Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject("Img");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = colour;
        return img;
    }

    // Overload that anchors to centre and uses SetRect helper.
    private Image MakeImage(GameObject parent, Color colour)
    {
        var go = new GameObject("Img");
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = colour;
        return img;
    }

    private void SetRect(Image img, Vector2 anchoredPos, Vector2 size)
    {
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    private GameObject MakePanel(Vector2 anchoredPos, Vector2 size)
    {
        var img = MakeImage(gameObject, new Color(0.07f, 0.07f, 0.12f, 0.96f));
        SetRect(img, anchoredPos, size);
        return img.gameObject;
    }

    private Text MakeText(GameObject parent, string content, int fontSize,
                          Color colour, FontStyle style,
                          Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject("Txt");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = uiFont;
        t.fontSize = fontSize;
        t.color = colour;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private void MakeButton(GameObject parent, string label, Color bgColour,
                            Vector2 anchoredPos, Vector2 size,
                            UnityEngine.Events.UnityAction onClick)
    {
        var img = MakeImage(parent, bgColour);
        SetRect(img, anchoredPos, size);

        var btn = img.gameObject.AddComponent<Button>();

        // Colour-tint transition.
        var cols = btn.colors;
        cols.normalColor      = bgColour;
        cols.highlightedColor = bgColour * 1.35f;
        cols.pressedColor     = bgColour * 0.7f;
        cols.colorMultiplier  = 1f;
        btn.colors = cols;
        btn.targetGraphic = img;

        btn.onClick.AddListener(onClick);

        MakeText(img.gameObject, label, 32, Color.white, FontStyle.Bold,
                 Vector2.zero, size);
    }
}
