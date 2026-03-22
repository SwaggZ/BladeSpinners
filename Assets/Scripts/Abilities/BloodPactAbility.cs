using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "BloodPactAbility", menuName = "Blade Spinners/Abilities/Blood Pact")]
    public class BloodPactAbility : BeyAbility
    {
        [Header("Blood Pact")]
        [SerializeField] private float spinSacrifice = 20f;
        [SerializeField] private float massBoost = 3f;
        [SerializeField] private float speedBoost = 1.8f;
        [SerializeField] private float duration = 5f;

        private void OnEnable()
        {
            abilityName = "Blood Pact";
            description = "Sacrifice your own spin to gain tremendous mass and speed — a desperate gambit.";
            manaCost = 25f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;

            // Sacrifice spin
            if (beyController.BeyConfiguration != null)
                beyController.BeyConfiguration.SetSpin(beyController.BeyConfiguration.CurrentSpin - spinSacrifice);

            // Gain mass + speed
            AbilityRuntimeEffects fx = AbilityRuntimeEffects.GetOrCreate(beyController);
            if (fx != null) fx.ApplyTempMassBoost(massBoost, duration);
            if (beyController.Rb != null)
                beyController.Rb.linearVelocity *= speedBoost;

            SpawnVisual(beyController, duration);
            Debug.Log("[Ability] Blood Pact!");
        }

        private void SpawnVisual(BeyMovementController ctrl, float dur)
        {
            DBZAuraHelper.Spawn(
                ctrl.transform, dur,
                new Color(0.7f, 0f, 0.05f),   // dark crimson core
                new Color(0.5f, 0f, 0f),       // deep red outer
                3f
            );

            // Rising blood droplets (on top of aura)
            for (int i = 0; i < 5; i++)
            {
                GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drop.name = "BloodDrop";
                drop.transform.position = ctrl.transform.position + Random.insideUnitSphere * 0.4f;
                drop.transform.localScale = Vector3.one * 0.1f;
                Collider dc = drop.GetComponent<Collider>(); if (dc != null) dc.enabled = false;
                ApplyMat(drop, new Color(0.6f, 0f, 0f), new Color(2.5f, 0f, 0f));
                drop.AddComponent<BloodDropRise>();
                Object.Destroy(drop, 1.5f);
            }
        }

        private static void ApplyMat(GameObject obj, Color baseCol, Color emission)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) return;
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
            mat.color = baseCol;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission); }
            r.material = mat;
        }
    }

    public class BloodPactPulse : MonoBehaviour
    {
        private float timer;
        public void Init(float dur) { timer = dur; }
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) return;
            float s = 1.6f + Mathf.Sin(Time.time * 6f) * 0.12f;
            transform.localScale = Vector3.one * s;
        }
    }

    public class BloodDropRise : MonoBehaviour
    {
        private void Update()
        {
            transform.position += Vector3.up * 1.2f * Time.deltaTime;
            transform.localScale *= (1f - Time.deltaTime * 1.5f);
        }
    }
}
