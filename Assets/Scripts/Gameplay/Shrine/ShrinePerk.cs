using System.Collections.Generic;
using UnityEngine;

namespace BladeSpinners.Gameplay.Shrine
{
    public enum ShrinePerkType
    {
        VampiricSpin,
        TitaniumTip,
        StaticOverload,
        HeavyweightCore,
        OverdriveCore,
        TurboRip,
        MagnetoRing,
        PhoenixRebirth
    }

    public enum PerkRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
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
        public Color ThemeColor { get; }

        public ShrinePerkData(
            ShrinePerkType type,
            string name,
            string japaneseName,
            string description,
            PerkCategory category,
            PerkRarity rarity,
            int baseCost,
            string iconSymbol,
            Color themeColor)
        {
            Type = type;
            Name = name;
            JapaneseName = japaneseName;
            Description = description;
            Category = category;
            Rarity = rarity;
            BaseCost = baseCost;
            IconSymbol = iconSymbol;
            ThemeColor = themeColor;
        }
    }

    public static class ShrinePerkCatalog
    {
        private static readonly Dictionary<ShrinePerkType, ShrinePerkData> perks = new Dictionary<ShrinePerkType, ShrinePerkData>
        {
            {
                ShrinePerkType.VampiricSpin,
                new ShrinePerkData(
                    ShrinePerkType.VampiricSpin,
                    "VAMPIRIC SPIN",
                    "吸血の回転",
                    "Absorb 15% of all collision and ability damage dealt to opponents back as Spin Stamina.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    250,
                    "[VAMP]",
                    new Color(0.95f, 0.22f, 0.35f, 1f)
                )
            },
            {
                ShrinePerkType.TitaniumTip,
                new ShrinePerkData(
                    ShrinePerkType.TitaniumTip,
                    "TITANIUM TIP",
                    "チタン製軸先",
                    "40% reduced friction decay on slopes. Enhanced drift acceleration when climbing the dish.",
                    PerkCategory.Mobility,
                    PerkRarity.Common,
                    175,
                    "[TIP]",
                    new Color(0.25f, 0.85f, 0.95f, 1f)
                )
            },
            {
                ShrinePerkType.StaticOverload,
                new ShrinePerkData(
                    ShrinePerkType.StaticOverload,
                    "STATIC OVERLOAD",
                    "電磁過負荷",
                    "Heavy collisions (>6 m/s) discharge chain lightning arcs to nearby enemies for 12 spin damage.",
                    PerkCategory.Combat,
                    PerkRarity.Rare,
                    260,
                    "[VOLT]",
                    new Color(0.2f, 0.75f, 1f, 1f)
                )
            },
            {
                ShrinePerkType.HeavyweightCore,
                new ShrinePerkData(
                    ShrinePerkType.HeavyweightCore,
                    "HEAVYWEIGHT CORE",
                    "重装甲コア",
                    "+35 Mass Weight. Increases wall-bounce elastic reflection force by 40% and knockback dealt by 25%.",
                    PerkCategory.Defense,
                    PerkRarity.Common,
                    180,
                    "[CORE]",
                    new Color(1f, 0.65f, 0.15f, 1f)
                )
            },
            {
                ShrinePerkType.OverdriveCore,
                new ShrinePerkData(
                    ShrinePerkType.OverdriveCore,
                    "OVERDRIVE CORE",
                    "限界突破炉",
                    "Mana regenerates 50% faster and ability cooldowns recover 25% faster during combat.",
                    PerkCategory.Energy,
                    PerkRarity.Rare,
                    220,
                    "[DRIVE]",
                    new Color(0.95f, 0.45f, 0.15f, 1f)
                )
            },
            {
                ShrinePerkType.TurboRip,
                new ShrinePerkData(
                    ShrinePerkType.TurboRip,
                    "TURBO RIP-CORD",
                    "超速発射紐",
                    "Launch sweet spot window widened by +50%. Perfect launches grant up to 140% starting spin!",
                    PerkCategory.Energy,
                    PerkRarity.Common,
                    150,
                    "[TURBO]",
                    new Color(1f, 0.88f, 0.2f, 1f)
                )
            },
            {
                ShrinePerkType.MagnetoRing,
                new ShrinePerkData(
                    ShrinePerkType.MagnetoRing,
                    "MAGNETO RING",
                    "磁力展開輪",
                    "Gravitationally attracts spin stamina pickups within 7m and drags lighter foes toward you.",
                    PerkCategory.Mobility,
                    PerkRarity.Epic,
                    320,
                    "[MAG]",
                    new Color(0.75f, 0.35f, 0.95f, 1f)
                )
            },
            {
                ShrinePerkType.PhoenixRebirth,
                new ShrinePerkData(
                    ShrinePerkType.PhoenixRebirth,
                    "PHOENIX REBIRTH",
                    "不死鳥の転生",
                    "ONCE PER RUN: If defeated, instantly resurrect with 60% Spin Stamina and a massive flame shockwave.",
                    PerkCategory.Defense,
                    PerkRarity.Legendary,
                    450,
                    "[PHX]",
                    new Color(1f, 0.8f, 0.1f, 1f)
                )
            }
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
