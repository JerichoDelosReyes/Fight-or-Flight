using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor menu command that deletes legacy HUD GameObjects from the currently
/// open scene and marks it dirty for saving.
///
/// Why this exists: at runtime, LegacyHUDCleanup destroys these objects on
/// every play — but the scene file itself still contains them, so they
/// flash briefly when the scene loads and they're impossible to remove
/// permanently without editing the scene from inside the Unity Editor.
///
/// Usage:
///   1. In Unity, open the scene you want to clean (MainScene or MainMenu).
///   2. Click  "Fight or Flight  →  Clean Legacy HUD (Active Scene)".
///   3. Save the scene (Ctrl+S).
///
/// You can also click "Clean Legacy HUD (All Scenes)" to process both
/// MainScene and MainMenu back-to-back without manually opening each.
/// </summary>
public static class LegacyHudCleanupTool
{
    // ── Public menu commands ──────────────────────────────────────────────────

    [MenuItem("Fight or Flight/Clean Legacy HUD (Active Scene)")]
    private static void CleanActive()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int removed = CleanScene(scene);
        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("Legacy HUD Cleanup",
                $"Removed {removed} legacy GameObject(s) from \"{scene.name}\".\n\nDon't forget to save the scene (Ctrl+S).",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Legacy HUD Cleanup",
                $"No legacy HUD GameObjects found in \"{scene.name}\".",
                "OK");
        }
    }

    [MenuItem("Fight or Flight/Clean Legacy HUD (All Scenes)")]
    private static void CleanAll()
    {
        string[] scenePaths =
        {
            "Assets/Fight or Flight/Content/Scenes/MainScene.unity",
            "Assets/Fight or Flight/Content/Scenes/MainMenu.unity",
        };

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int totalRemoved = 0;
        string report = "";
        foreach (var path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int removed = CleanScene(scene);
            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            totalRemoved += removed;
            report += $"\"{scene.name}\": removed {removed} GameObject(s)\n";
        }

        EditorUtility.DisplayDialog("Legacy HUD Cleanup — All Scenes",
            report + $"\nTotal: {totalRemoved}",
            "OK");
    }

    // ── Cleanup core ──────────────────────────────────────────────────────────

    private static readonly HashSet<string> SafeCanvasNames = new HashSet<string>
    {
        // None of these should exist in the SCENE file — they're all runtime-created.
        // If a scene happens to contain one with these exact names, we keep it
        // (the user may have deliberately staged it for testing).
        "RadarCanvas", "HeadingStripCanvas",
        "PlayerHUDCanvas", "ScoreHUDCanvas",
        "WaveHUD", "BoundaryVignetteCanvas",
        "EnemyHPCanvas",
        "DefeatScreen", "SettingsMenu",
        "ScreenFlash",
        "CenterCrosshairCanvas",
        "CompassBarCanvas",
    };

    private static readonly HashSet<string> SafeRootNames = new HashSet<string>
    {
        "PlayerHUD", "ScoreHUD", "Radar", "WaveManager",
        "ArenaBoundary", "ScreenFlash", "DefeatScreen", "SettingsMenu",
        "CenterCrosshair", "CompassBar",
    };

    /// <summary>
    /// Deletes legacy HUD GameObjects from the given scene. Returns the
    /// count of GameObjects removed.
    /// </summary>
    private static int CleanScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return 0;

        var toDelete = new HashSet<GameObject>();
        var roots = scene.GetRootGameObjects();

        // ── 1. Every Canvas that isn't part of the new HUD system ───────────
        foreach (var root in roots)
        {
            var canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (IsSafe(c.transform)) continue;
                toDelete.Add(c.gameObject);
            }
        }

        // ── 2. Legacy HUD scripts and their parent objects ──────────────────
        AddOwnersAndChildren<HUDManager>(roots, toDelete);
        AddOwnersAndChildren<HUDController>(roots, toDelete);
        AddOwnersAndChildren<HealthUI>(roots, toDelete);
        AddOwnersAndChildren<MouseCrosshairUI>(roots, toDelete);

        // ── 3. HUDManager-referenced UI elements (old health bar at bottom-left,
        //    old score/kill/heat/speed/throttle texts, etc.) ─────────────────
        foreach (var root in roots)
        {
            foreach (var hm in root.GetComponentsInChildren<HUDManager>(true))
            {
                AddRefIfPresent(hm.healthBar,      toDelete);
                AddRefIfPresent(hm.heatBar,        toDelete);
                AddRefIfPresent(hm.heatBarFill,    toDelete);
                AddRefIfPresent(hm.scoreText,      toDelete);
                AddRefIfPresent(hm.killText,       toDelete);
                AddRefIfPresent(hm.speedText,      toDelete);
                AddRefIfPresent(hm.throttleText,   toDelete);
            }
        }

        // ── 4. Image / RawImage with green-ish color outside the new HUD ────
        foreach (var root in roots)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                if (IsSafe(img.transform)) continue;
                if (IsGreenish(img.color)) toDelete.Add(img.gameObject);
            }
            foreach (var img in root.GetComponentsInChildren<RawImage>(true))
            {
                if (img == null) continue;
                if (IsSafe(img.transform)) continue;
                if (IsGreenish(img.color)) toDelete.Add(img.gameObject);
            }
        }

        // ── 5. GameObjects with legacy-sounding names ───────────────────────
        string[] suspectNames = {
            "RadarPanel", "RadarBG", "RadarBackground", "RadarFrame", "RadarBox",
            "Minimap", "MiniMap", "MiniMapPanel", "MinimapPanel", "MiniMapBG",
            "Map", "MapBG", "MapPanel", "MapBackground",
            "GreenSquare", "GreenPanel", "GreenBG", "GreenBackground",
            "OldRadar", "OldMiniMap", "RadarContainer",
            "HUD", "HUDCanvas", "OldHUD", "HUDPanel"
        };
        foreach (var root in roots)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                if (IsSafe(t)) continue;
                foreach (var name in suspectNames)
                {
                    if (t.gameObject.name == name)
                    {
                        toDelete.Add(t.gameObject);
                        break;
                    }
                }
            }
        }

        // ── Resolve: remove children of objects we'll already delete so we
        //    don't try to delete a child after its parent is gone ──────────
        var final = new List<GameObject>();
        foreach (var go in toDelete)
        {
            if (go == null) continue;
            if (HasAncestorIn(go.transform.parent, toDelete)) continue;
            final.Add(go);
        }

        foreach (var go in final)
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        return final.Count;
    }

    private static void AddOwnersAndChildren<T>(GameObject[] roots, HashSet<GameObject> bucket)
        where T : Component
    {
        foreach (var root in roots)
        {
            foreach (var comp in root.GetComponentsInChildren<T>(true))
            {
                if (comp == null) continue;
                // Walk up to the topmost Canvas-bearing ancestor (the legacy HUD root).
                Transform t = comp.transform;
                Transform canvasAncestor = null;
                while (t != null)
                {
                    if (t.GetComponent<Canvas>() != null) canvasAncestor = t;
                    t = t.parent;
                }
                bucket.Add(canvasAncestor != null ? canvasAncestor.gameObject : comp.gameObject);
            }
        }
    }

    private static void AddRefIfPresent(Component c, HashSet<GameObject> bucket)
    {
        if (c == null) return;
        bucket.Add(c.gameObject);
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

    private static bool HasAncestorIn(Transform t, HashSet<GameObject> set)
    {
        while (t != null)
        {
            if (set.Contains(t.gameObject)) return true;
            t = t.parent;
        }
        return false;
    }

    private static bool IsGreenish(Color c)
    {
        if (c.a < 0.3f) return false;
        if (c.g < 0.45f) return false;
        return c.g > c.r * 1.4f && c.g > c.b * 1.4f;
    }
}
