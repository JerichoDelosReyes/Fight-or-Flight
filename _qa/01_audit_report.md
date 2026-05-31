# Fight-or-Flight QA Audit Report
Date: 2026-05-31
Auditor: Agent 1

---

## Summary

| Category | Issues Found |
|----------|-------------|
| Broken LoadAssetAtPath paths | 0 |
| Broken Resources.Load paths | 1 |
| Broken GUID refs in prefabs | 0 |
| Orphaned .meta files | 0 |
| Assets missing .meta files | 0 |
| Verbose/trimmable XML summaries (>4 lines) | 9 |
| Long block comments (>3 lines) | 0 |
| 3+ consecutive // comment docblocks | 21 occurrences across 11 files |
| Commented-out code blocks | 4 lines across 2 files |
| TODO/FIXME/HACK/NOTE comments | 0 |
| AI/tool junk files | 2 directories (.claude, .gemini) |
| Stray runtime-saved prefabs in wrong folder | 2 |
| #if UNITY_EDITOR outside Editor/ | 7 occurrences across 4 files |
| Unused using statements | 2 confirmed (GameplayUtils.cs, GamePausedUISetup.cs) |
| SerializedField sprite/texture/audio fields | 10 fields across 5 files |

---

## SECTION 1A — Broken Asset References

### Step A1: AssetDatabase.LoadAssetAtPath Calls

All files scanned: 49 .cs files under `Assets/Fight or Flight/Code/`.

The following `AssetDatabase.LoadAssetAtPath` calls were found and verified:

| File | Line | Path Argument | Status |
|------|------|---------------|--------|
| `Code/Editor/PrefabSetup.cs` | 28 | `Assets/Fight or Flight/Content/Prefabs/VFX/ExplosionEffect.prefab` | OK |
| `Code/Editor/PrefabSetup.cs` | 38 | `Assets/Fight or Flight/Content/Prefabs/Player/PlayerShip.prefab` (template) | OK |
| `Code/Editor/PrefabSetup.cs` | 15-16 | `Assets/Fight or Flight/Content/Prefabs/Enemies/EnemyShip (1).prefab` | OK |
| `Code/UI/MainMenuController.cs` | 50 | `Assets/Fight or Flight/Content/Sprites/UI/SciFiButtonFrame.png` | OK |
| `Code/UI/MainMenuController.cs` | 58 | `Assets/Fight or Flight/Content/Fonts/Inter-VariableFont_opsz,wght.ttf` | OK |
| `Code/UI/PlayerHUD.cs` | 388 | Dynamic `iconPath` → `Assets/Fight or Flight/Content/Textures/UI/health.png`, `shield.png`, `heat.png` | OK (all 3 exist) |
| `Code/UI/SettingsMenu.cs` | 71 | `Assets/Fight or Flight/Content/Prefabs/UI/SettingsMenu.prefab` | OK |
| `Code/Utils/DebrisScatter.cs` | 221, 231 | Dynamic via `FindAssets` (Rocks prefabs, Asteroid_New) — path resolved at runtime | OK (folder exists, prefabs exist) |

**No broken `AssetDatabase.LoadAssetAtPath` paths found.**

---

### Step A1 (cont.): Resources.Load Calls

The following `Resources.Load` calls were found and verified. Unity's `Resources.Load` resolves paths relative to any `Resources/` folder in the project. The project has a `Resources/` folder at `Assets/Fight or Flight/Resources/`.

**RootResources/SciFiUI/** sprites (used by `MainMenuController.cs` and `SettingsMenu.cs`):

| Resource Path | File on Disk | Status |
|---------------|-------------|--------|
| `RootResources/SciFiUI/panel_frame` | `Resources/RootResources/SciFiUI/panel_frame.png` | OK |
| `RootResources/SciFiUI/header_bar` | `Resources/RootResources/SciFiUI/header_bar.png` | OK |
| `RootResources/SciFiUI/button_large` | `Resources/RootResources/SciFiUI/button_large.png` | OK |
| `RootResources/SciFiUI/button_small` | `Resources/RootResources/SciFiUI/button_small.png` | OK |
| `RootResources/SciFiUI/checkbox_bg` | `Resources/RootResources/SciFiUI/checkbox_bg.png` | OK |
| `RootResources/SciFiUI/checkmark` | `Resources/RootResources/SciFiUI/checkmark.png` | OK |
| `RootResources/SciFiUI/slider_track` | `Resources/RootResources/SciFiUI/slider_track.png` | OK |
| `RootResources/SciFiUI/slider_handle` | `Resources/RootResources/SciFiUI/slider_handle.png` | OK |
| `RootResources/SciFiUI/divider` | `Resources/RootResources/SciFiUI/divider.png` | OK |

**RootResources/UI/** prefabs (used by `SettingsMenu.cs`):

| Resource Path | File on Disk | Status |
|---------------|-------------|--------|
| `RootResources/UI/SettingsMenu` | `Resources/RootResources/UI/SettingsMenu.prefab` | **MISSING** |

> **BROKEN — `SettingsMenu.cs` line 77:** `Resources.Load<GameObject>("RootResources/UI/SettingsMenu")` — the file `Assets/Fight or Flight/Resources/RootResources/UI/SettingsMenu.prefab` does not exist. The `RootResources/UI/` folder contains only `InstructionsOverlay.prefab`. The actual SettingsMenu prefab lives at `Assets/Fight or Flight/Content/Prefabs/UI/SettingsMenu.prefab` (loaded first via `AssetDatabase` in editor, line 71), so this is a runtime-only fallback path that will silently fail in a build. The code then falls back to procedural construction (line 87), so it does not crash, but the prefab-based path is unreachable at runtime.

**UI/Sprites/** sprites (used by `GamePausedUI.cs`, `GamePausedUISetup.cs`, `DefeatScreen.cs`, `MissionCompleteScreen.cs`):

| Resource Path | File on Disk | Status |
|---------------|-------------|--------|
| `UI/Sprites/panel_background` | `Resources/UI/Sprites/panel_background.png` | OK |
| `UI/Sprites/button_base` | `Resources/UI/Sprites/button_base.png` | OK |
| `UI/Sprites/button_highlighted` | `Resources/UI/Sprites/button_highlighted.png` | OK |
| `UI/Sprites/defeat_helmet_new` | `Resources/UI/Sprites/defeat_helmet_new.png` | OK |
| `UI/Sprites/mission_comp` | `Resources/UI/Sprites/mission_comp.png` | OK |
| `UI/Sprites/Boxy/Icons/resume` | `Resources/UI/Sprites/Boxy/Icons/resume.png` | OK |
| `UI/Sprites/Boxy/Icons/settings` | `Resources/UI/Sprites/Boxy/Icons/settings.png` | OK |
| `UI/Sprites/Boxy/Icons/restart` | `Resources/UI/Sprites/Boxy/Icons/restart.png` | OK |
| `UI/Sprites/Boxy/Icons/quit` | `Resources/UI/Sprites/Boxy/Icons/quit.png` | OK |

---

### Step A2: Broken GUID References in Prefabs

All 44 prefab files under `Assets/Fight or Flight/` were scanned (excluding `_Archive/` and `Vendor/`). Every `{fileID: X, guid: Y, type: Z}` reference was extracted and checked against the complete GUID map built from 834 `.meta` files across the entire `Assets/` folder.

**Broken GUID refs found: 0**

Note: Two binary prefab files were found in a wrong location (see Section 1C below). Their internal GUIDs were verified as present in the meta map.

---

### Step A3: Orphaned .meta Files

All `.meta` files under `Assets/Fight or Flight/` were checked. For each, the corresponding asset file (path minus `.meta`) was verified to exist.

**Orphaned .meta files found: 0**

---

### Step A4: Assets Missing .meta Files

All non-.meta files under `Assets/Fight or Flight/` were checked for a corresponding `.meta` file.

**Assets missing .meta files: 0**

---

## SECTION 1B — Comment Quality Issues

All 49 `.cs` files under `Assets/Fight or Flight/Code/` were scanned.

### Verbose XML `<summary>` Blocks (longer than 4 lines)

| File | Start Line | Summary Length |
|------|-----------|----------------|
| `Code/Editor/GamePausedUISetup.cs` | 8 | 11 lines |
| `Code/Editor/LegacyHudCleanupTool.cs` | 8 | 15 lines |
| `Code/UI/CenterCrosshair.cs` | 5 | 6 lines |
| `Code/UI/CompassBar.cs` | 6 | 9 lines |
| `Code/UI/LegacyHUDCleanup.cs` | 6 | 10 lines |
| `Code/UI/Radar.cs` | 6 | 10 lines |
| `Code/Utils/ArenaBoundary.cs` | 6 | 10 lines |
| `Code/Utils/DebrisScatter.cs` | 6 | 11 lines |
| `Code/Utils/GameModeManager.cs` | 3 | 9 lines |

**Total: 9 files with verbose XML summaries.**

### Long `/* ... */` Block Comments (longer than 3 lines)

**None found.**

### 3+ Consecutive `//` Comment Lines (docblock-style)

| File | Start Line | Count |
|------|-----------|-------|
| `Code/Editor/LegacyHudCleanupTool.cs` | 85 | 3 lines |
| `Code/Enemy/EnemyMovement.cs` | 148 | 3 lines |
| `Code/Ship/Ship.cs` | 74 | 3 lines |
| `Code/Ship/ShipHealth.cs` | 33 | 4 lines (sync comment block) |
| `Code/Ship/ShipHealth.cs` | 159 | 5 lines (audio approach comment block) |
| `Code/Ship/ShipHealth.cs` | 180 | 3 lines |
| `Code/Ship/ShipHealth.cs` | 197 | 3 lines |
| `Code/Ship/ShipInput.cs` | 72 | 3 lines |
| `Code/UI/GamePausedUI.cs` | 111 | 3 lines |
| `Code/UI/GamePausedUI.cs` | 226 | 3 lines |
| `Code/UI/GamePausedUI.cs` | 246 | 3 lines |
| `Code/UI/HudScanlines.cs` | 14 | 3 lines (also commented-out code — see below) |
| `Code/UI/MainMenuController.cs` | 329 | 3 lines |
| `Code/UI/WaveManager.cs` | 24 | 3 lines |
| `Code/Utils/ArenaBoundary.cs` | 60 | 4 lines |
| `Code/Utils/ArenaBoundary.cs` | 68 | 3 lines |
| `Code/Utils/DebrisScatter.cs` | 116 | 3 lines |

**Total: 17 unique consecutive-comment blocks across 11 files.**

### Commented-Out Code

| File | Line | Content |
|------|------|---------|
| `Code/UI/HudScanlines.cs` | 11 | `// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` |
| `Code/UI/HudScanlines.cs` | 14 | `// SceneManager.sceneLoaded -= OnSceneLoadedStatic;` |
| `Code/UI/HudScanlines.cs` | 15 | `// SceneManager.sceneLoaded += OnSceneLoadedStatic;` |
| `Code/UI/HudScanlines.cs` | 16 | `// TryCreate(SceneManager.GetActiveScene());` |

Context: The entire `HookSceneLoad()` method body (lines 11–16) is commented out, leaving a dead empty method body. The auto-creation hook is disabled. This is a functional no-op — the class still works if manually placed in a scene, but the auto-create-on-scene-load feature is silently disabled.

Additionally, `Code/Ship/ShipHealth.cs` lines 159–163 contain a 5-line comment block that reads as thinking-out-loud/exploratory notes rather than documentation:
```
// Instead of PlayClipAtPoint which leaks, we try to use a persistent audio source
// But since this object is being destroyed, we use a static method or similar
// For now, let's use a simpler approach: play it at camera position or similar
// Actually, I'll use a simple pooler for audio if possible, but let's just use 
// AudioSource.PlayClipAtPoint sparingly or ensure it's not called 100 times.
```

### TODO/FIXME/HACK/NOTE Comments

**None found.**

---

## SECTION 1C — AI and Tool Config Junk Files

### AI Tool Directories at Project Root

| Path | Type | Note |
|------|------|------|
| `D:\Game Development\Fight-or-Flight\.claude\` | Directory | Claude Code config/skills directory |
| `D:\Game Development\Fight-or-Flight\.gemini\` | Directory | Gemini config/skills directory (contains full unity-skills skill tree with ~120+ files) |

Both are AI tool working directories at the project root. The `.gemini\` directory is notably large, containing a full skills reference tree (100+ `.md` files). Neither directory is gitignored or otherwise excluded from version control by default.

### .md Files at Project Root (excluding README.md and CHANGELOG.md)

**None found.** Only `README.md` is present at project root.

### .json Files at Project Root (excluding manifest.json and packages-lock.json)

**None found.**

### Stray Runtime-Saved Prefabs in Wrong Location

| Path | Issue |
|------|-------|
| `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone).prefab` | Binary prefab accidentally saved at runtime. Not a proper Unity text-format prefab. Wrong directory. |
| `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone)(Clone).prefab` | Same issue — double-instantiated clone prefab saved to disk in the Sprites/UI folder. |

These appear to be Unity-serialized runtime instances that were accidentally written to disk (possibly during an `AssetDatabase.SaveAssets()` call during a play-mode test). They are binary-format files (not YAML), live in the wrong folder (`Content/Sprites/UI/` instead of `Content/Prefabs/UI/`), and have names with `(Clone)` which is Unity's runtime naming convention. They should be deleted.

### .tmp, .bak, .orig, .log Files Outside Logs/

**None found.**

---

## SECTION 1D — Script Namespace and Import Issues

### `#if UNITY_EDITOR` Blocks Outside `Editor/` Subfolder

Scripts in non-Editor folders that contain `#if UNITY_EDITOR` blocks use `UnityEditor` APIs conditionally. This is a valid pattern (avoids stripping errors in builds) but means these scripts carry editor-only code paths in runtime assemblies. Flagged for awareness:

| File | Lines | Content |
|------|-------|---------|
| `Code/UI/MainMenuController.cs` | 34, 49, 57 | `UnityEditor.AssetDatabase.LoadAssetAtPath` for font/sprite fallback |
| `Code/UI/MainMenuController.cs` | 524 | `UnityEditor.EditorApplication.isPlaying = false` in `QuitGame()` |
| `Code/UI/PlayerHUD.cs` | 387 | `UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath)` |
| `Code/UI/SettingsMenu.cs` | 70 | `UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>` for prefab load |
| `Code/Utils/DebrisScatter.cs` | 213 | `UnityEditor.AssetDatabase.FindAssets` + `LoadAssetAtPath` for debris prefabs |

These are all intentional editor-fallback patterns, not bugs. However, `PlayerHUD.cs` and `DebrisScatter.cs` in particular mean that icon sprites and debris prefabs are **only loaded in the editor** — in a runtime build, icons will be null (sprites won't show) and debris will fall back to the scene-object-search path. This is a functional concern for builds, not an asset-reference error.

### Unused `using` Statements

The following `using` statements appear to have no actual usage of their namespace's types in the file:

| File | Line | Statement | Verdict |
|------|------|-----------|---------|
| `Code/Editor/GamePausedUISetup.cs` | 3 | `using UnityEditor.SceneManagement;` | **Unused** — only occurrence of `SceneManagement` in the file is the `using` line itself. No `StageUtility`, `PrefabStageUtility`, `OpenSceneMode` etc. used. |
| `Code/Utils/GameplayUtils.cs` | 4 | `using System.Collections.Generic;` | **Unused** — file uses `IEnumerator` (from `System.Collections`) and `Image` etc., but no `List<>`, `Dictionary<>`, `HashSet<>` or other Generic types. |

The following were initially flagged but confirmed as used:

| File | Statement | Used Types |
|------|-----------|-----------|
| `Code/Editor/GamePausedUISetup.cs` | `using UnityEngine.EventSystems;` | `EventSystem`, `BaseRaycaster` (4 matches) |
| `Code/Editor/LegacyHudCleanupTool.cs` | `using System.Collections.Generic;` | `List<>` (9 matches) |
| `Code/Editor/PrefabSetup.cs` | `using UnityEditor;` | `AssetDatabase`, `EditorApplication`, `PrefabUtility`, `MenuItem` (6 matches) |
| `Code/Enemy/EnemyAI.cs` | `using System.Collections.Generic;` | `List<>` (2 matches) |
| `Code/Enemy/EnemyMovement.cs` | `using System.Collections.Generic;` | `List<>` (2 matches) |
| `Code/UI/CompassBar.cs` | `using System.Collections.Generic;` | `List<>` (2 matches) |
| `Code/UI/DefeatScreen.cs` | `using UnityEngine.EventSystems;` | `EventSystem` (6 matches) |
| `Code/UI/GamePausedUI.cs` | `using UnityEngine.EventSystems;` | `EventSystem` (6 matches) |
| `Code/UI/LegacyHUDCleanup.cs` | `using System.Collections.Generic;` | `List<>` (4 matches) |
| `Code/UI/MissionCompleteScreen.cs` | `using UnityEngine.EventSystems;` | `EventSystem` (6 matches) |
| `Code/UI/Radar.cs` | `using System.Collections.Generic;` | `List<>` (5 matches) |
| `Code/UI/SettingsMenu.cs` | `using UnityEngine.EventSystems;` | `EventSystem` (6 matches) |
| `Code/UI/VictoryScreen.cs` | `using UnityEngine.EventSystems;` | `EventSystem` (6 matches) |
| `Code/Utils/DebrisScatter.cs` | `using System.Collections.Generic;` | `List<>` (3 matches) |

---

## SECTION 1E — Inspector Serialization Issues

The following `[SerializeField]` or `public` fields of types that reference visual/audio assets were found. These are fields that could appear blank in the Inspector after the post-cleanup reorganization.

| File | Line | Field Type | Field Name | Class | Notes |
|------|------|-----------|-----------|-------|-------|
| `Code/Combat/ShipLaserProjectile.cs` | 10 | `AudioClip` | `shotSound` | `ShipLaserProjectile` | public |
| `Code/Combat/ShipLaserProjectile.cs` | 96 | `AudioClip` | `impactSound` | `ShipLaserProjectile` | public (note: defined inside method scope area — verify line) |
| `Code/Ship/Ship.cs` | 37 | `AudioClip` | `engineRumbleSound` | `Ship` | public |
| `Code/Ship/ShipCombat.cs` | 18 | `AudioClip` | `laserShotSound` | `ShipCombat` | public |
| `Code/Ship/ShipHealth.cs` | 25 | `AudioClip` | `explosionSound` | `ShipHealth` | public |
| `Code/UI/HUDManager.cs` | 9 | `Image` | `heatBarFill` | `HUDManager` | public |
| `Code/UI/MainMenuController.cs` | 9 | `Sprite` | `buttonFrameSprite` | `MainMenuController` | public |
| `Code/UI/MainMenuController.cs` | 10 | `Font` | `menuFont` | `MainMenuController` | public (Font type, included for completeness) |
| `Code/UI/MainMenuController.cs` | 17 | `Sprite` | `panelFrameSprite` | `MainMenuController` | public |
| `Code/UI/MainMenuController.cs` | 18 | `Sprite` | `headerBarSprite` | `MainMenuController` | public |
| `Code/UI/MainMenuController.cs` | 19 | `Sprite` | `buttonLargeSprite` | `MainMenuController` | public |
| `Code/UI/MainMenuController.cs` | 20 | `Sprite` | `dividerSprite` | `MainMenuController` | public |

**Total: 12 fields (10 Sprite/Image/AudioClip types, 2 Font).**

**Inspector risk assessment after reorganization:**

- `MainMenuController` Sprite fields (`buttonFrameSprite`, `panelFrameSprite`, `headerBarSprite`, `buttonLargeSprite`, `dividerSprite`) — all have code-side fallbacks via `Resources.Load` or `AssetDatabase.LoadAssetAtPath`, so Inspector nulls are automatically recovered at runtime/editor time. Low risk.
- `AudioClip` fields (`shotSound`, `impactSound`, `engineRumbleSound`, `laserShotSound`, `explosionSound`) — AudioClip assets were **not** moved in this reorganization (only textures, fonts, models, prefabs, and scripts were moved). Low risk of breakage, but should be verified in the Inspector.
- `HUDManager.heatBarFill` (Image reference) — this is a scene object reference, not an asset reference, so reorganization does not affect it.

---

## Additional Notes

### Content/Scripts/UI Directory
The source folder `Assets/Fight or Flight/Content/Scripts/UI/` still exists as an empty directory (with its `.meta` file). The three scripts (`GamePausedUI.cs`, `MissionCompleteScreen.cs`, `SciFiUIStyle.cs`) have been successfully moved to `Code/UI/`. The empty directory can be removed to avoid clutter.

### Stray Clone Prefabs (Reiteration)
The files:
- `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone).prefab`
- `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone)(Clone).prefab`

...are binary Unity prefab files accidentally committed to disk. They have valid `.meta` files (GUIDs `d94399b77d5c04b18a2514def2f9bcc3` and `63c73a0aa357f415482d13e3174a69ba` respectively) but no script references them by GUID. They are safe to delete.

### Resources.Load Runtime Miss
`SettingsMenu.cs` line 77 (`Resources.Load<GameObject>("RootResources/UI/SettingsMenu")`) is a dead code path at runtime. The prefab it targets does not exist in the Resources folder. In a shipped build, the editor-only `AssetDatabase` path (line 71) is stripped, this Resources path silently returns null, and the code falls through to procedural construction. This means the prefab-based SettingsMenu is **never used in a build**. Either copy `Content/Prefabs/UI/SettingsMenu.prefab` to `Resources/RootResources/UI/` or remove the dead Resources path.
