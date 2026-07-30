using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.World
{
    /// <summary>
    /// Rechargeable spin or mana pickup. Collection consumes its current charge
    /// rather than destroying the arena object.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PickupPlaceholder : MonoBehaviour
    {
        [SerializeField] private PickupType pickupType;
        [SerializeField] private float rechargeDuration = 12f;
        [SerializeField] private float collectionLockout = 1.25f;

        private float charge = 1f;
        private float canCollectAt;

        public PickupType PickupType => pickupType;
        public bool IsCollected => charge < 0.999f;
        public float Charge01 => charge;
        public bool CanCollect =>
            Time.time >= canCollectAt && charge > 0.001f;

        public void Initialize(PickupType type)
        {
            pickupType = type;
            charge = 1f;
            canCollectAt = 0f;
        }

        private void Update()
        {
            AdvanceRecharge(Time.deltaTime);
        }

        public void AdvanceRecharge(float deltaTime)
        {
            if (charge >= 1f)
                return;

            charge = Mathf.MoveTowards(
                charge,
                1f,
                Mathf.Max(0f, deltaTime)
                    / Mathf.Max(0.1f, rechargeDuration));
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other);
        }

        private void TryCollect(Collider other)
        {
            if (!CanCollect)
                return;

            // Only the player can collect pickups
            var player = other.GetComponentInParent<BladeSpinners.Gameplay.PlayerManager>();
            if (player == null) return;

            BeyConfiguration config = player.BeyConfiguration;
            if (config == null) return;

            float consumedCharge = charge;
            charge = 0f;
            canCollectAt = Time.time + Mathf.Max(0f, collectionLockout);
            ApplyPickup(config, consumedCharge);
        }

        private void ApplyPickup(
            BeyConfiguration config,
            float chargeFraction)
        {
            switch (pickupType)
            {
                case PickupType.SpinSmall:
                    ApplySpinPickup(
                        config,
                        GameConstants.PICKUP_SPIN_SMALL,
                        "small",
                        chargeFraction);
                    break;
                case PickupType.SpinMedium:
                    ApplySpinPickup(
                        config,
                        GameConstants.PICKUP_SPIN_MEDIUM,
                        "medium",
                        chargeFraction);
                    break;
                case PickupType.SpinLarge:
                    ApplySpinPickup(
                        config,
                        GameConstants.PICKUP_SPIN_LARGE,
                        "large",
                        chargeFraction);
                    break;
                case PickupType.Mana:
                    float manaRestore = config.ModifyPickupAmount(
                        config.MaxMana * 0.3f * chargeFraction);
                    config.SetMana(config.CurrentMana + manaRestore);
                    Debug.Log(
                        $"[Pickup] +{manaRestore:0.#} mana " +
                        $"({chargeFraction:P0} charge)");
                    break;
            }
        }

        private static void ApplySpinPickup(
            BeyConfiguration config,
            float baseAmount,
            string size,
            float chargeFraction)
        {
            float amount = config.ModifyPickupAmount(
                baseAmount * chargeFraction);
            config.SetSpin(config.CurrentSpin + amount);
            Debug.Log(
                $"[Pickup] +{amount:0.#} spin ({size}, " +
                $"{chargeFraction:P0} charge)");
        }
    }
}
