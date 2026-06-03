using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryScreen : MonoBehaviour
{
    private static VictoryScreen instance;

    public static void Show(int score, int kills)
    {
        if (instance != null) return;

        var go = new GameObject("VictoryScreen");
        instance = go.AddComponent<VictoryScreen>();
        instance.Init(score, kills);
    }

    private int score;
    private int kills;
    private Font uiFont;

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        WaveManager.FadeOutBackgroundMusic();
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Init(int s, int k)
    {
        score = s;
        kills = k;
        BuildUI();
    }

    private void OnDestroy()
    {
        instance = null;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        MakeImage(gameObject, new Color(0f, 0f, 0f, 0.78f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var panel = MakePanel(new Vector2(0, 40), new Vector2(860, 600));

        MakeText(panel, "VICTORY", 110, new Color(0.1f, 0.95f, 0.1f), FontStyle.Bold,
                 new Vector2(0, 200), new Vector2(800, 130));

        MakeText(panel, "MISSION ACCOMPLISHED", 56, new Color(0.6f, 0.85f, 1f), FontStyle.Bold,
                 new Vector2(0, 110), new Vector2(700, 70));

        var line = MakeImage(panel, new Color(1f, 1f, 1f, 0.25f));
        SetRect(line, new Vector2(0, 55), new Vector2(700, 3));

        var scoreText = MakeText(panel, string.Format("FINAL SCORE:  {0:D6}", score),
                                 46, Color.white, FontStyle.Normal,
                                 new Vector2(0, 0), new Vector2(700, 60));
        scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;

        var killText = MakeText(panel, string.Format("ENEMIES KILLED:  {0}", kills),
                                42, new Color(0.8f, 0.8f, 0.8f), FontStyle.Normal,
                                new Vector2(0, -60), new Vector2(700, 55));
        killText.horizontalOverflow = HorizontalWrapMode.Overflow;

        MakeButton(panel, "PLAY AGAIN",  new Color(0.13f, 0.60f, 0.13f),
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

    private Image MakeImage(GameObject parent, Color colour,
                            Vector2 anchorMin, Vector2 anchorMax,
                            Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject("Img");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = colour;
        return img;
    }

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
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.text = content; t.font = uiFont; t.fontSize = fontSize;
        t.color = colour; t.fontStyle = style;
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
        var cols = btn.colors;
        cols.normalColor      = bgColour;
        cols.highlightedColor = bgColour * 1.35f;
        cols.pressedColor     = bgColour * 0.7f;
        cols.colorMultiplier  = 1f;
        btn.colors = cols;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        MakeText(img.gameObject, label, 32, Color.white, FontStyle.Bold, Vector2.zero, size);
    }
}
