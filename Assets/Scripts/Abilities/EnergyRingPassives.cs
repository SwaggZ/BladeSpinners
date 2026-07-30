using BladeSpinners.Gameplay.Parts;
using UnityEngine;

namespace BladeSpinners.Abilities
{
    public sealed class SpinRecoveryPassive : BeyPassive
    {
        private const float RecoveryDelay = 3f;
        private const float SpinPerSecond = 2.5f;

        public override void Tick(
            EnergyRingPassiveRuntime runtime,
            float deltaTime)
        {
            BeyConfiguration owner = runtime.Owner;
            if (runtime.TimeSinceCollision < RecoveryDelay
                || owner.CurrentSpin >= owner.StartingSpin)
            {
                return;
            }

            float before = owner.CurrentSpin;
            owner.SetSpin(
                Mathf.Min(
                    owner.StartingSpin,
                    before + SpinPerSecond * deltaTime));
            if (owner.CurrentSpin <= before)
                return;

            float nextFeedback = runtime.GetValue("nextFeedback");
            if (runtime.ElapsedTime >= nextFeedback)
            {
                runtime.TriggerFeedback("Spin Recovery");
                runtime.SetValue(
                    "nextFeedback", runtime.ElapsedTime + 1f);
            }
        }
    }

    public sealed class LowSpinSurgePassive : BeyPassive
    {
        public override float ModifyOutgoingCollisionDamage(
            EnergyRingPassiveRuntime runtime,
            BeyConfiguration target,
            float damage)
        {
            if (runtime.Owner.CurrentSpin
                > runtime.Owner.StartingSpin * 0.30f)
            {
                return damage;
            }

            runtime.TriggerFeedback("Low Spin Surge +25%");
            return damage * 1.25f;
        }
    }

    public sealed class ImpactGuardPassive : BeyPassive
    {
        public override float ModifyIncomingCollisionDamage(
            EnergyRingPassiveRuntime runtime,
            BeyConfiguration source,
            float damage)
        {
            if (damage > 0f)
                runtime.TriggerFeedback("Impact Guard -20%");
            return damage * 0.80f;
        }
    }

    public sealed class KineticBatteryPassive : BeyPassive
    {
        private const float ManaPerCollision = 10f;
        private const float Cooldown = 1.25f;

        public override void OnCollisionWithBey(
            EnergyRingPassiveRuntime runtime,
            EnergyRingCollisionInfo collision)
        {
            if (runtime.ElapsedTime
                    < runtime.GetValue("readyAt")
                || runtime.Owner.CurrentMana
                    >= runtime.Owner.MaxMana)
            {
                return;
            }

            float before = runtime.Owner.CurrentMana;
            runtime.Owner.SetMana(before + ManaPerCollision);
            float gained = runtime.Owner.CurrentMana - before;
            if (gained <= 0f)
                return;

            runtime.SetValue(
                "readyAt", runtime.ElapsedTime + Cooldown);
            runtime.TriggerFeedback(
                $"Kinetic Battery +{gained:0.#} mana");
        }
    }

    public sealed class RecoilRecoveryPassive : BeyPassive
    {
        public override void OnCollisionDamageTaken(
            EnergyRingPassiveRuntime runtime,
            BeyConfiguration source,
            float damageTaken)
        {
            float recovery = Mathf.Min(8f, damageTaken * 0.25f);
            if (recovery <= 0f || runtime.Owner.IsBurst)
                return;

            float before = runtime.Owner.CurrentSpin;
            runtime.Owner.SetSpin(before + recovery);
            float restored = runtime.Owner.CurrentSpin - before;
            if (restored > 0f)
            {
                runtime.TriggerFeedback(
                    $"Recoil Recovery +{restored:0.#} spin");
            }
        }
    }

    public sealed class ArcConversionPassive : BeyPassive
    {
        private const float ManaThreshold = 20f;
        private const float SpinPerThreshold = 4f;

        public override void OnManaSpent(
            EnergyRingPassiveRuntime runtime,
            float amount)
        {
            float accumulated =
                runtime.GetValue("manaSpent") + amount;
            int conversions = Mathf.FloorToInt(
                accumulated / ManaThreshold);
            runtime.SetValue(
                "manaSpent",
                accumulated - conversions * ManaThreshold);
            if (conversions <= 0 || runtime.Owner.IsBurst)
                return;

            float before = runtime.Owner.CurrentSpin;
            runtime.Owner.SetSpin(
                before + conversions * SpinPerThreshold);
            float restored = runtime.Owner.CurrentSpin - before;
            if (restored > 0f)
            {
                runtime.TriggerFeedback(
                    $"Arc Conversion +{restored:0.#} spin");
            }
        }
    }

    public sealed class ManaConduitPassive : BeyPassive
    {
        public override float ModifyManaRegeneration(
            EnergyRingPassiveRuntime runtime,
            float regeneration)
        {
            return regeneration * 1.35f;
        }
    }

    public sealed class EnduranceMatrixPassive : BeyPassive
    {
        public override float ModifyPassiveSpinDrain(
            EnergyRingPassiveRuntime runtime,
            float drain)
        {
            return drain * 0.80f;
        }
    }

    public sealed class SecondWindPassive : BeyPassive
    {
        private const float SavedSpin = 12f;

        public override float ModifyIncomingCollisionDamage(
            EnergyRingPassiveRuntime runtime,
            BeyConfiguration source,
            float damage)
        {
            if (runtime.HasFlag("used")
                || damage < runtime.Owner.CurrentSpin
                || runtime.Owner.CurrentSpin <= 0f)
            {
                return damage;
            }

            runtime.SetFlag("used");
            runtime.TriggerFeedback("Second Wind!");
            return Mathf.Max(
                0f, runtime.Owner.CurrentSpin - SavedSpin);
        }
    }

    public sealed class PickupAmplifierPassive : BeyPassive
    {
        public override float ModifyPickupAmount(
            EnergyRingPassiveRuntime runtime,
            float amount)
        {
            if (amount > 0f)
                runtime.TriggerFeedback("Collector's Prism +50%");
            return amount * 1.50f;
        }
    }
}
