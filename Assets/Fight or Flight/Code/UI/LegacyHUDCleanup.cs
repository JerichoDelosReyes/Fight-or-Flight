using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Nukes every legacy Canvas in MainScene.
///
/// Strategy: there is exactly one whitelist of NEW HUD canvases we created.
/// Anything else (including the green square panel, the old radar/minimap,
/// the old health/score/heat bars, etc.) is destroyed wholesale — regardless
/// of its color, sprite, or hierarchy.
///
/// Runs three passes (immediate, 0.3s, 1.5s) to catch any late-spawning UI.
///
/// Auto-runs on MainScene load — no scene wiring required.
/// </summary>
public class LegacyHUDCleanup : MonoBehaviour
{
    // Canvas GameObject names we explicitly create. Anything NOT in this set,
    // and not parented under one of these, will be destroyed.
    private static readonly HashSet<string> SafeCanvasNames = new HashSet<string>
    {
        "RadarCanvas", "HeadingStripCanvas",
        "PlayerHUDCanvas", "ScoreHUDCanvas",
        "WaveHUD",
        "EnemyHPCanvas",
        "DefeatScreen", "SettingsMenu",
        "ScreenFlash",
        "CenterCrosshairCanvas",
        "CompassBarCanvas",
        "PauseCanvas",
        "ScanlineCanvas",
    };

    // GameObject names (containers of the canvases above). Anything parented
    // beneath one of these is also safe.
    private static readonly HashSet<string> SafeRootNames = new HashSet<string>
    {
        "PlayerHUD", "ScoreHUD", "Radar", "WaveManager",
        "ArenaBoundary", "ScreenFlash", "DefeatScreen", "SettingsMenu",
        "CenterCrosshair", "CompassBar",
        "PauseManager", "HudScanlines",
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
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Cleanup();                                     // immediate pass
        yield return new WaitForSeconds(0.3f); Cleanup(); // late spawner pass
        yield return new WaitForSeconds(1.5f); Cleanup(); // very late pass
        Destroy(gameObject);
    }

    private static void Cleanup()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c == null) continue;
            if (IsSafe(c.transform)) continue;
            Destroy(c.gameObject);
        }
    }

    private static bool IsSafe(Transform t)
    {
        while (t != null)
        {
            if (SafeCanvasNames.Contains(t.name)) return true;
            if (SafeRootNames.Contains(t.name))   return true;
            t = t.parent;
        }
        return false;
    }
}
