# Blade Spinners - Development Checklist

## Core Foundation (Must come first)
- [x] Project structure & namespaces (Scripts/Core, Scripts/Gameplay, Scripts/UI, etc.)
- [x] Enums & Constants (PartType, TipBehaviorType, RarityTier, RoomType, etc.)
- [x] BeyPart ScriptableObject base class
- [x] BeyConfiguration runtime class with stat calculation

## Movement & Physics (Core gameplay)
- [x] ITipBehavior interface
- [x] Concrete Tip behaviors (FlatTip, SharpTip, RoundTip, RubberFlat, BallTip, SpikeTip, OrbitTip)
- [x] Spin system (drain rate, burst on zero)
- [x] BeyMovementController & steering/boost/brake logic
- [x] BeyTiltController (visual lean + wobble at low spin)
- [x] Physics collision detection & SpinExchangeHandler
- [x] Anti-stuck tuning: zero-friction Bey physics materials (dynamic/static friction = 0, combine = Minimum)

## Player & Camera
- [x] Player input handler (movement, boost, brake, jump, ability input)
- [x] Third-person camera orbiting Bey
- [x] Focus indicator arrow above currently focused enemy (hidden when no focus)
- [x] Main player Bey prefab coordination (PlayerManager - ready for GameObject setup)
- [x] Camera speed-line effect updated to animated triangle/wedge streaks with off-screen bases
- [x] Camera occluders between player and camera fade to configurable partial opacity and restore correctly

## Inventory & Part Management
- [x] PartInventory (run-temporary parts)
- [x] PartDatabase (permanent parts registry)
- [x] Part equipping/swapping logic (integrated into BeyConfiguration)
- [x] Duplicate run-drop guard prevents adding the same dropped part twice to run inventory

## World Generation
- [ ] Map chunk prefabs (flat, ramps, platforms, bowls, bridges, hazards)
- [x] Procedural arena platforms use cylindrical `MeshCollider` shape (not box/capsule)
- [x] Arena shape library — 10 named bowl variants (ClassicRound, TripleBattle, StarStorm, BoltBlast, NotchRing, Pentagon, Square, Triangle, MaxStampede, TwinBasin) with distinct footprints, depths, and lobe/gear counts; random selection per seed
- [x] Hole-aware bey spawn — arenas with center hole use `HoleRadiusRatio`; all beys are offset to a safe outer ring on start to avoid the pit
- [ ] DungeonLayoutGenerator (grid-based layout, room connection)
- [ ] MapChunkAssembler (build individual maps from chunks)
- [ ] Room types spawned (Combat, Loot, Workshop, Boss, Secret, Start, Exit)

## Static Pickups
- [ ] PickupController (Spin, Stamina variants)
- [ ] PickupSpawner & placement logic
- [ ] Pickup collection mechanics

## Procedural Generation
- [ ] PartTagSystem
- [ ] ProceduralPartGenerator (all slot types, depth scaling, rarity)
- [ ] Connect to part dropping & loot rooms

## Editor Tooling
- [x] Single-set part generator (`Generate Part Set`)
- [x] Massive batch set generator (`Generate Massive Part Sets`)
- [x] JSON-driven massive batch set generator (`Generate Massive Part Sets (JSON)` with load/save JSON datasets)
- [x] JSON batch duplicate protection (skip existing set names instead of overwriting)
- [x] Mass generator list-wide seed randomize button
- [x] Face Bolt emblem sprite field on part data (for UI/future hologram use)
- [x] Face Bolt ability report window supports one-click `Bake Unique Assignments` (writes explicit ability refs to Face Bolt assets and auto-generates unique variants when required)
- [x] Bowl prototype gallery (`BowlPrototypeGalleryGenerator`) — attach to a GameObject and trigger `GeneratePrototypes` from context menu to preview all arena shapes side-by-side

## Test Scene Setup
- [x] Enemy test Beys use existing part pool (no automatic per-enemy set generation)

## Enemies & Combat
- [ ] EnemyBeyAI state machine (Aggression, Reposition, StaminaConservation)
- [ ] EnemySpawner using dungeon theme & depth
- [x] Part drop system on enemy burst (3-roll logic: any-drop roll → equal part selection roll → rarity gate roll)
- [x] Part drops use part-shaped visual meshes with increased pickup radius and auto-collect when all enemies are destroyed
- [x] Player win no longer forces immediate auto-restart/teleport to center by default
- [x] Player spin drain pauses after all enemies are destroyed (victory state)
- [ ] Boss enemy system with named configs

## Abilities & Combat Enhancement
- [x] BeyAbility base & concrete implementations (Dash, Shield, Spin Drain, Ground Pound, Flash Step, Dragon Burst, Poison Cloud, Gravity Clash)
- [x] Abilities are attached to Face Bolt parts; different Face Bolts resolve to different abilities by default
- [x] BeyPassive base & implementations (SpinRecovery, LowSpinSurge, ImpactShield, MomentumHarvest)
- [x] ThresholdBehaviorModifier for spin-triggered changes
- [ ] Integrate abilities into ProceduralPartGenerator

## UI & Menus
- [ ] In-run HUD (spin bar, mana bar, ability icon, minimap)
- [x] Player-only world-space stat rings (speed/acceleration, mana, spin/stamina) — centered toon black outlines (inside/outside balanced) with black-capped fill ends, dark base arcs + vivid same-hue fill arcs, smaller labels fitting between rings with thicker 8-direction toon black text outline, wider front gap facing away from camera, speed normalized on 0→max-with-boost curve, horizontal lock, raised offset, double-sided transparent rendering
- [x] Stat ring labels + black text outlines render in front of curve layers
- [x] Main menu runtime GUI (Start Run, Inventory, Settings, Keybinds)
- [x] Main menu runtime GUI visual refresh (flat cel-shaded panels, black toon outlines, responsive layout scaling)
- [x] Runtime menu GUI concept refresh adds neon arena backdrop, gradient headers, and glass-panel chrome inspired by current visual references
- [x] Runtime menu art pass now favors high-energy speedline motifs and solid arcade accents over muddy gradients, with larger readable action buttons
- [x] Menu readability polish: left loadout text now sits on stronger dark backing, hot-side red striping reduced, and `START RUN` stripe effects are clipped strictly inside button bounds
- [x] Pause menu runtime GUI (Resume, Run Inventory, Settings, Keybinds, Return Main Menu)
- [x] Between-arena intermission menu for run inventory/build changes before advancing
- [x] Pause menu Esc toggle migrated to Input System API (fixes legacy Input.GetKeyDown exception)
- [x] Inventory/part swapping runtime MVP (main-menu loadout seeding + run-time equip from run inventory)
- [x] Garage redesign with top navigation, centered Bey stage, orbiting part nodes, and bottom action bar
- [x] Garage slot-swap modal for quick per-slot part replacement directly around the preview Bey
- [x] Runtime-only 3-slot build save/load flow added to the garage action bar (non-persistent until SaveManager exists)
- [x] Garage auto-optimize now equips the highest-rated owned part per slot using runtime UI scoring
- [x] Inventory and salvage selection show selected part stats plus ability details
- [x] Starter unlock flow now supports configurable base parts via `StarterPartsConfig` (`Resources/StarterPartsConfig.asset`)
- [x] Spinning Bey preview + live stats window in inventory GUI
- [x] Bey preview render texture uses transparent camera clear so menu UI shows behind the model
- [x] Garage preview stage keeps the UI backdrop visible behind the Bey by avoiding an opaque preview plate
- [x] Per-part 3D rendered previews in garage orbit slots, Current Build panel, and swap modal (FaceBolt retains emblem sprite everywhere)
- [x] Preview drag is vertical-only and resets when moving between menu states
- [x] Preview/Run stat panel now includes live Spin and Mana current values
- [x] Settings panel uses themed sliders and exposes clipping opacity control
- [x] Keybind reference folded into Settings while pause/intermission menus were rethemed to match the new garage shell
- [x] Settings panel exposes stat-rings UI opacity (0-100%) affecting both ring lines and ring text
- [x] Death screen layout scales more responsively and fits killer-build content more aggressively
- [x] Run-loss salvage UI allows choosing limited eligible parts to transfer to main inventory (depth-scaled count + rarity cap)
- [ ] Achievement screen
- [ ] Room transition UI

## Run Flow (Current Phase)
- [x] Start Run builds a structured run progression instead of a single placeholder arena
- [x] Runs currently use multi-level, multi-arena progression with deterministic arena seeds
- [x] Enemy count and enemy-part rarity scale with run depth
- [x] Arena clear opens an intermission menu instead of forcing an immediate next-arena transition
- [x] Full run completion transfers all run-inventory parts to main inventory
- [x] Mid-run loss supports limited salvage transfer selection instead of all-or-nothing loss

## Build & Runtime Safety
- [x] `ShaderProvider` utility centralizes build-safe shader loading via `Resources/` reference materials (`URPLitReference.mat`, `URPUnlitReference.mat`)
- [x] All runtime `Shader.Find()` calls replaced with `ShaderProvider.URPLit` / `ShaderProvider.URPUnlit` (BeyAssembler, ProceduralArenaGenerator, MatchManager, BeyStatRingsUI, BeyBurstEffect)
- [x] `link.xml` prevents IL2CPP managed code stripping of reflection targets
- [x] Scene includes a Main Camera tagged `MainCamera` (fallback camera also created at runtime if missing)
- [x] `RuntimeGameUiController` bootstrap wrapped in sectioned try-catch (camera, starter data, preview each fail independently)
- [x] `OnGUI` wrapped in try-catch with on-screen error overlay in builds
- [x] `StartRun()` catch block shows visible error text instead of silently returning to menu
- [x] Resolution-aware UI scaling (`GetUiScale()`) based on 1080p baseline, clamped 0.85–2.25×
- [x] `StarterPartsConfig` includes build-safe runtime part catalog references so non-base part sets are available in builds

## Visual Part Identity
- [x] Face Bolt emblem visible on Face Bolt mesh in-game
- [x] Face Bolt widened; Energy Ring center hole constrained to stay slightly larger than Face Bolt width (close-fit clearance)
- [x] Face Bolt vertical anchor aligned to Energy Ring connection location
- [x] Ability-activation hologram using Face Bolt emblem

## Persistence
- [ ] SaveManager & JSON serialization
- [ ] Persistent save (unlocked parts, achievements, base loadout)
- [ ] Run save (current dungeon state, inventory, spin/mana)

## Polish & Audio (Last)
- [x] Temporary placeholder hit particle spawns on Bey-to-Bey collision (for later replacement)
- [ ] Particle effects (pickups, abilities, impacts)
- [ ] Sound effects & SFX manager
- [ ] Visual feedback & juice

---

## Parallel work (can happen anytime)
- 3D asset creation (Bey model, parts, enemies, chunks, UI icons)
- Ability design & balance spreadsheet
- Part rarity curves & stat ranges per depth

---

**Last Updated:** March 21, 2026
