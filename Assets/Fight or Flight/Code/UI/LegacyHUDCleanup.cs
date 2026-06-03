using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LegacyHUDCleanup : MonoBehaviour
{
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
        Cleanup();
        yield return new WaitForSeconds(0.3f); Cleanup();
        yield return new WaitForSeconds(1.5f); Cleanup();
        Destroy(gameObject);
    }

    private static void Cleanup()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
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
