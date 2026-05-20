using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tiny screen-centre crosshair. Replaces the legacy crosshair hosted on the
/// old HUD canvas (which is nuked by LegacyHUDCleanup).
///
/// Only visible in Mouse + Keyboard mode (cursor is locked at screen centre,
/// so the crosshair sits exactly where shots land).
/// Auto-creates itself in MainScene.
/// </summary>
public class CenterCrosshair : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
        if (Object.FindAnyObjectByType<CenterCrosshair>() != null) return;
        new GameObject("CenterCrosshair").AddComponent<CenterCrosshair>();
    }

    private Canvas _canvas;

    private void Start()
    {
        Build();
    }

    private void Update()
    {
        if (_canvas != null)
            _canvas.enabled = ControlSchemeManager.IsMouseKeyboard;
    }

    private void Build()
    {
        var canvasGo = new GameObject("CenterCrosshairCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        Color col = new Color(1f, 1f, 1f, 0.7f);
        AddBar(canvasGo.transform, new Vector2(0,   0), new Vector2(22, 2), col); // horiz
        AddBar(canvasGo.transform, new Vector2(0,   0), new Vector2(2, 22), col); // vert
        AddBar(canvasGo.transform, new Vector2(0,   0), new Vector2(3,  3), col); // centre dot
    }

    private static void AddBar(Transform parent, Vector2 pos, Vector2 size, Color col)
    {
        var go = new GameObject("Bar");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
    }
}
