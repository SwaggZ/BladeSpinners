# BladeSpinners Implementation Checklist

Last updated: 2026-07-30

This file is the source of truth for the stabilization and enhancement work. Update an
item only after its acceptance checks pass.

Status legend:

- `[ ]` Not started
- `[~]` In progress
- `[x]` Completed and verified
- `[!]` Blocked or needs a design decision

## Phase 0 — Critical runtime stabilization

- [x] **Correct arena and bey physics layers**
  - Assign generated arena objects to `Ground`.
  - Assign runtime player/enemy roots, visual children, and colliders to `Bey`.
  - Preserve trigger-based spin exchange while suppressing physical mesh-to-mesh
    collisions between beys. Do not use a global Bey/Bey collision ignore because it
    also suppresses trigger callbacks.
  - Acceptance: player and enemy hierarchies are layer 9, arena hierarchy is layer 8,
    Ground raycasts hit the arena, and one trigger collision produces one spin exchange.

- [x] **Remove countdown initialization exceptions**
  - Initialize Rigidbody and ground-layer dependencies before a movement component can
    be disabled by the match countdown.
  - Make public velocity access safe before the first physics frame.
  - Acceptance: a complete countdown produces no `NullReferenceException`, and movement
    starts normally when the match enters `InProgress`.

- [x] **Keep one valid GameManager across arena transitions**
  - Reuse the run-level manager instead of destroying and recreating it in the same frame.
  - Clear the singleton reference when its owning object is actually destroyed.
  - Deactivate old run roots immediately before deferred destruction.
  - Acceptance: first and second arenas each have exactly one active `GameManager`, and
    `GameManager.Instance` remains valid after the transition frame.

- [x] **Ship the complete runtime part catalog**
  - Synchronize `StarterPartsConfig.runtimePartCatalog` with every authored `BeyPart`.
  - Add build-time validation for missing, duplicate, or stale catalog entries.
  - Acceptance: runtime catalog contains exactly the same 750 distinct assets as the
    authored part set, including in a standalone build.

## Phase 1 — Combat correctness

- [x] **Apply per-round diminishing returns to life-steal abilities**
  - Reduce the restored-spin percentage after every successful use in the same arena.
  - Cap first-use restoration at 50% even when generated ability tuning is higher.
  - Reset the usage counter when match resources reset for the next arena.
  - Cover Vampire Drain, Spin Drain, and Soul Link through one shared calculation.
  - Acceptance: successive casts restore less spin, target damage remains unchanged, and
    the first cast in a new arena returns to full configured efficiency.

- [x] **Expose the full ability pool through Face Bolts**
  - Reassign the 150 Face Bolts across all production-ready ability types or deliberately
    leave references empty for deterministic resolver assignment.
  - Repair `ChronoRecallAbility` metadata.
  - Acceptance: every approved ability type is reachable and every Face Bolt resolves.

- [x] **Deduplicate area-ability targets**
  - Resolve colliders to `BeyMovementController` and apply each effect once per unique bey.
  - Cover Black Hole, Gravity Well, Inferno, Spectral Chains, Static Discharge, Nightfall,
    War Cry, Magnetic Field, and other overlap-based abilities.
  - Acceptance: adding extra colliders to a target does not change damage or status count.

- [x] **Add faction-aware ability targeting**
  - Introduce a shared target query supporting self, ally, enemy, and all.
  - Remove direct `EnemyBeyController` searches from individual abilities.
  - Acceptance: player and enemy casts obey the same targeting rules without friendly fire
    unless the ability explicitly opts into it.

- [x] **Add ability cooldowns and correct effective mana costs**
  - Calculate the modified cost once and use it for both affordability and spending.
  - Give AI and player abilities a shared cooldown gate and expose remaining cooldown.
  - Acceptance: AI cannot cast every frame, discounts/surcharges behave correctly, and
    mana never clamps because a cast passed an incorrect affordability check.

- [x] **Correct collision knockback directions**
  - Push both beys away from the contact pair.
  - Acceptance: post-collision velocity has a positive dot product with each bey’s
    away-from-contact direction.

- [x] **Use real collision magnitude and facing**
  - Replace the hard-coded `1f` collision magnitude.
  - Fold relative contact velocity and head-on/side/grazing multipliers into damage.
  - Acceptance: head-on high-speed impacts deal more spin damage than slow grazing hits.

- [x] **Add Attack and Defense/Spin Retention stats**
  - Derive Attack primarily from Fusion Wheel geometry/contact characteristics.
  - Derive Defense or Spin Retention from appropriate part properties.
  - Include both in `BeyStatBlock`, combat math, garage comparison, and tooltips.
  - Acceptance: two equal-weight wheels with different profiles produce measurably
    different combat outcomes.

- [x] **Make every balance control functional**
  - Wire starting-spin, enemy starting-spin, enemy mana-pool, and authored wheel stamina
    drain values into gameplay.
  - Remove or implement unused combat tuning fields.

## Phase 2 — Five-part build identity

- [x] **Implement Energy Ring passives**
  - Connect `BeyPassive` lifecycle hooks to equipped Energy Rings.
  - Start with 8–12 distinct passives such as Spin Recovery, Low Spin Surge, collision
    shielding, mana conversion, and pickup amplification.
  - Acceptance: passive behavior is visible in combat and described in the garage.

- [ ] **Implement PartTag synergies**
  - Add understandable two-tag and three-tag bonuses.
  - Show active and prospective bonuses in garage comparison.

- [ ] **Curate a starter ladder**
  - Select 20–30 memorable sets with clear roles and difficulty progression.
  - Add collection/unlock gates so parts feel earned.

- [ ] **Improve garage explanations**
  - Explain tip behavior, ability behavior, passives, tags, and why a stat changed.
  - Add ability previews and meaningful part icons.

- [ ] **Balance spreadsheet pass**
  - Establish rarity targets for expected spin damage, mana economy, survivability,
    mobility, and tip aggressiveness.

## Phase 3 — AI, encounters, and run structure

- [x] **Add run and arena timing with persistent personal bests**
  - Count active combat time only; pause during countdowns, pause menus, results, and
    arena transitions.
  - Reset the arena timer for each arena while preserving the total run timer.
  - Persist the ten fastest completed runs and the ten deepest runs, with time as the
    depth-table tie breaker.
  - Acceptance: both timers render in-run, records survive relaunch, losses record
    completed arenas, and completed runs enter both tables.

- [ ] **Wire EnemyArchetype into behavior**
  - Implement distinct Aggressive, Stamina, Defense, and Gimmick decision profiles.

- [ ] **Add named bosses**
  - End each level with a curated boss loadout and arena modifier.
  - Candidate modifiers: hazard rim, rotating bumper, low-friction ice.

- [ ] **Clarify the run fantasy**
  - Decide between multi-room dungeon floors and a focused stadium-run structure.
  - Remove unused roadmap concepts after the decision.

- [x] **Finish rechargeable pickups**
  - Replace the stamina-as-mana proxy with explicit spin and mana pickups.
  - Keep each pickup in its arena, recharge it over time after collection, and grant a
    proportional amount when collected before fully charged.
  - Place pickups against the generated arena surface and show recharge state visually.
  - Acceptance: pickups never spawn below the bowl, never disappear permanently, and
    partial charge grants the same fraction of the configured maximum reward.

- [x] **Keep runs permadeath-only**
  - Mid-run checkpoints are intentionally excluded so abandoning or losing a run ends it.
  - Persist only long-term collection/loadout data and personal-best records.

## Phase 4 — Presentation and user experience

- [x] **Repair inventory and result part details**
  - Give the inventory a stable list/detail split at supported resolutions.
  - Use explicit detail rectangles in loss salvage and arena-clear inventory panels.
  - Acceptance: choosing/viewing a part always shows its stats, ability/passive, and
    description without overlapping the KEEP controls.

- [x] **Improve bowl contact and curved-surface movement**
  - Project drive and steering forces onto the contacted arena surface.
  - Remove high bounce from Bey mesh colliders and cancel unintended separating velocity
    against the averaged ground normal.
  - Acceptance: beys follow bowl curves without repeated ground launches or edge nudges.

- [x] **Fix hit-particle initialization assertion**
  - Configure particle systems before playback or use preconfigured pooled effects.

- [ ] **Add audio and impact juice**
  - [x] Add a persistent, pooled `SoundManager` with separate 3D SFX, 2D UI, and
    looping music playback.
  - [x] Generate and build-validate a runtime catalog from `Assets/SoundEffects`;
    include empty folders so new clips require no inspector wiring or code changes.
  - [x] Wire strength-scaled Bey-vs-Bey and Bey-vs-wall impacts.
  - [x] Wire every player/enemy ability to its matching `Abilities/<Ability Name>`
    folder.
  - [x] Wire button, part-equip, game-start, win, and lose GUI folder keys.
  - [x] Add the background-music folder key and automatic looping playback.
  - [x] Add situation playlists, soft music transitions, editable title/author
    metadata, and a temporary now-playing banner.
  - [x] Guarantee menu audio output, streaming music imports, non-repeating playlist
    rotation, silent-playback recovery, and one banner per song start.
  - [x] Randomize each music situation with an independent shuffle bag that consumes
    every category track before reshuffling.
  - [x] Pair every background track with same-name JPG artwork and render the logo in
    a launch-safe now-playing overlay above the main UI.
  - [x] Add saved Master, Sound Effects, Music, and GUI sliders with live
    Master-by-category mixing.
  - [x] Extend every now-playing banner appearance to approximately five seconds.
  - [x] Add a non-interrupting Main Menu/Inventory category queue and a manual
    next-song button.
  - [x] Add a launch Start Screen with replaceable logo, catchphrase, animated
    star field, any-input transition, optional single-track theme, and folder-driven
    transition sound.
  - [ ] Populate the currently empty ability and GUI folders.
  - [ ] Add burst sounds, arena ambience/music layers, hit-stop, and camera shake.

- [ ] **Replace placeholder ability VFX**
  - Move from per-cast primitives and `Shader.Find` calls to shared pooled effects and
    cached materials.

- [ ] **Build a real in-run HUD**
  - Show ability icon, mana cost, cooldown, status effects, enemy count, run depth, and
    pickup prompts.

- [ ] **Improve ability clarity**
  - Tag abilities as Movement, Offense, Control, or Sustain.
  - Bias loot choices to avoid redundant/random ability combinations.
  - Add telegraphs and faction-consistent effect colors.

- [ ] **Migrate UI incrementally**
  - Extract HUD first, then garage tab presenters, while retaining the current shell.
  - Move toward UI Toolkit or uGUI without a single all-at-once rewrite.

- [ ] **Use Input Actions**
  - Replace direct device polling with the existing Input System action asset.
  - Add remappable bindings, keyboard/gamepad parity, and accessibility options.

## Phase 5 — Architecture and production readiness

- [ ] **Replace reflection wiring**
  - Introduce explicit initialization APIs and a small `BeySpawnFactory`.

- [ ] **Create tunable prefabs**
  - Player, enemy, drop, and pickup prefabs should own stable component wiring.

- [ ] **Add automated tests**
  - Start with layers/countdown/arena transition/catalog regressions.
  - Add pure tests for spin exchange, rarity rolls, salvage caps, effective mana cost,
    targeting, passives, and save migrations.

- [ ] **Add CI build verification**
  - Compile tests, validate the runtime catalog, and produce a Windows player.

- [ ] **Cull unused packages**
  - Confirm and remove unused Visual Scripting, Timeline, and Multiplayer Center packages.

## Completed verification log

- 2026-07-30: Baseline audit completed on Unity 6000.3.8f1.
  - Windows Development build succeeded with zero errors.
  - 750 authored parts and 51 ability implementations discovered.
  - All 750 procedural part meshes generated without failure.
  - All 51 ability `Activate` methods completed after initialization.
  - Baseline critical failures reproduced: countdown exception, incorrect layers,
    incomplete runtime catalog, and lost `GameManager` after arena two.

- 2026-07-30: Critical stabilization batch completed.
  - Runtime player/enemy roots and all five generated part colliders validated on `Bey`.
  - Countdown completed without project exceptions.
  - Knockback produced positive away-direction velocity for both participants.
  - First and second arenas retained exactly one valid `GameManager`.
  - Runtime catalog synchronized and build-gated at 750/750 distinct authored parts.
  - All 51 ability activation smoke checks still passed.
  - Hit-particle initialization assertion no longer occurred.
  - Windows Development build succeeded with zero errors and launched without exceptions.

- 2026-07-30: Face Bolt ability-pool pass completed.
  - Removed all 150 legacy explicit ability references; the thematic resolver is now the
    single source of truth.
  - Validated 150/150 Face Bolts with zero unresolved entries.
  - All 51 runtime ability types are represented by at least one authored Face Bolt.
  - Added build-time validation for Face Bolt count, legacy overrides, resolver coverage,
    unexpected types, and ability metadata.
  - Completed Chrono Recall name, description, mana cost, and rarity metadata.
  - Static audit completed with zero failures; all 51 ability activation checks passed.
  - Windows Development build succeeded with zero errors.

- 2026-07-30: Area-ability target deduplication completed.
  - Added one shared radius query that resolves compound collider hits to unique
    `BeyMovementController` roots and supports excluding the caster.
  - Migrated all 13 overlap-based ability queries, including delayed zones, projectiles,
    nearest-target selection, and capped multi-target effects.
  - Compound-collider regression resolved 10 collider hits to exactly two unique
    non-caster beys.
  - Confirmed that no ability directly calls `Physics.OverlapSphere` outside the shared
    query.
  - Static audit completed with zero failures; all 51 ability activation checks passed.
  - Windows Development build succeeded with zero errors.

- 2026-07-30: Faction-aware ability targeting completed.
  - Added shared `Self`, `Ally`, `Enemy`, and `All` targeting relations plus radius,
    all-bey, nearest-target, and collider-resolution queries.
  - Migrated all 39 ability scripts that acquire external targets, including delayed
    zones, moving hazards, homing/ricochet projectiles, drains, chains, and area attacks.
  - Removed direct `EnemyBeyController` searches and per-ability `IsEnemy` comparisons.
  - Regression validated both player-side and enemy-side casts, caster exclusion,
    ally filtering, all-target selection, and 12 compound colliders without duplicates.
  - Static audit completed with zero failures; all 51 ability activation checks passed.
  - Windows Development build succeeded with zero errors.

- 2026-07-30: Folder-driven audio foundation completed.
  - Generated and validated 63 runtime folder keys, including all 51 ability folders,
    with 65 current AudioClips and zero missing references.
  - Added pooled spatial impact/ability playback, 2D GUI stingers, and looping music.
  - Wired Bey-vs-Bey, Bey-vs-wall, all gameplay ability activations, all shared GUI
    buttons, part equips, game start, win, and lose.
  - Play-mode audit completed with zero direct gameplay or audio failures; intentionally
    empty folders resolved as silent slots without exceptions.
  - Windows Development build succeeded with zero errors and packaged the runtime
    catalog/audio resources.

- 2026-07-30: Orbit-tip hovering regression completed.
  - Corrected the shared Orbit movement path used by all 12 authored Orbit tips.
  - Orbit steering is now horizontal-only and preserves Rigidbody vertical velocity,
    so gravity, jumps, knockback, and bowl floors below world Y=0 remain authoritative.
  - Orbit state re-anchors from the Bey's current position after airborne movement,
    input interruption, or a behavior change instead of snapping toward a stale point.
  - Exact Warden/Thornweed/Duskwarden/Scorpion/Ashbringer loadout regression passed at
    arena Y=-2.92 and world Y=0 with no injected upward velocity.
  - Focused checks passed for all 12 Orbit assets, grounded movement, landing re-entry,
    and unchanged airborne velocity.
  - Runtime audit completed with zero direct gameplay failures across all 51 abilities;
    Windows Development build succeeded with zero errors.

- 2026-07-30: Orbit-tip moving-anchor redesign completed.
  - Replaced the fixed 5 m arena-centered circle with a 0.75 m local orbit around an
    invisible anchor that travels along the player or AI's intended forward path.
  - Initial tuning: 18 m/s anchor travel and 240 degrees/second local rotation, producing
    one local revolution every 1.5 seconds.
  - Steering now bends the moving anchor's route without applying a second side force
    that would enlarge or distort the local circle.
  - Knockback, airborne movement, behavior changes, interrupted input, and large
    displacements reset the anchor at the Bey's current position to prevent tethering.
  - Full-revolution regression confirmed 27 m of global forward travel while retaining
    the 0.75 m local radius and preserving vertical velocity on sunken bowl floors.
  - Exact Warden loadout and all 12 authored Orbit tips passed; runtime audit reported
    zero direct gameplay failures and the Windows Development build had zero errors.

- 2026-07-30: Effective ability costs and shared cooldowns completed.
  - Replaced separate affordability/spending calls with one atomic commit that calculates
    the final global and enemy-modified mana cost once, validates it, spends that exact
    value, and starts cooldown.
  - Routed both player and AI casts through one `AbilityActivationService`; repeated
    attack-state frames can no longer produce repeated successful casts.
  - Added serialized per-ability cooldown overrides with automatic rarity/cost-based
    defaults for all existing ability assets and runtime-resolved abilities.
  - Exposed remaining duration, original duration, normalized progress, readiness, and
    effective mana cost through `BeyConfiguration` for the planned in-run HUD.
  - Match restarts reset cooldown state for both player and enemy beys.
  - Regression checks passed for insufficient modified mana, exact-cost casts, discounts,
    enemy surcharges, same-frame repeats, cooldown expiry, and all 51 ability definitions.
  - Runtime audit completed with zero direct gameplay failures; Windows Development build
    succeeded with zero errors.

- 2026-07-30: Real collision magnitude and facing completed.
  - Replaced the hard-coded collision magnitude with planar relative velocity, actual
    closing speed, and relative-velocity/contact-normal alignment.
  - Trigger contacts derive their contact normal from the two Bey roots and select the
    attacker by velocity contributed into that contact rather than total speed alone.
  - Fast parallel motion no longer qualifies as a damaging impact; direct, high-speed
    closing collisions scale substantially above slow grazing contacts.
  - Added defender exposure multipliers for front (0.85x), side (1.075x), and rear
    (1.30x) hits while retaining wheel-weight and personal-speed contributions.
  - Deterministic regression produced 18.18 head-on damage versus 2.26 slow-graze damage
    for equal weights and validated the front/side/rear ordering and full spin exchange.
  - Runtime audit completed with zero direct gameplay failures; Windows Development build
    succeeded with zero errors.

- 2026-07-30: Geometry-driven Attack, Defense, and Spin Retention completed.
  - Added one deterministic Fusion Wheel combat profile shared by procedural mesh
    generation, combat calculations, garage scoring, comparisons, and tooltips.
  - Attack derives primarily from blade protrusion, contact width, blade count, symmetry,
    and secondarily weight; Defense favors broad, round, symmetrical, stable wheel shapes.
  - Spin Retention combines wheel balance, authored mass drain, weight, and Tip behavior;
    it now modifies the real passive stamina-drain rate.
  - Collision damage now applies both attacker Attack and defender Defense in addition to
    weight, speed, contact alignment, and hit facing.
  - All 150 authored wheels validated in range with 47 distinct rounded Attack bands and
    54 distinct rounded Defense bands.
  - Equal-weight geometry regression changed identical-contact damage from 9.02 to 12.38;
    high Defense reduced the same hit from 11.85 to 7.85.
  - The existing head-on/grazing/facing regression still passed; runtime audit completed
    with zero direct gameplay failures and the Windows build succeeded with zero errors.

- 2026-07-30: Combat balance-control wiring completed.
  - Added faction-aware starting spin and maximum mana calculations, including stacked
    global and enemy multipliers, and centralized match resource resets.
  - Match start/restart now fills each Bey to its actual starting spin and effective
    Energy Ring mana pool after its faction and loadout are assigned.
  - Mana clamps, world rings, debug rings, garage runtime readouts, visual spin, and
    low-spin wobble now use the same effective resource values.
  - Authored Fusion Wheel stamina drain is now part of real passive spin drain instead
    of being replaced by a weight-only formula.
  - Removed obsolete or misleading serialized knobs for legacy visual spin, local turn
    speed, Adrenaline contact damage, and Solar Flare blindness; updated Adrenaline's
    description to match its implemented speed-and-mass effect.
  - Regression verified gameplay consumers for all 26 exposed `GameManager` controls,
    player/enemy starting spin of 120/60, faction mana caps of 300/120, and authored
    wheel drain producing 0.456 versus 2.076 spin loss per second.
  - All 51 ability activation checks still passed; runtime audit reported zero direct
    gameplay failures; the final Windows build succeeded with zero errors and only two
    unrelated UI-state warnings.

- 2026-07-30: Situation-based music and now-playing banner completed.
  - Added Main Menu, Inventory, Battle, Boss Battle, Victory, and Lose routing, with
    boss music currently tied to the reserved boss-depth constant.
  - Added JSON-backed title, author, and situation metadata for all seven background
    MP3s plus an editable `Blade Spinners/Audio/Music Metadata` Unity table.
  - Added automatic MP3 reconciliation and build-time coverage checks so every
    background track is cataloged and every situation retains at least one song.
  - Added two-source, unscaled-time 1.25-second crossfades and non-repeating Battle
    selection when multiple tracks are available.
  - Added a bottom-left Rocket League-style title/author banner with slide/fade
    animation lasting 2.36 seconds.
  - Preserved the music and banner services across arena cleanup and added self-healing
    situation requests if a service is ever recreated.
  - Focused validation passed with seven tracks, two Battle choices, and all six
    situations covered.
  - Play-mode transitions showed one banner each for Main Menu, Battle, Lose, and the
    second arena; runtime audit reported zero direct failures.
  - Windows Development build succeeded with zero errors and only two unrelated
    pre-existing UI-state warnings.

- 2026-07-30: Music playback reliability follow-up completed.
  - Fixed silent Main Menu and Inventory music by guaranteeing the fallback menu camera
    owns an `AudioListener` whenever no active listener exists.
  - Converted all 13 background MP3s to preloaded streaming clips and added import-time
    enforcement plus build validation for future files.
  - Added global last-started tracking so a playlist never selects the same clip for
    two consecutive starts when another authored track is available.
  - Multi-song situations now crossfade to another non-repeating track near the end;
    one-song situations continue looping because no alternative exists.
  - Added stalled-playback recovery without fading out a valid outgoing song first.
  - Added monotonic playback-start IDs; the banner consumes each start exactly once,
    including launch and service recovery.
  - Banner timing now begins on its first drawable GUI frame, preventing startup/import
    time from expiring the launch banner before it can appear.
  - Dedicated Play Mode coverage passed eight starts across all six situations,
    including consecutive Battle selections, with eight banners and zero adjacent
    repeats.
  - Full runtime audit reported zero direct failures; Windows Development build
    succeeded with zero errors and only two unrelated pre-existing UI-state warnings.

- 2026-07-30: Expanded music catalog, artwork, and launch-banner fix completed.
  - Audited 23 MP3s against `music-metadata.json`: all 23 are represented exactly once,
    with valid situations, non-empty titles/authors, and no stale records.
  - Validated same-name JPG coverage for every track under `Background/Logos`; the
    extra `Golden Core.jpg` remains unused because no matching MP3 exists yet.
  - Added each logo as a build-safe runtime catalog reference and made missing or
    mismatched track artwork fail validation/builds.
  - Replaced the banner's generic music-note tile with the current track's square logo.
  - Fixed the launch overlay ordering by making the main IMGUI controller explicitly
    draw the music banner after all menu and gameplay UI.
  - Banner timing still starts on the first eligible UI draw, so startup work cannot
    consume its visible duration.
  - Dedicated Play Mode coverage passed launch plus all six situations, including two
    consecutive Battle selections: eight starts, eight banners, non-null artwork, and
    zero adjacent repeats.
  - Full runtime audit reported zero direct failures; Windows Development build
    succeeded with zero errors and only two unrelated pre-existing UI-state warnings.

- 2026-07-30: Persistent category volume controls completed.
  - Repaired the previously cosmetic Master Volume slider so it now controls Unity's
    global listener volume.
  - Added independent Sound Effects, Music, and GUI sliders to both the main-menu and
    pause-menu Settings panels.
  - Saved all four values independently in player preferences and restored them before
    launch music begins.
  - Applied category levels to the pooled hit/ability sources, dual music sources, and
    GUI source; effective output is always `Master x Category`.
  - Made live Music changes preserve the current fade weight during transitions, and
    made SFX/GUI changes affect sounds already playing.
  - Focused validation passed clamping, persistence round-tripping, UI wiring, and
    effective-level calculations.
  - Play Mode verified `0.50 x 0.80 = 0.40` SFX, `0.50 x 0.60 = 0.30` Music, and
    `0.50 x 0.40 = 0.20` GUI with all 16 spatial voices updated; the full runtime audit
    retained zero direct failures across all 51 abilities and the second-arena test.
  - Windows Development build succeeded with zero errors and only two unrelated
    pre-existing UI-state warnings.

- 2026-07-30: Randomized situation playlists completed.
  - Replaced isolated random-index choices with independent Main Menu, Inventory,
    Battle, Boss Battle, Victory, and Lose shuffle bags.
  - Seeded each game session from runtime entropy so relaunching does not recreate a
    fixed catalog sequence.
  - Rebuilt the runtime catalog from the current 30-track JSON so all three Main Menu
    songs are available in standalone playback instead of only the older single entry.
  - Each category now plays every available track once before reshuffling, while still
    preventing the same song from playing twice in a row across category changes and
    shuffle boundaries.
  - Focused validation passed all 30 current tracks, 18 Battle choices, all six
    situations, three complete five-song shuffle cycles, and single-song fallback.
  - Two consecutive Play Mode launches produced different Inventory, Battle, Victory,
    and returning Main Menu choices; both completed eight starts with eight banners and
    zero adjacent repeats.
  - Windows Development build succeeded with zero errors and only two unrelated
    pre-existing UI-state warnings.

- 2026-07-30: Energy Ring passive system completed.
  - Replaced the unused passive shell with per-Bey runtime state, equip/unequip and
    match-reset lifecycle handling, fixed-step ticking, and hooks for collision damage,
    collision events, spin changes, mana spending/regeneration, stamina drain, and
    resource pickups.
  - Added ten concrete passives: Spin Recovery, Low Spin Surge, Impact Guard, Kinetic
    Battery, Recoil Recovery, Arc Conversion, Mana Conduit, Endurance Matrix, Second
    Wind, and Collector's Prism.
  - Added build-safe deterministic assignment for all authored Energy Rings while
    retaining an optional explicit passive override for future curated sets.
  - Added garage passive names, rarity, and full behavior descriptions; combat now shows
    the equipped passive and briefly highlights each triggered proc.
  - Focused validation passed all ten numeric behavior checks and resolved 150/150
    authored Energy Rings, with every passive represented by 9–23 rings.
  - The full 750-part static audit, existing spin-impact and balance-control regressions,
    all 51 ability activation smoke checks, collision/knockback checks, and second-arena
    transition completed with zero direct gameplay failures.
  - Windows Development build succeeded with zero errors and only two unrelated
    pre-existing UI-state warnings.

- 2026-07-30: Lifesteal, pickups, movement, run records, and result UI completed.
  - Added shared per-arena diminishing returns to Vampire Drain, Spin Drain, and Soul
    Link: restoration is capped at 50%, multiplied by 0.65 after each successful use,
    and reset with the next arena's match resources.
  - Replaced disposable pickups with explicit spin and mana pickups that recharge over
    12 seconds and grant rewards in proportion to their visible charge.
  - Validated 48 pickup positions across 12 generated arenas against the exact Ground
    surface; every pickup was approximately one metre above its arena floor.
  - Projected movement and steering onto the averaged Ground contact plane, removed
    high collider bounce, and retained the validated 0.75 m local Orbit-tip radius.
  - Added active-combat run and arena timers, persistent fastest/deepest top-ten tables,
    stable inventory/result detail panes, and an exact five-second music banner.
  - The focused enhancement suite and related ability, Orbit, passive, balance, impact,
    and music regressions passed. A Windows Development build completed with zero
    compiler errors, and a standalone smoke run logged zero runtime exceptions.

- 2026-07-30: Start Screen and non-interrupting menu music completed.
  - Added a launch-only Start Screen with a deterministic animated star field, generated
    placeholder logo, catchphrase, breathing any-input prompt, 1.15-second launch
    animation, and Input System keyboard, mouse, and controller button support.
  - Added drop-in hooks for `Resources/UI/GameLogo.png`, one optional JSON-tagged
    `StartScreen` theme, and the folder-driven `GUI/Start Screen Transition` sound.
    Until the theme is supplied, one Main Menu song loops as an audible fallback.
  - Added a unique FIFO music-category queue so Main Menu and Inventory navigation never
    interrupts the current track; gameplay/result situations still take immediate
    priority and clear stale menu entries.
  - Added a Main Menu `NEXT SONG` button that consumes the next queued category or
    advances the current category's randomized shuffle bag.
  - Focused validation covered all 30 current tracks, six required situations, the
    optional single-theme rule, unique queue ordering, asset hooks, and UI surface.
  - Play Mode generated a real Input System button press, verified Start Screen to Main
    Menu music transition, confirmed both navigation directions stayed uninterrupted,
    and confirmed Next Song consumed Inventory from the queue.
  - The related gameplay regression suite and Windows Development build passed; a
    seven-second standalone title-screen smoke run logged zero runtime exceptions.

- 2026-07-30: Catalog importer-crash recovery completed.
  - Traced a 749/750 build failure to Unity recovering the valid Galeforce Face Bolt as
    a `DefaultAsset` after an asset-import worker crash; its source YAML, meta GUID, and
    runtime-catalog reference were all intact.
  - Forced a clean Galeforce reimport and added a targeted pre-build repair pass that
    reimports only physically present part assets that fail to load as `BeyPart`.
  - Both runtime-catalog and Face Bolt validation now share the repair pass, preventing
    one poisoned Library artifact from producing conflicting stale/missing-part errors.
  - Validation passed 750 authored/runtime parts, 150 Face Bolts, all 51 ability types,
    and a Windows Development build with zero compiler or build errors.
