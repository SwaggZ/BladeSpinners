namespace BladeSpinners.Core
{
    /// <summary>
    /// Defines the 5 part slot types in the Beyblade configuration system.
    /// </summary>
    public enum PartType
    {
        Tip,
        Track,
        FusionWheel,
        EnergyRing,
        FaceBolt
    }

    /// <summary>
    /// All available Tip behaviors. These control movement characteristics.
    /// </summary>
    public enum TipBehaviorType
    {
        Flat,        // Fast, aggressive, low grip, wide momentum carry
        Sharp,       // Slow drifting, maximum stamina conservation
        Round,       // Orbits own axis, unpredictable arcs
        RubberFlat,  // High grip, tight arcs, highest behavior drain
        Ball,        // Balanced grip, slight tilt toward movement, good uphill
        Spike,       // Nearly stationary, maximum stamina drain resistance
        Orbit,       // Small local orbit around a forward-moving anchor

        // Metal Fight-inspired tip catalog (code suffix preserved)
        WideDefense_WD,
        Quake_Q,
        EternalSharp_ES,
        WideDefense2_W2D,
        MetalSharp_MS,
        EternalDefenseSharp_EDS,
        SemiFlat_SF,
        MetalBall_MB,
        BearingSpike_BS,
        SemiDefense_SD,
        HoleFlat_HF,
        DefenseSharp_DS,
        Sharp_S,
        FlatSharp_FS,
        Ball_B,
        RubberSharp_RS,
        Flat_F,
        Defense_D,
        Rubber2Flat_R2F,
        EternalWideDefense_EWD,
        DeltaDrive_D_D, // D:D
        CoatSharp_CS,
        BearingDrive_B_D, // B:D

        // Curated aliases/variants used by current visual catalog
        WideFlat_WF,
        RubberBall_RB,
        HoleFlatSharp_HF_S, // HF/S
        Fusion_F
    }

    /// <summary>
    /// Rarity tiers that affect stat ceilings and visual appearance.
    /// </summary>
    public enum RarityTier
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Room types in a dungeon layout.
    /// </summary>
    public enum RoomType
    {
        Start,      // Starting point, guaranteed safe
        Combat,     // Enemies must be cleared to unlock exit
        Loot,       // Parts available, no enemies
        Workshop,   // Player can customize parts
        Boss,       // Named enemy Bey with unique config
        Secret,     // Hidden, requires finding secret exit
        Exit,       // End of dungeon
        TreasureChest // Guaranteed high-value pickup
    }

    /// <summary>
    /// Pickup types that can spawn in maps.
    /// </summary>
    public enum PickupType
    {
        SpinSmall,        // Restores small spin amount
        SpinMedium,       // Restores medium spin amount
        SpinLarge,        // Restores large spin amount
        Mana              // Restores mana
    }

    /// <summary>
    /// Enemy AI archetypes that influence behavior weighting and part generation.
    /// </summary>
    public enum EnemyArchetype
    {
        Aggressive,  // Favors Aggression state, heavy Fusion Wheel, flat Tips
        Stamina,     // Favors StaminaConservation state, light Fusion Wheel, sharp Tips
        Balanced,    // Equal weighting across states
        Gimmick,     // Unusual part combinations
        Defense      // Favors Reposition state, medium weight
    }

    /// <summary>
    /// Dungeon themes that determine available chunks, enemies, and part drops.
    /// </summary>
    public enum DungeonTheme
    {
        ClassicStadium,
        RockyOutdoor,
        MechanicalFactory,
        IcyTundra,
        InfernoVolcano,
        CyberNetscape
    }

    /// <summary>
    /// Part tags for building synergistic setups.
    /// </summary>
    [System.Flags]
    public enum PartTag
    {
        Aggressive = 1 << 0,
        Stamina = 1 << 1,
        Gimmick = 1 << 2,
        Balanced = 1 << 3,
        Defense = 1 << 4,
        Hybrid = 1 << 5,
        ThresholdBased = 1 << 6,
        LongStamina = 1 << 7,
        HighWeight = 1 << 8,
        LowWeight = 1 << 9,
        HighMana = 1 << 10,
        LowMana = 1 << 11
    }

    /// <summary>
    /// Ability rarity that affects power and cost.
    /// </summary>
    public enum AbilityRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    /// <summary>
    /// Direction axis for steering input.
    /// </summary>
    public enum MovementAxis
    {
        Forward,
        Left,
        Right,
        Brake,
        Jump,
        Ability
    }
}
