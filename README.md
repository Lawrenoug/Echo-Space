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

- hold guard to defend
- press guard at the right timing to deflect
- attacks build posture
- posture break is becoming the main combat payoff
- HP remains as a supporting system instead of the only win condition

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
  - guard input
  - deflect timing window
  - player HP
  - player posture bar
- Enemy combat prototype
  - patrol movement
  - wall turnaround
  - attack windup
  - active attack hitbox
  - cooldown
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
- `J` / `K`: attack
- `L` / `Shift`: guard / deflect
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

- The current enemy posture prototype is intentionally simple.
- For now, if enemy posture fills up, the enemy is defeated directly.
- This is a placeholder step toward a future posture-break / deathblow flow.
- The current guard-break behavior is also a prototype and will likely be refined later.

## Recommended Next Steps

1. Add a visible enemy posture bar or lock-on target panel.
2. Split enemy attack into clearer anticipation / swing / recovery visuals.
3. Add player guard-break feedback:
   - stun
   - sound
   - stronger visual cue
4. Add a true posture-break follow-up state instead of immediate enemy death.
5. Start replacing placeholder geometry with whitebox combat rooms.

## Run

```powershell
dotnet build EchoSpace.csproj
```

Then open the project with Godot 4.6.2 Mono and run:

- `res://Scenes/Main.tscn`
