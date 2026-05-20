using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Destroys legacy scene-baked HUD UI that has been replaced by the new
/// PlayerHUD / ScoreHUD / Radar / WaveHUD scripts.
///
/// Targets:
///  • All UI elements referenced by HUDManager (old health, heat, score, kill,
///    speed, throttle).
///  • Any HealthUI component (old standalone health text).
///  • GameObjects named like legacy radar/minimap/map panels.
///  • Any leftover Image / RawImage with a strong green colour that isn't part
///    of one of our new canvases — kills the "green square" behind the radar.
///
/// Auto-runs on MainScene load; no scene wiring required.
/// </summary>
public class LegacyHUDCleanup : MonoBehaviour
{
    // Known new-HUD canvas/root names — anything inside one of these is safe.
    private static readonly HashSet<string> SafeRoots = new HashSet<string>
    {
        "RadarCanvas", "HeadingStripCanvas",
        "PlayerHUDCanvas", "ScoreHUDCanvas",
        "WaveHUD", "BoundaryVignetteCanvas",
        "EnemyHPCanvas",
        "PlayerHUD", "ScoreHUD", "Radar", "WaveManager", "ArenaBoundary",
        "ScreenFlash"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
        TryRun(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode) => TryRun(scene);

    private static void TryRun(Scene scene)
    {
        if (scene.name != "MainScene") return;
        var go = new GameObject("LegacyHUDCleanup");
        go.AddComponent<LegacyHUDCleanup>();
    }

    private void Start()
    {
        StartCoroutine(RunCleanup());
    }

    private IEnumerator RunCleanup()
    {
        // Wait one frame so HUDManager.Awake/Start can wire up its references.
        yield return null;

        // ── 1. Destroy HUDManager-referenced UI ──────────────────────────────
        var hudManager = Object.FindAnyObjectByType<HUDManager>();
        if (hudManager != null)
        {
            DestroyIfPresent(hudManager.healthBar);
            DestroyIfPresent(hudManager.heatBar);
            DestroyIfPresent(hudManager.heatBarFill);
            DestroyIfPresent(hudManager.scoreText);
            DestroyIfPresent(hudManager.killText);
            DestroyIfPresent(hudManager.speedText);
            DestroyIfPresent(hudManager.throttleText);
            hudManager.enabled = false;
        }

        // ── 2. Destroy old HealthUI texts ────────────────────────────────────
        foreach (var hu in Object.FindObjectsByType<HealthUI>(FindObjectsSortMode.None))
            if (hu != null) Destroy(hu.gameObject);

        // ── 3. Destroy GameObjects named like legacy radar/minimap/map ──────
        string[] suspectNames = {
            "RadarPanel", "RadarBG", "RadarBackground", "RadarFrame", "RadarBox",
            "Minimap", "MiniMap", "MiniMapPanel", "MinimapPanel", "MiniMapBG",
            "Map", "MapBG", "MapPanel", "MapBackground",
            "GreenSquare", "GreenPanel", "GreenBG", "GreenBackground",
            "OldRadar", "OldMiniMap", "RadarContainer"
        };
        foreach (var n in suspectNames)
        {
            var existing = GameObject.Find(n);
            if (existing == null) continue;
            if (IsInsideSafeRoot(existing.transform)) continue;
            Destroy(existing);
        }

        // ── 4. Sweep for green Image/RawImage outside our canvases ──────────
        foreach (var img in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
        {
            if (img == null) continue;
            if (IsInsideSafeRoot(img.transform)) continue;
            if (IsGreen(img.color)) Destroy(img.gameObject);
        }
        foreach (var img in Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None))
        {
            if (img == null) continue;
            if (IsInsideSafeRoot(img.transform)) continue;
            if (IsGreen(img.color)) Destroy(img.gameObject);
        }

        // Self-destruct
        Destroy(gameObject);
    }

    private static void DestroyIfPresent(Component c)
    {
        if (c == null) return;
        // Walk up to a top-level child of the canvas so we don't accidentally
        // destroy the entire HUD canvas (which still hosts the crosshair).
        Transform t = c.transform;
        while (t.parent != null && t.parent.GetComponent<Canvas>() == null)
            t = t.parent;
        Destroy(t.gameObject);
    }

    private static bool IsInsideSafeRoot(Transform t)
    {
        while (t != null)
        {
            if (SafeRoots.Contains(t.name)) return true;
            t = t.parent;
        }
        return false;
    }

    // Heuristic: green-dominant, reasonably opaque.
    private static bool IsGreen(Color c)
    {
        if (c.a < 0.3f) return false;
        if (c.g < 0.45f) return false;
        return c.g > c.r * 1.4f && c.g > c.b * 1.4f;
    }
}
