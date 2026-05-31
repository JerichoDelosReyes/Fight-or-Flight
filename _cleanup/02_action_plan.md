# Fight-or-Flight — Cleanup Action Plan
Generated: 2026-05-31
Planner: Agent 2

## Summary
| Metric | Count |
|--------|-------|
| Total files scanned | 744 |
| Files to MOVE | 19 |
| Files to QUARANTINE | 38 |
| Files to RENAME | 0 |
| Files to SKIP (issues, correctly skipped) | 18 (_Archive) + 477 (Vendor) + ~30 (GeneratedAssets) = 525+ |
| Files reviewed with no action needed | 8 (empty dirs — flag only) |

---

## Reference Check Notes

All duplicate files and misplaced assets were checked against the two main scene files (`MainScene.unity`, `MainMenu.unity`) and all `.prefab` files under `Assets/Fight or Flight/`. Both scene files are **binary-serialized** (Unity 6 binary format), which means GUID searches via text tools are unreliable. All text-format `.prefab` files were searched by GUID — zero hits for any of the flagged files. This indicates:

- None of the duplicate images (ButtonBG, MainMenuBackground, SciFiButtonFrame, mission_comp) are directly referenced by GUID in any text-format prefab.
- None of the misplaced prefabs (SettingsMenu, CloseBtn, PlayerHUD, ButtonBG_Refined, SciFiButtonFrame_WithBG) are referenced in text prefabs or found in the readable portion of scene data.
- The binary scenes cannot be read as text — Agent 3 must treat all MOVEs as **MEDIUM risk** minimum, and should open Unity Editor to let the engine re-resolve GUID paths after any move.

---

## Action Items

### Group: Misplaced C# Scripts
> Three `.cs` scripts were placed under `Content/Scripts/UI/` instead of the canonical `Code/UI/` folder used by the rest of the project.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| MOVE | `Assets/Fight or Flight/Content/Scripts/UI/GamePausedUI.cs` | `Assets/Fight or Flight/Code/UI/GamePausedUI.cs` | C# script in Content/ instead of Code/ — matches rule 8 | MEDIUM |
| MOVE | `Assets/Fight or Flight/Content/Scripts/UI/MissionCompleteScreen.cs` | `Assets/Fight or Flight/Code/UI/MissionCompleteScreen.cs` | C# script in Content/ instead of Code/ — matches rule 8 | MEDIUM |
| MOVE | `Assets/Fight or Flight/Content/Scripts/UI/SciFiUIStyle.cs` | `Assets/Fight or Flight/Code/UI/SciFiUIStyle.cs` | C# script in Content/ instead of Code/ — matches rule 8 | MEDIUM |

> Note: After moving all 3 scripts, the now-empty folder `Assets/Fight or Flight/Content/Scripts/UI/` and its parent `Assets/Fight or Flight/Content/Scripts/` should be removed (see Empty Directories section). Move the `.meta` files alongside their source files.

---

### Group: Clone Prefabs (Play-Mode Accidents)
> Two prefabs with `(Clone)` in the name were accidentally saved to disk from Unity play-mode instantiation. These are invalid on-disk assets.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone).prefab` | `_quarantine/Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone).prefab` | Unity runtime clone accidentally serialized to disk — rule 4 | LOW |
| QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone)(Clone).prefab` | `_quarantine/Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone)(Clone).prefab` | Unity runtime clone accidentally serialized to disk — rule 4 | LOW |

---

### Group: Duplicate Images
> Four image filenames exist in two locations each. Neither copy was found referenced by GUID in any text-format prefab file. Keep the copy in the more semantically correct folder; quarantine the other.

#### ButtonBG.png
- Copy 1 GUID: `c8106dd8...` — `Assets/Fight or Flight/Content/Sprites/UI/ButtonBG.png`
- Copy 2 GUID: `272e4985...` — `Assets/Fight or Flight/Content/UI/Backgrounds/ButtonBG.png`
- Decision: `Content/UI/Backgrounds/` is the semantically correct location for a background image used in UI panels. Keep `Content/UI/Backgrounds/ButtonBG.png`; quarantine `Content/Sprites/UI/ButtonBG.png`.
  - Note: The `Content/UI/Backgrounds/` copy has 9-slice borders set (20,20,20,20) and includes WebGL platform overrides — it appears to be a newer, properly-configured version.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/ButtonBG.png` | `_quarantine/Assets/Fight or Flight/Content/Sprites/UI/ButtonBG.png` | Duplicate of Content/UI/Backgrounds/ButtonBG.png; UI/Backgrounds copy is canonical | LOW |

#### MainMenuBackground.png
- Copy 1 GUID: `0a36d447...` — `Assets/Fight or Flight/Content/Sprites/UI/MainMenuBackground.png`
- Copy 2 GUID: `367cc507...` — `Assets/Fight or Flight/Content/Textures/MainMenuBackground.png`
- Decision: `Content/Textures/` is the correct folder for background textures. The `Content/Sprites/UI/` copy lacks the `UnityAI` label, indicating it is the older original. The `Content/Textures/` copy has the `UnityAI` label (newer). Keep `Content/Textures/MainMenuBackground.png`; quarantine the Sprites copy.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/MainMenuBackground.png` | `_quarantine/Assets/Fight or Flight/Content/Sprites/UI/MainMenuBackground.png` | Duplicate of Content/Textures/MainMenuBackground.png; Textures copy is canonical | LOW |

#### SciFiButtonFrame.png
- Copy 1 GUID: `5914f675...` — `Assets/Fight or Flight/Content/Sprites/UI/SciFiButtonFrame.png`
- Copy 2 GUID: `c11f72a0...` — `Assets/Fight or Flight/Content/Sprites/UI/ContentUI/SciFiButtonFrame.png`
- Decision: The `ContentUI/` subdirectory copy (`c11f72a0`) has `isReadable: 1` and 9-slice borders (24,24,24,24), indicating it is the actively-configured version. Keep `ContentUI/SciFiButtonFrame.png`; quarantine the parent-level copy.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/SciFiButtonFrame.png` | `_quarantine/Assets/Fight or Flight/Content/Sprites/UI/SciFiButtonFrame.png` | Duplicate of ContentUI/SciFiButtonFrame.png; ContentUI copy has correct read/border settings | LOW |

#### mission_comp.png
- Copy 1 GUID: `22f09167...` — `Assets/Fight or Flight/Content/UI/Icons/mission_comp.png`
- Copy 2 GUID: `f218b0f2...` — `Assets/Fight or Flight/Resources/UI/Sprites/mission_comp.png`
- Decision: The `Resources/` folder copy is accessible at runtime via `Resources.Load()` (important for UI). The `Content/UI/Icons/` copy uses `textureType: 0` (Default) and is set to `spriteMode: 0` (None) — it appears to be an older import. The `Resources/` copy uses `textureType: 8` (Sprite) and has proper sprite settings. Keep `Resources/UI/Sprites/mission_comp.png`; quarantine the Content/UI/Icons copy.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/UI/Icons/mission_comp.png` | `_quarantine/Assets/Fight or Flight/Content/UI/Icons/mission_comp.png` | Duplicate of Resources/UI/Sprites/mission_comp.png; Resources copy is correct sprite type and runtime-accessible | MEDIUM |

---

### Group: Rock Model Duplicates
> All 24 Rock `.dae` model files were copied from `Vendor/BrokenVector/Models/` into `Content/Prefabs/Environment/Rocks/Models/`. The vendor originals are canonical; the project copies are redundant and waste ~24 asset slots.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 01.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 01.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 02.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 02.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 03.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 03.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 04.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type1 04.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 01.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 01.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 02.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 02.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 03.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 03.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 04.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type2 04.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 01.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 01.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 02.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 02.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 03.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 03.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 04.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type3 04.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 01.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 01.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 02.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 02.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 03.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 03.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 04.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type4 04.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 01.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 01.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 02.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 02.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 03.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 03.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 04.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type5 04.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 01.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 01.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 02.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 02.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 03.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 03.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 04.dae` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type6 04.dae` | Byte-for-byte copy of Vendor/BrokenVector/Models/ original — rule 7 | MEDIUM |

> **WARNING for Agent 3:** The 24 Rock prefabs in `Content/Prefabs/Environment/Rocks/Prefabs/` reference the `.dae` models by GUID. If the quarantined `.dae` files are gone before those prefab references are updated, the prefabs will lose their meshes. Before executing the quarantine: open the Unity Editor, select all Rock prefabs, and verify their MeshFilter source. If the prefabs reference the project-copy GUIDs (not the Vendor GUIDs), you must re-point them to `Vendor/BrokenVector/Models/` first, then quarantine the copies. If they already reference Vendor GUIDs, quarantine is safe.

---

### Group: Model/Font Files in Wrong Folders
> Several non-model file types are placed inside `Content/Models/` and `Content/Prefabs/Enemies/`, and one prefab is inside `Code/UI/`.

#### Textures stranded in Materials/
| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| MOVE | `Assets/Fight or Flight/Content/Materials/health.png` | `Assets/Fight or Flight/Content/Textures/UI/health.png` | PNG texture in Materials/ folder — rule 10 | LOW |
| MOVE | `Assets/Fight or Flight/Content/Materials/heat.png` | `Assets/Fight or Flight/Content/Textures/UI/heat.png` | PNG texture in Materials/ folder — rule 10 | LOW |
| MOVE | `Assets/Fight or Flight/Content/Materials/shield.png` | `Assets/Fight or Flight/Content/Textures/UI/shield.png` | PNG texture in Materials/ folder — rule 10 | LOW |

> Create `Assets/Fight or Flight/Content/Textures/UI/` if it does not exist.

#### Font in Models/
| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| MOVE | `Assets/Fight or Flight/Content/Models/Inter-VariableFont_opsz,wght.ttf` | `Assets/Fight or Flight/Content/Fonts/Inter-VariableFont_opsz,wght.ttf` | Font file in Models/ folder — rule 11 | LOW |

> Create `Assets/Fight or Flight/Content/Fonts/` if it does not exist.

#### Prefab in Models/
| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| MOVE | `Assets/Fight or Flight/Content/Models/Mainenemy.prefab` | `Assets/Fight or Flight/Content/Prefabs/Enemies/Mainenemy.prefab` | Prefab file in Models/ folder — rule 9 | MEDIUM |

#### GLB models stranded in Prefabs/Enemies/
| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| MOVE | `Assets/Fight or Flight/Content/Prefabs/Enemies/boss.glb` | `Assets/Fight or Flight/Content/Models/Enemies/boss.glb` | GLB model in Prefabs/ folder — rule 12 | MEDIUM |
| MOVE | `Assets/Fight or Flight/Content/Prefabs/Enemies/vulcan_dkyr_class.glb` | `Assets/Fight or Flight/Content/Models/Enemies/vulcan_dkyr_class.glb` | GLB model in Prefabs/ folder — rule 12 | MEDIUM |

> Note: `enemyspaceships (1).glb` is also a GLB in Prefabs/Enemies/ but is handled below under Files with Auto-Number Suffixes (QUARANTINE, not MOVE).

#### Prefab in Code/UI/
| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| MOVE | `Assets/Fight or Flight/Code/UI/SettingsMenu.prefab` | `Assets/Fight or Flight/Content/Prefabs/UI/SettingsMenu.prefab` | Prefab file in Code/ folder — rule 13 | MEDIUM |

> Create `Assets/Fight or Flight/Content/Prefabs/UI/` if it does not exist.

#### Prefabs in wrong content folders
| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| MOVE | `Assets/Fight or Flight/Content/Scenes/MainMenu/CloseBtn.prefab` | `Assets/Fight or Flight/Content/Prefabs/UI/CloseBtn.prefab` | Prefab in Scenes/ folder — rule 14 | MEDIUM |
| MOVE | `Assets/Fight or Flight/Content/Sprites/UI/ButtonBG_Refined.prefab` | `Assets/Fight or Flight/Content/Prefabs/UI/ButtonBG_Refined.prefab` | Prefab in Sprites/ folder — no dedicated rule but consistent with prefab organisation | MEDIUM |
| MOVE | `Assets/Fight or Flight/Content/Sprites/UI/PlayerHUD.prefab` | `Assets/Fight or Flight/Content/Prefabs/UI/PlayerHUD.prefab` | Prefab in Sprites/ folder — consistent with prefab organisation | MEDIUM |
| MOVE | `Assets/Fight or Flight/Content/Sprites/UI/SciFiButtonFrame_WithBG.prefab` | `Assets/Fight or Flight/Content/Prefabs/UI/SciFiButtonFrame_WithBG.prefab` | Prefab in Sprites/ folder — consistent with prefab organisation | MEDIUM |

---

### Group: macOS Metadata
> One `.DS_Store` file was created by macOS in the project's Content folder and should not be in source control.

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/.DS_Store` | `_quarantine/Assets/Fight or Flight/Content/.DS_Store` | macOS metadata artifact — rule 5 | LOW |

---

### Group: Files with Auto-Number Suffixes
> Five files carry Unity auto-number `(1)` suffixes indicating accidental duplicate imports. Files where no base version exists are treated per rule 16: they should be renamed to remove the `(1)` suffix, **unless** they are in the wrong folder (then QUARANTINE so the base file can be properly imported later).

| Action | Source Path | Destination Path | Reason | Risk |
|--------|-------------|------------------|--------|------|
| QUARANTINE | `Assets/Fight or Flight/Content/Models/Ships/spaceship_ezno (1).glb` | `_quarantine/Assets/Fight or Flight/Content/Models/Ships/spaceship_ezno (1).glb` | Accidental duplicate of spaceship_ezno.glb in same folder — rule 16 | LOW |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Enemies/ENEMY (1).prefab` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Enemies/ENEMY (1).prefab` | Auto-numbered prefab; no base ENEMY.prefab exists — likely accidental import, review before use — rule 16 | LOW |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Enemies/EnemyShip (1).prefab` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Enemies/EnemyShip (1).prefab` | Auto-numbered prefab; no base EnemyShip.prefab exists — likely accidental import, review before use — rule 16 | LOW |
| QUARANTINE | `Assets/Fight or Flight/Content/Models/Environment/asteroid (1).glb` | `_quarantine/Assets/Fight or Flight/Content/Models/Environment/asteroid (1).glb` | Auto-numbered GLB; no base asteroid.glb exists — likely accidental import, review before use — rule 16 | LOW |
| QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Enemies/enemyspaceships (1).glb` | `_quarantine/Assets/Fight or Flight/Content/Prefabs/Enemies/enemyspaceships (1).glb` | Auto-numbered GLB in wrong folder (Prefabs not Models); no base version — quarantine covers both issues — rule 12 + 16 | LOW |

> **Note for Agent 3:** Before quarantining `ENEMY (1).prefab` and `EnemyShip (1).prefab`, check `WaveManager.cs` and `EnemySpawner.cs` (which grab enemy prefabs from the scene's EnemySpawner component). If either of these `(1)` prefabs is assigned to the EnemySpawner in the MainScene, it must NOT be quarantined until a properly-named replacement is in place. The MainScene uses binary serialization so this assignment cannot be verified from text search alone.

---

### Group: Empty Directories (Flag Only)
> Eight directories contain no files. They cannot be safely deleted from the filesystem because Unity tracks them via `.meta` files. Remove these from within the Unity Editor after all other cleanup is complete — Unity will clean the `.meta` files automatically.

| Action | Path | Note | Risk |
|--------|------|------|------|
| SKIP | `Assets/Fight or Flight/Code/AI/` | Empty placeholder — remove folder manually in Unity Editor after cleanup | LOW |
| SKIP | `Assets/Fight or Flight/Code/Player/` | Empty placeholder — remove folder manually in Unity Editor after cleanup | LOW |
| SKIP | `Assets/Fight or Flight/Content/Audio/Music/` | No music assets yet — intentional or placeholder | LOW |
| SKIP | `Assets/Fight or Flight/Content/Scenes/FlightSettings/` | Lighting subfolder with no assets — remove manually in Unity Editor | LOW |
| SKIP | `Assets/Fight or Flight/Content/Sprites/RootSprites/SciFiUI_Extracted/` | Extraction never completed — remove manually in Unity Editor | LOW |
| SKIP | `Assets/Fight or Flight/Resources/UI 1/` | Staging folder never populated — remove manually in Unity Editor | LOW |
| SKIP | `Assets/Fight or Flight/Resources/UI 2/` | Staging folder never populated — remove manually in Unity Editor | LOW |
| SKIP | `Assets/Sprites/Pause_ExtractedAssets/` | Extraction folder — contents moved elsewhere — remove manually in Unity Editor | LOW |

> Additionally, after the misplaced C# scripts are moved out of `Content/Scripts/UI/`, the now-empty `Content/Scripts/UI/` and `Content/Scripts/` directories should also be cleaned up manually in Unity Editor.

---

### Group: _Archive Contents (Protected)
> All 18 files under `Assets/_Archive/` are protected. No action is taken on any of them.

See "Skipped Files" section below.

---

### Group: Vendor Contents (Protected)
> All 477 files under `Assets/Vendor/` are vendor packages and are left untouched, including the 24 canonical Rock `.dae` originals in `Assets/Vendor/BrokenVector/Models/` and the CartoonFX internal duplicates. The CartoonFX internal duplication (~58 files in `Vendor/CartoonFX/Animations/`) is a vendor package structure issue and is out of scope.

See "Skipped Files" section below.

---

## Skipped Files (Protected or Correct)

### _Archive (18 files — SKIP ALL)
All files under `Assets/_Archive/` are protected. Reason: "protected archive — do not touch" (rule 2).
- `Assets/_Archive/Plans/main-menu-ui-imitation.md`
- `Assets/_Archive/Prefab scenes.unity`
- `Assets/_Archive/_Recovery/0.unity`
- `Assets/_Archive/_ScriptsReference/` — 15 archived `.cs` files (Asteroid.cs, AsteroidManager.cs, EnemySpawner.cs, FollowCam.cs, GameEventManager.cs, GameScore.cs, GameTimer.cs, GameUI.cs, Pickup.cs, PlayGameButton.cs, Player.cs, Rotate.cs, Shield.cs, ShieldUI.cs, Thruster.cs)

### Vendor (477 files — SKIP ALL)
All files under `Assets/Vendor/` are vendor packages. Reason: "vendor package — do not modify" (rule 3).
- `Assets/Vendor/BrokenVector/` — including all 24 canonical `Rock Type*.dae` originals (KEEP)
- `Assets/Vendor/CartoonFX/` — including the ~58 internally-duplicated files in `Animations/` (vendor structure, not project issue)
- `Assets/Vendor/RandomAreaSpawner/`
- `Assets/Vendor/TextMesh Pro/`
- `Assets/Vendor/AI Toolkit/`

### GeneratedAssets/ (project root — SKIP ALL)
All files under `GeneratedAssets/` at the project root are AI Toolkit outputs. Reason: "AI Toolkit outputs — not Unity assets" (rule 15). They are not inside `Assets/` and do not appear in the Unity asset database.

### Assets/AI Toolkit/ (11 files — SKIP)
The 11 hash-named `.png` files under `Assets/AI Toolkit/Temp/AssistantImageReferences/` are AI session reference screenshots. They are tracked by Unity but are not game assets. Leave in place unless the AI Toolkit plugin specifically instructs removal.

### Code/Editor Scripts (3 files — SKIP)
`Assets/Fight or Flight/Code/Editor/GamePausedUISetup.cs`, `LegacyHudCleanupTool.cs`, and `PrefabSetup.cs` compile correctly in their current location (Unity honors any folder named `Editor`). No action needed.

### Code/ScriptsReference.cs (1 file — SKIP)
`Assets/Fight or Flight/Code/ScriptsReference.cs` — lone file at the Code/ root. Appears intentional as a reference/scratch file. Leave in place.

---

## Notes for Agent 3

### Critical: Binary Scene Files
Both `MainScene.unity` and `MainMenu.unity` are stored in Unity's **binary serialized format**. GUID references inside them cannot be read as plain text. This means it was impossible to confirm whether the binary scenes reference any of the flagged files by GUID. Treat every MOVE and QUARANTINE as potentially affecting a binary scene reference. **Always perform moves through the Unity Editor (drag-and-drop in the Project window) rather than filesystem operations.** The Unity Editor will automatically rewrite GUIDs in all referencing files. If moves are done on the filesystem, Unity will break links on next editor open.

### Execution Order — Recommended
1. Open the Unity project in the Unity Editor before starting any moves.
2. Use the Unity Editor's Project window to drag files to new locations. This preserves GUIDs and updates all in-project references automatically.
3. Execute in this order to minimize broken references:
   a. Move C# scripts first (Misplaced C# Scripts group) — lowest risk, no prefab references.
   b. Move textures and font (Model/Font Files group) — low risk.
   c. Move GLB models from Prefabs/Enemies/ to Models/Enemies/ — after moving, check that no prefab/scene references are broken.
   d. Move SettingsMenu.prefab, CloseBtn.prefab, and the Sprites/UI prefabs.
   e. Move Mainenemy.prefab.
   f. Check Rock prefab source references BEFORE quarantining the 24 rock .dae duplicates.
   g. Quarantine Clone prefabs, duplicate images, auto-numbered files, and .DS_Store last.
4. After all moves, run `Assets > Reimport All` once to let Unity refresh all asset paths.

### Rock .dae Quarantine Warning (CRITICAL)
Before quarantining the 24 `.dae` files from `Content/Prefabs/Environment/Rocks/Models/`, open each of the 24 Rock prefabs in `Content/Prefabs/Environment/Rocks/Prefabs/` and check which GUID their MeshFilter/MeshRenderer references. If they reference the **project copy GUIDs** (not the Vendor GUIDs), you must re-point them to `Vendor/BrokenVector/Models/` first (easiest done by re-assigning in the Inspector). Only quarantine the project copies after confirming the prefabs use Vendor GUIDs.

### ENEMY (1) and EnemyShip (1) Prefab Warning
Before quarantining `ENEMY (1).prefab` and `EnemyShip (1).prefab`, confirm neither is assigned in the MainScene's `EnemySpawner` component field. The scene is binary so you must check visually in the Unity Inspector.

### Fonts Destination
`Content/Fonts/` does not currently exist. Agent 3 must create this folder before moving the Inter font file. The cleanest way is to create it in the Unity Editor Project window (Right-click > Create > Folder).

### Content/Textures/UI/ Sub-folder
`Content/Textures/UI/` does not currently exist. Create it before moving health.png, heat.png, shield.png.

### Content/Prefabs/UI/ Sub-folder
`Content/Prefabs/UI/` does not currently exist. Create it before moving SettingsMenu.prefab, CloseBtn.prefab, ButtonBG_Refined.prefab, PlayerHUD.prefab, SciFiButtonFrame_WithBG.prefab.

### .meta File Handling
For any filesystem-level move (if not done through the Unity Editor), always move the `.meta` file alongside its asset file. Never move an asset without its `.meta` — this will cause Unity to re-import the asset with a new GUID and break all existing references.

### Quarantine Folder Structure
The `_quarantine/` folder at the project root is currently empty. Replicate the full relative path under `_quarantine/` so quarantined files can be traced back to their original location and restored if needed. Example: `Assets/Fight or Flight/Content/Sprites/UI/ButtonBG.png` → `_quarantine/Assets/Fight or Flight/Content/Sprites/UI/ButtonBG.png`.
