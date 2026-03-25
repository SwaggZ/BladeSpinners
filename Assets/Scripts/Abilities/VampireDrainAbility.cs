using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "VampireDrainAbility", menuName = "Blade Spinners/Abilities/Vampire Drain")]
    public class VampireDrainAbility : BeyAbility
    {
        [Header("Vampire Drain")]
        [SerializeField] private float radius = 7f;
        [SerializeField] private float drainAmount = 25f;
        [SerializeField] private float healRatio = 0.07f;  // how much of drained spin is returned to caster

        private void OnEnable()
        {
            abilityName = "Vampire Drain";
            description = "Siphons spin energy from surrounding enemies, restoring your own.";
            manaCost = 60f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            BeyConfiguration ownerConfig = beyController.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);

            float totalDrained = 0f;
            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;

                float dist = Vector3.Distance(beyController.transform.position, bey.transform.position);
                if (dist > radius) continue;

                float falloff = 1f - (dist / radius);
                float drained = drainAmount * falloff;

                float enemySpin = bey.BeyConfiguration.CurrentSpin;
                float actualDrained = Mathf.Min(drained, enemySpin);

                bey.BeyConfiguration.SetSpin(enemySpin - actualDrained);
                totalDrained += actualDrained;
            }

            if (totalDrained > 0f)
            {
                float healAmount = totalDrained * healRatio;
                ownerConfig.SetSpin(ownerConfig.CurrentSpin + healAmount);
                SpawnDrainVisual(beyController.transform.position, radius);
            }

            Debug.Log($"[Ability] Vampire Drain! Drained {totalDrained:F1} spin.");
        }

        private void SpawnDrainVisual(Vector3 center, float r)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "DrainPulse";
            visual.transform.position = center;
            visual.transform.localScale = new Vector3(r * 2f, 0.15f, r * 2f);

            Collider col = visual.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.6f, 0f, 0.15f, 0.5f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1.2f, 0f, 0.3f));
                }
                rend.material = mat;
            }

            Object.Destroy(visual, 0.5f);
        }
    }
}
