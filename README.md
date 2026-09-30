# Spatial Rift Lab

Spatial Rift Lab is a desktop-playable Unity 6.5 portfolio game designed around the problems of adaptive mixed reality: mapping physical spaces, identifying usable surfaces and anchors, procedurally fitting content to different rooms, keeping the player inside a safe area, and turning the resulting layout into gameplay.

It is an honest simulation, not a commercial room-scanning SDK. Real scene data can replace `SpatialLayoutManager` through a Meta Scene API, PICO spatial-mapping, Apple RoomPlan/visionOS, or another room-mesh provider.

## What is new in version 3

- Three simulated scans: Compact Studio, Wide Lab and L-Shape Loft
- Scan, constraint-solving and procedural-build presentation flow
- Layout-dependent walls, props, safe areas, haptic anchors and no-go volumes
- Constraint-aware player, enemy and spawn placement
- Imported CC0 Quaternius sci-fi architecture, props and alien models
- XR device bridge for HMD pose/device detection when an XR loader is present
- Four combat waves, multiple enemies, boss, pickups, scoring and game states
- Authored industrial-deck art direction with textured surfaces, practical lighting and semantic colour roles
- Oxanium display typography paired with a highly legible HUD body face
- Resolution-aware UI validated at a small 856×480 window and fullscreen
- Enemy arrival telegraphs, hit flash/scale response, player engine trail and Guardian health display
- Subtle synthesized room tone and event-specific sound feedback
- Lit metallic and emissive gameplay materials with improved shape definition
- Structural framing, balanced key/fill lighting and a restrained desktop presentation grade
- XR-aware rendering path that skips the full-screen grade when a headset is active
- Automated layout, gameplay, screenshot and clean-exit QA
- Direct3D 11 Windows target for reliable portfolio-machine compatibility

No generated image, texture, 3D model, voice or music asset is shipped with the project. Third-party visual assets and typography are human-designed, openly licensed and documented in `ATTRIBUTION.md`.

## Run the finished game

See `RUN_ME_FIRST.md` for exact beginner-friendly instructions.

## Open the project

1. Install Unity Hub.
2. Install Unity **6000.5.3f1** with Windows Build Support.
3. In Unity Hub choose **Add → Add project from disk**.
4. Select this `NeonRiftArena` folder.
5. Open `Assets/NeonRift/Scenes/NeonRiftArena.unity`.
6. Press the Play triangle at the top of Unity.

## Important source files

- `SpatialAdaptation.cs` — scan simulation, constraints, procedural layout and XR device bridge
- `NeonRiftGame.cs` — game state, world setup and presentation HUD
- `PlayerPilot.cs` — movement, aim, dash, health and shooting
- `CombatActors.cs` — projectiles, enemy types, imported visuals and pickups
- `ArenaDirector.cs` — adaptive spawning and wave orchestration
- `NeonFX.cs` — particles, camera shake and generated sound
- `NeonRiftBuilder.cs` — reproducible Windows build automation
- `VISUAL_DIRECTION.md` — palette, hierarchy, polish decisions and visual QA checklist

## Build it yourself

In Unity choose **Neon Rift → Build Interview Demo**. The executable appears in `Build/Windows` (generated locally, not committed). A ready-made copy is in `Builds/NeonRiftArena-Windows.zip`.
