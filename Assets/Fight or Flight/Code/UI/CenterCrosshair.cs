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
    private Image[] _bars;
    private static readonly Color IdleColor = new Color(1f, 1f, 1f, 0.7f);
    private static readonly Color AimColor  = new Color(1f, 0.2f, 0.2f, 0.95f);

    private void Start()
    {
        Build();
    }

    private void Update()
    {
        if (_canvas == null) return;
        _canvas.enabled = ControlSchemeManager.IsMouseKeyboard;
        if (!_canvas.enabled) return;

        // Raycast from the camera through the screen centre — if the hit is on
        // an Enemy, switch the crosshair colour to red.
        bool aimingAtEnemy = false;
        var cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f,
                                                       Screen.height * 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, 15000f))
            {
                // Walk up the hit hierarchy to find an Enemy tag / EnemyAI.
                Transform t = hit.transform;
                while (t != null && !aimingAtEnemy)
                {
                    if (t.CompareTag("Enemy") || t.GetComponent<EnemyAI>() != null)
                        aimingAtEnemy = true;
                    t = t.parent;
                }
            }
        }

        Color col = aimingAtEnemy ? AimColor : IdleColor;
        if (_bars != null)
            for (int i = 0; i < _bars.Length; i++)
                if (_bars[i] != null) _bars[i].color = col;
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

        _bars = new Image[3];
        _bars[0] = AddBar(canvasGo.transform, new Vector2(0, 0), new Vector2(22, 2), IdleColor); // horiz
        _bars[1] = AddBar(canvasGo.transform, new Vector2(0, 0), new Vector2(2, 22), IdleColor); // vert
        _bars[2] = AddBar(canvasGo.transform, new Vector2(0, 0), new Vector2(3,  3), IdleColor); // centre dot
    }

    private static Image AddBar(Transform parent, Vector2 pos, Vector2 size, Color col)
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
        return img;
    }
}
