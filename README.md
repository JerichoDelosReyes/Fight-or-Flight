# Fight or Flight

![Fight or Flight](Assets/Fight%20or%20Flight/Content/Textures/MainMenuBackground.png)

A 3D space-flight action game. The player pilots a ship through an asteroid field, battles enemy waves, and pushes for a high score across two modes: Campaign and Survival.

## Quick Facts

- **Genre:** Third-person space flight shooter
- **Unity version:** `6000.4.6f1`
- **Core scenes:**
  - `Assets/Fight or Flight/Content/Scenes/MainMenu/MainMenu.unity`
  - `Assets/Fight or Flight/Content/Scenes/MainScene/MainScene.unity`

## Getting Started

1. Open the project in Unity `6000.4.6f1`.
2. Open `Assets/Fight or Flight/Content/Scenes/MainMenu/MainMenu.unity`.
3. Press Play.

## Controls

| Action | Mouse + Keyboard | Keyboard Only |
|--------|-----------------|---------------|
| Thrust / Brake | `W / S` | `Left Shift` |
| Strafe | `A / D` | — |
| Pitch | Mouse Y | `W / S` |
| Yaw | Mouse X | `A / D` |
| Roll | `Q / E` | `Q / E` |
| Boost | `Left Shift` | — |
| Fire | `Space` or `LMB` | `Space` or `LMB` |

Control scheme is switchable in the in-game Settings menu.

## Project Layout

```
Assets/Fight or Flight/
├── Code/
│   ├── Camera/       — LagCamera follow system
│   ├── Combat/       — Laser projectile, Billboard
│   ├── Editor/       — Editor-only setup tools (GamePausedUISetup, PrefabSetup, LegacyHudCleanupTool)
│   ├── Enemy/        — EnemyAI, EnemyMovement, EnemyAttack, EnemyHealthBar
│   ├── Ship/         — Ship coordinator, ShipInput, ShipPhysics, ShipCombat, ShipHealth
│   ├── UI/           — HUD, menus, radar, wave display, settings, pause, defeat/victory screens
│   └── Utils/        — ArenaBoundary, DebrisScatter, GameModeManager, DifficultyManager, Explosion
│
└── Content/
    ├── Audio/SFX/    — Sound effects (.wav, .mp3)
    ├── Fonts/        — Inter variable font
    ├── Materials/    — Laser materials
    ├── Models/       — Ship and environment GLB models
    │   └── Enemies/  — Enemy GLB models (boss, vulcan_dkyr_class, enemy)
    ├── Prefabs/
    │   ├── Enemies/  — Enemy and player ship prefabs
    │   ├── Environment/ — Asteroid and rock prefabs
    │   ├── UI/       — UI prefabs (CloseBtn, ButtonBG_Refined, PlayerHUD, etc.)
    │   └── VFX/      — Explosion and laser VFX prefabs
    ├── Resources/    — Runtime-loaded assets (SciFiUI sprites, UI overlays)
    ├── Scenes/       — MainMenu and MainScene scene files
    ├── Sprites/      — UI sprite sheets and extracted assets
    ├── Textures/
    │   └── UI/       — HUD icon textures (health, shield, heat)
    └── UI/           — UI background and icon images

Assets/Vendor/        — Third-party packages (CartoonFX, BrokenVector, TextMesh Pro, RandomAreaSpawner)
Assets/_Archive/      — Legacy prototype scripts and scenes (read-only reference)
_quarantine/          — Files moved here during cleanup (not deleted — restorable)
```

## Core Systems

| System | Script |
|--------|--------|
| Ship coordinator | `Code/Ship/Ship.cs` |
| Input | `Code/Ship/ShipInput.cs` |
| Physics & boundaries | `Code/Ship/ShipPhysics.cs` |
| Weapons & heat | `Code/Ship/ShipCombat.cs` |
| Damage & death | `Code/Ship/ShipHealth.cs` |
| Enemy AI | `Code/Enemy/EnemyAI.cs` |
| Enemy movement | `Code/Enemy/EnemyMovement.cs` |
| Laser projectile | `Code/Combat/ShipLaserProjectile.cs` |
| Wave spawning | `Code/UI/WaveManager.cs` |
| HUD | `Code/UI/PlayerHUD.cs`, `HUDManager.cs` |
| Radar | `Code/UI/Radar.cs` |
| Settings menu | `Code/UI/SettingsMenu.cs` |
| Pause menu | `Code/UI/GamePausedUI.cs` |
| Camera | `Code/Camera/LagCamera.cs` |
| Arena boundary | `Code/Utils/ArenaBoundary.cs` |
| Game mode | `Code/Utils/GameModeManager.cs` |

## Game Modes

- **Campaign** — Survive 5 waves of enemies. Clearing all waves unlocks Survival Mode.
- **Survival** — Endless enemy waves. Locked until Campaign is completed.

Save data (scores, unlocks) is stored in `PlayerPrefs`. Can be reset from the Settings menu.

## Dependencies

- `com.unity.inputsystem` `1.19.0`
- `com.unity.ugui` `2.0.0`
- `com.unity.ai.assistant` `2.8.0-pre.1`
- `com.unity.ai.inference` `2.6.1`

## Attribution

`RandomAreaSpawner`, portions of `ShipPhysics`, and `LagCamera` include MIT-licensed code by Brian Hernandez. See script headers for details.
