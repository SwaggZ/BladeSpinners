using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Core;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "LuckyStarAbility", menuName = "Blade Spinners/Abilities/Lucky Star")]
    public class LuckyStarAbility : BeyAbility
    {
        private enum LuckyEffect
        {
            SpinRestore,
            ManaRestore,
            SpeedBurst,
            Explosion,
            MultiFreeze,
        }

        private void OnEnable()
        {
            abilityName = "Lucky Star";
            description = "Spins the wheel of fate — triggers a random powerful effect. Could be anything!";
            manaCost = 40f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            LuckyEffect roll = (LuckyEffect)Random.Range(0, System.Enum.GetValues(typeof(LuckyEffect)).Length);
            Debug.Log($"[Ability] Lucky Star rolled: {roll}");

            switch (roll)
            {
                case LuckyEffect.SpinRestore:
                    TriggerSpinRestore(beyController);
                    break;
                case LuckyEffect.ManaRestore:
                    TriggerManaRestore(beyController);
                    break;
                case LuckyEffect.SpeedBurst:
                    TriggerSpeedBurst(beyController);
                    break;
                case LuckyEffect.Explosion:
                    TriggerExplosion(beyController);
                    break;
                case LuckyEffect.MultiFreeze:
                    TriggerMultiFreeze(beyController);
                    break;
            }

            SpawnStarVisual(beyController.transform.position);
        }

        private void TriggerSpinRestore(BeyMovementController ctrl)
        {
            float heal = GameConstants.MAX_SPIN * 0.35f;
            ctrl.BeyConfiguration.SetSpin(ctrl.BeyConfiguration.CurrentSpin + heal);
            Debug.Log($"[Lucky] Spin Restore: +{heal:F1}");
        }

        private void TriggerManaRestore(BeyMovementController ctrl)
        {
            float restore = GameConstants.DEFAULT_MANA_POOL * 0.5f;
            ctrl.BeyConfiguration.SetMana(ctrl.BeyConfiguration.CurrentMana + restore);
            Debug.Log($"[Lucky] Mana Restore: +{restore:F1}");
        }

        private void TriggerSpeedBurst(BeyMovementController ctrl)
        {
            if (ctrl.Rb != null)
                ctrl.Rb.AddForce(ctrl.transform.forward * 18f, ForceMode.VelocityChange);
            Debug.Log("[Lucky] Speed Burst!");
        }

        private void TriggerExplosion(BeyMovementController ctrl)
        {
            BeyMovementController[] all = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            foreach (BeyMovementController bey in all)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ctrl.BeyConfiguration) continue;
                if (bey.BeyConfiguration.IsEnemy == ctrl.BeyConfiguration.IsEnemy) continue;
                float dist = Vector3.Distance(ctrl.transform.position, bey.transform.position);
                if (dist > 8f) continue;
                float falloff = 1f - (dist / 8f);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - 22f * falloff);
                Rigidbody enemyRb = bey.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 dir = (bey.transform.position - ctrl.transform.position).normalized;
                    dir.y = 0.2f;
                    enemyRb.AddForce(dir.normalized * 16f * falloff, ForceMode.Impulse);
                }
            }
            Debug.Log("[Lucky] Explosion!");
        }

        private void TriggerMultiFreeze(BeyMovementController ctrl)
        {
            BeyMovementController[] all = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            foreach (BeyMovementController bey in all)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ctrl.BeyConfiguration) continue;
                if (bey.BeyConfiguration.IsEnemy == ctrl.BeyConfiguration.IsEnemy) continue;
                float dist = Vector3.Distance(ctrl.transform.position, bey.transform.position);
                if (dist > 10f) continue;
                FreezeRuntime.Apply(bey, 2f);
            }
            Debug.Log("[Lucky] Multi Freeze!");
        }

        private void SpawnStarVisual(Vector3 pos)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "LuckyStarFlash";
            star.transform.position = pos;
            star.transform.localScale = Vector3.one * 2f;

            Collider col = star.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = star.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.92f, 0.1f, 0.7f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(3f, 2.5f, 0.2f));
                }
                rend.material = mat;
            }

            Object.Destroy(star, 0.35f);
        }
    }
}
