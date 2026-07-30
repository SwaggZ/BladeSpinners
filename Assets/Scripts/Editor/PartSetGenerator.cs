using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Editor wizard: give it a name and a seed, it creates 5 BeyPart assets
    /// (Tip, Track, FusionWheel, EnergyRing, FaceBolt) with random stats driven by the seed.
    /// Each part gets its own meshSeed and color so every set looks unique.
    ///
    /// Menu: GameObject → Blade Spinners → Generate Part Set
    /// </summary>
    public class PartSetGenerator : EditorWindow
    {
        private static readonly System.Collections.Generic.HashSet<string> ensuredFolders =
            new System.Collections.Generic.HashSet<string>();

        private string setName = "MyBey";
        private int seed = 42;
        private RarityTier setRarity = RarityTier.Common;
        private Color mainColor = new Color(0.2f, 0.4f, 0.9f); // default blue
        private Sprite faceBoltEmblem;

        [MenuItem("Blade Spinners/Generate Part Set")]
        public static void ShowWindow()
        {
            var window = GetWindow<PartSetGenerator>("Part Set Generator");
            window.minSize = new Vector2(350, 180);
        }

        private void OnGUI()
        {
            GUILayout.Label("Generate a full Bey part set", EditorStyles.boldLabel);
            GUILayout.Space(8);

            setName = EditorGUILayout.TextField("Set Name", setName);
            seed = EditorGUILayout.IntField("Seed", seed);
            setRarity = (RarityTier)EditorGUILayout.EnumPopup("Set Rarity", setRarity);
            mainColor = EditorGUILayout.ColorField("Main Color", mainColor);
            faceBoltEmblem = (Sprite)EditorGUILayout.ObjectField("Face Bolt Emblem", faceBoltEmblem, typeof(Sprite), false);

            // Show the stat boost for the selected rarity
            float boost = GetRarityBoost(setRarity);
            EditorGUILayout.HelpBox(
                $"{setRarity} — stats boosted by {boost * 100f:0}%",
                boost > 0 ? MessageType.Info : MessageType.None);

            GUILayout.Space(8);

            if (GUILayout.Button("Generate Part Set", GUILayout.Height(30)))
            {
                GenerateSet(setName, seed, setRarity, mainColor, faceBoltEmblem);
            }
        }

        /// <summary>
        /// Creates 5 BeyPart ScriptableObject assets under Assets/Parts/{subfolder}/
        /// with random stats driven by the seed.
        /// </summary>
        public static void GenerateSet(
            string name,
            int seed,
            RarityTier rarity = RarityTier.Common,
            Color? color = null,
            Sprite faceBoltEmblem = null,
            bool saveAndRefresh = true)
        {
            System.Random rng = new System.Random(seed);
            float boost = GetRarityBoost(rarity);

            // Derive base HSV from chosen main color, or fall back to random
            float baseHue, baseSat, baseVal;
            if (color.HasValue)
            {
                Color.RGBToHSV(color.Value, out baseHue, out baseSat, out baseVal);
            }
            else
            {
                baseHue = (float)rng.NextDouble();
                baseSat = 0.6f;
                baseVal = 0.7f;
            }

            CreatePartAsset(name, PartType.Tip, rng, baseHue, baseSat, baseVal, rarity, boost, faceBoltEmblem);
            CreatePartAsset(name, PartType.Track, rng, baseHue, baseSat, baseVal, rarity, boost, faceBoltEmblem);
            CreatePartAsset(name, PartType.FusionWheel, rng, baseHue, baseSat, baseVal, rarity, boost, faceBoltEmblem);
            CreatePartAsset(name, PartType.EnergyRing, rng, baseHue, baseSat, baseVal, rarity, boost, faceBoltEmblem);
            CreatePartAsset(name, PartType.FaceBolt, rng, baseHue, baseSat, baseVal, rarity, boost, faceBoltEmblem);

            if (saveAndRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[PartSetGenerator] Created {rarity} part set \"{name}\" (seed {seed}, +{boost * 100f:0}%) — 5 assets in Assets/Parts/");
        }

        /// <summary>
        /// Returns the stat boost multiplier for a rarity tier.
        /// Common +0%, Uncommon +5%, Rare +10%, Epic +15%, Legendary +20%.
        /// </summary>
        public static float GetRarityBoost(RarityTier rarity)
        {
            return rarity switch
            {
                RarityTier.Common => 0f,
                RarityTier.Uncommon => 0.05f,
                RarityTier.Rare => 0.10f,
                RarityTier.Epic => 0.15f,
                RarityTier.Legendary => 0.20f,
                _ => 0f
            };
        }

        private static void CreatePartAsset(string setName, PartType type, System.Random rng, float baseHue,
            float baseSat, float baseVal, RarityTier rarity, float boost, Sprite faceBoltEmblem)
        {
            BeyPart part = ScriptableObject.CreateInstance<BeyPart>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            string typeName = type.ToString();
            string partName = $"{setName}_{typeName}";

            // === Identity ===
            part.GetType().GetField("partName", flags)?.SetValue(part, partName);
            part.GetType().GetField("partType", flags)?.SetValue(part, type);
            part.GetType().GetField("partID", flags)?.SetValue(part, $"{setName.ToLower()}_{typeName.ToLower()}_{rng.Next()}");
            part.GetType().GetField("occupiesSlots", flags)
                ?.SetValue(part, new System.Collections.Generic.List<PartType> { type });

            // === Rarity — all parts in a set share the chosen rarity ===
            part.GetType().GetField("rarity", flags)?.SetValue(part, rarity);

            // === Mesh seed (unique per part in the set) ===
            int meshSeed = rng.Next();
            part.GetType().GetField("meshSeed", flags)?.SetValue(part, meshSeed);

            // === Cohesive color from set's base color ===
            Color primaryColor = GeneratePartColor(type, rng, baseHue, baseSat, baseVal);
            Color secondaryColor = Color.Lerp(primaryColor, Color.white, 0.3f + (float)rng.NextDouble() * 0.4f);
            part.GetType().GetField("primaryColor", flags)?.SetValue(part, primaryColor);
            part.GetType().GetField("secondaryColor", flags)?.SetValue(part, secondaryColor);

            // === Type-specific random stats (boosted by rarity %) ===
            switch (type)
            {
                case PartType.Tip:
                    RandomizeTipStats(part, rng, flags, boost);
                    break;
                case PartType.Track:
                    RandomizeTrackStats(part, rng, flags, boost);
                    break;
                case PartType.FusionWheel:
                    RandomizeFusionWheelStats(part, rng, flags, boost);
                    break;
                case PartType.EnergyRing:
                    RandomizeEnergyRingStats(part, rng, flags, boost);
                    break;
                case PartType.FaceBolt:
                    // FaceBolt stats are just the equipped ability, leave null for now
                    part.GetType().GetField("faceBoltEmblem", flags)?.SetValue(part, faceBoltEmblem);
                    break;
            }

            // === Generate description ===
            part.GetType().GetField("description", flags)
                ?.SetValue(part, $"A {rarity} {typeName} from the {setName} set.");

            // === Save asset ===
            string folder = GetFolderForType(type);
            EnsureFolder(folder);
            string assetPath = $"{folder}/{partName}.asset";

            // If asset already exists, delete it first
            if (AssetDatabase.LoadAssetAtPath<BeyPart>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(part, assetPath);
            EditorUtility.SetDirty(part);
        }

        // ============================================================
        // STAT RANDOMIZATION
        // ============================================================

        private static void RandomizeTipStats(BeyPart part, System.Random rng, System.Reflection.BindingFlags flags, float boost)
        {
            float m = 1f + boost; // rarity multiplier

            // Random tip behavior (exclude Orbit from random generation; reserved for curated parts)
            TipBehaviorType[] tipTypes = GetRandomizableTipTypes();
            TipBehaviorType tipBehavior = tipTypes[rng.Next(tipTypes.Length)];
            part.GetType().GetField("tipBehavior", flags)?.SetValue(part, tipBehavior);

            // Stamina drain modifier: 0.5 – 2.5 (lower is better, so invert boost)
            float drainMod = (0.5f + (float)rng.NextDouble() * 2.0f) / m;
            part.GetType().GetField("behaviorBasedStaminaDrainModifier", flags)?.SetValue(part, drainMod);

            // Uphill resistance: 0.3 – 2.0 (higher is better)
            float uphill = (0.3f + (float)rng.NextDouble() * 1.7f) * m;
            part.GetType().GetField("uphillResistanceMultiplier", flags)?.SetValue(part, uphill);

            // Slope multiplier: 0.5 – 2.0 (higher is better)
            float slope = (0.5f + (float)rng.NextDouble() * 1.5f) * m;
            part.GetType().GetField("slopeMultiplier", flags)?.SetValue(part, slope);

            // 30% chance of having an alt tip behavior with threshold
            if (rng.NextDouble() < 0.3)
            {
                float threshold = 20f + (float)rng.NextDouble() * 60f; // 20–80 spin
                TipBehaviorType altTip = tipTypes[rng.Next(tipTypes.Length)];
                part.GetType().GetField("spinThreshold", flags)?.SetValue(part, threshold);
                part.GetType().GetField("altTipBehavior", flags)?.SetValue(part, altTip);
            }
            else
            {
                part.GetType().GetField("spinThreshold", flags)?.SetValue(part, -1f);
            }
        }

        private static TipBehaviorType[] GetRandomizableTipTypes()
        {
            TipBehaviorType[] all = (TipBehaviorType[])System.Enum.GetValues(typeof(TipBehaviorType));
            List<TipBehaviorType> filtered = new List<TipBehaviorType>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                TipBehaviorType t = all[i];
                if (t == TipBehaviorType.Orbit)
                    continue;
                filtered.Add(t);
            }

            if (filtered.Count == 0)
                filtered.Add(TipBehaviorType.Ball);

            return filtered.ToArray();
        }

        private static void RandomizeTrackStats(BeyPart part, System.Random rng, System.Reflection.BindingFlags flags, float boost)
        {
            float m = 1f + boost;

            // Track height: 0.5 – 2.5 (boosted)
            float height = GameConstants.MIN_TRACK_HEIGHT
                + (float)rng.NextDouble() * (GameConstants.MAX_TRACK_HEIGHT - GameConstants.MIN_TRACK_HEIGHT);
            height *= m;
            part.GetType().GetField("trackHeight", flags)?.SetValue(part, height);

            // Jump arc modifier: 0.5 – 1.5 (boosted)
            float jumpArc = (0.5f + (float)rng.NextDouble() * 1.0f) * m;
            part.GetType().GetField("jumpArcModifier", flags)?.SetValue(part, jumpArc);
        }

        private static void RandomizeFusionWheelStats(BeyPart part, System.Random rng, System.Reflection.BindingFlags flags, float boost)
        {
            float m = 1f + boost;

            // Weight: 10 – 50 (boosted)
            float weight = GameConstants.MIN_WEIGHT
                + (float)rng.NextDouble() * (GameConstants.MAX_WEIGHT - GameConstants.MIN_WEIGHT);
            weight *= m;
            part.GetType().GetField("weight", flags)?.SetValue(part, weight);

            // Mass-based stamina drain: 0.1 – 2.0 (lower is better, invert boost)
            float drain = (0.1f + (float)rng.NextDouble() * 1.9f) / m;
            part.GetType().GetField("massBasedStaminaDrainRate", flags)?.SetValue(part, drain);
        }

        private static void RandomizeEnergyRingStats(BeyPart part, System.Random rng, System.Reflection.BindingFlags flags, float boost)
        {
            float m = 1f + boost;

            // Mana pool: 50 – 300 (boosted)
            float pool = GameConstants.MIN_MANA_POOL
                + (float)rng.NextDouble() * (GameConstants.MAX_MANA_POOL - GameConstants.MIN_MANA_POOL);
            pool *= m;
            part.GetType().GetField("manaPoolSize", flags)?.SetValue(part, pool);

            // Mana regen: 5 – 50 (boosted)
            float regen = GameConstants.MIN_MANA_REGEN
                + (float)rng.NextDouble() * (GameConstants.MAX_MANA_REGEN - GameConstants.MIN_MANA_REGEN);
            regen *= m;
            part.GetType().GetField("manaRegenRate", flags)?.SetValue(part, regen);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Generates a cohesive color for a part type, derived from the set's chosen base color.
        /// All parts stay close to the user's chosen hue/sat/val with small per-type adjustments.
        /// </summary>
        private static Color GeneratePartColor(PartType type, System.Random rng, float baseHue, float baseSat, float baseVal)
        {
            float hue, sat, val;

            // Tiny hue drift (±0.02) — all parts stay very close to the chosen color
            float hueDrift = -0.02f + (float)rng.NextDouble() * 0.04f;

            switch (type)
            {
                case PartType.Tip:
                    // Slightly desaturated, slightly darker
                    hue = baseHue + hueDrift;
                    sat = Mathf.Clamp01(baseSat - 0.15f + (float)rng.NextDouble() * 0.1f);
                    val = Mathf.Clamp01(baseVal - 0.05f + (float)rng.NextDouble() * 0.1f);
                    break;
                case PartType.Track:
                    // Darker shade of the base color
                    hue = baseHue + hueDrift;
                    sat = Mathf.Clamp01(baseSat - 0.05f + (float)rng.NextDouble() * 0.1f);
                    val = Mathf.Clamp01(baseVal - 0.2f + (float)rng.NextDouble() * 0.1f);
                    break;
                case PartType.FusionWheel:
                    // Truest to the chosen color — signature part
                    hue = baseHue + hueDrift * 0.5f;
                    sat = Mathf.Clamp01(baseSat + (float)rng.NextDouble() * 0.1f);
                    val = Mathf.Clamp01(baseVal + (float)rng.NextDouble() * 0.1f);
                    break;
                case PartType.EnergyRing:
                    // Slightly lighter/brighter version
                    hue = baseHue + hueDrift;
                    sat = Mathf.Clamp01(baseSat - 0.05f + (float)rng.NextDouble() * 0.1f);
                    val = Mathf.Clamp01(baseVal + 0.05f + (float)rng.NextDouble() * 0.1f);
                    break;
                case PartType.FaceBolt:
                    // Brightest accent — high value
                    hue = baseHue + hueDrift;
                    sat = Mathf.Clamp01(baseSat - 0.1f + (float)rng.NextDouble() * 0.1f);
                    val = Mathf.Clamp01(baseVal + 0.1f + (float)rng.NextDouble() * 0.1f);
                    break;
                default:
                    hue = baseHue;
                    sat = baseSat;
                    val = baseVal;
                    break;
            }

            // Wrap hue to [0, 1]
            hue = hue - Mathf.Floor(hue);

            return Color.HSVToRGB(hue, sat, val);
        }

        private static string GetFolderForType(PartType type)
        {
            return type switch
            {
                PartType.Tip => "Assets/Parts/Tips",
                PartType.Track => "Assets/Parts/Tracks",
                PartType.FusionWheel => "Assets/Parts/Fusion Wheels",
                PartType.EnergyRing => "Assets/Parts/Energy Rings",
                PartType.FaceBolt => "Assets/Parts/Face Bolts",
                _ => "Assets/Parts"
            };
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            path = path.Trim();

            if (ensuredFolders.Contains(path))
                return;

            // Split the path and create each folder level
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                ensuredFolders.Add(next);
                current = next;
            }

            ensuredFolders.Add(path);
        }
    }
}
