using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "PhaseShiftAbility", menuName = "Blade Spinners/Abilities/Phase Shift")]
    public class PhaseShiftAbility : BeyAbility
    {
        [Header("Phase Shift")]
        [SerializeField] private float duration = 2f;
        [SerializeField] private float speedBoost = 1.5f;

        private void OnEnable()
        {
            abilityName = "Phase Shift";
            description = "Shift into another dimension briefly — enemies pass right through you.";
            manaCost = 60f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            PhaseShiftRuntime.Apply(beyController, duration, speedBoost);
            Debug.Log("[Ability] Phase Shift!");
        }
    }

    public class PhaseShiftRuntime : MonoBehaviour
    {
        private BeyMovementController controller;
        private float timer;
        private Collider[] colliders;
        private Material[] originalMats;
        private Renderer[] renderers;

        public static void Apply(BeyMovementController ctrl, float dur, float speedBoost)
        {
            PhaseShiftRuntime existing = ctrl.GetComponent<PhaseShiftRuntime>();
            if (existing != null) { existing.timer = Mathf.Max(existing.timer, dur); return; }
            PhaseShiftRuntime p = ctrl.gameObject.AddComponent<PhaseShiftRuntime>();
            p.controller = ctrl;
            p.timer = dur;
            p.EnablePhase(speedBoost);
        }

        private void EnablePhase(float speedBoost)
        {
            // Disable colliders
            colliders = controller.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                if (col != null) col.enabled = false;

            // Ghost visual
            renderers = controller.GetComponentsInChildren<Renderer>();
            originalMats = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                originalMats[i] = renderers[i].material;
                Material ghostMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                ghostMat.color = new Color(0.4f, 0.7f, 1f, 0.2f);
                if (ghostMat.HasProperty("_Surface")) ghostMat.SetFloat("_Surface", 1f);
                if (ghostMat.HasProperty("_EmissionColor")) { ghostMat.EnableKeyword("_EMISSION"); ghostMat.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 2f)); }
                renderers[i].material = ghostMat;
            }

            if (controller.Rb != null)
                controller.Rb.linearVelocity *= speedBoost;

            SpawnPhaseFlash(controller.transform.position);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                EndPhase();
        }

        private void EndPhase()
        {
            if (colliders != null)
                foreach (Collider col in colliders)
                    if (col != null) col.enabled = true;

            if (renderers != null && originalMats != null)
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && originalMats[i] != null)
                        renderers[i].material = originalMats[i];

            SpawnPhaseFlash(controller != null ? controller.transform.position : transform.position);
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (colliders != null)
                foreach (Collider col in colliders)
                    if (col != null) col.enabled = true;
        }

        private static void SpawnPhaseFlash(Vector3 pos)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "PhaseFlash";
            flash.transform.position = pos;
            flash.transform.localScale = Vector3.one * 1.5f;
            Collider c = flash.GetComponent<Collider>(); if (c != null) c.enabled = false;
            Renderer r = flash.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.4f, 0.7f, 1f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, 2f, 4f)); }
                r.material = mat;
            }
            Object.Destroy(flash, 0.2f);
        }
    }
}
