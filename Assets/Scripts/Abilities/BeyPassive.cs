using UnityEngine;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Immutable definition for an Energy Ring passive. Runtime counters and cooldowns
    /// live in EnergyRingPassiveRuntime so one definition can safely be shared by every
    /// player and enemy using that passive.
    /// </summary>
    public abstract class BeyPassive : ScriptableObject
    {
        [SerializeField]
        protected string passiveName = "New Passive";

        [SerializeField]
        [TextArea(2, 4)]
        protected string description = "";

        [SerializeField]
        protected Core.AbilityRarity rarity = Core.AbilityRarity.Common;

        [SerializeField]
        protected Sprite icon;

        public string PassiveName => passiveName;
        public string Description => description;
        public Core.AbilityRarity Rarity => rarity;
        public Sprite Icon => icon;

        internal void ConfigureRuntimeMetadata(
            string displayName,
            string passiveDescription,
            Core.AbilityRarity passiveRarity)
        {
            passiveName = displayName;
            description = passiveDescription;
            rarity = passiveRarity;
            name = $"{displayName} (Runtime)";
            hideFlags = HideFlags.HideAndDontSave;
        }

        public virtual void OnEquipped(EnergyRingPassiveRuntime runtime)
        {
        }

        public virtual void OnUnequipped(EnergyRingPassiveRuntime runtime)
        {
        }

        public virtual void Tick(
            EnergyRingPassiveRuntime runtime,
            float deltaTime)
        {
        }

        public virtual float ModifyOutgoingCollisionDamage(
            EnergyRingPassiveRuntime runtime,
            BeyConfiguration target,
            float damage)
        {
            return damage;
        }

        public virtual float ModifyIncomingCollisionDamage(
            EnergyRingPassiveRuntime runtime,
            BeyConfiguration source,
            float damage)
        {
            return damage;
        }

        public virtual float ModifyPassiveSpinDrain(
            EnergyRingPassiveRuntime runtime,
            float drain)
        {
            return drain;
        }

        public virtual float ModifyManaRegeneration(
            EnergyRingPassiveRuntime runtime,
            float regeneration)
        {
            return regeneration;
        }

        public virtual float ModifyPickupAmount(
            EnergyRingPassiveRuntime runtime,
            float amount)
        {
            return amount;
        }

        public virtual void OnCollisionWithBey(
            EnergyRingPassiveRuntime runtime,
            EnergyRingCollisionInfo collision)
        {
        }

        public virtual void OnCollisionDamageTaken(
            EnergyRingPassiveRuntime runtime,
            BeyConfiguration source,
            float damageTaken)
        {
        }

        public virtual void OnSpinChanged(
            EnergyRingPassiveRuntime runtime,
            float previousSpin,
            float newSpin)
        {
        }

        public virtual void OnManaSpent(
            EnergyRingPassiveRuntime runtime,
            float amount)
        {
        }
    }
}
