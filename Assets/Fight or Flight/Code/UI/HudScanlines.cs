using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HudScanlines : MonoBehaviour
{
    private static void HookSceneLoad()
    {
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode) => TryCreate(scene);

    private static void TryCreate(Scene scene)
    {
        if (scene.name != "MainScene") return;
        if (Object.FindAnyObjectByType<HudScanlines>() != null) return;
        new GameObject("HudScanlines").AddComponent<HudScanlines>();
    }

    private RawImage _overlay;
    private Texture2D _tex;

    private void Start()
    {
        Build();
    }

    private void Update()
    {
        if (_overlay == null) return;
        const float tilePx = 4f;
        float w = Screen.width  / tilePx;
        float h = Screen.height / tilePx;
        _overlay.uvRect = new Rect(0f, 0f, w, h);
    }

    private void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }

    private void Build()
    {
        var canvasGo = new GameObject("ScanlineCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("Scanline");
        imgGo.transform.SetParent(canvasGo.transform, false);
        var rt = imgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        _tex = MakeScanlineTex();
        _overlay = imgGo.AddComponent<RawImage>();
        _overlay.texture = _tex;
        _overlay.color   = new Color(0.6f, 0.9f, 1f, 0.05f);
        _overlay.raycastTarget = false;
    }

    private static Texture2D MakeScanlineTex()
    {
        var tex = new Texture2D(4, 4, TextureFormat.ARGB32, false);
        tex.wrapMode   = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float a = (y == 2) ? 1f : 0f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }
}
