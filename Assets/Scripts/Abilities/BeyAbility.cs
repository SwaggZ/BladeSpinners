using UnityEngine;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Base class for all Beyblade abilities equipped on Face Bolts.
    /// Abilities are activated by the player and consume mana from the Energy Ring.
    /// </summary>
    public abstract class BeyAbility : ScriptableObject
    {
        [SerializeField]
        protected string abilityName = "New Ability";

        [SerializeField]
        [TextArea(2, 4)]
        protected string description = "";

        [SerializeField]
        protected float manaCost = 50f;

        [SerializeField]
        protected Core.AbilityRarity rarity = Core.AbilityRarity.Common;

        [SerializeField]
        protected Sprite icon;

        public string AbilityName => abilityName;
        public string Description => description;
        public float ManaCost => manaCost;
        public Core.AbilityRarity Rarity => rarity;
        public Sprite Icon => icon;

        /// <summary>
        /// Called when the player activates this ability.
        /// The BeyConfiguration has already verified mana availability and deducted the cost.
        /// </summary>
        /// <param name="beyController">The BeyMovementController that owns this ability.</param>
        public abstract void Activate(Gameplay.Movement.BeyMovementController beyController);

        /// <summary>
        /// Called every frame while the ability might be active (e.g., for channeled abilities).
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Optional: Called when the ability is deactivated.
        /// </summary>
        public virtual void Deactivate()
        {
        }
    }
}
