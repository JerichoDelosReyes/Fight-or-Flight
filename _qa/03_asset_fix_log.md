# Asset Fix Log — Agent 3
Date: 2026-05-31
Agent: Agent 3 — ASSET PATH FIXER
Source audit: _qa/01_audit_report.md (Sections 1A and 1E)

---

## TASK 1 — Broken Resources.Load path in SettingsMenu.cs

**File:** `Assets/Fight or Flight/Code/UI/SettingsMenu.cs` line 77
**Issue:** `Resources.Load<GameObject>("RootResources/UI/SettingsMenu")` references a prefab that does not exist in any `Resources/` folder.

**Investigation:**
- Searched all `Resources/` folders recursively for `SettingsMenu.prefab` — no match found.
- `Assets/Fight or Flight/Resources/RootResources/UI/` contains only `InstructionsOverlay.prefab`.
- `Assets/Resources/` does not contain a `SettingsMenu.prefab`.
- The actual SettingsMenu prefab lives at `Assets/Fight or Flight/Content/Prefabs/UI/SettingsMenu.prefab` (not under any `Resources/` folder).

**Null-safety analysis:**
- Line 70–72: `#if UNITY_EDITOR` block loads the prefab via `AssetDatabase.LoadAssetAtPath` (editor-only, stripped in builds).
- Line 75–78: `Resources.Load<GameObject>("RootResources/UI/SettingsMenu")` — this will always return `null` in a runtime build.
- Lines 80–90: `if (prefab != null)` instantiates from prefab; the `else` branch (line 87–90) creates a bare `GameObject` and adds `SettingsMenu` as a component — graceful procedural fallback.
- `Awake()` line 117: `if (panel == null)` calls `BuildUI()`, which constructs the entire UI procedurally. This path is fully implemented and functional.

**Decision:** The prefab was NOT moved (requires Unity Editor meta-file handling). The dead Resources path is logged as flagged.

[FLAGGED] Assets/Fight or Flight/Code/UI/SettingsMenu.cs line 77 — `Resources.Load<GameObject>("RootResources/UI/SettingsMenu")` always returns null at runtime (no SettingsMenu.prefab exists under any Resources/ folder). The script handles null gracefully: editor uses AssetDatabase path (line 71), builds fall through to procedural BuildUI() construction (lines 87–90 + Awake line 117). No crash risk. To fix properly, copy Content/Prefabs/UI/SettingsMenu.prefab into Resources/RootResources/UI/ via Unity Editor so the .meta is regenerated correctly.

---

## TASK 2 — Orphaned / misplaced .meta files

Per audit Section 1A (Steps A3 and A4):
- Orphaned .meta files found: **0**
- Assets missing .meta files: **0**

[CLEAN] Section 1A Steps A3/A4 — no orphaned .meta files and no assets missing .meta files. No action required.

---

## TASK 3 — Additional broken asset paths from audit Section 1A

Per audit Section 1A:
- All `AssetDatabase.LoadAssetAtPath` calls: **0 broken** (all 7 verified OK)
- All `Resources.Load` calls for `RootResources/SciFiUI/*` sprites: **9 paths, all OK**
- All `Resources.Load` calls for `UI/Sprites/*` sprites: **9 paths, all OK**
- Only broken path: the `RootResources/UI/SettingsMenu` call covered in Task 1 above

[CLEAN] Section 1A Step A1 — no additional broken asset paths beyond the SettingsMenu Resources.Load already flagged above.

---

## Summary

| # | Item | Result |
|---|------|--------|
| 1 | `SettingsMenu.cs` line 77 `Resources.Load("RootResources/UI/SettingsMenu")` | [FLAGGED] — always null at runtime; code handles null gracefully via procedural fallback |
| 2 | Orphaned .meta files | [CLEAN] — none found |
| 3 | Assets missing .meta files | [CLEAN] — none found |
| 4 | Other broken AssetDatabase / Resources paths | [CLEAN] — none found |

**Files modified:** none — the single broken path is unfixable without Unity Editor (prefab copy + meta generation). All other asset references are intact.
