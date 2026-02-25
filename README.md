# Blade Spinners

A Beyblade-inspired roguelike built in Unity (URP). Build custom spinning tops from procedural parts, battle AI opponents in generated arenas using momentum-based physics combat. Collisions exchange spin — last bey spinning wins.

## Features

### Combat & Physics
- **Momentum-based movement** — Ice-skating style physics where velocity persists and redirects gradually, just like a real spinning top
- **Spin exchange combat** — Collisions transfer spin based on speed, weight, and attack stats; beys burst when spin hits zero
- **Knockback with hitstun** — Hits send beys flying with a brief stun window so impacts feel real
- **Weight-based inertia** — Heavier beys are harder to push but slower to accelerate

### Bey Assembly
- **5 part types** — Tip, Track, Fusion Wheel, Energy Ring, Face Bolt
- **Procedural generation** — Each part has unique stats, colors, and meshes generated at runtime
- **Tip behaviors** — Different tips change movement physics (aggressive, defensive, orbital patterns)
- **Stat-driven gameplay** — Parts determine speed, weight, attack, defense, stamina, and special abilities

### Camera
- **Free orbit mode** — GTA-style camera with mouse/gamepad orbit
- **Xenoverse-style lock-on** — Middle-click locks camera behind player facing the target enemy
- **Auto-target switching** — When a locked enemy bursts, camera instantly switches to the next closest enemy
- **Scroll cycling** — Scroll wheel to cycle between enemy targets

### AI Opponents
- **State machine AI** — Chase, Attack, and Reposition states with configurable ranges
- **7-ray obstacle avoidance** — Enemies detect and dodge walls/arena edges
- **Boost & ability usage** — AI activates boost during attacks and uses abilities when in range
- **Tunable difficulty** — 13 enemy-specific multipliers stack on global balance sliders

### Match System
- **Match lifecycle** — Countdown → In Progress → Win/Loss with auto-restart
- **Burst effects** — Dead beys stop, parts detach and fall to the ground, fading out over 7 seconds
- **Auto-discovery** — All beys are found at runtime — no manual wiring required
- **Balance sliders** — 26 GameManager sliders (13 global + 13 enemy) adjustable at runtime in the Inspector

### Arena
- **Procedural generation** — Arenas built at runtime with configurable geometry
- **Ground layer physics** — Proper layer separation for grounding, triggers, and collision detection

## Project Structure

```
Assets/
  Scripts/
    Core/           — GameManager, GameConstants, singleton infrastructure
    Gameplay/
      Movement/     — BeyMovementController, BeyTiltController, tip behaviors
      Combat/       — BeyCollisionDetector, SpinExchangeHandler
      Parts/        — BeyConfiguration, BeyAssembler, BeyStatBlock, procedural meshes
      Effects/      — BeyBurstEffect, part fade/despawn
      (root)        — MatchManager, EnemyBeyController, AIInputHandler, 
                      PlayerInputHandler, PlayerManager, ThirdPersonCameraController
    Editor/         — TestSceneSetup (builds the test scene from scratch)
```

## Tech Stack

- **Unity 6** with Universal Render Pipeline (URP)
- **New Input System** — Mouse, keyboard, and gamepad support
- **Pure C# gameplay** — No visual scripting; all systems are code-driven
- **Procedural meshes** — Parts generated via `ProceduralPartMeshGenerator`
- **Physics layers** — "Bey" layer for part meshes (ignored for bey-vs-bey physics), "Ground" layer for arena

## Getting Started

1. Open the project in Unity 6 (URP)
2. Open `Window → Blade Spinners → Setup Test Scene` to generate a test arena with player + enemies
3. Press Play
4. **WASD** — Move | **Mouse** — Camera | **Shift** — Boost | **Space** — Jump | **E** — Ability
5. **Middle-click** — Lock on to nearest enemy | **Scroll** — Cycle targets | **Middle-click** — Release

## Balance Tuning

Select the **GameManager** object in the Hierarchy during Play mode. All 26 balance sliders are exposed in the Inspector:

| Category | Global | Enemy-Specific |
|----------|--------|----------------|
| Movement | Speed, Acceleration, Turn, Jump, Boost | Enemy Speed, Accel, Turn, Jump, Boost |
| Combat | Knockback, Spin Exchange | Enemy Knockback, Spin Exchange |
| Stamina | Spin Drain, Starting Spin | Enemy Spin Drain, Starting Spin |
| Mana | Regen, Pool, Ability Cost | Enemy Regen, Pool, Cost |
| Visual | Visual Spin | Enemy Visual Spin |

Enemy multipliers **stack** on top of global ones (e.g., enemy speed = Speed × Enemy Speed).

## License

All rights reserved.
