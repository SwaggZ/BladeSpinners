# Blade Spinners

A Beyblade-inspired roguelike built in Unity (URP). Build custom spinning tops from procedural parts, battle AI opponents in generated arenas using momentum-based physics combat. Collisions exchange spin — last bey spinning wins.

## Features

### Combat & Physics
- **Momentum-based movement** — Ice-skating style physics where velocity persists and redirects gradually, just like a real spinning top
- **Spin exchange combat** — Collisions transfer spin based on speed, weight, and attack stats; beys burst when spin hits zero
- **Knockback with hitstun** — Hits send beys flying with a brief stun window so impacts feel real
- **Weight-based inertia** — Heavier beys are harder to push but slower to accelerate
- **Live stat rings** — Player-only world-space rings around the Bey show Speed/Acceleration (magenta), Mana (cyan), and Spin/Stamina (yellow)
- **Front-most stat labels** — Ring labels and black toon text outlines render in front of curve layers for readability

### Bey Assembly
- **5 part types** — Tip, Track, Fusion Wheel, Energy Ring, Face Bolt
- **Procedural generation** — Each part has unique stats, colors, and meshes generated at runtime
- **Tip behaviors** — Different tips change movement physics (aggressive, defensive, orbital patterns)
- **Stat-driven gameplay** — Parts determine speed, weight, attack, defense, stamina, and special abilities
- **Face Bolt emblems** — Each Face Bolt can have a unique emblem sprite rendered directly on the Face Bolt in-game (also planned for special-attack hologram visuals)

### Content Pipeline
- **Single-set generator** — `Blade Spinners → Generate Part Set`
- **Mass-set generator** — `Blade Spinners → Generate Massive Part Sets` for creating large batches quickly
- **JSON mass-set generator** — `Blade Spinners → Generate Massive Part Sets (JSON)` with reusable load/save JSON datasets
- **Duplicate-safe JSON generation** — existing set names are automatically skipped (no overwrite) during JSON batch runs
- **Starter JSON template** — `Assets/Settings/part_set_batch_template.json` for immediate load/edit/regenerate workflow
- **Deterministic seeds** — Rebuild identical sets from the same naming/seed inputs
- **Custom set entry list** — Resize a list and configure each set's Name, Seed, Rarity, Emblem, and Color
- **One-click seed randomization** — Randomize every list entry seed with `Randomize All Seeds`

### Camera
- **Free orbit mode** — GTA-style camera with mouse/gamepad orbit
- **Xenoverse-style lock-on** — Middle-click locks camera behind player facing the target enemy
- **Auto-target switching** — When a locked enemy bursts, camera instantly switches to the next closest enemy
- **Scroll cycling** — Scroll wheel to cycle between enemy targets
- **Focused enemy arrow** — Small arrow appears above the currently focused enemy and hides when no enemy is focused
- **Speed wedges** — High-speed motion uses animated off-screen triangle streaks instead of flat line strips
- **Occluder fade** — Walls and arena pieces between the camera and player fade to a configurable partial opacity instead of fully disappearing

### AI Opponents
- **State machine AI** — Chase, Attack, and Reposition states with configurable ranges
- **7-ray obstacle avoidance** — Enemies detect and dodge walls/arena edges
- **Boost & ability usage** — AI activates boost during attacks and uses abilities when in range
- **Tunable difficulty** — 13 enemy-specific multipliers stack on global balance sliders
- **Test scene uses existing parts** — Enemy setup pulls from existing part assets instead of auto-generating new sets

### Match System
- **Match lifecycle** — Countdown → In Progress → Win/Loss with auto-restart
- **Burst effects** — Dead beys stop, parts detach and fall to the ground, fading out over 7 seconds
- **Enemy part drops** — On enemy burst: roll for any drop, roll one equipped part (equal chance), then rarity gate roll (higher rarity drops less)
- **Dropped-part visuals** — Drop pickups now render as the actual procedural mesh of the dropped part
- **Larger pickup radius + clear reward** — Part pickups use a larger trigger radius and are auto-collected when all enemies are destroyed
- **No forced center teleport on win** — Auto-restart on player win is disabled by default (can be toggled in `MatchManager`)
- **Spin holds on enemy clear** — When all enemies are destroyed, player spin drain pauses during victory state
- **Run progression** — Runs now span multiple levels and arenas instead of a single placeholder combat room
- **Depth scaling** — Deeper arenas increase enemy count and bias enemy loadouts toward higher-rarity parts
- **Duplicate drop guard** — If a dropped part already exists in run inventory, it is skipped instead of duplicated
- **Auto-discovery** — All beys are found at runtime — no manual wiring required
- **Balance sliders** — 26 GameManager sliders (13 global + 13 enemy) adjustable at runtime in the Inspector

### Menus & Inventory (MVP)
- **Runtime main menu overlay** — Start Run, Inventory, Settings, Keybinds
- **Runtime pause menu** — Resume, Run Inventory, Settings, Keybinds, Return to Main Menu
- **Between-arena intermission** — Arena clears open a build-management menu before the next arena starts
- **Run inventory progression** — Run starts with selected main-menu loadout; dropped parts are added and can be equipped during run
- **Live spinning preview window** — Inventory shows a spinning Bey preview and live stats while parts are changed
- **Preview drag pitch** — Menu previews rotate vertically only and reset cleanly when changing panels
- **Runtime settings sliders** — Settings panel includes themed sliders for volume, sensitivity, and clipping opacity
- **Rings UI opacity** — Settings includes a 0-100% slider that fades both stat rings and ring text
- **Defeat salvage flow** — On run loss, players choose a limited number of eligible run parts to keep (cap scales with run depth and rarity gate)
- **Run completion transfer** — On full run completion, all run-inventory parts transfer to main inventory
- **Structured test run flow** — Start Run currently creates a 3-level, 3-arena-per-level progression loop with carry-over inventory and build changes between arenas
- **Resolution-aware scaling** — UI scales with screen resolution using a 1080p baseline (works at FHD, 1440p, 4K)

### Build Safety
- **ShaderProvider** — Centralized build-safe shader loading; reference materials in `Resources/` guarantee URP Lit and Unlit shaders are included in builds
- **IL2CPP stripping protection** — `link.xml` preserves all Assembly-CSharp code for reflection-heavy runtime wiring
- **Graceful error handling** — Bootstrap, initialization, and UI rendering wrapped in try-catch with on-screen error overlay in builds
- **Scene camera guarantee** — Main Camera baked into SampleScene + runtime fallback creation
- **Build-safe part catalog** — `Resources/StarterPartsConfig.asset` stores a runtime catalog reference list so all BeyPart assets are included in builds

### Arena
- **Procedural generation** — Arenas built at runtime with configurable geometry
- **Ground layer physics** — Proper layer separation for grounding, triggers, and collision detection

## Project Structure

```
Assets/
  Resources/        — URPLitReference.mat, URPUnlitReference.mat (build-safe shader refs)
  Scripts/
    Core/           — GameManager, GameConstants, singleton infrastructure
    Gameplay/
      Movement/     — BeyMovementController, BeyTiltController, tip behaviors
      Combat/       — BeyCollisionDetector, SpinExchangeHandler
      Parts/        — BeyConfiguration, BeyAssembler, BeyStatBlock, procedural meshes
      Effects/      — BeyBurstEffect, part fade/despawn
      UI/           — RuntimeGameUiController, RuntimeRunBuilder, StarterPartsConfig, RuntimePartFactory
      (root)        — MatchManager, EnemyBeyController, AIInputHandler, ShaderProvider,
                      PlayerInputHandler, PlayerManager, ThirdPersonCameraController
    Editor/         — TestSceneSetup, RuntimeMenuHierarchyGenerator
  link.xml          — IL2CPP stripping protection for reflection targets
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

## Additional Docs

- See `PART_SET_GENERATION_GUIDE.md` for single-set and massive-batch part generation workflows.

## Current Runtime Loop

1. Start a run from the main menu with your selected loadout.
2. Fight through a depth-based sequence of arenas grouped into levels.
3. Collect dropped parts and re-equip from run inventory during pause or intermission menus.
4. After each arena clear, use the between-arena menu to adjust your build before continuing.
5. Progress deeper into the run as enemy counts and rarity quality increase.

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
