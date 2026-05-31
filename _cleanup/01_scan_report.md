# Fight-or-Flight — Project Scan Report
Generated: 2026-05-31
Scanner: Agent 1

## Summary
- Total files scanned: 744 (non-meta files in Assets/)
- Issues found: 38
  - Duplicate filename groups: 9 (project-level); 24 (Rock .dae vendor cross-copy); ~58 (CartoonFX internal)
  - Misplaced files: 12
  - Orphaned .meta files: 0
  - Files missing .meta: 0
  - Empty directories: 8
  - Suspicious files: 9

---

## Section 1 — Full Folder Tree

### Project Root (direct children only)
```
Fight-or-Flight/
  Assets/
  GeneratedAssets/
  Library/          (Unity cache — not tracked)
  Logs/             (Unity logs — not tracked)
  Packages/
  ProjectSettings/
  README.md
  Temp/             (Unity temp — not tracked)
  UserSettings/     (Unity user settings — not tracked)
  _cleanup/         (Cleanup working folder)
  _quarantine/      (Empty — 0 files)
```

### Assets/ — Top-Level Folders
```
Assets/
  AI Toolkit/           (11 non-meta files)
  AI Toolkit.meta
  Fight or Flight/      (238 non-meta files — project content)
  Fight or Flight.meta
  Sprites/              (0 non-meta files — leaf is empty)
  Sprites.meta
  Vendor/               (477 non-meta files — third-party)
  Vendor.meta
  _Archive/             (18 non-meta files — archived scripts/scenes)
  _Archive.meta
```
Note: 5 `.meta` files sit directly in `Assets/` root (one per subfolder). No loose non-meta files at the Assets root.

---

### Assets/Fight or Flight/ — Full Tree

```
Fight or Flight/
  Code/
    AI/                         [EMPTY DIRECTORY]
    Camera/
      LagCamera.cs + .meta
    Combat/
      Billboard.cs + .meta
      Laser.cs + .meta
      ShipLaserProjectile.cs + .meta
    Editor/
      GamePausedUISetup.cs + .meta
      LegacyHudCleanupTool.cs + .meta
      PrefabSetup.cs + .meta
    Enemy/
      EnemyAI.cs + .meta
      EnemyAttack.cs + .meta
      EnemyHealthBar.cs + .meta
      EnemyMovement.cs + .meta
    Player/                     [EMPTY DIRECTORY]
    Ship/
      Ship.cs + .meta
      ShipCollisionEffects.cs + .meta
      ShipCombat.cs + .meta
      ShipHealth.cs + .meta
      ShipInput.cs + .meta
      ShipPhysics.cs + .meta
    UI/
      CenterCrosshair.cs + .meta
      CompassBar.cs + .meta
      DefeatScreen.cs + .meta
      HealthUI.cs + .meta
      HUDController.cs + .meta
      HUDManager.cs + .meta
      HudScanlines.cs + .meta
      LegacyHUDCleanup.cs + .meta
      MainMenuController.cs + .meta
      MouseCrosshairUI.cs + .meta
      PauseManager.cs + .meta
      PlayerHUD.cs + .meta
      Radar.cs + .meta
      RoundedRectSprite.cs + .meta
      ScoreHUD.cs + .meta
      ScorePopupFloat.cs + .meta
      SettingsMenu.cs + .meta
      SettingsMenu.prefab + .meta     [MISPLACED — prefab in Code/]
      SpeedUI.cs + .meta
      TitlePulse.cs + .meta
      VictoryScreen.cs + .meta
      WaveManager.cs + .meta
    Utils/
      ArenaBoundary.cs + .meta
      ControlSchemeManager.cs + .meta
      DebrisScatter.cs + .meta
      DifficultyManager.cs + .meta
      Explosion.cs + .meta
      GameModeManager.cs + .meta
      GameplayUtils.cs + .meta
    ScriptsReference.cs + .meta       [lone file at Code/ root]
  Content/
    Animations/
      LaserController.controller + .meta
      LaserController_Red.controller + .meta
      LaserFiring.anim + .meta
      LaserFiring_Red.anim + .meta
    Audio/
      Music/                          [EMPTY DIRECTORY]
      SFX/
        alarm-incoming.mp3 + .meta
        EngineHum.wav + .meta
        EngineRumble.wav + .meta
        Explosion 1.wav + .meta
        Explosion.wav + .meta
        LaserImpact.wav + .meta
        LaserShot.wav + .meta
    Materials/
      EnemyLaserMat.mat + .meta
      LaserAdditive.mat + .meta
      LaserYellow.mat + .meta
      health.png + .meta              [MISPLACED — png in Materials/]
      heat.png + .meta                [MISPLACED — png in Materials/]
      shield.png + .meta              [MISPLACED — png in Materials/]
    Models/
      Inter-VariableFont_opsz,wght.ttf + .meta  [MISPLACED — font in Models/]
      Mainenemy.prefab + .meta        [MISPLACED — prefab in Models/]
      Enemies/
        enemy.glb + .meta
      Environment/
        asteroid (1).glb + .meta      [SUSPICIOUS — (1) suffix]
        background.glb + .meta
      Ships/
        no_mans_sky_-_utopia_speeder.glb + .meta
        spaceship_ezno (1).glb + .meta   [SUSPICIOUS — (1) suffix duplicate]
        spaceship_ezno.glb + .meta
        the_lightsaber.glb + .meta
    Prefabs/
      Enemies/
        boss.glb + .meta              [MISPLACED — model in Prefabs/]
        ENEMY (1).prefab + .meta      [SUSPICIOUS — (1) suffix]
        EnemyShip (1).prefab + .meta  [SUSPICIOUS — (1) suffix]
        enemyspaceships (1).glb + .meta  [SUSPICIOUS — (1) suffix, model in Prefabs/]
        LaserShip.prefab + .meta
        vulcan_dkyr_class.glb + .meta [MISPLACED — model in Prefabs/]
      Environment/
        Asteroid_New.prefab + .meta
        Rocks/
          Models/                     (24 × Rock Type*.dae + .meta)  [DUPLICATED from Vendor]
          Prefabs/                    (24 × Rock Type*.prefab + .meta)
      Player/
        PlayerShip.prefab + .meta
      VFX/
        EnemyLaserProjectile.prefab + .meta
        ExplosionEffect.prefab + .meta
        LaserImpactParticles.prefab + .meta
        LaserProjectile.prefab + .meta
        ThrusterParticles.prefab + .meta
    Reference/
      Defeat.png + .meta
      Mission Complete.png + .meta
      Pause.png + .meta
      Pause_asset_sheet_1.png + .meta
      Pause_asset_sheet_2.png + .meta
    Scenes/
      FlightSettings/               [EMPTY DIRECTORY]
      FlightSettings.lighting + .meta
      MainMenu/
        CloseBtn.prefab + .meta     [MISPLACED — prefab in Scenes/]
        MainMenu.unity + .meta
        MainMenu 1.unity + .meta
      MainScene/
        Flight/
          LightingData.asset + .meta
          ReflectionProbe-0.exr + .meta
        MainScene.unity + .meta
    Scripts/                        [MISPLACED — .cs files under Content/Scripts instead of Code/]
      UI/
        GamePausedUI.cs + .meta
        MissionCompleteScreen.cs + .meta
        SciFiUIStyle.cs + .meta
    Sprites/
      RootSprites/
        SciFiUI_Extracted/          [EMPTY DIRECTORY]
        SciFiUI_asset_sheet_1.png + .meta
        SciFiUI_asset_sheet_2.png + .meta
      UI/
        ButtonBG.png + .meta
        ButtonBG_Refined.png + .meta
        ButtonBG_Refined.prefab + .meta  [MISPLACED — prefab in Sprites/]
        ButtonOutline.png + .meta
        Circle.png + .meta
        ContentUI/
          SciFiButtonFrame.png + .meta   [DUPLICATE of ../SciFiButtonFrame.png]
        Crosshair.png + .meta
        HexButton.png + .meta
        MainMenuBackground.png + .meta   [DUPLICATE of Content/Textures/MainMenuBackground.png]
        MouseCrosshair.png + .meta
        PlayerHUD.prefab + .meta         [MISPLACED — prefab in Sprites/UI/]
        SciFiButtonFrame.png + .meta
        SciFiButtonFrame_WithBG.png + .meta
        SciFiButtonFrame_WithBG.prefab + .meta  [MISPLACED — prefab in Sprites/UI/]
        SettingsMenu(Clone)(Clone).prefab + .meta  [CLONE ARTIFACT]
        SettingsMenu(Clone).prefab + .meta         [CLONE ARTIFACT]
        SoftCircle.png + .meta
        TitleFightOrFlight.png + .meta
        TitleFightOrFlight_V2.png + .meta
        TitleSprite.png + .meta
      LaserAnimation.png + .meta
      LaserAnimation_Red.png + .meta
      LaserBolt.png + .meta
      LaserBolt_Red.png + .meta
    Textures/
      MainMenuBackground.png + .meta   [DUPLICATE of Content/Sprites/UI/MainMenuBackground.png]
      ParticleFlare.png + .meta
    UI/
      Backgrounds/
        ButtonBG.png + .meta           [DUPLICATE of Content/Sprites/UI/ButtonBG.png]
        ButtonBG_Resume.png + .meta
        FinalPausePanelBG.png + .meta
        FinalResumeButtonBG.png + .meta
        FixedPausePanelBG.png + .meta
        FixedResumeButtonBG.png + .meta
        NewButtonBG.png + .meta
        NewButtonBG_Resume.png + .meta
        NewPausePanelBG.png + .meta
        PausePanelBG.png + .meta
      Icons/
        FinalQuitIcon.png + .meta
        FinalResumeIcon.png + .meta
        FinalSettingsIcon.png + .meta
        FixedQuitIcon.png + .meta
        mission_comp.png + .meta       [DUPLICATE of Resources/UI/Sprites/mission_comp.png]
        NewQuitIcon.png + .meta
        NewResumeIcon.png + .meta
        NewSettingsIcon.png + .meta
        QuitIcon.png + .meta
        RestartIcon.png + .meta
        ResumeIcon.png + .meta
        SettingsIcon.png + .meta
    .DS_Store                          [macOS metadata artifact]
  Resources/
    RootResources/
      SciFiUI/
        button_large.png + .meta
        button_small.png + .meta
        checkbox_bg.png + .meta
        checkmark.png + .meta
        divider.png + .meta
        header_bar.png + .meta
        panel_frame.png + .meta
        slider_handle.png + .meta
        slider_track.png + .meta
      UI/
        InstructionsOverlay.prefab + .meta
    UI/
      PauseOverlayPrefab.prefab + .meta
      Sprites/
        Boxy_UI_Sheet.png + .meta
        button_base.png + .meta
        button_highlighted.png + .meta
        defeat_helmet_new.png + .meta
        defeat_helmet_raw.png + .meta
        divider_line.png + .meta
        header_box.png + .meta
        mission_comp.png + .meta       [DUPLICATE of Content/UI/Icons/mission_comp.png]
        mission_complete_icon.png + .meta
        mission_complete_star.png + .meta
        panel_background.png + .meta
        quit_icon.png + .meta
        quit_icon_v2.png + .meta
        restart_icon.png + .meta
        restart_icon_v2.png + .meta
        resume_icon.png + .meta
        settings_icon.png + .meta
        settings_icon_v2.png + .meta
        Boxy/
          boxy_button_glow.png + .meta
          boxy_divider.png + .meta
          boxy_header_bg.png + .meta
          boxy_panel_bg.png + .meta
          Icons_Sheet.png + .meta
          Icons/
            quit.png + .meta
            restart.png + .meta
            resume.png + .meta
            settings.png + .meta
    UI 1/                             [EMPTY DIRECTORY]
    UI 2/                             [EMPTY DIRECTORY]
```

---

### Assets/Vendor/ — Subfolder Summary
```
Vendor/
  AI Toolkit/       (60 non-meta files — temp reference images)
  BrokenVector/     (33 non-meta files)
    Materials/      (4 .mat)
    Models/         (24 .dae — Rock Type* 1-6, variants 01-04)
    Textures/       (4 .png colorsheets)
  CartoonFX/        (213 non-meta files)
    Animations/     (122 files — duplicates many files in Materials/Textures/Shaders)
    Materials/      (44 .mat)
    Models/         (3 .asset/.FBX)
    Shaders/        (2 .shader)
    Textures/       (42 .png/.tga)
  RandomAreaSpawner/ (3 non-meta files)
    RandomAreaSpawner.cs
    Prefabs/
      AsteroidCube.prefab
      AsteroidSpawner.prefab
  TextMesh Pro/     (178 non-meta files)
```

---

### Assets/_Archive/ — Full Tree
```
_Archive/
  Plans/
    main-menu-ui-imitation.md + .meta
  _Recovery/
    0.unity + .meta
  _ScriptsReference/
    Asteroid.cs + .meta
    AsteroidManager.cs + .meta
    EnemySpawner.cs + .meta
    FollowCam.cs + .meta
    GameEventManager.cs + .meta
    GameScore.cs + .meta
    GameTimer.cs + .meta
    GameUI.cs + .meta
    Pickup.cs + .meta
    PlayGameButton.cs + .meta
    Player.cs + .meta
    Rotate.cs + .meta
    Shield.cs + .meta
    ShieldUI.cs + .meta
    Thruster.cs + .meta
  Prefab scenes.unity + .meta
```

---

### Assets/AI Toolkit/ (top-level, separate from Vendor/AI Toolkit)
```
AI Toolkit/
  Temp/
    AssistantImageReferences/
      23098ee7...png + .meta   (11 × hash-named PNG screenshots)
      2e0c64f5...png + .meta
      3e4eda6f...png + .meta
      4c20907b...png + .meta
      4d245348...png + .meta
      74d34975...png + .meta
      b36aef15...png + .meta
      c4566564...png + .meta
      cf8aeec2...png + .meta
      e6444cf4...png + .meta
      fa39e215...png + .meta
```

---

### Assets/Sprites/
```
Sprites/
  Pause_ExtractedAssets/   [EMPTY DIRECTORY — folder + .meta only]
```

---

### ProjectSettings/ (24 files)
```
AudioManager.asset, ClusterInputManager.asset, DynamicsManager.asset,
EditorBuildSettings.asset, EditorSettings.asset, GraphicsSettings.asset,
InputManager.asset, MemorySettings.asset, MultiplayerManager.asset,
NavMeshAreas.asset, NetworkManager.asset, PackageManagerSettings.asset,
Packages/ (subfolder), Physics2DSettings.asset, PresetManager.asset,
ProjectSettings.asset, ProjectVersion.txt, QualitySettings.asset,
SceneTemplateSettings.json, TagManager.asset, TimeManager.asset,
UnityConnectSettings.asset, VFXManager.asset, VersionControlSettings.asset
```

### Packages/
```
manifest.json
packages-lock.json
```

---

### GeneratedAssets/ (AI Toolkit output — project root, NOT under Assets/)
```
GeneratedAssets/
  0341fb2d.../  019e409a-*.png + .json  (×2)
  02489ecd.../  019e4ae7-*.png + .json, divider.png + .json
  058cdc25.../  019e4ae6-*.png + .json
  066e5ea0.../  019e4ae7-*.png + .json, checkmark.png + .json
  11c0d078.../  019e4ae7-*.png + .json, button_small.png + .json
  147996c6.../  019e40e4-*.wav + .json
  367cc507.../  019e445d-*.png + .json
  36e4f8ed.../  019e40df-*.png + .json  (×2)
  38aeec46.../  019e4ae7-*.png + .json, header_bar.png + .json
  40da1a80.../  019e574d-*.png + .json
  434125a7.../  019e4ae7-*.png + .json, slider_track.png + .json
  559c70b6.../  019e40e4-*.wav + .json
  6141ca6d.../  019e4ae7-*.png + .json, panel_frame.png + .json
  6d94b176.../  019e3fc6-*.mp4 + .json, 019e3fc6-*.png + .json
  71562d4a.../  019e3fbc-*.mp4 + .json, 019e3fbd-*.png + .json, 019e3fc3-*.png + .json
  713f30ed.../  019e4ae7-*.png + .json, button_large.png + .json
  4dd7811.../   019e3fba-*.png + .json, 019e3fc3-*.png + .json
  83271444.../  019e574d-*.png + .json
  8523230f.../  019e445e-*.png + .json  (×2)
  a37f142b.../  019e4ae7-*.png + .json, checkbox_bg.png + .json
  a3e7786d.../  019e4ae7-*.png + .json, slider_handle.png + .json
  a6131a11.../  019e40df-*.wav + .json
  a6947b09.../  019e40df-*.wav + .json
  a7a05289.../  019e409a-*.png + .json  (×2)
  c11f72a0.../  019e4a42-*.png + .json  (×2)
  c7331c5e.../  019e4ae6-*.png + .json
  c8106dd8.../  019e4092-*.png + .json  (×2)
  d88665a3.../  019e3fc4-*.png + .json
  e95fbeaf.../  019e574d-*.png + .json
  fd7815ca.../  019e40df-*.wav + .json
  fe70235b.../  019e40e4-*.wav + .json
  (Total: ~30 hash-folders, each with 1-3 generated media files + .json sidecars)
```

---

## Section 2 — Duplicate Filenames

### Project-Level Duplicates (within Assets/Fight or Flight/)

| Filename | Copy 1 | Copy 2 | Note |
|---|---|---|---|
| `ButtonBG.png` | `Content/Sprites/UI/ButtonBG.png` | `Content/UI/Backgrounds/ButtonBG.png` | True duplicate PNG |
| `MainMenuBackground.png` | `Content/Sprites/UI/MainMenuBackground.png` | `Content/Textures/MainMenuBackground.png` | True duplicate PNG |
| `SciFiButtonFrame.png` | `Content/Sprites/UI/SciFiButtonFrame.png` | `Content/Sprites/UI/ContentUI/SciFiButtonFrame.png` | True duplicate PNG |
| `mission_comp.png` | `Content/UI/Icons/mission_comp.png` | `Resources/UI/Sprites/mission_comp.png` | True duplicate PNG |

### Rock .dae Model Duplicates (vendor vs. project copy)

All 24 `Rock Type* **.dae` files are present in **three** locations:
- `Assets/Vendor/BrokenVector/Models/Rock Type* **.dae` — original vendor copy
- `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Models/Rock Type* **.dae` — copied into project
- `Assets/Fight or Flight/Content/Prefabs/Environment/Rocks/Prefabs/Rock Type* **.prefab` — (these are prefabs built from the models, not true filename duplicates, but co-located with the .dae copies)

The `.dae` files in `Content/Prefabs/Environment/Rocks/Models/` are byte-for-byte copies of the vendor originals in `Vendor/BrokenVector/Models/`. The vendor copy should be considered canonical.

### Unity Auto-Numbered Duplicates (likely accidental imports)

| Filename | Original | Duplicate |
|---|---|---|
| `spaceship_ezno.glb` | `Content/Models/Ships/spaceship_ezno.glb` | `Content/Models/Ships/spaceship_ezno (1).glb` |
| `asteroid.glb` | (no base version in same folder) | `Content/Models/Environment/asteroid (1).glb` |
| `ENEMY.prefab` | (no base version) | `Content/Prefabs/Enemies/ENEMY (1).prefab` |
| `EnemyShip.prefab` | (no base version) | `Content/Prefabs/Enemies/EnemyShip (1).prefab` |
| `enemyspaceships.glb` | (no base version) | `Content/Prefabs/Enemies/enemyspaceships (1).glb` |

### CartoonFX Internal Duplicates (vendor — lower priority)

The `Assets/Vendor/CartoonFX/Animations/` subfolder contains a near-complete mirror of the top-level `CartoonFX/Materials/`, `CartoonFX/Textures/`, `CartoonFX/Shaders/`, and `CartoonFX/Models/` folders. Approximately 58 files are duplicated within the CartoonFX vendor package. This appears to be how the CartoonFX asset package was structured and is a vendor issue, not a project issue.

---

## Section 3 — Misplaced Files

| File | Current Location | Issue |
|---|---|---|
| `SettingsMenu.prefab` | `Assets/Fight or Flight/Code/UI/` | Prefab in a Code/ folder; should be in `Content/Prefabs/` |
| `health.png` | `Assets/Fight or Flight/Content/Materials/` | PNG texture in Materials folder |
| `heat.png` | `Assets/Fight or Flight/Content/Materials/` | PNG texture in Materials folder |
| `shield.png` | `Assets/Fight or Flight/Content/Materials/` | PNG texture in Materials folder |
| `Inter-VariableFont_opsz,wght.ttf` | `Assets/Fight or Flight/Content/Models/` | Font file in Models folder |
| `Mainenemy.prefab` | `Assets/Fight or Flight/Content/Models/` | Prefab file in Models folder |
| `boss.glb` | `Assets/Fight or Flight/Content/Prefabs/Enemies/` | Model (.glb) in Prefabs folder |
| `enemyspaceships (1).glb` | `Assets/Fight or Flight/Content/Prefabs/Enemies/` | Model (.glb) in Prefabs folder |
| `vulcan_dkyr_class.glb` | `Assets/Fight or Flight/Content/Prefabs/Enemies/` | Model (.glb) in Prefabs folder |
| `CloseBtn.prefab` | `Assets/Fight or Flight/Content/Scenes/MainMenu/` | Prefab in Scenes folder |
| `ButtonBG_Refined.prefab` | `Assets/Fight or Flight/Content/Sprites/UI/` | Prefab in Sprites folder |
| `PlayerHUD.prefab` | `Assets/Fight or Flight/Content/Sprites/UI/` | Prefab in Sprites folder |
| `SciFiButtonFrame_WithBG.prefab` | `Assets/Fight or Flight/Content/Sprites/UI/` | Prefab in Sprites folder |
| `GamePausedUI.cs` | `Assets/Fight or Flight/Content/Scripts/UI/` | .cs file in Content/Scripts instead of Code/ |
| `MissionCompleteScreen.cs` | `Assets/Fight or Flight/Content/Scripts/UI/` | .cs file in Content/Scripts instead of Code/ |
| `SciFiUIStyle.cs` | `Assets/Fight or Flight/Content/Scripts/UI/` | .cs file in Content/Scripts instead of Code/ |

### Note on Editor Scripts
The following editor-only scripts are under `Code/Editor/` rather than `Assets/Editor/` (Unity's standard location for editor scripts). Functionally they still compile correctly as long as they use `#if UNITY_EDITOR` or are placed in any folder named `Editor`. This is a style concern, not a compilation error.

- `Assets/Fight or Flight/Code/Editor/GamePausedUISetup.cs`
- `Assets/Fight or Flight/Code/Editor/LegacyHudCleanupTool.cs`
- `Assets/Fight or Flight/Code/Editor/PrefabSetup.cs`

---

## Section 4 — Orphaned .meta Files

**None found.** All 857 `.meta` files in `Assets/` have a corresponding non-meta file alongside them.

---

## Section 5 — Assets Missing .meta Files

**None found.** All 744 non-meta files in `Assets/` have a corresponding `.meta` file alongside them.

---

## Section 6 — Empty Folders

All 8 empty directories confirmed (contain no files at any depth):

| Directory | Note |
|---|---|
| `Assets/Fight or Flight/Code/AI/` | Empty placeholder — no AI scripts yet |
| `Assets/Fight or Flight/Code/Player/` | Empty placeholder — player code is in Ship/ and UI/ |
| `Assets/Fight or Flight/Content/Audio/Music/` | No music tracks; SFX are in the sibling SFX/ folder |
| `Assets/Fight or Flight/Content/Scenes/FlightSettings/` | Lighting folder with no assets inside |
| `Assets/Fight or Flight/Content/Sprites/RootSprites/SciFiUI_Extracted/` | Extraction never completed or assets moved |
| `Assets/Fight or Flight/Resources/UI 1/` | Appears to be a staging folder that was never populated |
| `Assets/Fight or Flight/Resources/UI 2/` | Same as UI 1/ |
| `Assets/Sprites/Pause_ExtractedAssets/` | Extraction folder — contents moved elsewhere |

Each empty folder does have a `.meta` file (so Unity is tracking them), but they contain no actual assets.

---

## Section 7 — Unusual/Suspicious Files

### macOS Metadata
| File | Path |
|---|---|
| `.DS_Store` | `Assets/Fight or Flight/Content/.DS_Store` |

### Clone Prefabs (Unity play-mode accidents saved to disk)
| File | Path | Severity |
|---|---|---|
| `SettingsMenu(Clone).prefab` | `Assets/Fight or Flight/Content/Sprites/UI/` | High — should be deleted |
| `SettingsMenu(Clone)(Clone).prefab` | `Assets/Fight or Flight/Content/Sprites/UI/` | High — should be deleted |

These are clearly runtime-instantiated prefabs that were accidentally saved as asset files. They have no place on disk.

### Auto-Numbered Files (likely accidental import duplicates)
| File | Path | Note |
|---|---|---|
| `spaceship_ezno (1).glb` | `Content/Models/Ships/` | Duplicate of `spaceship_ezno.glb` in same folder |
| `asteroid (1).glb` | `Content/Models/Environment/` | No base `asteroid.glb` present; name suggests a copy |
| `ENEMY (1).prefab` | `Content/Prefabs/Enemies/` | No base `ENEMY.prefab` present |
| `EnemyShip (1).prefab` | `Content/Prefabs/Enemies/` | No base `EnemyShip.prefab` present |
| `enemyspaceships (1).glb` | `Content/Prefabs/Enemies/` | No base `enemyspaceships.glb` present; model in Prefabs/ |

### GeneratedAssets/ Folder (project root)
This folder is generated by the AI Toolkit Claude Code plugin. It sits at the **project root** (not under `Assets/`) and contains AI-generated images, audio, and video from assistant sessions, paired with `.json` metadata. It is not part of the Unity asset database and does not appear in the Unity Editor. It is safe to keep but can be large.

Contents: ~30 hash-named subdirectories, each containing 1–3 generated media files (`.png`, `.mp4`, `.wav`) and corresponding `.json` sidecars.

### AI Toolkit Temp Images
`Assets/AI Toolkit/Temp/AssistantImageReferences/` contains 11 hash-named `.png` files — these are screenshot reference images uploaded during AI assistant sessions. They are tracked by Unity (have `.meta` files) but are not game assets. They can be safely removed if the AI Toolkit plugin no longer needs them.

### Vendor Folder — CartoonFX Animations Sub-Package
`Assets/Vendor/CartoonFX/Animations/` appears to be a legacy or alternate installation of CartoonFX that duplicates ~122 files (materials, textures, shaders, models) already present in the top-level CartoonFX subfolders. This is a vendor-internal duplication issue.

---

## Section 8 — Scripts Analysis

### All Active .cs Files

**Under `Assets/Fight or Flight/Code/` (canonical location):**

| Script | Subfolder |
|---|---|
| `LagCamera.cs` | Camera/ |
| `Billboard.cs` | Combat/ |
| `Laser.cs` | Combat/ |
| `ShipLaserProjectile.cs` | Combat/ |
| `GamePausedUISetup.cs` | Editor/ |
| `LegacyHudCleanupTool.cs` | Editor/ |
| `PrefabSetup.cs` | Editor/ |
| `EnemyAI.cs` | Enemy/ |
| `EnemyAttack.cs` | Enemy/ |
| `EnemyHealthBar.cs` | Enemy/ |
| `EnemyMovement.cs` | Enemy/ |
| `Ship.cs` | Ship/ |
| `ShipCollisionEffects.cs` | Ship/ |
| `ShipCombat.cs` | Ship/ |
| `ShipHealth.cs` | Ship/ |
| `ShipInput.cs` | Ship/ |
| `ShipPhysics.cs` | Ship/ |
| `CenterCrosshair.cs` | UI/ |
| `CompassBar.cs` | UI/ |
| `DefeatScreen.cs` | UI/ |
| `HealthUI.cs` | UI/ |
| `HUDController.cs` | UI/ |
| `HUDManager.cs` | UI/ |
| `HudScanlines.cs` | UI/ |
| `LegacyHUDCleanup.cs` | UI/ |
| `MainMenuController.cs` | UI/ |
| `MouseCrosshairUI.cs` | UI/ |
| `PauseManager.cs` | UI/ |
| `PlayerHUD.cs` | UI/ |
| `Radar.cs` | UI/ |
| `RoundedRectSprite.cs` | UI/ |
| `ScoreHUD.cs` | UI/ |
| `ScorePopupFloat.cs` | UI/ |
| `SettingsMenu.cs` | UI/ |
| `SpeedUI.cs` | UI/ |
| `TitlePulse.cs` | UI/ |
| `VictoryScreen.cs` | UI/ |
| `WaveManager.cs` | UI/ |
| `ArenaBoundary.cs` | Utils/ |
| `ControlSchemeManager.cs` | Utils/ |
| `DebrisScatter.cs` | Utils/ |
| `DifficultyManager.cs` | Utils/ |
| `Explosion.cs` | Utils/ |
| `GameModeManager.cs` | Utils/ |
| `GameplayUtils.cs` | Utils/ |
| `ScriptsReference.cs` | **(Code/ root — lone file)** |

**Flagged: Scripts in `Content/Scripts/` instead of `Code/` (MISPLACED):**

| Script | Path |
|---|---|
| `GamePausedUI.cs` | `Assets/Fight or Flight/Content/Scripts/UI/GamePausedUI.cs` |
| `MissionCompleteScreen.cs` | `Assets/Fight or Flight/Content/Scripts/UI/MissionCompleteScreen.cs` |
| `SciFiUIStyle.cs` | `Assets/Fight or Flight/Content/Scripts/UI/SciFiUIStyle.cs` |

These 3 files should be moved to `Assets/Fight or Flight/Code/UI/` to match the project's code organization pattern.

**Lone file at Code/ root:**
- `Assets/Fight or Flight/Code/ScriptsReference.cs` — sits directly at the Code/ root rather than in a named subfolder. This appears to be an intentional reference/scratch file, not a game system.

**Editor scripts in `Code/Editor/`:**
Three editor-utility scripts (`GamePausedUISetup.cs`, `LegacyHudCleanupTool.cs`, `PrefabSetup.cs`) are located in `Code/Editor/`. Unity requires any editor-only script to be in a folder named `Editor` anywhere in the hierarchy — this folder name satisfies that requirement. However, Unity's conventional recommendation is `Assets/Editor/` at the root. These compile correctly in their current location.

**Vendor scripts (not project code — no action needed):**
- `Assets/Vendor/CartoonFX/Animations/Scripts/` — 4 CartoonFX helper scripts
- `Assets/Vendor/RandomAreaSpawner/RandomAreaSpawner.cs`
- `Assets/Vendor/TextMesh Pro/Examples & Extras/Scripts/` — 27 TMP example scripts

---

## Section 9 — _Archive Contents

Location: `Assets/_Archive/` (inside the Unity Assets database, tracked by Unity)

All files listed below are archived and should NOT be modified, moved, or deleted without explicit instruction:

| File | Path |
|---|---|
| `main-menu-ui-imitation.md` | `Assets/_Archive/Plans/main-menu-ui-imitation.md` |
| `Prefab scenes.unity` | `Assets/_Archive/Prefab scenes.unity` |
| `0.unity` | `Assets/_Archive/_Recovery/0.unity` |
| `Asteroid.cs` | `Assets/_Archive/_ScriptsReference/Asteroid.cs` |
| `AsteroidManager.cs` | `Assets/_Archive/_ScriptsReference/AsteroidManager.cs` |
| `EnemySpawner.cs` | `Assets/_Archive/_ScriptsReference/EnemySpawner.cs` |
| `FollowCam.cs` | `Assets/_Archive/_ScriptsReference/FollowCam.cs` |
| `GameEventManager.cs` | `Assets/_Archive/_ScriptsReference/GameEventManager.cs` |
| `GameScore.cs` | `Assets/_Archive/_ScriptsReference/GameScore.cs` |
| `GameTimer.cs` | `Assets/_Archive/_ScriptsReference/GameTimer.cs` |
| `GameUI.cs` | `Assets/_Archive/_ScriptsReference/GameUI.cs` |
| `Pickup.cs` | `Assets/_Archive/_ScriptsReference/Pickup.cs` |
| `PlayGameButton.cs` | `Assets/_Archive/_ScriptsReference/PlayGameButton.cs` |
| `Player.cs` | `Assets/_Archive/_ScriptsReference/Player.cs` |
| `Rotate.cs` | `Assets/_Archive/_ScriptsReference/Rotate.cs` |
| `Shield.cs` | `Assets/_Archive/_ScriptsReference/Shield.cs` |
| `ShieldUI.cs` | `Assets/_Archive/_ScriptsReference/ShieldUI.cs` |
| `Thruster.cs` | `Assets/_Archive/_ScriptsReference/Thruster.cs` |

Total: 18 files in 3 sub-groups (Plans, _Recovery, _ScriptsReference). All have corresponding `.meta` files. No orphans.

---

## Raw File Counts

Counts are for all non-meta files under `Assets/` (744 total):

| Extension | Count | Notes |
|---|---|---|
| `.png` | 249 | UI sprites, textures, CartoonFX textures, AI Toolkit temp images |
| `.mat` | 121 | Materials — large count due to CartoonFX duplication |
| `.cs` | 103 | Scripts — includes Vendor/Archive scripts |
| `.prefab` | 76 | Prefabs |
| `.dae` | 48 | 24 in Vendor/BrokenVector + 24 duplicate copies in project |
| `.unity` | 36 | Scenes (includes TMP examples) |
| `.asset` | 20 | Unity asset files |
| `.shader` | 18 | Shaders (mostly CartoonFX vendor) |
| `.glb` | 10 | 3D models |
| `.jpg` | 10 | Textures |
| `.txt` | 9 | All in Vendor/TextMesh Pro (license/line-breaking files) |
| `.ttf` | 8 | Fonts — 1 misplaced in Content/Models, 7 in Vendor/TextMesh Pro |
| `.psd` | 6 | Photoshop source files (likely in CartoonFX) |
| `.wav` | 6 | Audio SFX |
| `.FBX` | 4 | CartoonFX mesh models |
| `.cginc` | 4 | Shader include files |
| `.shadergraph` | 4 | Shader graph files |
| `.tga` | 2 | Textures (CartoonFX) |
| `.anim` | 2 | Animation clips |
| `.controller` | 2 | Animator controllers |
| `.md` | 1 | `_Archive/Plans/main-menu-ui-imitation.md` |
| `.hlsl` | 1 | Shader file |
| `.mp3` | 1 | `Content/Audio/SFX/alarm-incoming.mp3` |
| `.DS_Store` | 1 | macOS metadata (should be deleted) |
| `.exr` | 1 | Reflection probe bake |
| `.lighting` | 1 | Lighting settings |
| **Total** | **744** | |

### By Top-Level Folder (non-meta files)
| Folder | Count |
|---|---|
| `Assets/Fight or Flight/` | 238 |
| `Assets/Vendor/` | 477 |
| `Assets/_Archive/` | 18 |
| `Assets/AI Toolkit/` | 11 |
| `Assets/Sprites/` | 0 |
| **Total** | **744** |
