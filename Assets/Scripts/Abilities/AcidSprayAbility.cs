using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "AcidSprayAbility", menuName = "Blade Spinners/Abilities/Acid Spray")]
    public class AcidSprayAbility : BeyAbility
    {
        [Header("Acid Spray")]
        [SerializeField] private float coneAngle = 60f;
        [SerializeField] private float range = 6f;
        [SerializeField] private float damagePerTick = 5f;
        [SerializeField] private float duration = 3f;

        private void OnEnable()
        {
            abilityName = "Acid Spray";
            description = "Spray corrosive acid in a wide cone — enemies caught melt away over time.";
            manaCost = 40f;
            rarity = Core.AbilityRarity.Common;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            Vector3 pos = beyController.transform.position;
            Vector3 dir = beyController.Rb != null && beyController.Rb.linearVelocity.sqrMagnitude > 0.1f
                ? beyController.Rb.linearVelocity.normalized
                : beyController.transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            // Apply burn to enemies in cone
            float halfAngle = coneAngle * 0.5f;
            Collider[] hits = Physics.OverlapSphere(pos, range);
            foreach (Collider col in hits)
            {
                if (col.gameObject == beyController.gameObject) continue;
                BeyMovementController enemy = col.GetComponentInParent<BeyMovementController>();
                if (enemy == null || enemy == beyController) continue;
                Vector3 toEnemy = (enemy.transform.position - pos).normalized;
                if (Vector3.Angle(dir, toEnemy) > halfAngle) continue;
                BurnRuntime.Apply(enemy, damagePerTick, duration);
            }

            SpawnVisual(pos, dir, range, coneAngle, duration);
            Debug.Log("[Ability] Acid Spray!");
        }

        private void SpawnVisual(Vector3 origin, Vector3 dir, float len, float angle, float dur)
        {
            // Acid cone (widening flat cylinder)
            float endWidth = len * Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * 2f;
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = "AcidCone";
            cone.transform.position = origin + dir * len * 0.5f;
            cone.transform.localScale = new Vector3(endWidth, 0.03f, len);
            cone.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            Collider c = cone.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(cone, new Color(0.3f, 0.8f, 0f, 0.25f), new Color(0.6f, 2f, 0f));
            Object.Destroy(cone, 1.5f);

            // Acid droplets
            for (int i = 0; i < 8; i++)
            {
                GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drop.name = "AcidDrop";
                float t = Random.Range(0.2f, 1f);
                float lateral = Random.Range(-1f, 1f) * endWidth * 0.3f * t;
                Vector3 side = Vector3.Cross(dir, Vector3.up);
                drop.transform.position = origin + dir * len * t + side * lateral + Vector3.up * Random.Range(0.1f, 0.4f);
                drop.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
                Collider dc = drop.GetComponent<Collider>(); if (dc != null) dc.enabled = false;
                float hue = Random.Range(0.22f, 0.35f);
                Color col = Color.HSVToRGB(hue, 0.9f, 0.9f);
                ApplyMat(drop, col, col * 2f);
                drop.AddComponent<AcidDropletDrip>();
                Object.Destroy(drop, 1.2f);
            }

            // Spray origin flash
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "AcidFlash";
            flash.transform.position = origin + Vector3.up * 0.2f;
            flash.transform.localScale = Vector3.one * 0.5f;
            Collider fc = flash.GetComponent<Collider>(); if (fc != null) fc.enabled = false;
            ApplyMat(flash, new Color(0.4f, 1f, 0f, 0.5f), new Color(1f, 3f, 0f));
            Object.Destroy(flash, 0.3f);
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

    public class AcidDropletDrip : MonoBehaviour
    {
        private void Update()
        {
            transform.position += Vector3.down * 0.8f * Time.deltaTime;
            transform.localScale *= (1f - Time.deltaTime * 1.5f);
        }
    }
}
