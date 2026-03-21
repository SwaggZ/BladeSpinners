using System.Collections.Generic;
using UnityEngine;

namespace BladeSpinners.Abilities
{
    public static class AbilityFactory
    {
        public static List<BeyAbility> CreateRuntimeAbilityPool()
        {
            return new List<BeyAbility>
            {
                // Original abilities
                ScriptableObject.CreateInstance<DashAbility>(),
                ScriptableObject.CreateInstance<ShieldAbility>(),
                ScriptableObject.CreateInstance<SpinDrainAbility>(),
                ScriptableObject.CreateInstance<GroundPoundAbility>(),
                ScriptableObject.CreateInstance<FlashStepAbility>(),
                ScriptableObject.CreateInstance<PoisonCloudAbility>(),
                ScriptableObject.CreateInstance<DragonBurstAbility>(),
                ScriptableObject.CreateInstance<GravityClashAbility>(),
                // New thematic abilities
                ScriptableObject.CreateInstance<FreezeAbility>(),
                ScriptableObject.CreateInstance<FireBoltAbility>(),
                ScriptableObject.CreateInstance<ChainLightningAbility>(),
                ScriptableObject.CreateInstance<VampireDrainAbility>(),
                ScriptableObject.CreateInstance<BerserkAbility>(),
                ScriptableObject.CreateInstance<ThornsAbility>(),
                ScriptableObject.CreateInstance<TidalWaveAbility>(),
                ScriptableObject.CreateInstance<SolarFlareAbility>(),
                ScriptableObject.CreateInstance<SerpentCoilAbility>(),
                ScriptableObject.CreateInstance<MirageCloneAbility>(),
                ScriptableObject.CreateInstance<RicochetShotAbility>(),
                ScriptableObject.CreateInstance<LuckyStarAbility>()
            };
        }

        /// <summary>
        /// Creates a single instance of each ability type keyed by ability class name.
        /// Used by FaceBoltAbilityResolver for name-based lookups.
        /// </summary>
        public static Dictionary<System.Type, BeyAbility> CreateAbilityInstanceMap()
        {
            var pool = CreateRuntimeAbilityPool();
            var map = new Dictionary<System.Type, BeyAbility>();
            foreach (BeyAbility ability in pool)
            {
                System.Type t = ability.GetType();
                if (!map.ContainsKey(t))
                    map[t] = ability;
            }
            return map;
        }
    }
}
