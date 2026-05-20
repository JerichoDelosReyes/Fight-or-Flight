# Fight or Flight

A 3D space-flight action game where you pilot a ship through an asteroid field, fight enemy ships, collect pickups, and chase a high score. The project contains both legacy prototype scripts and a newer gameplay stack; see the notes below so tooling (like Claude Code) can reason about the correct systems.

## Quick overview
- Genre: third-person space flight shooter
- Player: one ship with thrust, roll, and twin-laser combat
- Objectives: survive, destroy enemies, collect pickups, and maximize score
- Core scenes: `Assets/Fight or Flight/Content/Scenes/MainMenu.unity`, `Assets/Fight or Flight/Content/Scenes/MainScene.unity`
- Unity version: 6000.4.6f1

## Scenes and main menu
- `MainMenu.unity` includes the main menu UI.
- `MainScene.unity` is the gameplay scene.
- Main menu logic is handled by `MainMenuController` (Start -> loads `MainScene`).
- Instructions/Settings buttons are currently placeholders (Debug.Log only).
- There is also a legacy `PlayGameButton` script that triggers `GameEventManager.StartGame()` for the older UI system.

## Controls (default Input Manager)
These are based on the current `ShipInput` and `ShipCombat` scripts:
- Pitch: W/S (inverted, W noses down, S noses up)
- Yaw: A/D
- Roll: Q/E
- Throttle: Left Shift
- Fire: Space or Fire1 (typically left mouse)
- Camera zoom: Hold Shift + Up/Down Arrow (or PageUp/PageDown)

If a legacy scene or input profile is used, some scripts expect `Fire3` (throttle) and `Roll` axes instead.

## Core gameplay systems (current stack)
- Ship root and coordination: `Assets/Fight or Flight/Code/Ship/Ship.cs`
- Input: `Assets/Fight or Flight/Code/Ship/ShipInput.cs`
- Physics and boundaries: `Assets/Fight or Flight/Code/Ship/ShipPhysics.cs`
- Combat + heat/overheat: `Assets/Fight or Flight/Code/Ship/ShipCombat.cs`
- Health, collisions, explosions: `Assets/Fight or Flight/Code/Ship/ShipHealth.cs`
- Enemy AI and firing: `Assets/Fight or Flight/Code/AI/EnemyAI.cs`
- Projectiles: `Assets/Fight or Flight/Code/Combat/ShipLaserProjectile.cs`
- HUD (health/heat/score/speed/throttle): `Assets/Fight or Flight/Code/UI/HUDManager.cs`
- Radar: `Assets/Fight or Flight/Code/UI/Radar.cs`
- Camera: `Assets/Fight or Flight/Code/Camera/LagCamera.cs`
- Utility: `Assets/Fight or Flight/Code/Utils/GameplayUtils.cs` (screen shake, score)

## Legacy prototype systems (still in project)
Older scripts live in `Assets/ScriptsReference` and include:
- `GameEventManager`, `GameUI`, `GameScore`, `GameTimer`
- `AsteroidManager`, `Asteroid`, `Pickup`
- `EnemySpawner`, `EnemyMovement`, `EnemyAttack`
- `Player`, `Laser`, `Shield`, `FollowCam`

These can still compile and may be used by legacy prefabs or scenes, but they are separate from the newer `Assets/Fight or Flight/Code` stack.

## Assets and content
Primary game assets are in `Assets/Fight or Flight/Content`:
- `Animations/` - animation clips and controllers
- `Audio/` - SFX/music
- `GLB/` - 3D models
- `Materials/` - materials and shaders
- `Prefabs/` - ships, enemies, UI, VFX, etc.
- `Sprites/` - UI sprites
- `Textures/` - texture maps
- `UI/` - UI layouts and prefabs
- `Vendor/` - third-party assets or imported packages

Other notable folders:
- `Assets/TextMesh Pro/` - TMP essentials + examples
- `Assets/RandomAreaSpawner/` - optional random spawner utility
- `Assets/AI Toolkit/` - Unity AI tools

## Dependencies
Key packages (see `Packages/manifest.json`):
- Unity Input System (`com.unity.inputsystem`)
- UGUI (`com.unity.ugui`)
- Unity AI assistant/inference
- Standard Unity modules (physics, audio, UI, etc.)

## Potential bugs / risk list (non-exhaustive)
These are issues to watch for based on current code. They are not guaranteed to occur in every scene or prefab.
- Mixed systems: legacy `Assets/ScriptsReference` and the newer `Assets/Fight or Flight/Code` stacks can both be present, which risks double-scoring, duplicate UI, or conflicting input if both are wired in the same scene.
- Enemy count drift: `EnemySpawner` relies on `OnEnemyDestroyed` to decrement counts and only periodically reconciles via tag search; missing calls can stall spawning.
- Spawn-on-menu: `EnemySpawner.Start()` starts spawning immediately even if the game has not been started from the menu.
- Enemy cleanup: `EnemyMovement` subscribes `OnStartGame` -> `SelfDestruct`, which can destroy enemies right when the game starts if that legacy script is used.
- Pickup spawn crash: `AsteroidManager.SpawnPickup()` assumes the asteroid list is non-empty; repeated pickup collection can eventually empty the list and cause an exception.
- Duplicate players: `GameUI.ShowGameUI()` instantiates a new player each `StartGame` without checking for an existing player.
- Crosshair hidden: `Ship.UsingMouseInput` always returns false, so `MouseCrosshairUI` never shows the cursor crosshair even if mouse aiming is desired.
- Kill count inflation: `ScoreManager.AddScore()` increments `Kills` for any score event (including pickups).
- Null references: several UI scripts (`HUDManager`, `HealthUI`, `SpeedUI`, `GameTimer`) assume their UI references are set; missing references will throw NREs.
- Laser log spam: `Laser` logs every raycast hit/miss, which can flood the console during automatic fire.

## How to run
1. Open the project in Unity 6000.4.6f1 or newer.
2. Open `MainMenu.unity` and press Play.
3. Start the game from the menu, which loads `MainScene`.

## Attribution
Some utility scripts (e.g., `RandomAreaSpawner`, `ShipPhysics`, `LagCamera`) include MIT-licensed code by Brian Hernandez. See source headers for details.
