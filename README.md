# Fight or Flight

Fight or Flight is a 3D space-flight action game where the player pilots a ship through an asteroid field, fights enemy ships, collects pickups, and pushes for a high score.

This repository currently contains two gameplay stacks:
- A newer gameplay stack under `Assets/Fight or Flight/Code`
- A legacy prototype stack under `Assets/ScriptsReference`

Keep this split in mind when wiring scenes and prefabs.

## Quick Facts
- Genre: third-person space flight shooter
- Core scenes:
	- `Assets/Fight or Flight/Content/Scenes/MainMenu.unity`
	- `Assets/Fight or Flight/Content/Scenes/MainScene.unity`
- Recommended Unity editor: `6000.4.6f1`
- Input package in project: `com.unity.inputsystem` (`1.19.0`)

## Getting Started
1. Open the project in Unity `6000.4.6f1`.
2. Open `Assets/Fight or Flight/Content/Scenes/MainMenu.unity`.
3. Press Play.
4. Start the game from the menu to load `MainScene`.

## Controls
Based on `ShipInput` and `ShipCombat` in the current stack:
- Pitch: `W/S` (inverted: `W` noses down, `S` noses up)
- Yaw: `A/D`
- Roll: `Q/E`
- Throttle: `Left Shift`
- Fire: `Space` or `Fire1` (typically left mouse)
- Camera zoom: `Shift` + `Up/Down Arrow` (or `PageUp/PageDown`)

Legacy scenes may still expect `Fire3` (throttle) and `Roll` axes.

## Project Layout
Main gameplay code:
- `Assets/Fight or Flight/Code/Ship` - player ship systems (input, physics, combat, health)
- `Assets/Fight or Flight/Code/AI` - enemy behavior and health bar logic
- `Assets/Fight or Flight/Code/Combat` - projectiles and combat helpers
- `Assets/Fight or Flight/Code/UI` - HUD, menu, pause/settings, radar, wave UI
- `Assets/Fight or Flight/Code/Camera` - follow/lag camera scripts
- `Assets/Fight or Flight/Code/Utils` - shared gameplay utility components

Content and assets:
- `Assets/Fight or Flight/Content` - scenes, prefabs, audio, models, textures, materials, UI assets
- `Assets/TextMesh Pro` - TMP resources
- `Assets/RandomAreaSpawner` - optional random spawn utility
- `Assets/AI Toolkit` - Unity AI tooling assets

Legacy prototype scripts:
- `Assets/ScriptsReference`

## Current Core Systems
- Ship coordinator: `Assets/Fight or Flight/Code/Ship/Ship.cs`
- Input: `Assets/Fight or Flight/Code/Ship/ShipInput.cs`
- Movement and boundaries: `Assets/Fight or Flight/Code/Ship/ShipPhysics.cs`
- Weapons and heat/overheat: `Assets/Fight or Flight/Code/Ship/ShipCombat.cs`
- Damage and death flow: `Assets/Fight or Flight/Code/Ship/ShipHealth.cs`
- Enemy AI: `Assets/Fight or Flight/Code/AI/EnemyAI.cs`
- Laser projectile: `Assets/Fight or Flight/Code/Combat/ShipLaserProjectile.cs`
- HUD manager: `Assets/Fight or Flight/Code/UI/HUDManager.cs`
- Radar UI: `Assets/Fight or Flight/Code/UI/Radar.cs`
- Camera follow: `Assets/Fight or Flight/Code/Camera/LagCamera.cs`

## Dependencies
From `Packages/manifest.json`:
- `com.unity.inputsystem` (`1.19.0`)
- `com.unity.ugui` (`2.0.0`)
- `com.unity.ai.assistant` (`2.8.0-pre.1`)
- `com.unity.ai.inference` (`2.6.1`)
- Standard Unity modules for physics, audio, particles, UI, and rendering

## Known Risks
Non-exhaustive list of issues to watch for:
- Mixing legacy and current stacks in one scene can cause duplicate UI/input/score behavior.
- Legacy enemy spawning can drift if destroy events are missed.
- Some legacy spawners start immediately in `Start()` before menu/game start state is validated.
- Some UI scripts assume references are assigned and can throw null reference exceptions if not wired.
- Legacy laser scripts can spam the console during rapid fire.

## Attribution
Some utility scripts (for example `RandomAreaSpawner`, `ShipPhysics`, and `LagCamera`) include MIT-licensed code by Brian Hernandez. See script headers for source and license details.
