using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "FlashStepAbility", menuName = "Blade Spinners/Abilities/Flash Step")]
    public class FlashStepAbility : BeyAbility
    {
        [Header("Flash Step")]
        [SerializeField] private float stepDistance = 7f;
        [SerializeField] private float endImpulse = 8f;

        private void OnEnable()
        {
            abilityName = "Flash Step";
            description = "Instantly step forward and keep momentum.";
            manaCost = 40f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null)
                return;

            Vector3 direction = beyController.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.forward;
            direction.Normalize();

            Vector3 start = beyController.transform.position;
            Vector3 target = start + direction * stepDistance;

            if (Physics.Linecast(start + Vector3.up * 0.2f, target + Vector3.up * 0.2f, out RaycastHit hit))
            {
                target = hit.point - direction * 0.75f;
            }

            // Afterimage at origin
            SpawnAfterimage(start, beyController.transform.localScale);

            beyController.transform.position = target;
            if (beyController.Rb != null)
            {
                beyController.Rb.linearVelocity = new Vector3(0f, beyController.Rb.linearVelocity.y, 0f);
                beyController.Rb.AddForce(direction * endImpulse, ForceMode.VelocityChange);
            }

            // Arrival flash
            SpawnArrivalFlash(target);
            Debug.Log("[Ability] Flash Step!");
        }

        private void SpawnAfterimage(Vector3 pos, Vector3 scale)
        {
            GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ghost.name = "FlashAfterimage";
            ghost.transform.position = pos;
            ghost.transform.localScale = scale * 1.1f;
            Collider col = ghost.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Renderer rend = ghost.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.4f, 0.6f, 1f, 0.35f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.3f, 0.5f, 1.5f)); }
                rend.material = mat;
            }
            ghost.AddComponent<FlashAfterimgFade>();
            Object.Destroy(ghost, 0.4f);
        }

        private void SpawnArrivalFlash(Vector3 pos)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "FlashArrival";
            flash.transform.position = pos;
            flash.transform.localScale = Vector3.one * 0.3f;
            Collider col = flash.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Renderer rend = flash.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.7f, 0.85f, 1f, 0.7f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(2f, 2.5f, 4f)); }
                rend.material = mat;
            }
            flash.AddComponent<FlashArrivalExpand>();
            Object.Destroy(flash, 0.25f);
        }
    }

    public class FlashAfterimgFade : MonoBehaviour
    {
        private void Update()
        {
            float s = transform.localScale.x * (1f - Time.deltaTime * 3f);
            transform.localScale = Vector3.one * Mathf.Max(s, 0.05f);
        }
    }

    public class FlashArrivalExpand : MonoBehaviour
    {
        private void Update()
        {
            float s = transform.localScale.x + Time.deltaTime * 8f;
            transform.localScale = Vector3.one * s;
        }
    }
}
