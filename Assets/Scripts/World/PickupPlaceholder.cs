using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.World
{
    /// <summary>
    /// Collectible pickup that restores spin or reduces stamina drain on contact.
    /// Uses a trigger SphereCollider — any bey (player or enemy) can collect it.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PickupPlaceholder : MonoBehaviour
    {
        [SerializeField] private PickupType pickupType;

        private bool collected;

        public PickupType PickupType => pickupType;
        public bool IsCollected => collected;

        public void Initialize(PickupType type)
        {
            pickupType = type;
            collected = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            // Only the player can collect pickups
            var player = other.GetComponentInParent<BladeSpinners.Gameplay.PlayerManager>();
            if (player == null) return;

            BeyConfiguration config = player.BeyConfiguration;
            if (config == null) return;

            collected = true;
            ApplyPickup(config);
            Destroy(gameObject);
        }

        private void ApplyPickup(BeyConfiguration config)
        {
            switch (pickupType)
            {
                case PickupType.SpinSmall:
                    config.SetSpin(config.CurrentSpin + GameConstants.PICKUP_SPIN_SMALL);
                    Debug.Log($"[Pickup] +{GameConstants.PICKUP_SPIN_SMALL} spin (small)");
                    break;
                case PickupType.SpinMedium:
                    config.SetSpin(config.CurrentSpin + GameConstants.PICKUP_SPIN_MEDIUM);
                    Debug.Log($"[Pickup] +{GameConstants.PICKUP_SPIN_MEDIUM} spin (medium)");
                    break;
                case PickupType.SpinLarge:
                    config.SetSpin(config.CurrentSpin + GameConstants.PICKUP_SPIN_LARGE);
                    Debug.Log($"[Pickup] +{GameConstants.PICKUP_SPIN_LARGE} spin (large)");
                    break;
                case PickupType.StaminaTemporary:
                    // Restore a moderate amount of mana as a proxy for stamina buff
                    float manaRestore = GameConstants.DEFAULT_MANA_POOL * 0.3f;
                    config.SetMana(config.CurrentMana + manaRestore);
                    Debug.Log($"[Pickup] +{manaRestore} mana (stamina pickup)");
                    break;
            }
        }
    }
}
