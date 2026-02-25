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

## Player & Camera
- [x] Player input handler (movement, boost, brake, jump, ability input)
- [x] Third-person camera orbiting Bey
- [x] Main player Bey prefab coordination (PlayerManager - ready for GameObject setup)

## Inventory & Part Management
- [x] PartInventory (run-temporary parts)
- [x] PartDatabase (permanent parts registry)
- [x] Part equipping/swapping logic (integrated into BeyConfiguration)

## World Generation
- [ ] Map chunk prefabs (flat, ramps, platforms, bowls, bridges, hazards)
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

## Enemies & Combat
- [ ] EnemyBeyAI state machine (Aggression, Reposition, StaminaConservation)
- [ ] EnemySpawner using dungeon theme & depth
- [ ] Part drop system on enemy burst
- [ ] Boss enemy system with named configs

## Abilities & Combat Enhancement
- [x] BeyAbility base & concrete implementations (SpinDash, GravityWell, ShieldBurst, OrbitLock, StaminaLeech)
- [x] BeyPassive base & implementations (SpinRecovery, LowSpinSurge, ImpactShield, MomentumHarvest)
- [x] ThresholdBehaviorModifier for spin-triggered changes
- [ ] Integrate abilities into ProceduralPartGenerator

## UI & Menus
- [ ] In-run HUD (spin bar, mana bar, ability icon, minimap)
- [ ] Inventory/part swapping screen
- [ ] Main menu with Bey customization
- [ ] Achievement screen
- [ ] Room transition UI

## Persistence
- [ ] SaveManager & JSON serialization
- [ ] Persistent save (unlocked parts, achievements, base loadout)
- [ ] Run save (current dungeon state, inventory, spin/mana)

## Polish & Audio (Last)
- [ ] Particle effects (pickups, abilities, impacts)
- [ ] Sound effects & SFX manager
- [ ] Visual feedback & juice

---

## Parallel work (can happen anytime)
- 3D asset creation (Bey model, parts, enemies, chunks, UI icons)
- Ability design & balance spreadsheet
- Part rarity curves & stat ranges per depth

---

**Last Updated:** February 23, 2026
