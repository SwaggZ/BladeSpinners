using UnityEngine;
using BladeSpinners.Audio;

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
        [Min(0f)]
        [Tooltip("Seconds before this ability can be cast again. Leave at 0 for automatic rarity/cost-based tuning.")]
        protected float cooldownDuration = 0f;

        [SerializeField]
        protected Core.AbilityRarity rarity = Core.AbilityRarity.Common;

        [SerializeField]
        protected Sprite icon;

        public string AbilityName => abilityName;
        public string Description => description;
        public float ManaCost => manaCost;
        public float CooldownDuration =>
            cooldownDuration > 0f
                ? cooldownDuration
                : CalculateAutomaticCooldown(manaCost, rarity);
        public Core.AbilityRarity Rarity => rarity;
        public Sprite Icon => icon;

        /// <summary>
        /// Provides useful per-ability defaults without requiring every existing asset
        /// to be reauthored. Individual assets can override the serialized duration.
        /// </summary>
        public static float CalculateAutomaticCooldown(
            float baseManaCost, Core.AbilityRarity abilityRarity)
        {
            float rarityBase = abilityRarity switch
            {
                Core.AbilityRarity.Common => 2.5f,
                Core.AbilityRarity.Uncommon => 3.25f,
                Core.AbilityRarity.Rare => 4f,
                Core.AbilityRarity.Legendary => 5f,
                _ => 3f
            };

            return rarityBase + Mathf.Clamp(baseManaCost, 0f, 100f) * 0.02f;
        }

        /// <summary>
        /// Called when the player activates this ability.
        /// The BeyConfiguration has already verified mana availability and deducted the cost.
        /// </summary>
        /// <param name="beyController">The BeyMovementController that owns this ability.</param>
        public abstract void Activate(Gameplay.Movement.BeyMovementController beyController);

        /// <summary>
        /// Central activation entry point used by gameplay. It keeps each ability's
        /// folder-based audio cue paired with successful activations.
        /// </summary>
        public void ActivateWithAudio(Gameplay.Movement.BeyMovementController beyController)
        {
            Activate(beyController);

            if (beyController != null)
                SoundManager.PlayAbility(AbilityName, beyController.transform.position);
        }

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
