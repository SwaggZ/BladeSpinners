# Phase 1: Movement & Player Gameplay - COMPLETE ✅

> Date baseline: all existing entries in this file are tagged as updated on 21/3/2026.

> Historical milestone note: this document describes the completed phase-1 foundation. The live project has since advanced far beyond this snapshot with 52 abilities, 150 Face Bolts, procedural arenas with obstacle types, runtime garage/inventory menus, multi-arena run progression, death-screen overlays, DBZ-style auras, slope-aware physics, and camera occluder fading.

## Summary
Phase 1 is now complete! All core movement, physics, and player control systems are implemented and ready for integration into scenes.

## Architecture Overview

### Core Systems (BladeSpinners.Core)
- **GameEnums.cs**: Defines all enums (PartType, TipBehaviorType, RarityTier, DungeonTheme, etc.)
- **GameConstants.cs**: Global constants for tweaking gameplay (spawn rates, movement speeds, drain rates, etc.)

### Part System (BladeSpinners.Gameplay.Parts)
- **BeyPart.cs**: ScriptableObject base class for all parts. Owns specific stats based on slot type (Tip, Track, FusionWheel, EnergyRing, FaceBolt). Supports hybrid parts.
- **BeyConfiguration.cs**: Runtime class that:
  - Holds the 5 equipped parts
  - Calculates combined stat block
  - Manages spin (health) and mana
  - Notifies of spin threshold crossings for behavior switching
  - Delegates stamina drain calculation to Tip and FusionWheel
- **PartInventory.cs**: Tracks run-temporary parts collected during gameplay
- **PartDatabase.cs**: ScriptableObject registry of all game parts with fast lookup by ID, type, tag, or rarity
- **ThresholdBehaviorModifier.cs**: System for spin-triggered part behavior changes (used by final drive and any threshold-based parts)

### Movement System (BladeSpinners.Gameplay.Movement)
- **ITipBehavior.cs**: Interface for all tip behaviors
- **8 Concrete Behaviors**: FlatTip, SharpTip, RoundTip, RubberFlatTip, BallTip, SpikeTip, OrbitTip, plus 23+ MFB-style catalog variants (WD, Q, ES, W2D, MS, EDS, SF, MB, BS, SD, HF, DS, S, FS, B, RS, F, D, R2F, EWD, D:D, CS, B:D)
  - Each controls grip, uphill resistance, tilt amount, and stamina drain characteristics
  - Force is ONLY applied along forward axis - steering rotates facing direction
  - Physics momentum handles all curved paths
- **TipBehaviorFactory.cs**: Factory for instantiating behaviors by type
- **BeyMovementController.cs**: Central movement system that:
  - Applies forward force along the Bey's forward axis only
  - Handles steering, boost, brake, jump inputs
  - Manages uphill resistance and slope effects
  - Supports special orbital movement for OrbitTip
  - Delegates physics modifiers to active ITipBehavior
  - Drains spin and regenerates mana
  - Handles burst (spin = 0) detection
- **BeyTiltController.cs**: Visual feedback system
  - Applies tilt (lean) based on velocity and tip behavior
  - Enters "wobble" state when spin is critically low
  - Wobble serves as low-health indicator

### Combat System (BladeSpinners.Gameplay.Combat)
- **SpinExchangeHandler.cs**: Calculates spin damage on collision
  - Weight differential multiplier (heavier Beys deal more damage)
  - Velocity-based damage scaling
  - Symmetric calculation for both colliding Beys
- **BeyCollisionDetector.cs**: Detects Bey-to-Bey collisions
  - Checks collision cooldown to prevent frame spam
  - Calls SpinExchangeHandler for spin exchange
  - Fires collision events

### Ability System (BladeSpinners.Abilities)
- **BeyAbility.cs**: Base class for Face Bolt abilities
  - Activate() called when player uses ability
  - ManaCost paid from Energy Ring
  - Stored reference in BeyConfiguration
- **BeyPassive.cs**: Base class for Energy Ring passives
  - Always-active effects
  - Hooks into collision and spin events
  - Can modify gameplay in real-time

### Player Control (BladeSpinners.Gameplay)
- **PlayerInputHandler.cs**: Translates player input to Bey commands
  - Keyboard: WASD for movement, Shift for boost, C for brake, Space for jump, E for ability
  - Gamepad support ready (customizable button mapping)
  - Forwards input to BeyMovementController and ITipBehavior
- **ThirdPersonCameraController.cs**: Camera system
  - Orbits around Bey with customizable distance and height
  - Right-stick gamepad control (mouse support ready)
  - Pitch/yaw clamping for smooth viewing
  - Easy zoom via SetOrbitDistance()
- **PlayerManager.cs**: Master coordinator
  - Initializes all player systems
  - Wires up component references
  - Manages run inventory
  - Provides unified access to all player systems

## Physics Setup Required
To use this system in a scene, you need:
1. **Rigidbody** on the Bey GameObject (not kinematic, gravity enabled)
2. **Collider** on the Bey (sphere or capsule works well)
3. **Trigger collider** for collision detection (BeyCollisionDetector needs this)
4. **Ground layer** setup (Platform or similar)
5. **Physics raycast** will check for ground contact

## Key Design Decisions

### Force Application
- **Only forward force is applied**. No lateral (strafe) force ever.
- Turning rotates the facing direction, and momentum carries the Bey in arcs.
- Physics naturally handles drifting and momentum based on tip behavior's drag values.
- This creates a "vehicle-like" feel true to Beyblade physics.

### Spin Drain
- **Two-component drain system**:
  - Fusion Wheel: `mass_based_drain = BASE_MASS_DRAIN * (weight / 25)`
  - Tip Behavior: `behavior_based_drain = BASE_BEHAVIOR_DRAIN * behavior_modifier`
  - Total: `mass_drain + behavior_drain`
- Boost multiplies drain by `BOOST_STAMINA_DRAIN_MULTIPLIER` (3x)
- Uphill increases drain through reduced forward force (not direct drain increase)

### Stat Ownership (Non-Overlapping)
- **Tip**: All movement behavior, grip, uphill resistance, tilt, behavior-drain modifier
- **Track**: Height (for hitbox/center of gravity), jump arc modifier
- **Fusion Wheel**: Weight (knockback/stamina drain), mass-based drain rate
- **Energy Ring**: Mana pool, mana regen rate
- **Face Bolt**: Ability reference, mana cost (ability determines cost)

Each stat belongs to exactly one slot type, making composition transparent and predictable.

## Next Steps (Phase 2: World Generation)
This section is preserved as the original handoff from the end of phase 1:
1. Create dungeon layout generator
2. Build map chunk system
3. Implement room types and transitions
4. Set up procedural generation

## Files Created
```
Assets/Scripts/
├── Core/
│   ├── GameEnums.cs
│   ├── GameConstants.cs
├── Gameplay/
│   ├── PlayerInputHandler.cs
│   ├── PlayerManager.cs
│   ├── ThirdPersonCameraController.cs
│   ├── Parts/
│   │   ├── BeyPart.cs
│   │   ├── BeyConfiguration.cs
│   │   ├── PartInventory.cs
│   │   ├── PartDatabase.cs
│   │   ├── ThresholdBehaviorModifier.cs
│   ├── Movement/
│   │   ├── ITipBehavior.cs
│   │   ├── TipBehaviorFactory.cs
│   │   ├── BeyMovementController.cs
│   │   ├── BeyTiltController.cs
│   │   ├── Tips/
│   │   │   ├── FlatTip.cs
│   │   │   ├── SharpTip.cs
│   │   │   ├── RoundTip.cs
│   │   │   ├── RubberFlatTip.cs
│   │   │   ├── BallTip.cs
│   │   │   ├── SpikeTip.cs
│   │   │   ├── OrbitTip.cs
│   ├── Combat/
│   │   ├── SpinExchangeHandler.cs
│   │   ├── BeyCollisionDetector.cs
├── Abilities/
│   ├── BeyAbility.cs
│   ├── BeyPassive.cs
```

## Testing Recommendations
1. **Movement**: Create a simple arena scene, test movement in all directions with different tips
2. **Tilt**: Verify visual tilt matches velocity direction
3. **Collision**: Spawn two Beys, verify spin exchange works
4. **Spin Drain**: Check that spin drains over time, faster with boost
5. **Tips**: Test each tip behavior's unique feel and characteristics
6. **Camera**: Verify orbit feels smooth and responsive

---
**Status**: ✅ Complete and ready for Phase 2 (World Generation)
**Last Updated**: March 22, 2026
