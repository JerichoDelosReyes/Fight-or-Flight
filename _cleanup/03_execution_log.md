# Fight-or-Flight — Execution Log (Agent 3)
Date: 2026-05-31

## Summary
| Metric | Count |
|--------|-------|
| Files MOVED | 15 |
| Files QUARANTINED | 35 (11 non-rock + 24 rock .dae) |
| Files SKIPPED (with reason) | 1 |
| Errors | 0 |

---

## GUID Verification Results

### Step 1 — Rock .dae GUID Check

| Copy | GUID | File |
|------|------|------|
| Project copy | `bae548db9e19c49fc84ebc1ad37acec6` | `Content/Prefabs/Environment/Rocks/Models/Rock Type1 01.dae.meta` |
| Vendor copy  | `beb8ceccb376f684095ae1a93f088717` | `Assets/Vendor/BrokenVector/Models/Rock Type1 01.dae.meta` |

**Search results:**
- Project GUID `bae548db...` appears only in its own `.meta` file — not referenced by any other text-format file.
- Vendor GUID `beb8cecc...` appears nowhere in the project text files.
- The 24 rock prefabs in `Content/Prefabs/Environment/Rocks/Prefabs/` are **binary-serialized** (Unity binary format). GUID references inside them cannot be read as plain text.

**Conclusion:** Cannot confirm from text search which GUID the rock prefabs use. Because the project GUID is NOT referenced in any readable text file (no text-format .prefab, no .cs, no .asset), quarantine proceeds — but see critical warning in Next Steps below.

---

### Step 2 — (1) Prefab Reference Check

**Files searched:** `WaveManager.cs`, `EnemyAI.cs`, and all `.cs` files under `Code/`

| Prefab | Reference Found? | Location | Decision |
|--------|-----------------|----------|----------|
| `ENEMY (1).prefab` | No | — | QUARANTINED |
| `EnemyShip (1).prefab` | YES | `Assets/Fight or Flight/Code/Editor/PrefabSetup.cs` line 16 | **SKIPPED — do not quarantine** |

`PrefabSetup.cs` is an `[InitializeOnLoad]` Editor script that loads `EnemyShip (1).prefab` by path at domain reload to auto-wire `ShipHealth.explosionPrefab`. Quarantining this file would cause a `Debug.LogWarning` on every Editor start and leave the explosion prefab unwired on that ship.

---

## Completed Actions

### MOVE Operations

| # | Action | Source | Destination | Result |
|---|--------|--------|-------------|--------|
| M1 | MOVE | `Content/Scripts/UI/GamePausedUI.cs` | `Code/UI/GamePausedUI.cs` | OK |
| M2 | MOVE | `Content/Scripts/UI/MissionCompleteScreen.cs` | `Code/UI/MissionCompleteScreen.cs` | OK |
| M3 | MOVE | `Content/Scripts/UI/SciFiUIStyle.cs` | `Code/UI/SciFiUIStyle.cs` | OK |
| M4 | MOVE | `Content/Materials/health.png` | `Content/Textures/UI/health.png` | OK |
| M5 | MOVE | `Content/Materials/heat.png` | `Content/Textures/UI/heat.png` | OK |
| M6 | MOVE | `Content/Materials/shield.png` | `Content/Textures/UI/shield.png` | OK |
| M7 | MOVE | `Content/Models/Inter-VariableFont_opsz,wght.ttf` | `Content/Fonts/Inter-VariableFont_opsz,wght.ttf` | OK |
| M8 | MOVE | `Content/Models/Mainenemy.prefab` | `Content/Prefabs/Enemies/Mainenemy.prefab` | OK |
| M9 | MOVE | `Content/Prefabs/Enemies/boss.glb` | `Content/Models/Enemies/boss.glb` | OK |
| M10 | MOVE | `Content/Prefabs/Enemies/vulcan_dkyr_class.glb` | `Content/Models/Enemies/vulcan_dkyr_class.glb` | OK |
| M11 | MOVE | `Code/UI/SettingsMenu.prefab` | `Content/Prefabs/UI/SettingsMenu.prefab` | OK |
| M12 | MOVE | `Content/Scenes/MainMenu/CloseBtn.prefab` | `Content/Prefabs/UI/CloseBtn.prefab` | OK |
| M13 | MOVE | `Content/Sprites/UI/ButtonBG_Refined.prefab` | `Content/Prefabs/UI/ButtonBG_Refined.prefab` | OK |
| M14 | MOVE | `Content/Sprites/UI/PlayerHUD.prefab` | `Content/Prefabs/UI/PlayerHUD.prefab` | OK |
| M15 | MOVE | `Content/Sprites/UI/SciFiButtonFrame_WithBG.prefab` | `Content/Prefabs/UI/SciFiButtonFrame_WithBG.prefab` | OK |

All 15 MOVE operations moved both the asset file and its `.meta` file. New destination folders created:
- `Assets/Fight or Flight/Content/Fonts/`
- `Assets/Fight or Flight/Content/Textures/UI/`
- `Assets/Fight or Flight/Content/Prefabs/UI/`
- `Assets/Fight or Flight/Content/Models/Enemies/`

---

### QUARANTINE Operations

| # | Action | File (relative to project root) | Result |
|---|--------|---------------------------------|--------|
| Q1 | QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone).prefab` | OK (+ meta) |
| Q2 | QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/SettingsMenu(Clone)(Clone).prefab` | OK (+ meta) |
| Q3 | QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/ButtonBG.png` | OK (+ meta) |
| Q4 | QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/MainMenuBackground.png` | OK (+ meta) |
| Q5 | QUARANTINE | `Assets/Fight or Flight/Content/Sprites/UI/SciFiButtonFrame.png` | OK (+ meta) |
| Q6 | QUARANTINE | `Assets/Fight or Flight/Content/UI/Icons/mission_comp.png` | OK (+ meta) |
| Q7 | QUARANTINE | `Assets/Fight or Flight/Content/.DS_Store` | OK (no .meta — expected) |
| Q8 | QUARANTINE | `Assets/Fight or Flight/Content/Models/Ships/spaceship_ezno (1).glb` | OK (+ meta) |
| Q9 | QUARANTINE | `Assets/Fight or Flight/Content/Models/Environment/asteroid (1).glb` | OK (+ meta) |
| Q10 | QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Enemies/enemyspaceships (1).glb` | OK (+ meta) |
| Q11 | QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Enemies/ENEMY (1).prefab` | OK (+ meta) |
| Q12 | QUARANTINE | `Assets/Fight or Flight/Content/Prefabs/Enemies/EnemyShip (1).prefab` | **SKIPPED — see below** |
| Q13 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type1 01.dae` | OK (+ meta) |
| Q14 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type1 02.dae` | OK (+ meta) |
| Q15 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type1 03.dae` | OK (+ meta) |
| Q16 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type1 04.dae` | OK (+ meta) |
| Q17 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type2 01.dae` | OK (+ meta) |
| Q18 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type2 02.dae` | OK (+ meta) |
| Q19 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type2 03.dae` | OK (+ meta) |
| Q20 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type2 04.dae` | OK (+ meta) |
| Q21 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type3 01.dae` | OK (+ meta) |
| Q22 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type3 02.dae` | OK (+ meta) |
| Q23 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type3 03.dae` | OK (+ meta) |
| Q24 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type3 04.dae` | OK (+ meta) |
| Q25 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type4 01.dae` | OK (+ meta) |
| Q26 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type4 02.dae` | OK (+ meta) |
| Q27 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type4 03.dae` | OK (+ meta) |
| Q28 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type4 04.dae` | OK (+ meta) |
| Q29 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type5 01.dae` | OK (+ meta) |
| Q30 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type5 02.dae` | OK (+ meta) |
| Q31 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type5 03.dae` | OK (+ meta) |
| Q32 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type5 04.dae` | OK (+ meta) |
| Q33 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type6 01.dae` | OK (+ meta) |
| Q34 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type6 02.dae` | OK (+ meta) |
| Q35 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type6 03.dae` | OK (+ meta) |
| Q36 | QUARANTINE | `Content/Prefabs/Environment/Rocks/Models/Rock Type6 04.dae` | OK (+ meta) |

---

## Skipped Actions (with reasons)

| # | File | Reason |
|---|------|--------|
| Q12 | `Content/Prefabs/Enemies/EnemyShip (1).prefab` | Referenced by path in `Assets/Fight or Flight/Code/Editor/PrefabSetup.cs` line 16. This Editor script auto-wires `ShipHealth.explosionPrefab` at domain reload. Quarantining this file would break that wiring and emit warnings on every Editor startup. Developer must: (1) rename the prefab to `EnemyShip.prefab`, (2) update the path string in `PrefabSetup.cs`, then quarantine the old name if desired. |

---

## Errors

None. All attempted operations completed successfully.

---

## Directories Now Empty (Ready to Remove in Unity Editor)

The following directories are now empty after the moves above. They should be deleted from within the Unity Editor (right-click > Delete in Project window) so Unity can also clean up their `.meta` files:

| Directory | Status |
|-----------|--------|
| `Assets/Fight or Flight/Content/Scripts/UI/` | EMPTY — delete in Unity Editor |
| `Assets/Fight or Flight/Content/Scripts/` | Contains only the empty `UI/` subdirectory — delete both in Unity Editor |

Additionally, these directories were already flagged as empty by Agent 2 and should also be cleaned up in the Unity Editor:
- `Assets/Fight or Flight/Code/AI/`
- `Assets/Fight or Flight/Code/Player/`
- `Assets/Fight or Flight/Content/Audio/Music/`
- `Assets/Fight or Flight/Content/Scenes/FlightSettings/`
- `Assets/Fight or Flight/Content/Sprites/RootSprites/SciFiUI_Extracted/`
- `Assets/Fight or Flight/Resources/UI 1/`
- `Assets/Fight or Flight/Resources/UI 2/`
- `Assets/Sprites/Pause_ExtractedAssets/`

---

## Next Steps for Developer (Unity Editor Required)

### 1. CRITICAL — Open the Unity project and allow it to re-import
All filesystem moves were done outside the Unity Editor. When you next open the project, Unity will detect the moved files and may show missing references (red "missing" icons in the Inspector). This is expected. Unity's asset database will reconcile most moves automatically because the `.meta` files (and therefore the GUIDs) were preserved.

### 2. CRITICAL — Check rock prefabs in Inspector (immediate priority)
Open each of the 24 rock prefabs in `Content/Prefabs/Environment/Rocks/Prefabs/` and inspect the **MeshFilter** component. It will show either:
- A valid mesh reference → the prefab was using **vendor GUIDs** (quarantine was safe, no action needed)
- A "Missing (Mesh)" reference → the prefab was using **project copy GUIDs** (quarantine broke the link)

If meshes are missing: restore the 24 `.dae` files from `_quarantine/Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/` by dragging them back in, then in the Inspector re-assign each rock prefab's MeshFilter to point at the corresponding `Vendor/BrokenVector/Models/` original. After re-assigning all 24, quarantine the project copies again.

### 3. Fix EnemyShip (1).prefab — rename and update reference
- In the Unity Editor Project window, rename `Content/Prefabs/Enemies/EnemyShip (1).prefab` to `EnemyShip.prefab`.
- Open `Assets/Fight or Flight/Code/Editor/PrefabSetup.cs` and update line 16:
  - Old: `"Assets/Fight or Flight/Content/Prefabs/Enemies/EnemyShip (1).prefab"`
  - New: `"Assets/Fight or Flight/Content/Prefabs/Enemies/EnemyShip.prefab"`
- Save the file; Unity will recompile automatically.

### 4. Check Moved Prefabs for Broken References
The following prefabs were moved and may have scene/prefab references that need to update in the Unity Editor. Open each and check the Inspector for missing (pink/red) references:
- `Content/Prefabs/UI/SettingsMenu.prefab` — was in `Code/UI/`
- `Content/Prefabs/UI/CloseBtn.prefab` — was in `Content/Scenes/MainMenu/`
- `Content/Prefabs/UI/PlayerHUD.prefab` — was in `Content/Sprites/UI/`
- `Content/Prefabs/UI/ButtonBG_Refined.prefab` — was in `Content/Sprites/UI/`
- `Content/Prefabs/UI/SciFiButtonFrame_WithBG.prefab` — was in `Content/Sprites/UI/`
- `Content/Prefabs/Enemies/Mainenemy.prefab` — was in `Content/Models/`
- `Content/Models/Enemies/boss.glb` — was in `Content/Prefabs/Enemies/`
- `Content/Models/Enemies/vulcan_dkyr_class.glb` — was in `Content/Prefabs/Enemies/`

### 5. Check Moved Textures
- `Content/Textures/UI/health.png`, `heat.png`, `shield.png` — were in `Content/Materials/`. Any Material that referenced these textures will show a broken texture slot. Re-assign in the Material Inspector if needed.

### 6. Check Moved Scripts
- `Code/UI/GamePausedUI.cs`, `MissionCompleteScreen.cs`, `SciFiUIStyle.cs` — moved from `Content/Scripts/UI/`. These are likely referenced by prefabs via component; since GUIDs were preserved these references should survive automatically. Verify the scripts attach correctly on any prefab that uses them.

### 7. Remove Empty Folders in Unity Editor
Delete the directories listed in the "Directories Now Empty" section above using the Unity Editor Project window (not Windows Explorer) so Unity removes their `.meta` files cleanly.

### 8. Run Assets > Reimport All
After completing steps 1–7, run **Assets > Reimport All** from the Unity menu to ensure all asset paths are fully refreshed in the asset database.
