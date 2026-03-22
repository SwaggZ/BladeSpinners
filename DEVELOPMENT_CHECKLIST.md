# Blade Spinners - Development Checklist

> Date baseline: all existing entries in this file are tagged as updated on 22/3/2026.

## Core Foundation (Must come first)
- [x] Project structure & namespaces (Scripts/Core, Scripts/Gameplay, Scripts/UI, etc.)
- [x] Enums & Constants (PartType, TipBehaviorType, RarityTier, RoomType, etc.)
- [x] BeyPart ScriptableObject base class
- [x] BeyConfiguration runtime class with stat calculation

## Movement & Physics (Core gameplay)
- [x] ITipBehavior interface
- [x] Concrete Tip behaviors (FlatTip, SharpTip, RoundTip, RubberFlat, BallTip, SpikeTip, OrbitTip)
- [x] Expanded MFB-style tip catalog (22/3/2026): WD, Q, ES, W2D, MS, EDS, SF, MB, BS, SD, HF, DS, S, FS, B, RS, F, D, R2F, EWD, D:D, CS, B:D with IRL-inspired movement presets
- [x] Procedural tip mesh realism pass (22/3/2026): tip families rebuilt to layered IRL-like silhouettes, including separate flat/rubber-flat wide bases, sharp vs spike profiles, distinct round vs ball rounded contacts, and richer orbit tip geometry
- [x] Tip orientation fix (22/3/2026): tip orientation now uses adaptive profile detection (rotates only when bottom radial profile indicates inversion), keeping non-flat tips from flipping upside down while preserving bottom-at-y=0 stacking
- [x] Tip profile taper refinement (22/3/2026): all generated tip meshes now shrink in diameter toward the lowest point for a more IRL-like downward taper
- [x] Catalog tip silhouette pass (22/3/2026): all MFB-style non-flat tip codes now map to distinct procedural profile families (defense-wide, defense-sharp, flat-sharp, hole-flat, bearing-drive, delta-drive, quake, etc.) instead of collapsing to a generic round/ball mesh
- [x] Curated tip-set pass (22/3/2026): added explicit curated tip variants WF, RB, HF/S, and Fusion(F) with behavior-factory support plus dedicated procedural mappings so the active gameplay tip list aligns with the intended catalog while keeping legacy tip enums compatible
- [x] Spin system (drain rate, burst on zero)
- [x] BeyMovementController & steering/boost/brake logic
- [x] BeyTiltController (visual lean + wobble at low spin)
- [x] Physics collision detection & SpinExchangeHandler
- [x] Anti-stuck tuning: zero-friction Bey physics materials (dynamic/static friction = 0, combine = Minimum)
- [x] Arena zero-friction colliders (22/3/2026): shared PhysicMaterial (zero friction/bounce, Minimum combine) applied to all bowl, lip, wall, platform, ramp, bumper, pillar, and spire colliders
- [x] Slope-aware grounding force (22/3/2026): steep surfaces use surface-normal push instead of straight-down force; prevents wedging into polygon edges
- [x] Edge-catch stuck/bounce recovery (22/3/2026): detects sudden velocity loss from polygon catches and wild vertical bounces; applies recovery nudge after 0.12 s delay
- [x] Physics solver tuning (22/3/2026): solver iterations 6→10, velocity iterations 1→3, contact offset 0.01→0.04, max depenetration velocity 10→20

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
- [x] Face Bolt strict unique-behavior baking (22/3/2026): `Bake Unique Assignments` now generates/updates one per-FaceBolt ability variant with deterministic behavior-parameter tuning and collision checks so no two baked FaceBolts share the same behavior signature
- [x] Bowl prototype gallery (`BowlPrototypeGalleryGenerator`) — attach to a GameObject and trigger `GeneratePrototypes` from context menu to preview all arena shapes side-by-side
- [x] Parts debug world tooling (22/3/2026): menu action creates a no-gameplay inspection scene with free-fly camera and runtime grid spawning of all standalone parts (optional orientation variants + spacing)
- [x] Parts debug scene isolation (22/3/2026): runtime menu/bootstrap and editor auto-hierarchy generation are disabled in `PartsDebugScene` so only the inspection world runs
- [x] Parts debug FaceBolt emblems (22/3/2026): standalone FaceBolts in the debug scene render their emblem sprite over the cap for visual inspection
- [x] Parts debug catalog loading fix (22/3/2026): debug scene now merges all `BeyPart` assets from the project (Editor) instead of only runtime starter catalog entries, so expanded sets (e.g., 150) are fully visible

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
- [x] Runtime resolver uniqueness fallback (22/3/2026): if pooled abilities are exhausted, resolver now instantiates deterministic runtime variants instead of reusing identical fallback behavior
- [x] BeyPassive base & implementations (SpinRecovery, LowSpinSurge, ImpactShield, MomentumHarvest)
- [x] ThresholdBehaviorModifier for spin-triggered changes
- [x] Ability expansion pass: 30 new ability types added (Meteor Strike, Whirlwind, Shadow Strike, Thunder Clap, Ice Shard, Arcane Nova, Void Pulse, Tornado, Phantom Slash, Iron Fortress, Phase Shift, Time Warp, Earthquake, Adrenaline Rush, Regeneration, Overcharge, War Cry, Molten Rain, Magnetic Field, Soul Link, Gravity Well, Nightfall, Crystal Barrage, Inferno, Static Discharge, Black Hole, Razor Wind, Acid Spray, Spectral Chains, Blood Pact) — total pool now 50 distinct ability types
- [x] VFX overhaul for Freeze, Berserk, Tidal Wave (multi-layer primitives: pulsing shells, orbiting crystals, ground rings, rising particles, water droplets/mist)
- [x] VFX added to previously bare abilities: Dash (speed streaks), Shield (golden dome), Spin Drain (contracting rings), Ground Pound (shockwave + dust), Flash Step (afterimage + arrival flash), Dragon Burst (fire cone + scorch), Gravity Clash (vortex + pull lines)
- [x] FaceBoltAbilityResolver expanded: all 150 FaceBolts mapped thematically to 52 ability types; no bey is unmapped
- [x] Ice transparency fix: FreezeAbility and IceShardAbility now use full URP transparency (SrcAlpha/OneMinusSrcAlpha, ZWrite off, transparent queue + keywords) — ice shell ~25% alpha, shards ~30%, sparkles ~35%
- [x] DBZ-style charging aura system (`DBZAuraHelper`): multi-layer aura with inner pulsing core (~35% alpha), outer counter-pulsing shell (~15% alpha), 8 upward-flowing energy streaks (flame-like looping rise), and ground energy ring — creates Dragon Ball Z-style power-up effect
- [x] Buff abilities upgraded to DBZ aura: Berserk (red/orange), Adrenaline Rush (green-yellow), Blood Pact (dark crimson), Overcharge (yellow-electric), Regeneration (green), War Cry (gold) — all use DBZAuraHelper.Spawn while keeping unique overlays (arcs, sparkles, droplets)
- [x] Chrono Recall ability (Ekko-style recast): first cast spawns a ghostly afterimage that replays the bey's movement with a 3-second delay; second cast smoothly warps the bey back to the shadow's position (0.25 s SmoothStep lerp); VFX includes pulsing ghost sphere, orbiting time wisps, spinning ground ring, rewind trail dots, and arrival burst; registered as 51st ability type, mapped to Phantom FaceBolt
- [x] Overall stats recalculation fix: `BeyConfiguration.RecalculateStats()` now reads each slot deterministically (Tip/Track/FusionWheel/EnergyRing/FaceBolt) instead of unordered unique-part overwrite, so stat bars and derived values update correctly when swapping parts
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
- [x] Garage swap modal input isolation (22/3/2026): while modal is open, clicks/drags inside modal bounds no longer trigger underlying orbit slot or stage interactions
- [x] Runtime-only 3-slot build save/load flow added to the garage action bar (backed by SaveManager for cross-session persistence)
- [x] Garage auto-optimize now equips the highest-rated owned part per slot using runtime UI scoring
- [x] Inventory and salvage selection show selected part stats plus ability details
- [x] Starter unlock flow now supports configurable base parts via `StarterPartsConfig` (`Resources/StarterPartsConfig.asset`)
- [x] Spinning Bey preview + live stats window in inventory GUI
- [x] Bey preview render texture uses transparent camera clear so menu UI shows behind the model
- [x] Garage preview stage keeps the UI backdrop visible behind the Bey by avoiding an opaque preview plate
- [x] Per-part 3D rendered previews in garage orbit slots, Current Build panel, and swap modal (FaceBolt retains emblem sprite everywhere)
- [x] Material polish pass (22/3/2026): Fusion Wheels forced to full metallic response; Energy Rings rendered semi-transparent plastic across gameplay model, drop pickups, and garage part previews
- [x] Pegasus-style material refinement (22/3/2026): Energy Ring mesh thickness halved; Fusion Wheel smoothness increased for a brighter polished-metal look
- [x] FusionWheel metal-coat tint refinement (22/3/2026): Fusion Wheel base color is now heavily desaturated toward grayscale with only subtle color coating, plus max smoothness (1.0) and full specular/environment reflections in both gameplay assembly and parts debug rendering
- [x] FusionWheel solid-core mesh pass (22/3/2026): Fusion Wheel procedural geometry now generates as a solid modulated disc (no center hole)
- [x] Preview drag is vertical-only and resets when moving between menu states
- [x] Preview/Run stat panel now includes live Spin and Mana current values
- [x] Settings panel uses themed sliders and exposes clipping opacity control
- [x] Keybind reference folded into Settings while pause/intermission menus were rethemed to match the new garage shell
- [x] Settings panel exposes stat-rings UI opacity (0-100%) affecting both ring lines and ring text
- [x] Death screen layout scales more responsively and fits killer-build content more aggressively
- [x] Run-loss salvage UI allows choosing limited eligible parts to transfer to main inventory (depth-scaled count + rarity cap)
- [x] Death-screen control lock (22/3/2026): when defeated, gameplay camera rotation plus lock-on/target switching input are disabled
- [x] Death-screen per-part score labels (22/3/2026): killer-build and salvage rows now include each part's score value\n- [x] Part display order (22/3/2026): all build/loadout part lists now render top-down as FaceBolt → EnergyRing → FusionWheel → Track → Tip (matching physical Bey stacking order)
- [ ] Achievement screen
- [ ] Room transition UI

## Run Flow (Current Phase)
- [x] Start Run builds a structured run progression instead of a single placeholder arena
- [x] Runs currently use multi-level, multi-arena progression with deterministic arena seeds
- [x] Enemy count and enemy-part rarity scale with run depth
- [x] Enemy loadout randomization fix (22/3/2026): enemy seed now incorporates the run-unique `RunSeed` via `ComputeArenaSeed` so enemy builds differ between runs
- [x] Arena clear opens an intermission menu instead of forcing an immediate next-arena transition
- [x] Arena-clear intermission now defaults to Garage tab on win (instead of Inventory)
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
- [x] FaceBolt size standardization (22/3/2026): all procedural FaceBolts now use one fixed radius across every set for consistent scale and Energy Ring fit
- [x] FaceBolt emblem fit pass (22/3/2026): emblem overlay scale normalized by sprite bounds so all emblems render at the same world-space diameter (0.082 units) regardless of source PNG resolution
- [x] FaceBolt emblem completion (22/3/2026): assigned the one previously unused emblem sprite to `Venom Fang_FaceBolt`
- [x] Face Bolt widened; Energy Ring center hole constrained to stay slightly larger than Face Bolt width (close-fit clearance)
- [x] Face Bolt vertical anchor aligned to Energy Ring connection location
- [x] Ability-activation hologram using Face Bolt emblem
- [x] FaceBolt geometry standardization (22/3/2026): all procedural FaceBolts now generate as hexagonal caps
- [x] FaceBolt shared mesh standardization (22/3/2026): all FaceBolts now reuse one cached hex mesh so only colors, emblem textures, names, and abilities differ
- [x] FaceBolt shared-prefab cleanup (22/3/2026): removed all per-FaceBolt RNG/seed variation from mesh generation and radius queries — FaceBolt mesh is a single canonical template (radius 0.038, height 0.015, 6-sided hex)

## Persistence
- [x] SaveManager & JSON serialization (22/3/2026): `SaveManager` static class writes/reads `bladespinners_save.json` via `JsonUtility` + `Application.persistentDataPath`
- [x] Persistent save (22/3/2026): auto-saves owned parts and equipped main-menu loadout on every inventory/garage mutation; loads on startup and merges with starter parts
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

**Last Updated:** March 22, 2026
