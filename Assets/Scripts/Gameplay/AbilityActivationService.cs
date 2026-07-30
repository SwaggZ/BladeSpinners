using BladeSpinners.Abilities;
using BladeSpinners.Gameplay.Effects;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Shared player/AI ability activation path. A successful commit is the only place
    /// gameplay spends ability mana and starts a cooldown.
    /// </summary>
    public static class AbilityActivationService
    {
        public static bool TryActivateEquipped(
            BeyConfiguration configuration,
            BeyMovementController movementController)
        {
            if (configuration == null || movementController == null)
                return false;

            BeyAbility ability =
                configuration.GetStatBlock().EquippedAbility;
            if (!configuration.TryCommitAbilityUse(
                    ability, out _))
            {
                return false;
            }

            ability.ActivateWithAudio(movementController);
            AbilityEmblemHologramEffect.Spawn(movementController);
            return true;
        }
    }
}
