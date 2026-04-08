# Echo Space

Echo Space is a Godot 4.6.2 + C# 2D action prototype built around real-time world switching:

- Reality world
- Soul world

The current goal is not content completion. The goal is to build a clean gameplay foundation that can support:

- platforming
- dual-world puzzles
- Sekiro-inspired melee combat

## Current Focus

The combat direction is now shifting from simple touch damage to a posture-driven melee loop inspired by Sekiro:

- right mouse to defend
- right mouse at the right timing to deflect
- attacks build posture
- enemy posture break creates an execution window
- left mouse is the current execution input after a break

This repository currently contains a playable prototype layer for that direction.

## Implemented Systems

- Player movement
  - run
  - jump
  - fall
  - buffered jump / attack
  - coyote time
- Player combat prototype
  - attack hitbox
  - mouse-based attack / guard
  - deflect timing window
  - visible player HP bar
  - visible player stamina bar
- Enemy combat prototype
  - patrol movement
  - wall turnaround
  - attack windup
  - active attack phase
  - cooldown
  - visible HP bar
  - visible posture bar
  - breakable posture state
  - execution vulnerability after posture break
  - posture gain on hit / deflect
  - hit flash and death fade
- Dual-world gameplay foundation
  - `WorldManager`
  - `DualWorldObject`
  - world-exclusive platforms
  - dual-world breakable wall
- UI prototype
  - world indicator
  - player HP display
  - player posture display
- Pooling foundation
  - `PoolManager`
  - `NodePool`

## Controls

- `A` / `Left`: move left
- `D` / `Right`: move right
- `Space` / `W` / `Up`: jump
- `Left Mouse`: attack / execute
- `Right Mouse`: guard / deflect
- `Tab`: switch world

## Key Files

- Main scene: `Scenes/Main.tscn`
- Player controller: `Scripts/Player/PlayerController.cs`
- Player states: `Scripts/Player/States/`
- Enemy prototype: `Scripts/Gameplay/Enemies/EnemyController.cs`
- Dual-world core: `Scripts/Core/World/`
- HUD: `Scripts/UI/WorldOverlay.cs`
- Breakable wall: `Scripts/Gameplay/Environment/BreakableWall.cs`

## Manual Content Still Needed

These are still expected to be filled in manually later:

- final player animation set
- enemy art and animation
- hit FX
- deflect FX
- world-switch FX
- audio and BGM
- proper level art
- tutorial prompts
- final HUD art

## Current Prototype Notes

- The player currently has both HP and stamina UI.
- Enemy UI currently shows both HP and posture.
- When enemy posture fills up, the enemy enters a temporary broken state.
- Left click attacks reduce enemy HP and also chip some posture.
- Deflecting is meant to build enemy posture faster than normal attacks.
- Attacking a broken enemy currently executes it immediately.
- The current guard-break behavior is also a prototype and will likely be refined later.

## Recommended Next Steps

1. Split enemy attack into clearer anticipation / swing / recovery visuals.
2. Add player guard-break feedback:
   - stun
   - sound
   - stronger visual cue
3. Add enemy attack telegraph VFX and SFX.
4. Start replacing placeholder geometry with whitebox combat rooms.
5. Add a reusable enemy base scene that already contains:
   - hurtbox
   - attack box
   - posture bar
   - break / execute hooks

## Run

```powershell
dotnet build EchoSpace.csproj
```

Then open the project with Godot 4.6.2 Mono and run:

- `res://Scenes/Main.tscn`
