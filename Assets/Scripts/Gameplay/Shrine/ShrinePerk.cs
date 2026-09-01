using System.Collections.Generic;
using UnityEngine;

namespace BladeSpinners.Gameplay.Shrine
{
    public enum ShrinePerkType
    {
        TitaniumTip,
        HeavyweightCore,
        BladeSharpening,
        RubberDampener,
        ManaSiphon,
        ApexTraction,
        HyperDrift,
        QuickstepLaunch,
        StaminaConservation,
        SpikeTread,
        KineticBattery,
        BalancedGyro,
        StreamlinedChassis,
        SparkIgnition,
        LightAlloyWeight,
        FrictionPad,
        SteelBumper,
        EdgePolishing,
        ManaCapacitor,
        TractionGrip,
        BronzeStriker,
        BronzeGuard,
        BronzeSprinter,
        BronzeBattery,
        BronzeGyro,
        BronzeEdge,
        BronzeTread,
        BronzeShield,
        BronzeCell,
        BronzeFin,
        BronzeScraper,
        BronzeAnchor,
        SteelStriker,
        SteelGuard,
        SteelSprinter,
        SteelBattery,
        SteelGyro,
        SteelEdge,
        SteelTread,
        SteelShield,
        SteelCell,
        SteelFin,
        SteelScraper,
        SteelAnchor,
        CopperStriker,
        CopperGuard,
        CopperSprinter,
        CopperBattery,
        CopperGyro,
        CopperEdge,
        CopperTread,
        CopperShield,
        CopperCell,
        CopperFin,
        CopperScraper,
        CopperAnchor,
        IronStriker,
        IronGuard,
        IronSprinter,
        IronBattery,
        IronGyro,
        IronEdge,
        IronTread,
        IronShield,
        IronCell,
        IronFin,
        IronScraper,
        IronAnchor,
        CarbonStriker,
        CarbonGuard,
        CarbonSprinter,
        CarbonBattery,
        CarbonGyro,
        CarbonEdge,
        CarbonTread,
        CarbonShield,
        CarbonCell,
        CarbonFin,
        CarbonScraper,
        CarbonAnchor,
        AlloyStriker,
        AlloyGuard,
        AlloySprinter,
        AlloyBattery,
        AlloyGyro,
        AlloyEdge,
        AlloyTread,
        AlloyShield,
        AlloyCell,
        AlloyFin,
        AlloyScraper,
        AlloyAnchor,
        QuickStriker,
        QuickGuard,
        QuickSprinter,
        QuickBattery,
        QuickGyro,
        QuickEdge,
        QuickTread,
        QuickShield,
        QuickCell,
        QuickFin,
        QuickScraper,
        QuickAnchor,
        StoutStriker,
        StoutGuard,
        StoutSprinter,
        StoutBattery,
        StoutGyro,
        StoutEdge,
        StoutTread,
        StoutShield,
        StoutCell,
        StoutFin,
        StoutScraper,
        StoutAnchor,
        SharpStriker,
        SharpGuard,
        SharpSprinter,
        SharpBattery,
        SharpGyro,
        SharpEdge,
        SharpTread,
        SharpShield,
        SharpCell,
        SharpFin,
        SharpScraper,
        SharpAnchor,
        AgileStriker,
        AgileGuard,
        AgileSprinter,
        AgileBattery,
        AgileGyro,
        AgileEdge,
        AgileTread,
        AgileShield,
        AgileCell,
        AgileFin,
        AgileScraper,
        AgileAnchor,
        FirmStriker,
        FirmGuard,
        FirmSprinter,
        FirmBattery,
        FirmGyro,
        FirmEdge,
        FirmTread,
        FirmShield,
        FirmCell,
        FirmFin,
        FirmScraper,
        FirmAnchor,
        SturdyStriker,
        SturdyGuard,
        SturdySprinter,
        SturdyBattery,
        SturdyGyro,
        SturdyEdge,
        SturdyTread,
        SturdyShield,
        SturdyCell,
        SturdyFin,
        SturdyScraper,
        SturdyAnchor,
        SolidStriker,
        SolidGuard,
        SolidSprinter,
        SolidBattery,
        SolidGyro,
        SolidEdge,
        SolidTread,
        SolidShield,
        SolidCell,
        SolidFin,
        SolidScraper,
        SolidAnchor,
        GripStriker,
        GripGuard,
        GripSprinter,
        GripBattery,
        GripGyro,
        GripEdge,
        GripTread,
        GripShield,
        GripCell,
        GripFin,
        GripScraper,
        GripAnchor,
        TorqueStriker,
        TorqueGuard,
        TorqueSprinter,
        TorqueBattery,
        TorqueGyro,
        TorqueEdge,
        TorqueTread,
        TorqueShield,
        TorqueCell,
        TorqueFin,
        TorqueScraper,
        TorqueAnchor,
        TurboRip,
        MagnetoRing,
        ImpactAbsorber,
        CentrifugalClutch,
        FlankStriker,
        IronPlating,
        SlipstreamSurge,
        OverchargeCapacitor,
        CounterWeight,
        ResonantVibration,
        SerratedRing,
        HydraulicSpring,
        PyroclasticSparks,
        HeavyAlloyRing,
        FluxCapacitor,
        AeroFinStabilizer,
        ShockwaveDisperser,
        SpurStriker,
        KineticSiphon,
        GildedFlywheel,
        ChilledPerimeter,
        ReactiveArmor,
        ManaSurgeRelay,
        VortexImpeller,
        DualEdgeCutter,
        TungstenCore,
        SpiritDynamo,
        OverdriveIgniter,
        ArmorPiercer,
        StabilizerWeights,
        ChargeRelay,
        TornadoBlade,
        PrismReflector,
        NitroBooster,
        LeechFangs,
        CapacitorBank,
        IroncladTip,
        MomentumKeeper,
        ShatterStrike,
        HeavyInertia,
        TeslaDischarge,
        NanoMeshArmor,
        ManaOverclock,
        RallySpur,
        SpeedSkater,
        BastionRing,
        EnergyHarvester,
        BladeHone,
        VibroDampener,
        CentrifugalSurge,
        CryoCoating,
        MagneticRepulsor,
        DynamoWheel,
        GravityAnchor,
        OverdriveIgnition,
        OrbitStriker,
        ElasticHub,
        ThermalPlating,
        ManaCapacitorII,
        BladeSerrated,
        FlashStepDampener,
        TurbineExhaust,
        KineticAbsorber,
        ThunderSpark,
        PrismCapacitor,
        ApexClimber,
        ReflectiveEdge,
        GyroStabilizer,
        ManaSurgeCore,
        CycloneFin,
        BatteringRam,
        AeroSkate,
        StaticBurst,
        ArmorMesh,
        ManaTransducer,
        VortexClutch,
        SeismicPulse,
        FortressRing,
        EnergyPrismII,
        SwiftThrust,
        BloodSpur,
        TitaniumPlating,
        ManaBattery,
        DriftMaster,
        StaggerStrike,
        ReinforcedHub,
        OverclockRegen,
        SlipstreamMaster,
        PiercingContact,
        DeflectionShield,
        SpiritSiphon,
        TurboThrust,
        ImpactCrusher,
        IronCore,
        ManaConduitII,
        AeroDynamics,
        ViperFangs,
        BulwarkPlating,
        CapacitorCore,
        SonicImpulse,
        VampiricSpin,
        StaticOverload,
        InfernoAura,
        CycloneVortex,
        IronFortress,
        SpiritSurge,
        RazorEdge,
        VortexShield,
        SeismicSlam,
        ArcaneResonance,
        PhantomDash,
        GlacialArmor,
        TempestDrive,
        SoulReaper,
        ObsidianCore,
        ManaConduit,
        Thunderstrike,
        MirrorPlating,
        SonicBoom,
        SolarRadiance,
        OverdriveCore,
        AegisBarrier,
        CosmicSynergy,
        TitanCrusher,
        NullifyArmor,
        GravityWell,
        SupernovaBurst,
        EternalDynamo,
        ValkyrieFlight,
        DiamondCarapace,
        PhoenixRebirth,
        PlasmaCoil,
        ChronosShift,
        DragonHeart,
        CelestialSingularity
    }

    public enum PerkRarity
    {
        Common,      // #007DCC Bright Teal Blue
        Uncommon,    // #4BCC00 Lime Green
        Rare,        // #DF0000 Racing Red
        Epic,        // #B0008A Raspberry Plum
        Legendary    // #FFB900 Amber Flame
    }

    public enum PerkCategory
    {
        Combat,
        Mobility,
        Energy,
        Defense
    }

    public class ShrinePerkData
    {
        public ShrinePerkType Type { get; }
        public string Name { get; }
        public string JapaneseName { get; }
        public string Description { get; }
        public PerkCategory Category { get; }
        public PerkRarity Rarity { get; }
        public int BaseCost { get; }
        public string IconSymbol { get; }
        public Color ThemeColor
        {
            get
            {
                switch (Rarity)
                {
                    case PerkRarity.Common: return new Color(0.000f, 0.490f, 0.800f, 1f); // #007DCC Bright Teal Blue
                    case PerkRarity.Uncommon: return new Color(0.294f, 0.800f, 0.000f, 1f); // #4BCC00 Lime Green
                    case PerkRarity.Rare: return new Color(0.875f, 0.000f, 0.000f, 1f); // #DF0000 Racing Red
                    case PerkRarity.Epic: return new Color(0.690f, 0.000f, 0.541f, 1f); // #B0008A Raspberry Plum
                    case PerkRarity.Legendary: return new Color(1.000f, 0.725f, 0.000f, 1f); // #FFB900 Amber Flame
                    default: return new Color(0.000f, 0.490f, 0.800f, 1f);
                }
            }
        }

        public ShrinePerkData(
            ShrinePerkType type,
            string name,
            string japaneseName,
            string description,
            PerkCategory category,
            PerkRarity rarity,
            int baseCost,
            string iconSymbol,
            Color themeColor = default)
        {
            Type = type;
            Name = name;
            JapaneseName = japaneseName;
            Description = description;
            Category = category;
            Rarity = rarity;
            BaseCost = baseCost;
            IconSymbol = iconSymbol;
        }
    }

    public static class ShrinePerkCatalog
    {
        private static readonly Dictionary<ShrinePerkType, ShrinePerkData> perks = new Dictionary<ShrinePerkType, ShrinePerkData>
        {
            {
                ShrinePerkType.TitaniumTip,
                new ShrinePerkData(
                    ShrinePerkType.TitaniumTip,
                    "TITANIUM TIP",
                    "チタン製軸先",
                    "35% reduced friction decay on bowl slopes. Enhanced drift acceleration.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "TT"
                )
            },
            {
                ShrinePerkType.HeavyweightCore,
                new ShrinePerkData(
                    ShrinePerkType.HeavyweightCore,
                    "HEAVYWEIGHT CORE",
                    "重装甲コア",
                    "+25 Mass Weight. Increases wall-bounce elastic reflection force by 30%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    240,
                    "HC"
                )
            },
            {
                ShrinePerkType.BladeSharpening,
                new ShrinePerkData(
                    ShrinePerkType.BladeSharpening,
                    "BLADE SHARPENING",
                    "刃先研磨",
                    "Honed contact points deliver +14% flat bonus spin damage on all collisions.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    230,
                    "BS"
                )
            },
            {
                ShrinePerkType.RubberDampener,
                new ShrinePerkData(
                    ShrinePerkType.RubberDampener,
                    "RUBBER DAMPENER",
                    "衝撃吸収護謨",
                    "Absorbs incoming collision recoil by 35%, preventing outward knockback displacement.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    200,
                    "RD"
                )
            },
            {
                ShrinePerkType.ManaSiphon,
                new ShrinePerkData(
                    ShrinePerkType.ManaSiphon,
                    "MANA SIPHON",
                    "霊力吸収",
                    "Every 1 second of spinning naturally siphons +3 bonus Mana from the stadium atmosphere.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    220,
                    "MS"
                )
            },
            {
                ShrinePerkType.ApexTraction,
                new ShrinePerkData(
                    ShrinePerkType.ApexTraction,
                    "APEX TRACTION",
                    "頂点摩擦力",
                    "Prevents slipping on arena rim edges and increases uphill climbing torque by +30%.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    210,
                    "AT"
                )
            },
            {
                ShrinePerkType.HyperDrift,
                new ShrinePerkData(
                    ShrinePerkType.HyperDrift,
                    "HYPER DRIFT",
                    "超機動滑走",
                    "Steering into curved bowl slopes grants 25% bonus speed and +40% drift momentum.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "HD"
                )
            },
            {
                ShrinePerkType.QuickstepLaunch,
                new ShrinePerkData(
                    ShrinePerkType.QuickstepLaunch,
                    "QUICKSTEP LAUNCH",
                    "瞬速発進",
                    "+25% initial burst launch speed and 40% faster acceleration recovery from stops.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    190,
                    "QL"
                )
            },
            {
                ShrinePerkType.StaminaConservation,
                new ShrinePerkData(
                    ShrinePerkType.StaminaConservation,
                    "STAMINA CONSERVATION",
                    "持久保全",
                    "Aerodynamic balancing reduces passive baseline spin stamina friction decay by 20%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    230,
                    "SC"
                )
            },
            {
                ShrinePerkType.SpikeTread,
                new ShrinePerkData(
                    ShrinePerkType.SpikeTread,
                    "SPIKE TREAD",
                    "突起踏面",
                    "Micro-toothed perimeter scratches opponents during glancing contacts, shaving 6 spin.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    210,
                    "ST"
                )
            },
            {
                ShrinePerkType.KineticBattery,
                new ShrinePerkData(
                    ShrinePerkType.KineticBattery,
                    "KINETIC BATTERY",
                    "運動電池",
                    "Hard wall rebounds and dish impacts convert kinetic energy directly into +5 instant Mana.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    200,
                    "KB"
                )
            },
            {
                ShrinePerkType.BalancedGyro,
                new ShrinePerkData(
                    ShrinePerkType.BalancedGyro,
                    "BALANCED GYRO",
                    "平衡独楽",
                    "Internal gyroscopic stabilizers reduce destabilizing wobble and tilt by 45%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    210,
                    "BG"
                )
            },
            {
                ShrinePerkType.StreamlinedChassis,
                new ShrinePerkData(
                    ShrinePerkType.StreamlinedChassis,
                    "STREAMLINED CHASSIS",
                    "流線型胴体",
                    "Reduces aerodynamic dish drag, increasing flat cruising top speed by +15%.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    190,
                    "Sl"
                )
            },
            {
                ShrinePerkType.SparkIgnition,
                new ShrinePerkData(
                    ShrinePerkType.SparkIgnition,
                    "SPARK IGNITION",
                    "火花点火",
                    "Contact friction ignites blinding sparks on opponents, disorienting their steering for 0.4s.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    240,
                    "SI"
                )
            },
            {
                ShrinePerkType.LightAlloyWeight,
                new ShrinePerkData(
                    ShrinePerkType.LightAlloyWeight,
                    "LIGHT ALLOY WEIGHT",
                    "軽量合金錘",
                    "+12 Mass Weight with zero penalty to acceleration.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    200,
                    "LW"
                )
            },
            {
                ShrinePerkType.FrictionPad,
                new ShrinePerkData(
                    ShrinePerkType.FrictionPad,
                    "FRICTION PAD",
                    "摩擦受板",
                    "Increases braking grip force by +40% when pulling back steering.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    185,
                    "FP"
                )
            },
            {
                ShrinePerkType.SteelBumper,
                new ShrinePerkData(
                    ShrinePerkType.SteelBumper,
                    "STEEL BUMPER",
                    "鋼鉄緩衝器",
                    "+15 Base Defense against perimeter wall impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "SB"
                )
            },
            {
                ShrinePerkType.EdgePolishing,
                new ShrinePerkData(
                    ShrinePerkType.EdgePolishing,
                    "EDGE POLISHING",
                    "刃縁研磨",
                    "+11% damage dealt when striking opponents at high velocity.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    225,
                    "EP"
                )
            },
            {
                ShrinePerkType.ManaCapacitor,
                new ShrinePerkData(
                    ShrinePerkType.ManaCapacitor,
                    "MANA CAPACITOR",
                    "小型蓄電器",
                    "Increases maximum Mana capacity pool by +16.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    235,
                    "MC"
                )
            },
            {
                ShrinePerkType.TractionGrip,
                new ShrinePerkData(
                    ShrinePerkType.TractionGrip,
                    "TRACTION GRIP",
                    "高摩擦保持",
                    "30% reduced slide slip when turning sharply on dish incline.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    195,
                    "TG"
                )
            },
            {
                ShrinePerkType.BronzeStriker,
                new ShrinePerkData(
                    ShrinePerkType.BronzeStriker,
                    "BRONZE STRIKER",
                    "青銅の強襲刃",
                    "Deals +8% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    200,
                    "BS"
                )
            },
            {
                ShrinePerkType.BronzeGuard,
                new ShrinePerkData(
                    ShrinePerkType.BronzeGuard,
                    "BRONZE GUARD",
                    "青銅の防護甲",
                    "+10 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    190,
                    "BG"
                )
            },
            {
                ShrinePerkType.BronzeSprinter,
                new ShrinePerkData(
                    ShrinePerkType.BronzeSprinter,
                    "BRONZE SPRINTER",
                    "青銅の疾走機",
                    "+10% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    185,
                    "BS"
                )
            },
            {
                ShrinePerkType.BronzeBattery,
                new ShrinePerkData(
                    ShrinePerkType.BronzeBattery,
                    "BRONZE BATTERY",
                    "青銅の蓄電器",
                    "Restores +3 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    190,
                    "BB"
                )
            },
            {
                ShrinePerkType.BronzeGyro,
                new ShrinePerkData(
                    ShrinePerkType.BronzeGyro,
                    "BRONZE GYRO",
                    "青銅の平衡独楽",
                    "Reduces tilt wobble by 29% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "BG"
                )
            },
            {
                ShrinePerkType.BronzeEdge,
                new ShrinePerkData(
                    ShrinePerkType.BronzeEdge,
                    "BRONZE EDGE",
                    "青銅の鋭刃",
                    "+10% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    225,
                    "BE"
                )
            },
            {
                ShrinePerkType.BronzeTread,
                new ShrinePerkData(
                    ShrinePerkType.BronzeTread,
                    "BRONZE TREAD",
                    "青銅の接地爪",
                    "+20% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    190,
                    "BT"
                )
            },
            {
                ShrinePerkType.BronzeShield,
                new ShrinePerkData(
                    ShrinePerkType.BronzeShield,
                    "BRONZE SHIELD",
                    "青銅の防壁",
                    "Reduces damage taken from glancing scrapes by 20%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    210,
                    "BS"
                )
            },
            {
                ShrinePerkType.BronzeCell,
                new ShrinePerkData(
                    ShrinePerkType.BronzeCell,
                    "BRONZE CELL",
                    "青銅の魔力管",
                    "Increases maximum Mana capacity pool by +15.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    210,
                    "BC"
                )
            },
            {
                ShrinePerkType.BronzeFin,
                new ShrinePerkData(
                    ShrinePerkType.BronzeFin,
                    "BRONZE FIN",
                    "青銅の整流翼",
                    "+25% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    210,
                    "BF"
                )
            },
            {
                ShrinePerkType.BronzeScraper,
                new ShrinePerkData(
                    ShrinePerkType.BronzeScraper,
                    "BRONZE SCRAPER",
                    "青銅の削剥刃",
                    "Glancing side hits shave off +6 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    225,
                    "BS"
                )
            },
            {
                ShrinePerkType.BronzeAnchor,
                new ShrinePerkData(
                    ShrinePerkType.BronzeAnchor,
                    "BRONZE ANCHOR",
                    "青銅の重碇",
                    "+16 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    235,
                    "BA"
                )
            },
            {
                ShrinePerkType.SteelStriker,
                new ShrinePerkData(
                    ShrinePerkType.SteelStriker,
                    "STEEL STRIKER",
                    "鋼鉄の強襲刃",
                    "Deals +14% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "SS"
                )
            },
            {
                ShrinePerkType.SteelGuard,
                new ShrinePerkData(
                    ShrinePerkType.SteelGuard,
                    "STEEL GUARD",
                    "鋼鉄の防護甲",
                    "+19 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "SG"
                )
            },
            {
                ShrinePerkType.SteelSprinter,
                new ShrinePerkData(
                    ShrinePerkType.SteelSprinter,
                    "STEEL SPRINTER",
                    "鋼鉄の疾走機",
                    "+17% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "SS"
                )
            },
            {
                ShrinePerkType.SteelBattery,
                new ShrinePerkData(
                    ShrinePerkType.SteelBattery,
                    "STEEL BATTERY",
                    "鋼鉄の蓄電器",
                    "Restores +6 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    220,
                    "SB"
                )
            },
            {
                ShrinePerkType.SteelGyro,
                new ShrinePerkData(
                    ShrinePerkType.SteelGyro,
                    "STEEL GYRO",
                    "鋼鉄の平衡独楽",
                    "Reduces tilt wobble by 43% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    245,
                    "SG"
                )
            },
            {
                ShrinePerkType.SteelEdge,
                new ShrinePerkData(
                    ShrinePerkType.SteelEdge,
                    "STEEL EDGE",
                    "鋼鉄の鋭刃",
                    "+17% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    255,
                    "SE"
                )
            },
            {
                ShrinePerkType.SteelTread,
                new ShrinePerkData(
                    ShrinePerkType.SteelTread,
                    "STEEL TREAD",
                    "鋼鉄の接地爪",
                    "+31% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    225,
                    "ST"
                )
            },
            {
                ShrinePerkType.SteelShield,
                new ShrinePerkData(
                    ShrinePerkType.SteelShield,
                    "STEEL SHIELD",
                    "鋼鉄の防壁",
                    "Reduces damage taken from glancing scrapes by 31%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    240,
                    "SS"
                )
            },
            {
                ShrinePerkType.SteelCell,
                new ShrinePerkData(
                    ShrinePerkType.SteelCell,
                    "STEEL CELL",
                    "鋼鉄の魔力管",
                    "Increases maximum Mana capacity pool by +10.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    190,
                    "SC"
                )
            },
            {
                ShrinePerkType.SteelFin,
                new ShrinePerkData(
                    ShrinePerkType.SteelFin,
                    "STEEL FIN",
                    "鋼鉄の整流翼",
                    "+17% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    185,
                    "SF"
                )
            },
            {
                ShrinePerkType.SteelScraper,
                new ShrinePerkData(
                    ShrinePerkType.SteelScraper,
                    "STEEL SCRAPER",
                    "鋼鉄の削剥刃",
                    "Glancing side hits shave off +4 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    205,
                    "SS"
                )
            },
            {
                ShrinePerkType.SteelAnchor,
                new ShrinePerkData(
                    ShrinePerkType.SteelAnchor,
                    "STEEL ANCHOR",
                    "鋼鉄の重碇",
                    "+11 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "SA"
                )
            },
            {
                ShrinePerkType.CopperStriker,
                new ShrinePerkData(
                    ShrinePerkType.CopperStriker,
                    "COPPER STRIKER",
                    "銅製の強襲刃",
                    "Deals +10% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    210,
                    "CS"
                )
            },
            {
                ShrinePerkType.CopperGuard,
                new ShrinePerkData(
                    ShrinePerkType.CopperGuard,
                    "COPPER GUARD",
                    "銅製の防護甲",
                    "+13 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    205,
                    "CG"
                )
            },
            {
                ShrinePerkType.CopperSprinter,
                new ShrinePerkData(
                    ShrinePerkType.CopperSprinter,
                    "COPPER SPRINTER",
                    "銅製の疾走機",
                    "+12% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    195,
                    "CS"
                )
            },
            {
                ShrinePerkType.CopperBattery,
                new ShrinePerkData(
                    ShrinePerkType.CopperBattery,
                    "COPPER BATTERY",
                    "銅製の蓄電器",
                    "Restores +4 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    200,
                    "CB"
                )
            },
            {
                ShrinePerkType.CopperGyro,
                new ShrinePerkData(
                    ShrinePerkType.CopperGyro,
                    "COPPER GYRO",
                    "銅製の平衡独楽",
                    "Reduces tilt wobble by 34% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "CG"
                )
            },
            {
                ShrinePerkType.CopperEdge,
                new ShrinePerkData(
                    ShrinePerkType.CopperEdge,
                    "COPPER EDGE",
                    "銅製の鋭刃",
                    "+12% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "CE"
                )
            },
            {
                ShrinePerkType.CopperTread,
                new ShrinePerkData(
                    ShrinePerkType.CopperTread,
                    "COPPER TREAD",
                    "銅製の接地爪",
                    "+24% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    205,
                    "CT"
                )
            },
            {
                ShrinePerkType.CopperShield,
                new ShrinePerkData(
                    ShrinePerkType.CopperShield,
                    "COPPER SHIELD",
                    "銅製の防壁",
                    "Reduces damage taken from glancing scrapes by 23%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    220,
                    "CS"
                )
            },
            {
                ShrinePerkType.CopperCell,
                new ShrinePerkData(
                    ShrinePerkType.CopperCell,
                    "COPPER CELL",
                    "銅製の魔力管",
                    "Increases maximum Mana capacity pool by +18.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    225,
                    "CC"
                )
            },
            {
                ShrinePerkType.CopperFin,
                new ShrinePerkData(
                    ShrinePerkType.CopperFin,
                    "COPPER FIN",
                    "銅製の整流翼",
                    "+29% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "CF"
                )
            },
            {
                ShrinePerkType.CopperScraper,
                new ShrinePerkData(
                    ShrinePerkType.CopperScraper,
                    "COPPER SCRAPER",
                    "銅製の削剥刃",
                    "Glancing side hits shave off +7 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "CS"
                )
            },
            {
                ShrinePerkType.CopperAnchor,
                new ShrinePerkData(
                    ShrinePerkType.CopperAnchor,
                    "COPPER ANCHOR",
                    "銅製の重碇",
                    "+19 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    245,
                    "CA"
                )
            },
            {
                ShrinePerkType.IronStriker,
                new ShrinePerkData(
                    ShrinePerkType.IronStriker,
                    "IRON STRIKER",
                    "鉄製の強襲刃",
                    "Deals +16% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "IS"
                )
            },
            {
                ShrinePerkType.IronGuard,
                new ShrinePerkData(
                    ShrinePerkType.IronGuard,
                    "IRON GUARD",
                    "鉄製の防護甲",
                    "+21 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    240,
                    "IG"
                )
            },
            {
                ShrinePerkType.IronSprinter,
                new ShrinePerkData(
                    ShrinePerkType.IronSprinter,
                    "IRON SPRINTER",
                    "鉄製の疾走機",
                    "+19% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    230,
                    "IS"
                )
            },
            {
                ShrinePerkType.IronBattery,
                new ShrinePerkData(
                    ShrinePerkType.IronBattery,
                    "IRON BATTERY",
                    "鉄製の蓄電器",
                    "Restores +6 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    230,
                    "IB"
                )
            },
            {
                ShrinePerkType.IronGyro,
                new ShrinePerkData(
                    ShrinePerkType.IronGyro,
                    "IRON GYRO",
                    "鉄製の平衡独楽",
                    "Reduces tilt wobble by 25% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    205,
                    "IG"
                )
            },
            {
                ShrinePerkType.IronEdge,
                new ShrinePerkData(
                    ShrinePerkType.IronEdge,
                    "IRON EDGE",
                    "鉄製の鋭刃",
                    "+7% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    215,
                    "IE"
                )
            },
            {
                ShrinePerkType.IronTread,
                new ShrinePerkData(
                    ShrinePerkType.IronTread,
                    "IRON TREAD",
                    "鉄製の接地爪",
                    "+16% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    180,
                    "IT"
                )
            },
            {
                ShrinePerkType.IronShield,
                new ShrinePerkData(
                    ShrinePerkType.IronShield,
                    "IRON SHIELD",
                    "鉄製の防壁",
                    "Reduces damage taken from glancing scrapes by 16%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    200,
                    "IS"
                )
            },
            {
                ShrinePerkType.IronCell,
                new ShrinePerkData(
                    ShrinePerkType.IronCell,
                    "IRON CELL",
                    "鉄製の魔力管",
                    "Increases maximum Mana capacity pool by +12.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    200,
                    "IC"
                )
            },
            {
                ShrinePerkType.IronFin,
                new ShrinePerkData(
                    ShrinePerkType.IronFin,
                    "IRON FIN",
                    "鉄製の整流翼",
                    "+21% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    200,
                    "IF"
                )
            },
            {
                ShrinePerkType.IronScraper,
                new ShrinePerkData(
                    ShrinePerkType.IronScraper,
                    "IRON SCRAPER",
                    "鉄製の削剥刃",
                    "Glancing side hits shave off +5 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    215,
                    "IS"
                )
            },
            {
                ShrinePerkType.IronAnchor,
                new ShrinePerkData(
                    ShrinePerkType.IronAnchor,
                    "IRON ANCHOR",
                    "鉄製の重碇",
                    "+14 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "IA"
                )
            },
            {
                ShrinePerkType.CarbonStriker,
                new ShrinePerkData(
                    ShrinePerkType.CarbonStriker,
                    "CARBON STRIKER",
                    "炭素の強襲刃",
                    "Deals +12% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    220,
                    "CS"
                )
            },
            {
                ShrinePerkType.CarbonGuard,
                new ShrinePerkData(
                    ShrinePerkType.CarbonGuard,
                    "CARBON GUARD",
                    "炭素の防護甲",
                    "+16 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "CG"
                )
            },
            {
                ShrinePerkType.CarbonSprinter,
                new ShrinePerkData(
                    ShrinePerkType.CarbonSprinter,
                    "CARBON SPRINTER",
                    "炭素の疾走機",
                    "+15% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    210,
                    "CS"
                )
            },
            {
                ShrinePerkType.CarbonBattery,
                new ShrinePerkData(
                    ShrinePerkType.CarbonBattery,
                    "CARBON BATTERY",
                    "炭素の蓄電器",
                    "Restores +5 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    210,
                    "CB"
                )
            },
            {
                ShrinePerkType.CarbonGyro,
                new ShrinePerkData(
                    ShrinePerkType.CarbonGyro,
                    "CARBON GYRO",
                    "炭素の平衡独楽",
                    "Reduces tilt wobble by 38% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    235,
                    "CG"
                )
            },
            {
                ShrinePerkType.CarbonEdge,
                new ShrinePerkData(
                    ShrinePerkType.CarbonEdge,
                    "CARBON EDGE",
                    "炭素の鋭刃",
                    "+14% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "CE"
                )
            },
            {
                ShrinePerkType.CarbonTread,
                new ShrinePerkData(
                    ShrinePerkType.CarbonTread,
                    "CARBON TREAD",
                    "炭素の接地爪",
                    "+27% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    215,
                    "CT"
                )
            },
            {
                ShrinePerkType.CarbonShield,
                new ShrinePerkData(
                    ShrinePerkType.CarbonShield,
                    "CARBON SHIELD",
                    "炭素の防壁",
                    "Reduces damage taken from glancing scrapes by 27%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    230,
                    "CS"
                )
            },
            {
                ShrinePerkType.CarbonCell,
                new ShrinePerkData(
                    ShrinePerkType.CarbonCell,
                    "CARBON CELL",
                    "炭素の魔力管",
                    "Increases maximum Mana capacity pool by +21.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    235,
                    "CC"
                )
            },
            {
                ShrinePerkType.CarbonFin,
                new ShrinePerkData(
                    ShrinePerkType.CarbonFin,
                    "CARBON FIN",
                    "炭素の整流翼",
                    "+33% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    235,
                    "CF"
                )
            },
            {
                ShrinePerkType.CarbonScraper,
                new ShrinePerkData(
                    ShrinePerkType.CarbonScraper,
                    "CARBON SCRAPER",
                    "炭素の削剥刃",
                    "Glancing side hits shave off +8 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "CS"
                )
            },
            {
                ShrinePerkType.CarbonAnchor,
                new ShrinePerkData(
                    ShrinePerkType.CarbonAnchor,
                    "CARBON ANCHOR",
                    "炭素の重碇",
                    "+21 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    255,
                    "CA"
                )
            },
            {
                ShrinePerkType.AlloyStriker,
                new ShrinePerkData(
                    ShrinePerkType.AlloyStriker,
                    "ALLOY STRIKER",
                    "合金の強襲刃",
                    "Deals +8% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    200,
                    "AS"
                )
            },
            {
                ShrinePerkType.AlloyGuard,
                new ShrinePerkData(
                    ShrinePerkType.AlloyGuard,
                    "ALLOY GUARD",
                    "合金の防護甲",
                    "+10 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    190,
                    "AG"
                )
            },
            {
                ShrinePerkType.AlloySprinter,
                new ShrinePerkData(
                    ShrinePerkType.AlloySprinter,
                    "ALLOY SPRINTER",
                    "合金の疾走機",
                    "+10% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    185,
                    "AS"
                )
            },
            {
                ShrinePerkType.AlloyBattery,
                new ShrinePerkData(
                    ShrinePerkType.AlloyBattery,
                    "ALLOY BATTERY",
                    "合金の蓄電器",
                    "Restores +3 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    190,
                    "AB"
                )
            },
            {
                ShrinePerkType.AlloyGyro,
                new ShrinePerkData(
                    ShrinePerkType.AlloyGyro,
                    "ALLOY GYRO",
                    "合金の平衡独楽",
                    "Reduces tilt wobble by 29% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "AG"
                )
            },
            {
                ShrinePerkType.AlloyEdge,
                new ShrinePerkData(
                    ShrinePerkType.AlloyEdge,
                    "ALLOY EDGE",
                    "合金の鋭刃",
                    "+10% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    225,
                    "AE"
                )
            },
            {
                ShrinePerkType.AlloyTread,
                new ShrinePerkData(
                    ShrinePerkType.AlloyTread,
                    "ALLOY TREAD",
                    "合金の接地爪",
                    "+20% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    190,
                    "AT"
                )
            },
            {
                ShrinePerkType.AlloyShield,
                new ShrinePerkData(
                    ShrinePerkType.AlloyShield,
                    "ALLOY SHIELD",
                    "合金の防壁",
                    "Reduces damage taken from glancing scrapes by 20%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    210,
                    "AS"
                )
            },
            {
                ShrinePerkType.AlloyCell,
                new ShrinePerkData(
                    ShrinePerkType.AlloyCell,
                    "ALLOY CELL",
                    "合金の魔力管",
                    "Increases maximum Mana capacity pool by +15.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    210,
                    "AC"
                )
            },
            {
                ShrinePerkType.AlloyFin,
                new ShrinePerkData(
                    ShrinePerkType.AlloyFin,
                    "ALLOY FIN",
                    "合金の整流翼",
                    "+25% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    210,
                    "AF"
                )
            },
            {
                ShrinePerkType.AlloyScraper,
                new ShrinePerkData(
                    ShrinePerkType.AlloyScraper,
                    "ALLOY SCRAPER",
                    "合金の削剥刃",
                    "Glancing side hits shave off +6 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    225,
                    "AS"
                )
            },
            {
                ShrinePerkType.AlloyAnchor,
                new ShrinePerkData(
                    ShrinePerkType.AlloyAnchor,
                    "ALLOY ANCHOR",
                    "合金の重碇",
                    "+16 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    235,
                    "AA"
                )
            },
            {
                ShrinePerkType.QuickStriker,
                new ShrinePerkData(
                    ShrinePerkType.QuickStriker,
                    "QUICK STRIKER",
                    "迅速の強襲刃",
                    "Deals +14% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "QS"
                )
            },
            {
                ShrinePerkType.QuickGuard,
                new ShrinePerkData(
                    ShrinePerkType.QuickGuard,
                    "QUICK GUARD",
                    "迅速の防護甲",
                    "+19 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "QG"
                )
            },
            {
                ShrinePerkType.QuickSprinter,
                new ShrinePerkData(
                    ShrinePerkType.QuickSprinter,
                    "QUICK SPRINTER",
                    "迅速の疾走機",
                    "+17% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "QS"
                )
            },
            {
                ShrinePerkType.QuickBattery,
                new ShrinePerkData(
                    ShrinePerkType.QuickBattery,
                    "QUICK BATTERY",
                    "迅速の蓄電器",
                    "Restores +6 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    220,
                    "QB"
                )
            },
            {
                ShrinePerkType.QuickGyro,
                new ShrinePerkData(
                    ShrinePerkType.QuickGyro,
                    "QUICK GYRO",
                    "迅速の平衡独楽",
                    "Reduces tilt wobble by 43% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    245,
                    "QG"
                )
            },
            {
                ShrinePerkType.QuickEdge,
                new ShrinePerkData(
                    ShrinePerkType.QuickEdge,
                    "QUICK EDGE",
                    "迅速の鋭刃",
                    "+17% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    255,
                    "QE"
                )
            },
            {
                ShrinePerkType.QuickTread,
                new ShrinePerkData(
                    ShrinePerkType.QuickTread,
                    "QUICK TREAD",
                    "迅速の接地爪",
                    "+31% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    225,
                    "QT"
                )
            },
            {
                ShrinePerkType.QuickShield,
                new ShrinePerkData(
                    ShrinePerkType.QuickShield,
                    "QUICK SHIELD",
                    "迅速の防壁",
                    "Reduces damage taken from glancing scrapes by 31%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    240,
                    "QS"
                )
            },
            {
                ShrinePerkType.QuickCell,
                new ShrinePerkData(
                    ShrinePerkType.QuickCell,
                    "QUICK CELL",
                    "迅速の魔力管",
                    "Increases maximum Mana capacity pool by +10.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    190,
                    "QC"
                )
            },
            {
                ShrinePerkType.QuickFin,
                new ShrinePerkData(
                    ShrinePerkType.QuickFin,
                    "QUICK FIN",
                    "迅速の整流翼",
                    "+17% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    185,
                    "QF"
                )
            },
            {
                ShrinePerkType.QuickScraper,
                new ShrinePerkData(
                    ShrinePerkType.QuickScraper,
                    "QUICK SCRAPER",
                    "迅速の削剥刃",
                    "Glancing side hits shave off +4 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    205,
                    "QS"
                )
            },
            {
                ShrinePerkType.QuickAnchor,
                new ShrinePerkData(
                    ShrinePerkType.QuickAnchor,
                    "QUICK ANCHOR",
                    "迅速の重碇",
                    "+11 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "QA"
                )
            },
            {
                ShrinePerkType.StoutStriker,
                new ShrinePerkData(
                    ShrinePerkType.StoutStriker,
                    "STOUT STRIKER",
                    "堅牢の強襲刃",
                    "Deals +10% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    210,
                    "SS"
                )
            },
            {
                ShrinePerkType.StoutGuard,
                new ShrinePerkData(
                    ShrinePerkType.StoutGuard,
                    "STOUT GUARD",
                    "堅牢の防護甲",
                    "+13 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    205,
                    "SG"
                )
            },
            {
                ShrinePerkType.StoutSprinter,
                new ShrinePerkData(
                    ShrinePerkType.StoutSprinter,
                    "STOUT SPRINTER",
                    "堅牢の疾走機",
                    "+12% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    195,
                    "SS"
                )
            },
            {
                ShrinePerkType.StoutBattery,
                new ShrinePerkData(
                    ShrinePerkType.StoutBattery,
                    "STOUT BATTERY",
                    "堅牢の蓄電器",
                    "Restores +4 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    200,
                    "SB"
                )
            },
            {
                ShrinePerkType.StoutGyro,
                new ShrinePerkData(
                    ShrinePerkType.StoutGyro,
                    "STOUT GYRO",
                    "堅牢の平衡独楽",
                    "Reduces tilt wobble by 34% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "SG"
                )
            },
            {
                ShrinePerkType.StoutEdge,
                new ShrinePerkData(
                    ShrinePerkType.StoutEdge,
                    "STOUT EDGE",
                    "堅牢の鋭刃",
                    "+12% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "SE"
                )
            },
            {
                ShrinePerkType.StoutTread,
                new ShrinePerkData(
                    ShrinePerkType.StoutTread,
                    "STOUT TREAD",
                    "堅牢の接地爪",
                    "+24% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    205,
                    "ST"
                )
            },
            {
                ShrinePerkType.StoutShield,
                new ShrinePerkData(
                    ShrinePerkType.StoutShield,
                    "STOUT SHIELD",
                    "堅牢の防壁",
                    "Reduces damage taken from glancing scrapes by 23%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    220,
                    "SS"
                )
            },
            {
                ShrinePerkType.StoutCell,
                new ShrinePerkData(
                    ShrinePerkType.StoutCell,
                    "STOUT CELL",
                    "堅牢の魔力管",
                    "Increases maximum Mana capacity pool by +18.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    225,
                    "SC"
                )
            },
            {
                ShrinePerkType.StoutFin,
                new ShrinePerkData(
                    ShrinePerkType.StoutFin,
                    "STOUT FIN",
                    "堅牢の整流翼",
                    "+29% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "SF"
                )
            },
            {
                ShrinePerkType.StoutScraper,
                new ShrinePerkData(
                    ShrinePerkType.StoutScraper,
                    "STOUT SCRAPER",
                    "堅牢の削剥刃",
                    "Glancing side hits shave off +7 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "SS"
                )
            },
            {
                ShrinePerkType.StoutAnchor,
                new ShrinePerkData(
                    ShrinePerkType.StoutAnchor,
                    "STOUT ANCHOR",
                    "堅牢の重碇",
                    "+19 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    245,
                    "SA"
                )
            },
            {
                ShrinePerkType.SharpStriker,
                new ShrinePerkData(
                    ShrinePerkType.SharpStriker,
                    "SHARP STRIKER",
                    "鋭利の強襲刃",
                    "Deals +16% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "SS"
                )
            },
            {
                ShrinePerkType.SharpGuard,
                new ShrinePerkData(
                    ShrinePerkType.SharpGuard,
                    "SHARP GUARD",
                    "鋭利の防護甲",
                    "+21 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    240,
                    "SG"
                )
            },
            {
                ShrinePerkType.SharpSprinter,
                new ShrinePerkData(
                    ShrinePerkType.SharpSprinter,
                    "SHARP SPRINTER",
                    "鋭利の疾走機",
                    "+19% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    230,
                    "SS"
                )
            },
            {
                ShrinePerkType.SharpBattery,
                new ShrinePerkData(
                    ShrinePerkType.SharpBattery,
                    "SHARP BATTERY",
                    "鋭利の蓄電器",
                    "Restores +6 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    230,
                    "SB"
                )
            },
            {
                ShrinePerkType.SharpGyro,
                new ShrinePerkData(
                    ShrinePerkType.SharpGyro,
                    "SHARP GYRO",
                    "鋭利の平衡独楽",
                    "Reduces tilt wobble by 25% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    205,
                    "SG"
                )
            },
            {
                ShrinePerkType.SharpEdge,
                new ShrinePerkData(
                    ShrinePerkType.SharpEdge,
                    "SHARP EDGE",
                    "鋭利の鋭刃",
                    "+7% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    215,
                    "SE"
                )
            },
            {
                ShrinePerkType.SharpTread,
                new ShrinePerkData(
                    ShrinePerkType.SharpTread,
                    "SHARP TREAD",
                    "鋭利の接地爪",
                    "+16% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    180,
                    "ST"
                )
            },
            {
                ShrinePerkType.SharpShield,
                new ShrinePerkData(
                    ShrinePerkType.SharpShield,
                    "SHARP SHIELD",
                    "鋭利の防壁",
                    "Reduces damage taken from glancing scrapes by 16%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    200,
                    "SS"
                )
            },
            {
                ShrinePerkType.SharpCell,
                new ShrinePerkData(
                    ShrinePerkType.SharpCell,
                    "SHARP CELL",
                    "鋭利の魔力管",
                    "Increases maximum Mana capacity pool by +12.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    200,
                    "SC"
                )
            },
            {
                ShrinePerkType.SharpFin,
                new ShrinePerkData(
                    ShrinePerkType.SharpFin,
                    "SHARP FIN",
                    "鋭利の整流翼",
                    "+21% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    200,
                    "SF"
                )
            },
            {
                ShrinePerkType.SharpScraper,
                new ShrinePerkData(
                    ShrinePerkType.SharpScraper,
                    "SHARP SCRAPER",
                    "鋭利の削剥刃",
                    "Glancing side hits shave off +5 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    215,
                    "SS"
                )
            },
            {
                ShrinePerkType.SharpAnchor,
                new ShrinePerkData(
                    ShrinePerkType.SharpAnchor,
                    "SHARP ANCHOR",
                    "鋭利の重碇",
                    "+14 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "SA"
                )
            },
            {
                ShrinePerkType.AgileStriker,
                new ShrinePerkData(
                    ShrinePerkType.AgileStriker,
                    "AGILE STRIKER",
                    "敏捷の強襲刃",
                    "Deals +12% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    220,
                    "AS"
                )
            },
            {
                ShrinePerkType.AgileGuard,
                new ShrinePerkData(
                    ShrinePerkType.AgileGuard,
                    "AGILE GUARD",
                    "敏捷の防護甲",
                    "+16 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "AG"
                )
            },
            {
                ShrinePerkType.AgileSprinter,
                new ShrinePerkData(
                    ShrinePerkType.AgileSprinter,
                    "AGILE SPRINTER",
                    "敏捷の疾走機",
                    "+15% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    210,
                    "AS"
                )
            },
            {
                ShrinePerkType.AgileBattery,
                new ShrinePerkData(
                    ShrinePerkType.AgileBattery,
                    "AGILE BATTERY",
                    "敏捷の蓄電器",
                    "Restores +5 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    210,
                    "AB"
                )
            },
            {
                ShrinePerkType.AgileGyro,
                new ShrinePerkData(
                    ShrinePerkType.AgileGyro,
                    "AGILE GYRO",
                    "敏捷の平衡独楽",
                    "Reduces tilt wobble by 38% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    235,
                    "AG"
                )
            },
            {
                ShrinePerkType.AgileEdge,
                new ShrinePerkData(
                    ShrinePerkType.AgileEdge,
                    "AGILE EDGE",
                    "敏捷の鋭刃",
                    "+14% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "AE"
                )
            },
            {
                ShrinePerkType.AgileTread,
                new ShrinePerkData(
                    ShrinePerkType.AgileTread,
                    "AGILE TREAD",
                    "敏捷の接地爪",
                    "+27% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    215,
                    "AT"
                )
            },
            {
                ShrinePerkType.AgileShield,
                new ShrinePerkData(
                    ShrinePerkType.AgileShield,
                    "AGILE SHIELD",
                    "敏捷の防壁",
                    "Reduces damage taken from glancing scrapes by 27%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    230,
                    "AS"
                )
            },
            {
                ShrinePerkType.AgileCell,
                new ShrinePerkData(
                    ShrinePerkType.AgileCell,
                    "AGILE CELL",
                    "敏捷の魔力管",
                    "Increases maximum Mana capacity pool by +21.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    235,
                    "AC"
                )
            },
            {
                ShrinePerkType.AgileFin,
                new ShrinePerkData(
                    ShrinePerkType.AgileFin,
                    "AGILE FIN",
                    "敏捷の整流翼",
                    "+33% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    235,
                    "AF"
                )
            },
            {
                ShrinePerkType.AgileScraper,
                new ShrinePerkData(
                    ShrinePerkType.AgileScraper,
                    "AGILE SCRAPER",
                    "敏捷の削剥刃",
                    "Glancing side hits shave off +8 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "AS"
                )
            },
            {
                ShrinePerkType.AgileAnchor,
                new ShrinePerkData(
                    ShrinePerkType.AgileAnchor,
                    "AGILE ANCHOR",
                    "敏捷の重碇",
                    "+21 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    255,
                    "AA"
                )
            },
            {
                ShrinePerkType.FirmStriker,
                new ShrinePerkData(
                    ShrinePerkType.FirmStriker,
                    "FIRM STRIKER",
                    "強固の強襲刃",
                    "Deals +8% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    200,
                    "FS"
                )
            },
            {
                ShrinePerkType.FirmGuard,
                new ShrinePerkData(
                    ShrinePerkType.FirmGuard,
                    "FIRM GUARD",
                    "強固の防護甲",
                    "+10 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    190,
                    "FG"
                )
            },
            {
                ShrinePerkType.FirmSprinter,
                new ShrinePerkData(
                    ShrinePerkType.FirmSprinter,
                    "FIRM SPRINTER",
                    "強固の疾走機",
                    "+10% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    185,
                    "FS"
                )
            },
            {
                ShrinePerkType.FirmBattery,
                new ShrinePerkData(
                    ShrinePerkType.FirmBattery,
                    "FIRM BATTERY",
                    "強固の蓄電器",
                    "Restores +3 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    190,
                    "FB"
                )
            },
            {
                ShrinePerkType.FirmGyro,
                new ShrinePerkData(
                    ShrinePerkType.FirmGyro,
                    "FIRM GYRO",
                    "強固の平衡独楽",
                    "Reduces tilt wobble by 29% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "FG"
                )
            },
            {
                ShrinePerkType.FirmEdge,
                new ShrinePerkData(
                    ShrinePerkType.FirmEdge,
                    "FIRM EDGE",
                    "強固の鋭刃",
                    "+10% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    225,
                    "FE"
                )
            },
            {
                ShrinePerkType.FirmTread,
                new ShrinePerkData(
                    ShrinePerkType.FirmTread,
                    "FIRM TREAD",
                    "強固の接地爪",
                    "+20% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    190,
                    "FT"
                )
            },
            {
                ShrinePerkType.FirmShield,
                new ShrinePerkData(
                    ShrinePerkType.FirmShield,
                    "FIRM SHIELD",
                    "強固の防壁",
                    "Reduces damage taken from glancing scrapes by 20%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    210,
                    "FS"
                )
            },
            {
                ShrinePerkType.FirmCell,
                new ShrinePerkData(
                    ShrinePerkType.FirmCell,
                    "FIRM CELL",
                    "強固の魔力管",
                    "Increases maximum Mana capacity pool by +15.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    210,
                    "FC"
                )
            },
            {
                ShrinePerkType.FirmFin,
                new ShrinePerkData(
                    ShrinePerkType.FirmFin,
                    "FIRM FIN",
                    "強固の整流翼",
                    "+25% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    210,
                    "FF"
                )
            },
            {
                ShrinePerkType.FirmScraper,
                new ShrinePerkData(
                    ShrinePerkType.FirmScraper,
                    "FIRM SCRAPER",
                    "強固の削剥刃",
                    "Glancing side hits shave off +6 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    225,
                    "FS"
                )
            },
            {
                ShrinePerkType.FirmAnchor,
                new ShrinePerkData(
                    ShrinePerkType.FirmAnchor,
                    "FIRM ANCHOR",
                    "強固の重碇",
                    "+16 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    235,
                    "FA"
                )
            },
            {
                ShrinePerkType.SturdyStriker,
                new ShrinePerkData(
                    ShrinePerkType.SturdyStriker,
                    "STURDY STRIKER",
                    "頑丈の強襲刃",
                    "Deals +14% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "SS"
                )
            },
            {
                ShrinePerkType.SturdyGuard,
                new ShrinePerkData(
                    ShrinePerkType.SturdyGuard,
                    "STURDY GUARD",
                    "頑丈の防護甲",
                    "+19 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "SG"
                )
            },
            {
                ShrinePerkType.SturdySprinter,
                new ShrinePerkData(
                    ShrinePerkType.SturdySprinter,
                    "STURDY SPRINTER",
                    "頑丈の疾走機",
                    "+17% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "SS"
                )
            },
            {
                ShrinePerkType.SturdyBattery,
                new ShrinePerkData(
                    ShrinePerkType.SturdyBattery,
                    "STURDY BATTERY",
                    "頑丈の蓄電器",
                    "Restores +6 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    220,
                    "SB"
                )
            },
            {
                ShrinePerkType.SturdyGyro,
                new ShrinePerkData(
                    ShrinePerkType.SturdyGyro,
                    "STURDY GYRO",
                    "頑丈の平衡独楽",
                    "Reduces tilt wobble by 43% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    245,
                    "SG"
                )
            },
            {
                ShrinePerkType.SturdyEdge,
                new ShrinePerkData(
                    ShrinePerkType.SturdyEdge,
                    "STURDY EDGE",
                    "頑丈の鋭刃",
                    "+17% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    255,
                    "SE"
                )
            },
            {
                ShrinePerkType.SturdyTread,
                new ShrinePerkData(
                    ShrinePerkType.SturdyTread,
                    "STURDY TREAD",
                    "頑丈の接地爪",
                    "+31% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    225,
                    "ST"
                )
            },
            {
                ShrinePerkType.SturdyShield,
                new ShrinePerkData(
                    ShrinePerkType.SturdyShield,
                    "STURDY SHIELD",
                    "頑丈の防壁",
                    "Reduces damage taken from glancing scrapes by 31%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    240,
                    "SS"
                )
            },
            {
                ShrinePerkType.SturdyCell,
                new ShrinePerkData(
                    ShrinePerkType.SturdyCell,
                    "STURDY CELL",
                    "頑丈の魔力管",
                    "Increases maximum Mana capacity pool by +10.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    190,
                    "SC"
                )
            },
            {
                ShrinePerkType.SturdyFin,
                new ShrinePerkData(
                    ShrinePerkType.SturdyFin,
                    "STURDY FIN",
                    "頑丈の整流翼",
                    "+17% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    185,
                    "SF"
                )
            },
            {
                ShrinePerkType.SturdyScraper,
                new ShrinePerkData(
                    ShrinePerkType.SturdyScraper,
                    "STURDY SCRAPER",
                    "頑丈の削剥刃",
                    "Glancing side hits shave off +4 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    205,
                    "SS"
                )
            },
            {
                ShrinePerkType.SturdyAnchor,
                new ShrinePerkData(
                    ShrinePerkType.SturdyAnchor,
                    "STURDY ANCHOR",
                    "頑丈の重碇",
                    "+11 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "SA"
                )
            },
            {
                ShrinePerkType.SolidStriker,
                new ShrinePerkData(
                    ShrinePerkType.SolidStriker,
                    "SOLID STRIKER",
                    "中実の強襲刃",
                    "Deals +10% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    210,
                    "SS"
                )
            },
            {
                ShrinePerkType.SolidGuard,
                new ShrinePerkData(
                    ShrinePerkType.SolidGuard,
                    "SOLID GUARD",
                    "中実の防護甲",
                    "+13 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    205,
                    "SG"
                )
            },
            {
                ShrinePerkType.SolidSprinter,
                new ShrinePerkData(
                    ShrinePerkType.SolidSprinter,
                    "SOLID SPRINTER",
                    "中実の疾走機",
                    "+12% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    195,
                    "SS"
                )
            },
            {
                ShrinePerkType.SolidBattery,
                new ShrinePerkData(
                    ShrinePerkType.SolidBattery,
                    "SOLID BATTERY",
                    "中実の蓄電器",
                    "Restores +4 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    200,
                    "SB"
                )
            },
            {
                ShrinePerkType.SolidGyro,
                new ShrinePerkData(
                    ShrinePerkType.SolidGyro,
                    "SOLID GYRO",
                    "中実の平衡独楽",
                    "Reduces tilt wobble by 34% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "SG"
                )
            },
            {
                ShrinePerkType.SolidEdge,
                new ShrinePerkData(
                    ShrinePerkType.SolidEdge,
                    "SOLID EDGE",
                    "中実の鋭刃",
                    "+12% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "SE"
                )
            },
            {
                ShrinePerkType.SolidTread,
                new ShrinePerkData(
                    ShrinePerkType.SolidTread,
                    "SOLID TREAD",
                    "中実の接地爪",
                    "+24% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    205,
                    "ST"
                )
            },
            {
                ShrinePerkType.SolidShield,
                new ShrinePerkData(
                    ShrinePerkType.SolidShield,
                    "SOLID SHIELD",
                    "中実の防壁",
                    "Reduces damage taken from glancing scrapes by 23%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    220,
                    "SS"
                )
            },
            {
                ShrinePerkType.SolidCell,
                new ShrinePerkData(
                    ShrinePerkType.SolidCell,
                    "SOLID CELL",
                    "中実の魔力管",
                    "Increases maximum Mana capacity pool by +18.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    225,
                    "SC"
                )
            },
            {
                ShrinePerkType.SolidFin,
                new ShrinePerkData(
                    ShrinePerkType.SolidFin,
                    "SOLID FIN",
                    "中実の整流翼",
                    "+29% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    220,
                    "SF"
                )
            },
            {
                ShrinePerkType.SolidScraper,
                new ShrinePerkData(
                    ShrinePerkType.SolidScraper,
                    "SOLID SCRAPER",
                    "中実の削剥刃",
                    "Glancing side hits shave off +7 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    235,
                    "SS"
                )
            },
            {
                ShrinePerkType.SolidAnchor,
                new ShrinePerkData(
                    ShrinePerkType.SolidAnchor,
                    "SOLID ANCHOR",
                    "中実の重碇",
                    "+19 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    245,
                    "SA"
                )
            },
            {
                ShrinePerkType.GripStriker,
                new ShrinePerkData(
                    ShrinePerkType.GripStriker,
                    "GRIP STRIKER",
                    "把握の強襲刃",
                    "Deals +16% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "GS"
                )
            },
            {
                ShrinePerkType.GripGuard,
                new ShrinePerkData(
                    ShrinePerkType.GripGuard,
                    "GRIP GUARD",
                    "把握の防護甲",
                    "+21 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    240,
                    "GG"
                )
            },
            {
                ShrinePerkType.GripSprinter,
                new ShrinePerkData(
                    ShrinePerkType.GripSprinter,
                    "GRIP SPRINTER",
                    "把握の疾走機",
                    "+19% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    230,
                    "GS"
                )
            },
            {
                ShrinePerkType.GripBattery,
                new ShrinePerkData(
                    ShrinePerkType.GripBattery,
                    "GRIP BATTERY",
                    "把握の蓄電器",
                    "Restores +6 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    230,
                    "GB"
                )
            },
            {
                ShrinePerkType.GripGyro,
                new ShrinePerkData(
                    ShrinePerkType.GripGyro,
                    "GRIP GYRO",
                    "把握の平衡独楽",
                    "Reduces tilt wobble by 25% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    205,
                    "GG"
                )
            },
            {
                ShrinePerkType.GripEdge,
                new ShrinePerkData(
                    ShrinePerkType.GripEdge,
                    "GRIP EDGE",
                    "把握の鋭刃",
                    "+7% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    215,
                    "GE"
                )
            },
            {
                ShrinePerkType.GripTread,
                new ShrinePerkData(
                    ShrinePerkType.GripTread,
                    "GRIP TREAD",
                    "把握の接地爪",
                    "+16% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    180,
                    "GT"
                )
            },
            {
                ShrinePerkType.GripShield,
                new ShrinePerkData(
                    ShrinePerkType.GripShield,
                    "GRIP SHIELD",
                    "把握の防壁",
                    "Reduces damage taken from glancing scrapes by 16%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    200,
                    "GS"
                )
            },
            {
                ShrinePerkType.GripCell,
                new ShrinePerkData(
                    ShrinePerkType.GripCell,
                    "GRIP CELL",
                    "把握の魔力管",
                    "Increases maximum Mana capacity pool by +12.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    200,
                    "GC"
                )
            },
            {
                ShrinePerkType.GripFin,
                new ShrinePerkData(
                    ShrinePerkType.GripFin,
                    "GRIP FIN",
                    "把握の整流翼",
                    "+21% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    200,
                    "GF"
                )
            },
            {
                ShrinePerkType.GripScraper,
                new ShrinePerkData(
                    ShrinePerkType.GripScraper,
                    "GRIP SCRAPER",
                    "把握の削剥刃",
                    "Glancing side hits shave off +5 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    215,
                    "GS"
                )
            },
            {
                ShrinePerkType.GripAnchor,
                new ShrinePerkData(
                    ShrinePerkType.GripAnchor,
                    "GRIP ANCHOR",
                    "把握の重碇",
                    "+14 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    225,
                    "GA"
                )
            },
            {
                ShrinePerkType.TorqueStriker,
                new ShrinePerkData(
                    ShrinePerkType.TorqueStriker,
                    "TORQUE STRIKER",
                    "回転力の強襲刃",
                    "Deals +12% bonus damage on direct contact smashes.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    220,
                    "TS"
                )
            },
            {
                ShrinePerkType.TorqueGuard,
                new ShrinePerkData(
                    ShrinePerkType.TorqueGuard,
                    "TORQUE GUARD",
                    "回転力の防護甲",
                    "+16 Base Defense against incoming collision strikes.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    215,
                    "TG"
                )
            },
            {
                ShrinePerkType.TorqueSprinter,
                new ShrinePerkData(
                    ShrinePerkType.TorqueSprinter,
                    "TORQUE SPRINTER",
                    "回転力の疾走機",
                    "+15% throttle acceleration across the arena bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    210,
                    "TS"
                )
            },
            {
                ShrinePerkType.TorqueBattery,
                new ShrinePerkData(
                    ShrinePerkType.TorqueBattery,
                    "TORQUE BATTERY",
                    "回転力の蓄電器",
                    "Restores +5 Mana upon making contact with stadium perimeter walls.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    210,
                    "TB"
                )
            },
            {
                ShrinePerkType.TorqueGyro,
                new ShrinePerkData(
                    ShrinePerkType.TorqueGyro,
                    "TORQUE GYRO",
                    "回転力の平衡独楽",
                    "Reduces tilt wobble by 38% after sustaining heavy impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    235,
                    "TG"
                )
            },
            {
                ShrinePerkType.TorqueEdge,
                new ShrinePerkData(
                    ShrinePerkType.TorqueEdge,
                    "TORQUE EDGE",
                    "回転力の鋭刃",
                    "+14% damage when striking opponent while moving faster than them.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "TE"
                )
            },
            {
                ShrinePerkType.TorqueTread,
                new ShrinePerkData(
                    ShrinePerkType.TorqueTread,
                    "TORQUE TREAD",
                    "回転力の接地爪",
                    "+27% uphill grip when ascending steep stadium walls.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    215,
                    "TT"
                )
            },
            {
                ShrinePerkType.TorqueShield,
                new ShrinePerkData(
                    ShrinePerkType.TorqueShield,
                    "TORQUE SHIELD",
                    "回転力の防壁",
                    "Reduces damage taken from glancing scrapes by 27%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    230,
                    "TS"
                )
            },
            {
                ShrinePerkType.TorqueCell,
                new ShrinePerkData(
                    ShrinePerkType.TorqueCell,
                    "TORQUE CELL",
                    "回転力の魔力管",
                    "Increases maximum Mana capacity pool by +21.",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    235,
                    "TC"
                )
            },
            {
                ShrinePerkType.TorqueFin,
                new ShrinePerkData(
                    ShrinePerkType.TorqueFin,
                    "TORQUE FIN",
                    "回転力の整流翼",
                    "+33% drift recovery torque when exiting sharp turns.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    235,
                    "TF"
                )
            },
            {
                ShrinePerkType.TorqueScraper,
                new ShrinePerkData(
                    ShrinePerkType.TorqueScraper,
                    "TORQUE SCRAPER",
                    "回転力の削剥刃",
                    "Glancing side hits shave off +8 extra spin from opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Common,
                    245,
                    "TS"
                )
            },
            {
                ShrinePerkType.TorqueAnchor,
                new ShrinePerkData(
                    ShrinePerkType.TorqueAnchor,
                    "TORQUE ANCHOR",
                    "回転力の重碇",
                    "+21 Mass Weight, increasing center bowl stability.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    255,
                    "TA"
                )
            },
            {
                ShrinePerkType.TurboRip,
                new ShrinePerkData(
                    ShrinePerkType.TurboRip,
                    "TURBO RIP",
                    "急速発射",
                    "Expands launch sweetspot window by 40%. Perfect launch awards +16% initial spin stamina.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    380,
                    "TR"
                )
            },
            {
                ShrinePerkType.MagnetoRing,
                new ShrinePerkData(
                    ShrinePerkType.MagnetoRing,
                    "MAGNETO RING",
                    "磁気誘引",
                    "Electromagnetic field pulls dropped salvage parts and spirit orbs within 6.8m toward your Bey.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    420,
                    "MR"
                )
            },
            {
                ShrinePerkType.ImpactAbsorber,
                new ShrinePerkData(
                    ShrinePerkType.ImpactAbsorber,
                    "IMPACT ABSORBER",
                    "衝撃緩衝",
                    "Reinforced dampening struts reduce direct head-on collision damage taken from opponents by 22%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    410,
                    "IA"
                )
            },
            {
                ShrinePerkType.CentrifugalClutch,
                new ShrinePerkData(
                    ShrinePerkType.CentrifugalClutch,
                    "CENTRIFUGAL CLUTCH",
                    "遠心連結",
                    "Spinning above 70% max speed unlocks rapid rotational dynamo charging, boosting Mana regen by +38%.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    440,
                    "CC"
                )
            },
            {
                ShrinePerkType.FlankStriker,
                new ShrinePerkData(
                    ShrinePerkType.FlankStriker,
                    "FLANK STRIKER",
                    "側面強襲",
                    "Striking opponents on their sides or rear arc deals +29% bonus ambush collision damage.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    450,
                    "FS"
                )
            },
            {
                ShrinePerkType.IronPlating,
                new ShrinePerkData(
                    ShrinePerkType.IronPlating,
                    "IRON PLATING",
                    "重鉄装甲",
                    "+22 Base Defense. Reduces knockback displacement taken from enemy special attack bursts by 32%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    430,
                    "IP"
                )
            },
            {
                ShrinePerkType.SlipstreamSurge,
                new ShrinePerkData(
                    ShrinePerkType.SlipstreamSurge,
                    "SLIPSTREAM SURGE",
                    "追従加速",
                    "Drafting behind an opponent within 3.5m grants +36% top pursuit speed and rapid dash buildup.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    400,
                    "Sl"
                )
            },
            {
                ShrinePerkType.OverchargeCapacitor,
                new ShrinePerkData(
                    ShrinePerkType.OverchargeCapacitor,
                    "OVERCHARGE CAPACITOR",
                    "過充電器",
                    "Expands maximum Mana capacity by +28 (raising total pool from 100 to 128).",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    460,
                    "OC"
                )
            },
            {
                ShrinePerkType.CounterWeight,
                new ShrinePerkData(
                    ShrinePerkType.CounterWeight,
                    "COUNTER WEIGHT",
                    "均衡錘",
                    "Wall bounces convert rebound stress into an instant forward sprint impulse with +22% boost.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    410,
                    "CW"
                )
            },
            {
                ShrinePerkType.ResonantVibration,
                new ShrinePerkData(
                    ShrinePerkType.ResonantVibration,
                    "RESONANT VIBRATION",
                    "共振振動",
                    "Direct collisions cause severe resonant rattles in enemy Beys, increasing their stamina decay by 32% for 2.5s.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    470,
                    "RV"
                )
            },
            {
                ShrinePerkType.SerratedRing,
                new ShrinePerkData(
                    ShrinePerkType.SerratedRing,
                    "SERRATED RING",
                    "鋸歯外輪",
                    "Outer perimeter features micro-serrations dealing +19% damage on glancing swipes.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    435,
                    "SR"
                )
            },
            {
                ShrinePerkType.HydraulicSpring,
                new ShrinePerkData(
                    ShrinePerkType.HydraulicSpring,
                    "HYDRAULIC SPRING",
                    "油圧懸架",
                    "Absorbs upward stadium floor bumps, preserving 32% more forward momentum on bowl banks.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    415,
                    "HS"
                )
            },
            {
                ShrinePerkType.PyroclasticSparks,
                new ShrinePerkData(
                    ShrinePerkType.PyroclasticSparks,
                    "PYROCLASTIC SPARKS",
                    "火砕流花火",
                    "Clash impacts ignite high-temperature sparks dealing 9 bonus thermal damage over 2 seconds.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    465,
                    "PS"
                )
            },
            {
                ShrinePerkType.HeavyAlloyRing,
                new ShrinePerkData(
                    ShrinePerkType.HeavyAlloyRing,
                    "HEAVY ALLOY RING",
                    "重合金輪",
                    "+30 Mass Weight. Increases collision momentum and reduces lateral deflection.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    425,
                    "HA"
                )
            },
            {
                ShrinePerkType.FluxCapacitor,
                new ShrinePerkData(
                    ShrinePerkType.FluxCapacitor,
                    "FLUX CAPACITOR",
                    "磁束蓄電器",
                    "Stores surplus kinetic energy, granting +14 instant Mana whenever top speed is reached.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    455,
                    "FC"
                )
            },
            {
                ShrinePerkType.AeroFinStabilizer,
                new ShrinePerkData(
                    ShrinePerkType.AeroFinStabilizer,
                    "AERO FIN STABILIZER",
                    "整流安定翼",
                    "Twin aerodynamic fins stabilize high-speed cornering, increasing banking drift torque by +37%.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    425,
                    "AF"
                )
            },
            {
                ShrinePerkType.ShockwaveDisperser,
                new ShrinePerkData(
                    ShrinePerkType.ShockwaveDisperser,
                    "SHOCKWAVE DISPERSER",
                    "衝撃拡散器",
                    "Reduces burst damage taken from opponent special attacks by 28%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    445,
                    "SD"
                )
            },
            {
                ShrinePerkType.SpurStriker,
                new ShrinePerkData(
                    ShrinePerkType.SpurStriker,
                    "SPUR STRIKER",
                    "拍車突撃",
                    "Bursting an opponent restores +32 Spin Stamina and +26 Mana instantly.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    485,
                    "SS"
                )
            },
            {
                ShrinePerkType.KineticSiphon,
                new ShrinePerkData(
                    ShrinePerkType.KineticSiphon,
                    "KINETIC SIPHON",
                    "運動吸水管",
                    "Absorbs 12% of opponent's forward speed on collision and adds it to your own velocity.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    465,
                    "KS"
                )
            },
            {
                ShrinePerkType.GildedFlywheel,
                new ShrinePerkData(
                    ShrinePerkType.GildedFlywheel,
                    "GILDED FLYWHEEL",
                    "黄金弾車",
                    "Increases spin retention on steep stadium walls by +42%, enabling higher orbit trajectories.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    435,
                    "GF"
                )
            },
            {
                ShrinePerkType.ChilledPerimeter,
                new ShrinePerkData(
                    ShrinePerkType.ChilledPerimeter,
                    "CHILLED PERIMETER",
                    "冷却外周",
                    "Freezes opponent contact points, reducing their friction grip by 32% for 2s after collision.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    475,
                    "CP"
                )
            },
            {
                ShrinePerkType.ReactiveArmor,
                new ShrinePerkData(
                    ShrinePerkType.ReactiveArmor,
                    "REACTIVE ARMOR",
                    "反応装甲",
                    "When taking damage greater than 14, releases a kinetic burst that knocks back the attacker 3.2m.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    460,
                    "RA"
                )
            },
            {
                ShrinePerkType.ManaSurgeRelay,
                new ShrinePerkData(
                    ShrinePerkType.ManaSurgeRelay,
                    "MANA SURGE RELAY",
                    "魔力中継器",
                    "Ability usage refunds 22% of consumed mana if it directly damages an opponent.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    475,
                    "MR"
                )
            },
            {
                ShrinePerkType.VortexImpeller,
                new ShrinePerkData(
                    ShrinePerkType.VortexImpeller,
                    "VORTEX IMPELLER",
                    "渦流推進機",
                    "Generates an updraft while spinning, reducing ground friction decay by 28%.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    430,
                    "VI"
                )
            },
            {
                ShrinePerkType.DualEdgeCutter,
                new ShrinePerkData(
                    ShrinePerkType.DualEdgeCutter,
                    "DUAL EDGE CUTTER",
                    "双刃切断",
                    "Critical collision hits carve deep into enemy balance, dealing +32% critical bonus damage.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    465,
                    "DE"
                )
            },
            {
                ShrinePerkType.TungstenCore,
                new ShrinePerkData(
                    ShrinePerkType.TungstenCore,
                    "TUNGSTEN CORE",
                    "タングステン核",
                    "+32 Mass Weight and +16 Defense. Increases center-dish gravitational anchoring.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    455,
                    "TC"
                )
            },
            {
                ShrinePerkType.SpiritDynamo,
                new ShrinePerkData(
                    ShrinePerkType.SpiritDynamo,
                    "SPIRIT DYNAMO",
                    "精霊発電機",
                    "Generate +2.5 Mana every time you perform a successful drift turn.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    425,
                    "SD"
                )
            },
            {
                ShrinePerkType.OverdriveIgniter,
                new ShrinePerkData(
                    ShrinePerkType.OverdriveIgniter,
                    "OVERDRIVE IGNITER",
                    "点火加速器",
                    "Boost propulsion duration extended by +42% and costs 22% less mana per second.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    455,
                    "OI"
                )
            },
            {
                ShrinePerkType.ArmorPiercer,
                new ShrinePerkData(
                    ShrinePerkType.ArmorPiercer,
                    "ARMOR PIERCER",
                    "装甲貫徹",
                    "Direct blade strikes bypass 38% of enemy defense rating.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    475,
                    "AP"
                )
            },
            {
                ShrinePerkType.StabilizerWeights,
                new ShrinePerkData(
                    ShrinePerkType.StabilizerWeights,
                    "STABILIZER WEIGHTS",
                    "安定錘",
                    "Reduces spin loss from wall collisions and rebounds by 52%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    420,
                    "SW"
                )
            },
            {
                ShrinePerkType.ChargeRelay,
                new ShrinePerkData(
                    ShrinePerkType.ChargeRelay,
                    "CHARGE RELAY",
                    "蓄電中継",
                    "Collecting energy orbs grants an immediate +22% speed burst for 2.6 seconds.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    415,
                    "CR"
                )
            },
            {
                ShrinePerkType.TornadoBlade,
                new ShrinePerkData(
                    ShrinePerkType.TornadoBlade,
                    "TORNADO BLADE",
                    "竜巻の刃",
                    "Spinning creates a localized vortex pulling nearby light debris and opponent Beys inward by 2.2m.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    485,
                    "TB"
                )
            },
            {
                ShrinePerkType.PrismReflector,
                new ShrinePerkData(
                    ShrinePerkType.PrismReflector,
                    "PRISM REFLECTOR",
                    "角柱反射器",
                    "Special ability attacks taken deal 28% reduced damage to spin stamina.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    455,
                    "PR"
                )
            },
            {
                ShrinePerkType.NitroBooster,
                new ShrinePerkData(
                    ShrinePerkType.NitroBooster,
                    "NITRO BOOSTER",
                    "窒素加速器",
                    "Initial boost takeoff velocity increased by +38%.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    435,
                    "NB"
                )
            },
            {
                ShrinePerkType.LeechFangs,
                new ShrinePerkData(
                    ShrinePerkType.LeechFangs,
                    "LEECH FANGS",
                    "吸血牙",
                    "Glancing strikes and scratches restore +4.5 spin stamina back to your Bey.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    465,
                    "LF"
                )
            },
            {
                ShrinePerkType.CapacitorBank,
                new ShrinePerkData(
                    ShrinePerkType.CapacitorBank,
                    "CAPACITOR BANK",
                    "蓄電堤",
                    "Maximum Mana pool increased by +38.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    475,
                    "CB"
                )
            },
            {
                ShrinePerkType.IroncladTip,
                new ShrinePerkData(
                    ShrinePerkType.IroncladTip,
                    "IRONCLAD TIP",
                    "重装軸先",
                    "Prevents tip degradation, preserving maximum stamina over long duels by +28%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    425,
                    "IT"
                )
            },
            {
                ShrinePerkType.MomentumKeeper,
                new ShrinePerkData(
                    ShrinePerkType.MomentumKeeper,
                    "MOMENTUM KEEPER",
                    "運動量保持",
                    "Maintains top cruising speed 55% longer after releasing boost throttle.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    435,
                    "MK"
                )
            },
            {
                ShrinePerkType.ShatterStrike,
                new ShrinePerkData(
                    ShrinePerkType.ShatterStrike,
                    "SHATTER STRIKE",
                    "破砕打撃",
                    "Hits against enemies below 30% spin deal +42% execute finisher damage.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    485,
                    "Sh"
                )
            },
            {
                ShrinePerkType.HeavyInertia,
                new ShrinePerkData(
                    ShrinePerkType.HeavyInertia,
                    "HEAVY INERTIA",
                    "重慣性",
                    "Reduces outward centrifuge drift on steep wall rims by 34%.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    425,
                    "HI"
                )
            },
            {
                ShrinePerkType.TeslaDischarge,
                new ShrinePerkData(
                    ShrinePerkType.TeslaDischarge,
                    "TESLA DISCHARGE",
                    "テスラ放電",
                    "Wall impacts arc static electricity to nearest enemy within 5.2m for 11 spin damage.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    465,
                    "TD"
                )
            },
            {
                ShrinePerkType.NanoMeshArmor,
                new ShrinePerkData(
                    ShrinePerkType.NanoMeshArmor,
                    "NANO MESH ARMOR",
                    "微細網装甲",
                    "+24 Defense and +12% resistance to all collision impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    445,
                    "NM"
                )
            },
            {
                ShrinePerkType.ManaOverclock,
                new ShrinePerkData(
                    ShrinePerkType.ManaOverclock,
                    "MANA OVERCLOCK",
                    "魔力過回転",
                    "Mana regenerates 42% faster when your Bey is at high spin velocity (>80%).",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    465,
                    "MO"
                )
            },
            {
                ShrinePerkType.RallySpur,
                new ShrinePerkData(
                    ShrinePerkType.RallySpur,
                    "RALLY SPUR",
                    "反撃拍車",
                    "Taking a heavy hit grants +28% bonus attack damage for your next counter-attack within 3s.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    455,
                    "RS"
                )
            },
            {
                ShrinePerkType.SpeedSkater,
                new ShrinePerkData(
                    ShrinePerkType.SpeedSkater,
                    "SPEED SKATER",
                    "超速滑走",
                    "Drifting on curved arena slopes generates frictionless ice-skating propulsion (+32% speed).",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    445,
                    "SK"
                )
            },
            {
                ShrinePerkType.BastionRing,
                new ShrinePerkData(
                    ShrinePerkType.BastionRing,
                    "BASTION RING",
                    "砦壁外輪",
                    "+26 Base Defense. Decreases wobble vibration amplitude by 42%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    435,
                    "BR"
                )
            },
            {
                ShrinePerkType.EnergyHarvester,
                new ShrinePerkData(
                    ShrinePerkType.EnergyHarvester,
                    "ENERGY HARVESTER",
                    "能量収穫機",
                    "Absorb +7 Mana whenever any enemy in the stadium is hit by an ability.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    455,
                    "EH"
                )
            },
            {
                ShrinePerkType.BladeHone,
                new ShrinePerkData(
                    ShrinePerkType.BladeHone,
                    "BLADE HONE",
                    "研ぎ澄まされし刃",
                    "+22% flat damage on all direct frontal head-on blade clashes.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    465,
                    "BH"
                )
            },
            {
                ShrinePerkType.VibroDampener,
                new ShrinePerkData(
                    ShrinePerkType.VibroDampener,
                    "VIBRO DAMPENER",
                    "防振装置",
                    "Reduces balance destabilization from rapid consecutive enemy strikes by 48%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    425,
                    "VD"
                )
            },
            {
                ShrinePerkType.CentrifugalSurge,
                new ShrinePerkData(
                    ShrinePerkType.CentrifugalSurge,
                    "CENTRIFUGAL SURGE",
                    "遠心激震",
                    "Steering from top rim down to stadium center gains massive downhill gravity dive velocity (+44%).",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    445,
                    "CS"
                )
            },
            {
                ShrinePerkType.CryoCoating,
                new ShrinePerkData(
                    ShrinePerkType.CryoCoating,
                    "CRYO COATING",
                    "極冷被膜",
                    "Contacts coat enemy Bey in frost, increasing their wall bounce self-damage by 35%.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    450,
                    "CC"
                )
            },
            {
                ShrinePerkType.MagneticRepulsor,
                new ShrinePerkData(
                    ShrinePerkType.MagneticRepulsor,
                    "MAGNETIC REPULSOR",
                    "磁気反撥機",
                    "When an enemy attempts to ram you, their acceleration is slowed by 25% within 1.5m.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    440,
                    "MR"
                )
            },
            {
                ShrinePerkType.DynamoWheel,
                new ShrinePerkData(
                    ShrinePerkType.DynamoWheel,
                    "DYNAMO WHEEL",
                    "発電車輪",
                    "Moving at high speeds generates +3 Mana every 2 seconds continuously.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    430,
                    "DW"
                )
            },
            {
                ShrinePerkType.GravityAnchor,
                new ShrinePerkData(
                    ShrinePerkType.GravityAnchor,
                    "GRAVITY ANCHOR",
                    "重力碇",
                    "+34 Mass Weight. Reduces launch displacement from floor geysers and traps by 50%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    445,
                    "GA"
                )
            },
            {
                ShrinePerkType.OverdriveIgnition,
                new ShrinePerkData(
                    ShrinePerkType.OverdriveIgnition,
                    "OVERDRIVE IGNITION",
                    "過駆動点火",
                    "First ability used in each arena costs 50% less Mana and triggers 20% faster.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    470,
                    "OI"
                )
            },
            {
                ShrinePerkType.OrbitStriker,
                new ShrinePerkData(
                    ShrinePerkType.OrbitStriker,
                    "ORBIT STRIKER",
                    "軌道強襲",
                    "Attacking from upper rim bowl deals +26% bonus diving kinetic smash damage.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    455,
                    "OS"
                )
            },
            {
                ShrinePerkType.ElasticHub,
                new ShrinePerkData(
                    ShrinePerkType.ElasticHub,
                    "ELASTIC HUB",
                    "弾性輪胴",
                    "Wall rebounds propel your Bey with +28% extra rebound spring velocity.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    420,
                    "EH"
                )
            },
            {
                ShrinePerkType.ThermalPlating,
                new ShrinePerkData(
                    ShrinePerkType.ThermalPlating,
                    "THERMAL PLATING",
                    "耐熱装甲",
                    "Immune to environmental floor hazards and takes 40% less damage from fire/inferno effects.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    435,
                    "TP"
                )
            },
            {
                ShrinePerkType.ManaCapacitorII,
                new ShrinePerkData(
                    ShrinePerkType.ManaCapacitorII,
                    "MANA CAPACITOR II",
                    "大型蓄電器",
                    "Maximum Mana capacity expanded by +32.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    460,
                    "MC"
                )
            },
            {
                ShrinePerkType.BladeSerrated,
                new ShrinePerkData(
                    ShrinePerkType.BladeSerrated,
                    "BLADE SERRATED",
                    "鋸刃突起",
                    "Glancing side hits carve 8 extra spin damage and trigger spark bursts.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    445,
                    "BS"
                )
            },
            {
                ShrinePerkType.FlashStepDampener,
                new ShrinePerkData(
                    ShrinePerkType.FlashStepDampener,
                    "FLASH STEP DAMPENER",
                    "瞬歩制振器",
                    "After dashing or using mobility abilities, receive 30% less damage for 1.5 seconds.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    450,
                    "FS"
                )
            },
            {
                ShrinePerkType.TurbineExhaust,
                new ShrinePerkData(
                    ShrinePerkType.TurbineExhaust,
                    "TURBINE EXHAUST",
                    "噴射排気",
                    "Releases air blasts behind your Bey when boosting, shoving trailing pursuers back.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    435,
                    "TE"
                )
            },
            {
                ShrinePerkType.KineticAbsorber,
                new ShrinePerkData(
                    ShrinePerkType.KineticAbsorber,
                    "KINETIC ABSORBER",
                    "運動吸収体",
                    "Converts 15% of incoming collision damage into instant spin stamina recovery.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    480,
                    "KA"
                )
            },
            {
                ShrinePerkType.ThunderSpark,
                new ShrinePerkData(
                    ShrinePerkType.ThunderSpark,
                    "THUNDER SPARK",
                    "雷火火花",
                    "Collisions have a 30% chance to zap the opponent for 14 bonus electrical spin damage.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    465,
                    "TS"
                )
            },
            {
                ShrinePerkType.PrismCapacitor,
                new ShrinePerkData(
                    ShrinePerkType.PrismCapacitor,
                    "PRISM CAPACITOR",
                    "角柱充電器",
                    "Stores up to +20 excess Mana beyond normal capacity when picking up energy pickups.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    455,
                    "PC"
                )
            },
            {
                ShrinePerkType.ApexClimber,
                new ShrinePerkData(
                    ShrinePerkType.ApexClimber,
                    "APEX CLIMBER",
                    "頂上登坂",
                    "Climbing stadium slopes gains +32% bonus speed and reduces slope friction decay by 35%.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    430,
                    "AC"
                )
            },
            {
                ShrinePerkType.ReflectiveEdge,
                new ShrinePerkData(
                    ShrinePerkType.ReflectiveEdge,
                    "REFLECTIVE EDGE",
                    "反射刃縁",
                    "Reflects 22% of enemy attack power back at them during direct collisions.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    470,
                    "RE"
                )
            },
            {
                ShrinePerkType.GyroStabilizer,
                new ShrinePerkData(
                    ShrinePerkType.GyroStabilizer,
                    "GYRO STABILIZER",
                    "独楽安定装置",
                    "Recovers from tilt wobble 60% faster after being hit by heavy smashes.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    425,
                    "GS"
                )
            },
            {
                ShrinePerkType.ManaSurgeCore,
                new ShrinePerkData(
                    ShrinePerkType.ManaSurgeCore,
                    "MANA SURGE CORE",
                    "魔力奔流核",
                    "Whenever spin drops below 40%, Mana regeneration is boosted by +60%.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    465,
                    "MS"
                )
            },
            {
                ShrinePerkType.CycloneFin,
                new ShrinePerkData(
                    ShrinePerkType.CycloneFin,
                    "CYCLONE FIN",
                    "旋風安定翼",
                    "Spinning at high speed forms an air curtain that deflects 20% of incoming damage.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    450,
                    "CF"
                )
            },
            {
                ShrinePerkType.BatteringRam,
                new ShrinePerkData(
                    ShrinePerkType.BatteringRam,
                    "BATTERING RAM",
                    "破城槌",
                    "Head-on collisions deal +30% bonus knockback force to opponents.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    460,
                    "BR"
                )
            },
            {
                ShrinePerkType.AeroSkate,
                new ShrinePerkData(
                    ShrinePerkType.AeroSkate,
                    "AERO SKATE",
                    "整流滑走",
                    "Reduces friction drag across the arena floor by 24%, boosting top cruising speed.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    425,
                    "AS"
                )
            },
            {
                ShrinePerkType.StaticBurst,
                new ShrinePerkData(
                    ShrinePerkType.StaticBurst,
                    "STATIC BURST",
                    "静電放電",
                    "Bouncing off walls discharges static pulses dealing 8 spin damage to foes within 3m.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    445,
                    "SB"
                )
            },
            {
                ShrinePerkType.ArmorMesh,
                new ShrinePerkData(
                    ShrinePerkType.ArmorMesh,
                    "ARMOR MESH",
                    "装甲編網",
                    "+28 Defense against continuous spin grind damage.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    435,
                    "AM"
                )
            },
            {
                ShrinePerkType.ManaTransducer,
                new ShrinePerkData(
                    ShrinePerkType.ManaTransducer,
                    "MANA TRANSDUCER",
                    "魔力変換器",
                    "Converts 20% of damage dealt into bonus Mana points.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    475,
                    "MT"
                )
            },
            {
                ShrinePerkType.VortexClutch,
                new ShrinePerkData(
                    ShrinePerkType.VortexClutch,
                    "VORTEX CLUTCH",
                    "渦巻連結",
                    "Drifting on curved bowl surfaces generates +18% bonus acceleration torque.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    430,
                    "VC"
                )
            },
            {
                ShrinePerkType.SeismicPulse,
                new ShrinePerkData(
                    ShrinePerkType.SeismicPulse,
                    "SEISMIC PULSE",
                    "地震波動",
                    "Heavy arena collisions emit a tremor that shakes enemy stability within 4m.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    465,
                    "SP"
                )
            },
            {
                ShrinePerkType.FortressRing,
                new ShrinePerkData(
                    ShrinePerkType.FortressRing,
                    "FORTRESS RING",
                    "要塞外輪",
                    "+30 Base Defense and +20 Mass Weight.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    455,
                    "FR"
                )
            },
            {
                ShrinePerkType.EnergyPrismII,
                new ShrinePerkData(
                    ShrinePerkType.EnergyPrismII,
                    "ENERGY PRISM II",
                    "増幅角柱",
                    "Reduces all ability cooldown timers by 18%.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    465,
                    "EP"
                )
            },
            {
                ShrinePerkType.SwiftThrust,
                new ShrinePerkData(
                    ShrinePerkType.SwiftThrust,
                    "SWIFT THRUST",
                    "疾風突進",
                    "Initial dash speed increased by +28% and costs 15% less Mana.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    440,
                    "ST"
                )
            },
            {
                ShrinePerkType.BloodSpur,
                new ShrinePerkData(
                    ShrinePerkType.BloodSpur,
                    "BLOOD SPUR",
                    "吸血拍車",
                    "Absorbs 8% of all collision damage dealt back as Spin Stamina.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    470,
                    "BS"
                )
            },
            {
                ShrinePerkType.TitaniumPlating,
                new ShrinePerkData(
                    ShrinePerkType.TitaniumPlating,
                    "TITANIUM PLATING",
                    "チタン装甲",
                    "+26 Defense and complete immunity to arena edge chipping damage.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    445,
                    "TP"
                )
            },
            {
                ShrinePerkType.ManaBattery,
                new ShrinePerkData(
                    ShrinePerkType.ManaBattery,
                    "MANA BATTERY",
                    "魔力蓄電池",
                    "Starting any arena duel gives +25 bonus instant Mana.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    435,
                    "MB"
                )
            },
            {
                ShrinePerkType.DriftMaster,
                new ShrinePerkData(
                    ShrinePerkType.DriftMaster,
                    "DRIFT MASTER",
                    "滑走達人",
                    "+35% drift turning speed and +50% faster steering response while banking.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    440,
                    "DM"
                )
            },
            {
                ShrinePerkType.StaggerStrike,
                new ShrinePerkData(
                    ShrinePerkType.StaggerStrike,
                    "STAGGER STRIKE",
                    "眩暈一撃",
                    "Direct hits above 6m/s stagger enemy steering trajectory for 0.6s.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    460,
                    "SS"
                )
            },
            {
                ShrinePerkType.ReinforcedHub,
                new ShrinePerkData(
                    ShrinePerkType.ReinforcedHub,
                    "REINFORCED HUB",
                    "強化輪胴",
                    "+18 Mass Weight and 35% reduced recoil knockback displacement.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    430,
                    "RH"
                )
            },
            {
                ShrinePerkType.OverclockRegen,
                new ShrinePerkData(
                    ShrinePerkType.OverclockRegen,
                    "OVERCLOCK REGEN",
                    "過負荷再生",
                    "Mana regen rate increased by +32% across all battle conditions.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    450,
                    "OR"
                )
            },
            {
                ShrinePerkType.SlipstreamMaster,
                new ShrinePerkData(
                    ShrinePerkType.SlipstreamMaster,
                    "SLIPSTREAM MASTER",
                    "追従の極み",
                    "Drafting behind enemies grants +42% pursuit speed and +25% attack damage.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    465,
                    "SM"
                )
            },
            {
                ShrinePerkType.PiercingContact,
                new ShrinePerkData(
                    ShrinePerkType.PiercingContact,
                    "PIERCING CONTACT",
                    "刺突接触",
                    "Direct blade hits ignore 25% of opponent's armor defense.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    455,
                    "PC"
                )
            },
            {
                ShrinePerkType.DeflectionShield,
                new ShrinePerkData(
                    ShrinePerkType.DeflectionShield,
                    "DEFLECTION SHIELD",
                    "受け流し盾",
                    "Deflects 20% of incoming collision damage when struck from the sides.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    440,
                    "DS"
                )
            },
            {
                ShrinePerkType.SpiritSiphon,
                new ShrinePerkData(
                    ShrinePerkType.SpiritSiphon,
                    "SPIRIT SIPHON",
                    "霊力吸水管",
                    "Passively generates +2.8 Mana per second while in active combat.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    445,
                    "SS"
                )
            },
            {
                ShrinePerkType.TurboThrust,
                new ShrinePerkData(
                    ShrinePerkType.TurboThrust,
                    "TURBO THRUST",
                    "超速推力",
                    "Boost acceleration force increased by +34%.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    435,
                    "TT"
                )
            },
            {
                ShrinePerkType.ImpactCrusher,
                new ShrinePerkData(
                    ShrinePerkType.ImpactCrusher,
                    "IMPACT CRUSHER",
                    "衝撃粉砕",
                    "Collisions against lighter opponents deal +28% extra spin damage.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    460,
                    "IC"
                )
            },
            {
                ShrinePerkType.IronCore,
                new ShrinePerkData(
                    ShrinePerkType.IronCore,
                    "IRON CORE",
                    "重鉄核",
                    "+30 Mass Weight and +14 Defense.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    435,
                    "IC"
                )
            },
            {
                ShrinePerkType.ManaConduitII,
                new ShrinePerkData(
                    ShrinePerkType.ManaConduitII,
                    "MANA CONDUIT II",
                    "魔力伝導管II",
                    "All special ability costs discounted by 18%.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    465,
                    "MC"
                )
            },
            {
                ShrinePerkType.AeroDynamics,
                new ShrinePerkData(
                    ShrinePerkType.AeroDynamics,
                    "AERODYNAMICS",
                    "空気力学",
                    "Reduces air friction, allowing top speed to reach +18% higher maximum.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    430,
                    "AD"
                )
            },
            {
                ShrinePerkType.ViperFangs,
                new ShrinePerkData(
                    ShrinePerkType.ViperFangs,
                    "VIPER FANGS",
                    "毒蛇の牙",
                    "Attacks inflict a venomous friction drain dealing 4 spin/sec for 3 seconds.",
                    PerkCategory.Combat,
                    PerkRarity.Uncommon,
                    470,
                    "VF"
                )
            },
            {
                ShrinePerkType.BulwarkPlating,
                new ShrinePerkData(
                    ShrinePerkType.BulwarkPlating,
                    "BULWARK PLATING",
                    "防壁装甲",
                    "Reduces maximum knockback taken from collisions by 35%.",
                    PerkCategory.Defense,
                    PerkRarity.Uncommon,
                    440,
                    "BP"
                )
            },
            {
                ShrinePerkType.CapacitorCore,
                new ShrinePerkData(
                    ShrinePerkType.CapacitorCore,
                    "CAPACITOR CORE",
                    "蓄電核",
                    "Max Mana pool increased by +30 and start match with full Mana.",
                    PerkCategory.Energy,
                    PerkRarity.Uncommon,
                    475,
                    "CC"
                )
            },
            {
                ShrinePerkType.SonicImpulse,
                new ShrinePerkData(
                    ShrinePerkType.SonicImpulse,
                    "SONIC IMPULSE",
                    "音速衝動",
                    "Bouncing off walls grants an instantaneous +25% sprint boost for 2 seconds.",
                    PerkCategory.Mobility,
                    PerkRarity.Uncommon,
                    445,
                    "SI"
                )
            },
            {
                ShrinePerkType.VampiricSpin,
                new ShrinePerkData(
                    ShrinePerkType.VampiricSpin,
                    "VAMPIRIC SPIN",
                    "吸血の回転",
                    "Absorbs 15% of all collision and ability damage dealt to opponents back as Spin Stamina.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    680,
                    "VP"
                )
            },
            {
                ShrinePerkType.StaticOverload,
                new ShrinePerkData(
                    ShrinePerkType.StaticOverload,
                    "STATIC OVERLOAD",
                    "電磁過負荷",
                    "Heavy collisions (>5.5 m/s) discharge chain lightning arcs to nearby enemies for 16 spin damage.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    720,
                    "SO"
                )
            },
            {
                ShrinePerkType.InfernoAura,
                new ShrinePerkData(
                    ShrinePerkType.InfernoAura,
                    "INFERNO AURA",
                    "業火の闘気",
                    "Radiates blistering thermal friction, burning all nearby enemies within 2.5m for 6 spin/sec.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    750,
                    "IA"
                )
            },
            {
                ShrinePerkType.CycloneVortex,
                new ShrinePerkData(
                    ShrinePerkType.CycloneVortex,
                    "CYCLONE VORTEX",
                    "疾風旋回",
                    "High-speed spinning generates localized vortex turbulence, slowing down nearby enemies within 3.5m by 30%.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    780,
                    "CV"
                )
            },
            {
                ShrinePerkType.IronFortress,
                new ShrinePerkData(
                    ShrinePerkType.IronFortress,
                    "IRON FORTRESS",
                    "鋼鉄要塞",
                    "+35 Base Defense. Eliminates all self-damage from arena wall rebounds and impacts.",
                    PerkCategory.Defense,
                    PerkRarity.Rare,
                    720,
                    "IF"
                )
            },
            {
                ShrinePerkType.SpiritSurge,
                new ShrinePerkData(
                    ShrinePerkType.SpiritSurge,
                    "SPIRIT SURGE",
                    "聖霊奔流",
                    "Landing heavy critical hits or burst knockouts instantly refills 40% of your maximum Mana pool.",
                    PerkCategory.Energy,
                    PerkRarity.Rare,
                    680,
                    "Sp"
                )
            },
            {
                ShrinePerkType.RazorEdge,
                new ShrinePerkData(
                    ShrinePerkType.RazorEdge,
                    "RAZOR EDGE",
                    "鋭刃連撃",
                    "Hitting enemies in rapid succession builds up momentum, increasing damage dealt by +10% per hit (up to +40%).",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    700,
                    "RE"
                )
            },
            {
                ShrinePerkType.VortexShield,
                new ShrinePerkData(
                    ShrinePerkType.VortexShield,
                    "VORTEX SHIELD",
                    "渦巻防壁",
                    "Spinning above 60% generates a barrier that deflects hostile projectiles and repels enemy dash surges.",
                    PerkCategory.Defense,
                    PerkRarity.Rare,
                    760,
                    "VS"
                )
            },
            {
                ShrinePerkType.SeismicSlam,
                new ShrinePerkData(
                    ShrinePerkType.SeismicSlam,
                    "SEISMIC SLAM",
                    "大地激震",
                    "Wall bounces at high speed send shock tremors through the arena floor, knocking back opponents by 4.0m.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    710,
                    "SS"
                )
            },
            {
                ShrinePerkType.ArcaneResonance,
                new ShrinePerkData(
                    ShrinePerkType.ArcaneResonance,
                    "ARCANE RESONANCE",
                    "神秘共鳴",
                    "Using an ability grants +20% collision damage and +25% top speed for 3 seconds.",
                    PerkCategory.Energy,
                    PerkRarity.Rare,
                    740,
                    "AR"
                )
            },
            {
                ShrinePerkType.PhantomDash,
                new ShrinePerkData(
                    ShrinePerkType.PhantomDash,
                    "PHANTOM DASH",
                    "幻影突進",
                    "Dashing through opponents shaves 15 spin stamina while making you intangible for 0.3s.",
                    PerkCategory.Mobility,
                    PerkRarity.Rare,
                    760,
                    "PD"
                )
            },
            {
                ShrinePerkType.GlacialArmor,
                new ShrinePerkData(
                    ShrinePerkType.GlacialArmor,
                    "GLACIAL ARMOR",
                    "氷結甲冑",
                    "When hit, encases the attacker in sub-zero frost, slowing their movement and spin acceleration by 35% for 2s.",
                    PerkCategory.Defense,
                    PerkRarity.Rare,
                    730,
                    "GA"
                )
            },
            {
                ShrinePerkType.TempestDrive,
                new ShrinePerkData(
                    ShrinePerkType.TempestDrive,
                    "TEMPEST DRIVE",
                    "暴風駆動",
                    "High velocity movement generates tailwind currents, continuously accelerating up to +40% top speed over 4s.",
                    PerkCategory.Mobility,
                    PerkRarity.Rare,
                    750,
                    "TD"
                )
            },
            {
                ShrinePerkType.SoulReaper,
                new ShrinePerkData(
                    ShrinePerkType.SoulReaper,
                    "SOUL REAPER",
                    "魂魄収穫",
                    "Every opponent burst in the match permanently increases your collision damage by +20% for the rest of the arena.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    790,
                    "SR"
                )
            },
            {
                ShrinePerkType.ObsidianCore,
                new ShrinePerkData(
                    ShrinePerkType.ObsidianCore,
                    "OBSIDIAN CORE",
                    "黒曜石核",
                    "+35 Mass Weight and +25 Defense. Completely immune to tilt instability from heavy slams.",
                    PerkCategory.Defense,
                    PerkRarity.Rare,
                    740,
                    "OC"
                )
            },
            {
                ShrinePerkType.ManaConduit,
                new ShrinePerkData(
                    ShrinePerkType.ManaConduit,
                    "MANA CONDUIT",
                    "魔力伝導管",
                    "Ability mana costs are reduced by 25% and Mana regen delay after spending mana is eliminated.",
                    PerkCategory.Energy,
                    PerkRarity.Rare,
                    770,
                    "MC"
                )
            },
            {
                ShrinePerkType.Thunderstrike,
                new ShrinePerkData(
                    ShrinePerkType.Thunderstrike,
                    "THUNDERSTRIKE",
                    "迅雷一撃",
                    "Every 5th direct hit calls down a divine thunderbolt on the opponent for 22 spin damage.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    730,
                    "TS"
                )
            },
            {
                ShrinePerkType.MirrorPlating,
                new ShrinePerkData(
                    ShrinePerkType.MirrorPlating,
                    "MIRROR PLATING",
                    "鏡面装甲",
                    "Reflects 30% of all collision damage received directly back onto the attacking Bey.",
                    PerkCategory.Defense,
                    PerkRarity.Rare,
                    750,
                    "MP"
                )
            },
            {
                ShrinePerkType.SonicBoom,
                new ShrinePerkData(
                    ShrinePerkType.SonicBoom,
                    "SONIC BOOM",
                    "音速衝撃",
                    "Breaking top speed barrier emits a conical sonic shockwave that knocks back foes ahead of you.",
                    PerkCategory.Mobility,
                    PerkRarity.Rare,
                    710,
                    "SB"
                )
            },
            {
                ShrinePerkType.SolarRadiance,
                new ShrinePerkData(
                    ShrinePerkType.SolarRadiance,
                    "SOLAR RADIANCE",
                    "太陽光輝",
                    "At full Mana, emits blinding solar beams that disorient enemies and boost your speed by +25%.",
                    PerkCategory.Energy,
                    PerkRarity.Rare,
                    760,
                    "So"
                )
            },
            {
                ShrinePerkType.OverdriveCore,
                new ShrinePerkData(
                    ShrinePerkType.OverdriveCore,
                    "OVERDRIVE CORE",
                    "限界突破炉",
                    "Freezes natural spin decay for the first 8s of match, and accelerates all ability cooldowns by 25%.",
                    PerkCategory.Energy,
                    PerkRarity.Epic,
                    1100,
                    "OD"
                )
            },
            {
                ShrinePerkType.AegisBarrier,
                new ShrinePerkData(
                    ShrinePerkType.AegisBarrier,
                    "AEGIS BARRIER",
                    "絶対防壁",
                    "Solid kinetic shield that reduces incoming collision damage by 40% when below 50% Spin.",
                    PerkCategory.Defense,
                    PerkRarity.Epic,
                    1000,
                    "AB"
                )
            },
            {
                ShrinePerkType.CosmicSynergy,
                new ShrinePerkData(
                    ShrinePerkType.CosmicSynergy,
                    "COSMIC SYNERGY",
                    "宇宙共鳴",
                    "Heavy impacts instantly restore +18 Mana and reduce current ability cooldowns by 2.0s.",
                    PerkCategory.Energy,
                    PerkRarity.Epic,
                    1150,
                    "CY"
                )
            },
            {
                ShrinePerkType.TitanCrusher,
                new ShrinePerkData(
                    ShrinePerkType.TitanCrusher,
                    "TITAN CRUSHER",
                    "巨人粉砕",
                    "Colossal head-on smash! Collisions above 5.8 m/s deal +50% bonus critical damage and trigger an explosive shockwave.",
                    PerkCategory.Combat,
                    PerkRarity.Epic,
                    1120,
                    "TC"
                )
            },
            {
                ShrinePerkType.NullifyArmor,
                new ShrinePerkData(
                    ShrinePerkType.NullifyArmor,
                    "NULLIFY ARMOR",
                    "完全無効装甲",
                    "Generates a kinetic buffer every 7 seconds that completely absorbs and negates 1 full enemy collision impact.",
                    PerkCategory.Defense,
                    PerkRarity.Epic,
                    1050,
                    "NA"
                )
            },
            {
                ShrinePerkType.GravityWell,
                new ShrinePerkData(
                    ShrinePerkType.GravityWell,
                    "GRAVITY WELL",
                    "重力渦発生",
                    "Gravitationally pulls nearby opponents toward the center dish whenever you occupy the lower bowl.",
                    PerkCategory.Mobility,
                    PerkRarity.Epic,
                    1150,
                    "GW"
                )
            },
            {
                ShrinePerkType.SupernovaBurst,
                new ShrinePerkData(
                    ShrinePerkType.SupernovaBurst,
                    "SUPERNOVA BURST",
                    "超新星爆発",
                    "When bursting an opponent, trigger a massive stadium shockwave that deals 35 spin damage to all remaining enemies.",
                    PerkCategory.Combat,
                    PerkRarity.Epic,
                    1180,
                    "SB"
                )
            },
            {
                ShrinePerkType.EternalDynamo,
                new ShrinePerkData(
                    ShrinePerkType.EternalDynamo,
                    "ETERNAL DYNAMO",
                    "永久発電炉",
                    "Max Mana pool increased by +50 and Mana regenerates at 200% speed when spinning above 60% spin stamina.",
                    PerkCategory.Energy,
                    PerkRarity.Epic,
                    1140,
                    "ED"
                )
            },
            {
                ShrinePerkType.ValkyrieFlight,
                new ShrinePerkData(
                    ShrinePerkType.ValkyrieFlight,
                    "VALKYRIE FLIGHT",
                    "戦乙女の飛翔",
                    "Boost grants complete immunity to wall friction and allows launching airborne ramps for high-dive aerial smashes.",
                    PerkCategory.Mobility,
                    PerkRarity.Epic,
                    1160,
                    "VF"
                )
            },
            {
                ShrinePerkType.DiamondCarapace,
                new ShrinePerkData(
                    ShrinePerkType.DiamondCarapace,
                    "DIAMOND CARAPACE",
                    "金剛甲殻",
                    "+50 Defense. Completely prevents spin drain from glancing enemy scratches and reduces maximum knockback received by 50%.",
                    PerkCategory.Defense,
                    PerkRarity.Epic,
                    1080,
                    "DC"
                )
            },
            {
                ShrinePerkType.PhoenixRebirth,
                new ShrinePerkData(
                    ShrinePerkType.PhoenixRebirth,
                    "PHOENIX REBIRTH",
                    "不死鳥の転生",
                    "ONCE PER RUN: If defeated, instantly resurrect with 65% Spin Stamina and a massive flame shockwave.",
                    PerkCategory.Defense,
                    PerkRarity.Legendary,
                    1650,
                    "PR"
                )
            },
            {
                ShrinePerkType.PlasmaCoil,
                new ShrinePerkData(
                    ShrinePerkType.PlasmaCoil,
                    "PLASMA COIL",
                    "超高圧電磁界",
                    "Surrounds your Bey with high-voltage plasma. Dealing collision damage shocks the target for 25% of their current spin over 3s.",
                    PerkCategory.Combat,
                    PerkRarity.Legendary,
                    1600,
                    "PC"
                )
            },
            {
                ShrinePerkType.ChronosShift,
                new ShrinePerkData(
                    ShrinePerkType.ChronosShift,
                    "CHRONOS SHIFT",
                    "時空跳躍",
                    "Boost speed increased by +45%. Double-tapping Boost performs an instantaneous phase-dash forward.",
                    PerkCategory.Mobility,
                    PerkRarity.Legendary,
                    1700,
                    "C$"
                )
            },
            {
                ShrinePerkType.DragonHeart,
                new ShrinePerkData(
                    ShrinePerkType.DragonHeart,
                    "DRAGON HEART",
                    "龍心覚醒",
                    "Awakens ultimate dragon vitality: Doubles all combat Mana gains, cuts all ability costs by 35%, and empowers special moves with critical strikes.",
                    PerkCategory.Energy,
                    PerkRarity.Legendary,
                    1750,
                    "DH"
                )
            },
            {
                ShrinePerkType.CelestialSingularity,
                new ShrinePerkData(
                    ShrinePerkType.CelestialSingularity,
                    "CELESTIAL SINGULARITY",
                    "天体特異点",
                    "Creates a perpetual gravitational pull around your Bey that continuously drains 8 spin/sec from all opponents within 6.0m.",
                    PerkCategory.Combat,
                    PerkRarity.Legendary,
                    1800,
                    "CS"
                )
            },
        };

        public static ShrinePerkData GetPerk(ShrinePerkType type)
        {
            return perks.TryGetValue(type, out var data) ? data : null;
        }

        public static IEnumerable<ShrinePerkData> GetAllPerks()
        {
            return perks.Values;
        }
    }
}
