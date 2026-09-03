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
                Create<DashAbility>(),
                Create<ShieldAbility>(),
                Create<SpinDrainAbility>(),
                Create<GroundPoundAbility>(),
                Create<FlashStepAbility>(),
                Create<PoisonCloudAbility>(),
                Create<DragonBurstAbility>(),
                Create<GravityClashAbility>(),
                // New thematic abilities
                Create<FreezeAbility>(),
                Create<FireBoltAbility>(),
                Create<ChainLightningAbility>(),
                Create<VampireDrainAbility>(),
                Create<BerserkAbility>(),
                Create<ThornsAbility>(),
                Create<TidalWaveAbility>(),
                Create<SolarFlareAbility>(),
                Create<SerpentCoilAbility>(),
                Create<MirageCloneAbility>(),
                Create<RicochetShotAbility>(),
                Create<LuckyStarAbility>(),

                // ── Expansion wave ──
                Create<MeteorStrikeAbility>(),
                Create<WhirlwindAbility>(),
                Create<ShadowStrikeAbility>(),
                Create<ThunderClapAbility>(),
                Create<IceShardAbility>(),
                Create<ArcaneNovaAbility>(),
                Create<VoidPulseAbility>(),
                Create<TornadoAbility>(),
                Create<PhantomSlashAbility>(),
                Create<IronFortressAbility>(),
                Create<PhaseShiftAbility>(),
                Create<TimeWarpAbility>(),
                Create<EarthquakeAbility>(),
                Create<AdrenalineRushAbility>(),
                Create<RegenerationAbility>(),
                Create<OverchargeAbility>(),
                Create<WarCryAbility>(),
                Create<MoltenRainAbility>(),
                Create<MagneticFieldAbility>(),
                Create<SoulLinkAbility>(),
                Create<GravityWellAbility>(),
                Create<NightfallAbility>(),
                Create<CrystalBarrageAbility>(),
                Create<InfernoAbility>(),
                Create<StaticDischargeAbility>(),
                Create<BlackHoleAbility>(),
                Create<RazorWindAbility>(),
                Create<AcidSprayAbility>(),
                Create<SpectralChainsAbility>(),
                Create<BloodPactAbility>(),
                Create<ChronoRecallAbility>()
            };
        }

        private static T Create<T>() where T : BeyAbility
        {
            T ability = ScriptableObject.CreateInstance<T>();
            ability.hideFlags = HideFlags.HideAndDontSave;
            return ability;
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
