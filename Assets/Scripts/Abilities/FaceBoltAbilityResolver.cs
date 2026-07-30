using System;
using System.Collections.Generic;
using System.Reflection;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;
using UnityEngine;

namespace BladeSpinners.Abilities
{
    public static class FaceBoltAbilityResolver
    {
        private static readonly Dictionary<string, BeyAbility> AssignedByFaceBoltId = new Dictionary<string, BeyAbility>();
        private static readonly List<BeyAbility> AbilityPool = AbilityFactory.CreateRuntimeAbilityPool();
        private static readonly Dictionary<Type, BeyAbility> InstanceMap = AbilityFactory.CreateAbilityInstanceMap();

        /// <summary>
        /// Maps face bolt names (case-insensitive) to specific ability types.
        /// Thematically matched so each bey's identity reflects its ability.
        /// </summary>
        private static readonly Dictionary<string, Type> NameToAbilityType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Freeze / Ice ──
            { "Arctic Fox", typeof(FreezeAbility) },
            { "Blizzard", typeof(FreezeAbility) },
            { "Frost Bite", typeof(FreezeAbility) },
            { "Glacier", typeof(FreezeAbility) },
            { "Sapphire Edge", typeof(FreezeAbility) },
            { "Eternal Frost", typeof(IceShardAbility) },
            { "Frostfang", typeof(IceShardAbility) },
            { "Coldsnap", typeof(IceShardAbility) },
            { "Icebreaker", typeof(FreezeAbility) },
            { "Glacial Wrath", typeof(IceShardAbility) },
            { "Frostfire", typeof(IceShardAbility) },

            // ── Fire / Flame ──
            { "Ashen Wolf", typeof(FireBoltAbility) },
            { "Crimson Slash", typeof(FireBoltAbility) },
            { "Inferno", typeof(InfernoAbility) },
            { "Wildfire", typeof(FireBoltAbility) },
            { "Ashbringer", typeof(InfernoAbility) },
            { "Ashfall", typeof(InfernoAbility) },
            { "Cinderpaw", typeof(FireBoltAbility) },
            { "Duskfire", typeof(InfernoAbility) },
            { "Ember", typeof(FireBoltAbility) },
            { "Eternal Flame", typeof(MoltenRainAbility) },
            { "Foxfire", typeof(FireBoltAbility) },
            { "Pyreborn", typeof(MoltenRainAbility) },
            { "Molten Crown", typeof(MoltenRainAbility) },
            { "Flintlock", typeof(StaticDischargeAbility) },

            // ── Lightning / Storm ──
            { "Chain Lightning", typeof(ChainLightningAbility) },
            { "Electra", typeof(ChainLightningAbility) },
            { "Tempest", typeof(ChainLightningAbility) },
            { "Thunder Bolt", typeof(ThunderClapAbility) },
            { "Stormcaller", typeof(ThunderClapAbility) },
            { "Stormking", typeof(ThunderClapAbility) },
            { "Stormveil", typeof(ChainLightningAbility) },
            { "Cobalt Rush", typeof(ThunderClapAbility) },

            // ── Wind / Tornado ──
            { "Galeforce", typeof(TornadoAbility) },
            { "Galestrike", typeof(RazorWindAbility) },
            { "Windcutter", typeof(RazorWindAbility) },
            { "Sandstorm", typeof(WhirlwindAbility) },
            { "Phantom Storm", typeof(TornadoAbility) },

            // ── Water / Tide ──
            { "Emerald Storm", typeof(TidalWaveAbility) },
            { "Tidal Wave", typeof(TidalWaveAbility) },
            { "Abyssal Tide", typeof(TidalWaveAbility) },
            { "Crimson Tide", typeof(TidalWaveAbility) },
            { "Tidecaller", typeof(TidalWaveAbility) },
            { "Undertow", typeof(WhirlwindAbility) },
            { "Iron Tide", typeof(TidalWaveAbility) },
            { "Brackwater", typeof(AcidSprayAbility) },
            { "Mudslide", typeof(EarthquakeAbility) },
            { "Mudpaw", typeof(GroundPoundAbility) },
            { "Silt", typeof(GroundPoundAbility) },
            { "Siltcrawler", typeof(GroundPoundAbility) },
            { "Driftwood", typeof(WhirlwindAbility) },

            // ── Earth / Rock ──
            { "Iron Claw", typeof(GroundPoundAbility) },
            { "Boulderback", typeof(EarthquakeAbility) },
            { "Cragback", typeof(EarthquakeAbility) },
            { "Gravel", typeof(GroundPoundAbility) },
            { "Gravelmaw", typeof(EarthquakeAbility) },
            { "Pebble", typeof(GroundPoundAbility) },
            { "Stone Guard", typeof(IronFortressAbility) },
            { "Stonecrusher", typeof(EarthquakeAbility) },
            { "Stonefist", typeof(GroundPoundAbility) },

            // ── Poison / Nature ──
            { "Plague Doctor", typeof(PoisonCloudAbility) },
            { "Scorpion", typeof(PoisonCloudAbility) },
            { "Venom Fang", typeof(PoisonCloudAbility) },
            { "Acid Rain", typeof(AcidSprayAbility) },
            { "Bramble", typeof(ThornsAbility) },
            { "Briar Patch", typeof(ThornsAbility) },
            { "Nettleback", typeof(ThornsAbility) },
            { "Thornqueen", typeof(ThornsAbility) },
            { "Thornwall", typeof(ThornsAbility) },
            { "Thornweed", typeof(ThornsAbility) },
            { "Venomstrike", typeof(PoisonCloudAbility) },
            { "Mirewalker", typeof(AcidSprayAbility) },
            { "Bog Trotter", typeof(PoisonCloudAbility) },
            { "Mossbark", typeof(RegenerationAbility) },
            { "Verdant", typeof(RegenerationAbility) },
            { "Pinecone", typeof(RegenerationAbility) },
            { "Tumbleweed", typeof(RazorWindAbility) },
            { "Splinter", typeof(RicochetShotAbility) },

            // ── Thorns / Defense ──
            { "Deflector", typeof(ThornsAbility) },
            { "Thornback", typeof(ThornsAbility) },
            { "Barrier", typeof(ShieldAbility) },
            { "Warden", typeof(ShieldAbility) },

            // ── Metal / Armor ──
            { "Ironbark", typeof(IronFortressAbility) },
            { "Ironclad", typeof(IronFortressAbility) },
            { "Ironhide", typeof(IronFortressAbility) },
            { "Ironveil", typeof(ShieldAbility) },
            { "Coppercoil", typeof(MagneticFieldAbility) },
            { "Copperhead", typeof(StaticDischargeAbility) },
            { "Rustblade", typeof(AcidSprayAbility) },
            { "Patchwork", typeof(IronFortressAbility) },

            // ── Dark / Shadow / Void ──
            { "Night Stalker", typeof(FlashStepAbility) },
            { "Obsidian", typeof(GravityClashAbility) },
            { "Dusk Blade", typeof(ShadowStrikeAbility) },
            { "Duskwarden", typeof(NightfallAbility) },
            { "Umbra", typeof(NightfallAbility) },
            { "Voidheart", typeof(VoidPulseAbility) },
            { "Voidwalker", typeof(VoidPulseAbility) },
            { "Wraithblade", typeof(ShadowStrikeAbility) },
            { "Wraithcoil", typeof(SpectralChainsAbility) },
            { "Cursed Steel", typeof(SoulLinkAbility) },
            { "Necrotic Edge", typeof(SpinDrainAbility) },
            { "Gravestone", typeof(NightfallAbility) },
            { "Greymantle", typeof(NightfallAbility) },
            { "Hellbound", typeof(BlackHoleAbility) },

            // ── Dragon / Beast ──
            { "Dragonheart", typeof(DragonBurstAbility) },
            { "Shadow Wyrm", typeof(DragonBurstAbility) },
            { "Serpent King", typeof(SerpentCoilAbility) },
            { "Amber Bite", typeof(SerpentCoilAbility) },
            { "Jade Fang", typeof(SerpentCoilAbility) },
            { "Lunar Fang", typeof(PhantomSlashAbility) },
            { "Scarlet Fang", typeof(BerserkAbility) },
            { "Amber Gale", typeof(TornadoAbility) },

            // ── Blood / Vampire / Drain ──
            { "Blood Moon", typeof(VampireDrainAbility) },
            { "Soul Reaver", typeof(VampireDrainAbility) },
            { "Death Dealer", typeof(SpinDrainAbility) },
            { "Hexblade", typeof(SpinDrainAbility) },
            { "Void Reaper", typeof(SpinDrainAbility) },
            { "Gorebound", typeof(BloodPactAbility) },
            { "Sanguine Blade", typeof(BloodPactAbility) },
            { "Soul Eater", typeof(VampireDrainAbility) },

            // ── Speed / Dash ──
            { "Swiftfoot", typeof(DashAbility) },
            { "Dustwalker", typeof(AdrenalineRushAbility) },

            // ── Berserk / Rage ──
            { "Berserker", typeof(BerserkAbility) },
            { "Gilded Wrath", typeof(BerserkAbility) },
            { "Rampager", typeof(BerserkAbility) },

            // ── Ricochet / Multi-hit ──
            { "Buzz Blade", typeof(RicochetShotAbility) },
            { "Multishot", typeof(RicochetShotAbility) },
            { "Phantom Arrow", typeof(RicochetShotAbility) },
            { "Ricochet", typeof(RicochetShotAbility) },

            // ── Mirage / Clone ──
            { "Eclipse", typeof(MirageCloneAbility) },
            { "Mirage", typeof(MirageCloneAbility) },
            { "Phantom", typeof(ChronoRecallAbility) },

            // ── Solar / Cosmic ──
            { "Magma Core", typeof(SolarFlareAbility) },
            { "Solar Flare", typeof(SolarFlareAbility) },
            { "Supernova", typeof(SolarFlareAbility) },
            { "Starfall", typeof(MeteorStrikeAbility) },
            { "Celestial Edge", typeof(ArcaneNovaAbility) },
            { "Dawnbreaker", typeof(SolarFlareAbility) },
            { "Genesis", typeof(TimeWarpAbility) },

            // ── Arcane / Magic ──
            { "Arcane Fury", typeof(ArcaneNovaAbility) },
            { "Spellweaver", typeof(ArcaneNovaAbility) },
            { "Runebreaker", typeof(PhaseShiftAbility) },
            { "Runic Edge", typeof(ArcaneNovaAbility) },
            { "Crystalline", typeof(CrystalBarrageAbility) },

            // ── War / Titan / Power ──
            { "Lucky Star", typeof(LuckyStarAbility) },
            { "Apex", typeof(OverchargeAbility) },
            { "Ragnarok", typeof(MeteorStrikeAbility) },
            { "Ruinbringer", typeof(GravityWellAbility) },
            { "Titan's Wrath", typeof(WarCryAbility) },
            { "Worldender", typeof(BlackHoleAbility) },

            // ── Misc thematic matches ──
            { "Gravity Well", typeof(GravityWellAbility) },
        };

        public static BeyAbility Resolve(BeyPart faceBolt)
        {
            if (faceBolt == null || faceBolt.PartType != PartType.FaceBolt)
                return null;

            if (faceBolt.EquippedAbility != null)
                return faceBolt.EquippedAbility;

            // 1. Try exact name match
            string partName = NormalizeFaceBoltName(faceBolt.PartName, faceBolt.PartID);
            if (!string.IsNullOrEmpty(partName) && NameToAbilityType.TryGetValue(partName, out Type abilityType))
            {
                if (InstanceMap.TryGetValue(abilityType, out BeyAbility namedAbility))
                    return namedAbility;
            }

            if (AbilityPool.Count == 0)
                return null;

            // 2. Fall back to deterministic hash assignment (unique per bolt where possible)
            string key = BuildFaceBoltKey(faceBolt);
            if (AssignedByFaceBoltId.TryGetValue(key, out BeyAbility existing) && existing != null)
                return existing;

            int startIndex = Math.Abs(key.GetHashCode()) % AbilityPool.Count;
            HashSet<BeyAbility> used = new HashSet<BeyAbility>(AssignedByFaceBoltId.Values);
            for (int offset = 0; offset < AbilityPool.Count; offset++)
            {
                int index = (startIndex + offset) % AbilityPool.Count;
                BeyAbility candidate = AbilityPool[index];
                if (!used.Contains(candidate))
                {
                    AssignedByFaceBoltId[key] = candidate;
                    return candidate;
                }
            }

            BeyAbility fallback = AbilityPool[startIndex];
            BeyAbility uniqueVariant = CreateRuntimeUniqueVariant(fallback, key);
            AssignedByFaceBoltId[key] = uniqueVariant != null ? uniqueVariant : fallback;
            return AssignedByFaceBoltId[key];
        }

        private static BeyAbility CreateRuntimeUniqueVariant(BeyAbility template, string key)
        {
            if (template == null)
                return null;

            BeyAbility variant = UnityEngine.Object.Instantiate(template);
            ApplyRuntimeUniqueTuning(variant, key);
            return variant;
        }

        private static void ApplyRuntimeUniqueTuning(BeyAbility ability, string key)
        {
            if (ability == null)
                return;

            int baseHash = (key ?? string.Empty).GetHashCode();

            float Scale(float min, float max, int mix)
            {
                uint u = (uint)(baseHash ^ (mix * 73856093));
                float t = (u % 10000u) / 9999f;
                return Mathf.Lerp(min, max, t);
            }

            Type type = ability.GetType();
            while (type != null && type != typeof(ScriptableObject))
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field.IsStatic)
                        continue;

                    string name = field.Name;
                    if (name == "abilityName")
                    {
                        string current = field.GetValue(ability) as string;
                        string token = Mathf.Abs(baseHash).ToString("X");
                        field.SetValue(ability, string.IsNullOrWhiteSpace(current) ? $"Ability [{token}]" : $"{current} [{token}]");
                        continue;
                    }

                    if (name == "description")
                    {
                        string current = field.GetValue(ability) as string;
                        string token = Mathf.Abs(baseHash).ToString("X");
                        field.SetValue(ability, string.IsNullOrWhiteSpace(current) ? $"Unique runtime variant [{token}]" : $"{current} [{token}]");
                        continue;
                    }

                    if (field.FieldType == typeof(float))
                    {
                        float current = (float)field.GetValue(ability);
                        if (Mathf.Approximately(current, 0f))
                            continue;

                        float factor = Scale(0.82f, 1.28f, name.GetHashCode());
                        float value = current * factor;

                        string lower = name.ToLowerInvariant();
                        if (lower.Contains("duration")) value = Mathf.Clamp(value, 0.1f, 12f);
                        else if (lower.Contains("radius")) value = Mathf.Clamp(value, 0.2f, 12f);
                        else if (lower.Contains("damage") || lower.Contains("drain")) value = Mathf.Clamp(value, 0.1f, 200f);
                        else if (lower.Contains("speed") || lower.Contains("force") || lower.Contains("impulse")) value = Mathf.Clamp(value, 0.1f, 300f);
                        else value = Mathf.Clamp(value, -500f, 500f);

                        field.SetValue(ability, value);
                        continue;
                    }

                    if (field.FieldType == typeof(int))
                    {
                        int current = (int)field.GetValue(ability);
                        if (current == 0)
                            continue;

                        float factor = Scale(0.85f, 1.20f, name.GetHashCode());
                        int value = Mathf.RoundToInt(current * factor);
                        field.SetValue(ability, Mathf.Clamp(value, -10000, 10000));
                    }
                }

                type = type.BaseType;
            }
        }

        private static string BuildFaceBoltKey(BeyPart faceBolt)
        {
            string id = string.IsNullOrWhiteSpace(faceBolt.PartID) ? "NO_ID" : faceBolt.PartID.Trim();
            string name = string.IsNullOrWhiteSpace(faceBolt.PartName) ? "NO_NAME" : faceBolt.PartName.Trim();
            return id + "|" + name;
        }

        private static string NormalizeFaceBoltName(string partName, string partId)
        {
            string raw = string.IsNullOrWhiteSpace(partName) ? string.Empty : partName.Trim();
            if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(partId))
                raw = partId.Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            raw = raw.Replace("_", " ");

            if (raw.EndsWith(" FaceBolt", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(0, raw.Length - " FaceBolt".Length);

            if (raw.EndsWith(" Face Bolt", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(0, raw.Length - " Face Bolt".Length);

            int suffixIndex = raw.IndexOf(" facebolt ", StringComparison.OrdinalIgnoreCase);
            if (suffixIndex >= 0)
                raw = raw.Substring(0, suffixIndex);

            int idSuffixIndex = raw.IndexOf(" face bolt ", StringComparison.OrdinalIgnoreCase);
            if (idSuffixIndex >= 0)
                raw = raw.Substring(0, idSuffixIndex);

            return raw.Trim();
        }
    }
}
