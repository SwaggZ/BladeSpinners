using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "IronFortressAbility", menuName = "Blade Spinners/Abilities/Iron Fortress")]
    public class IronFortressAbility : BeyAbility
    {
        [Header("Iron Fortress")]
        [SerializeField] private float massBoost = 8f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private float speedPenalty = 0.4f;

        private void OnEnable()
        {
            abilityName = "Iron Fortress";
            description = "Become an immovable fortress — massively increased weight but reduced speed.";
            manaCost = 50f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            AbilityRuntimeEffects fx = AbilityRuntimeEffects.GetOrCreate(beyController);
            if (fx == null) return;
            fx.ApplyTempMassBoost(massBoost, duration);
            if (beyController.Rb != null)
                beyController.Rb.linearVelocity *= speedPenalty;
            IronFortressVisual.Spawn(beyController, duration);
            Debug.Log("[Ability] Iron Fortress!");
        }
    }

    public class IronFortressVisual : MonoBehaviour
    {
        private float timer;

        public static void Spawn(BeyMovementController ctrl, float dur)
        {
            // Metallic dome
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "FortressDome";
            dome.transform.SetParent(ctrl.transform, false);
            dome.transform.localPosition = Vector3.zero;
            dome.transform.localScale = Vector3.one * 2f;
            Collider c = dome.GetComponent<Collider>(); if (c != null) c.enabled = false;
            Renderer r = dome.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.6f, 0.6f, 0.65f, 0.25f);
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 1f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 0.6f)); }
                r.material = mat;
            }
            Object.Destroy(dome, dur + 0.1f);

            // Armored ring at base
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "FortressRing";
            ring.transform.SetParent(ctrl.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            ring.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            Renderer rr = ring.GetComponent<Renderer>();
            if (rr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.7f, 0.65f, 0.5f, 0.4f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.8f, 0.7f, 0.4f)); }
                rr.material = mat;
            }
            Object.Destroy(ring, dur + 0.1f);
        }
    }
}
