# Part Set Generation Guide

> Date baseline: all existing entries in this file are tagged as updated on 21/3/2026.

This guide covers both generation tools:
- **Generate Part Set** (single set)
- **Generate Massive Part Sets** (bulk sets)

## 1) Single Set Generator

Open:
- `Blade Spinners → Generate Part Set`

Fields:
- **Set Name**: Base name used for each part asset (example: `Pegasus`)
- **Seed**: Controls deterministic stat/mesh randomization
- **Set Rarity**: Applies a rarity stat boost to generated values
- **Main Color**: Base color used to derive cohesive part colors
- **Face Bolt Emblem**: Optional sprite assigned to the generated Face Bolt

Output:
- 5 `BeyPart` assets created/overwritten:
  - `Tip`
  - `Track`
  - `FusionWheel`
  - `EnergyRing`
  - `FaceBolt`

## 2) Massive Part Set Generator

Open:
- `Blade Spinners → Generate Massive Part Sets`

This tool now uses a **resizable list of set entries**.

Fields:
- **Set Entry Count**: Changes list size
- **Randomize Base Seed**: Base value used when randomizing the whole list
- **Randomize All Seeds**: Randomizes every entry seed in the list
- For each entry:
  - **Name**
  - **Seed**
  - **Rarity**
  - **Emblem**
  - **Color**

Each list entry generates one full 5-part set using that entry's exact values.

Performance behavior:
- Bulk generation defers asset save/refresh until the full batch is done for faster creation of large volumes.

## 3) Face Bolt Emblems

`BeyPart` now includes an optional **Face Bolt Emblem** sprite field.

Current use:
- Per-Face-Bolt identity icon art

Planned use:
- Reuse the same emblem sprite as the special-attack hologram visual above the Bey.

## 4) Naming and Organization

Generated assets are stored under:
- `Assets/Parts/Tips`
- `Assets/Parts/Tracks`
- `Assets/Parts/Fusion Wheels`
- `Assets/Parts/Energy Rings`
- `Assets/Parts/Face Bolts`

Bulk generation reuses these shared part-type folders and does not create per-set folders.

If an asset with the same name already exists, it is replaced.

## 5) Recommended Workflow

1. Import emblem sprites for your Face Bolts.
2. Run **Generate Massive Part Sets** for enemy/content pools.
3. Use **Generate Part Set** for hero or named signature Beys.
4. Hook `FaceBoltEmblem` into your future hologram VFX system for ability activation.
