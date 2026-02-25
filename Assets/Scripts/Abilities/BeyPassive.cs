using UnityEngine;
using System;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Base class for all Beyblade passive abilities equipped on Energy Rings.
    /// Passive effects are always active and hook into combat and movement events.
    /// Each Energy Ring can have one passive in addition to owning mana stats.
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

        /// <summary>
        /// Called when this passive is equipped to an Energy Ring.
        /// Use to hook into events or initialize state.
        /// </summary>
        /// <param name="beyController">The BeyMovementController this passive is attached to.</param>
        public virtual void OnEquipped(Gameplay.Movement.BeyMovementController beyController)
        {
        }

        /// <summary>
        /// Called when this passive is unequipped.
        /// Use to unhook from events or cleanup state.
        /// </summary>
        public virtual void OnUnequipped()
        {
        }

        /// <summary>
        /// Called every frame while the passive is active.
        /// </summary>
        /// <param name="beyController">The BeyMovementController this passive is attached to.</param>
        public virtual void Update(Gameplay.Movement.BeyMovementController beyController)
        {
        }

        /// <summary>
        /// Called when this Bey collides with another Bey.
        /// </summary>
        public virtual void OnCollisionWithBey(object otherBeyInfo)
        {
        }

        /// <summary>
        /// Called when spin value changes.
        /// </summary>
        public virtual void OnSpinChanged(float newSpin)
        {
        }
    }
}
