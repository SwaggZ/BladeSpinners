using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "RazorWindAbility", menuName = "Blade Spinners/Abilities/Razor Wind")]
    public class RazorWindAbility : BeyAbility
    {
        [Header("Razor Wind")]
        [SerializeField] private float range = 8f;
        [SerializeField] private float width = 3f;
        [SerializeField] private float damage = 18f;
        [SerializeField] private float knockbackForce = 10f;

        private void OnEnable()
        {
            abilityName = "Razor Wind";
            description = "Slash the air with a cutting gust — a wide wind blade tears through enemies in a line.";
            manaCost = 50f;
            rarity = Core.AbilityRarity.Uncommon;
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

            // Line damage check
            Collider[] hits = Physics.OverlapSphere(pos, range);
            foreach (Collider col in hits)
            {
                if (col.gameObject == beyController.gameObject) continue;
                BeyMovementController enemy = col.GetComponentInParent<BeyMovementController>();
                if (enemy == null || enemy == beyController) continue;
                Vector3 toEnemy = enemy.transform.position - pos;
                float forward = Vector3.Dot(toEnemy, dir);
                if (forward < 0f || forward > range) continue;
                float lateral = Mathf.Abs(Vector3.Dot(toEnemy, Vector3.Cross(dir, Vector3.up)));
                if (lateral > width * 0.5f) continue;
                if (enemy.BeyConfiguration != null)
                    enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - damage);
                if (enemy.Rb != null)
                    enemy.Rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }

            SpawnVisual(pos, dir, range, width);
            Debug.Log("[Ability] Razor Wind!");
        }

        private void SpawnVisual(Vector3 origin, Vector3 dir, float len, float w)
        {
            // Wind blade (flat stretched cube)
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "RazorWindBlade";
            blade.transform.position = origin + dir * len * 0.5f + Vector3.up * 0.3f;
            blade.transform.localScale = new Vector3(w, 0.05f, len);
            blade.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            Collider c = blade.GetComponent<Collider>(); if (c != null) c.enabled = false;
            ApplyMat(blade, new Color(0.7f, 1f, 0.7f, 0.25f), new Color(1f, 2f, 1f));
            blade.AddComponent<RazorWindSlashFade>();
            Object.Destroy(blade, 0.6f);

            // Wind trail streaks
            for (int i = 0; i < 4; i++)
            {
                GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                streak.name = "WindStreak";
                float offset = (i - 1.5f) * w * 0.25f;
                Vector3 lateral = Vector3.Cross(dir, Vector3.up) * offset;
                streak.transform.position = origin + lateral + dir * Random.Range(1f, len * 0.8f) + Vector3.up * Random.Range(0.1f, 0.5f);
                streak.transform.localScale = new Vector3(0.04f, 0.04f, Random.Range(1.5f, 3f));
                streak.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                Collider sc = streak.GetComponent<Collider>(); if (sc != null) sc.enabled = false;
                ApplyMat(streak, new Color(0.8f, 1f, 0.8f, 0.4f), new Color(1.5f, 3f, 1.5f));
                streak.AddComponent<RazorWindStreakMove>().Init(dir, 12f);
                Object.Destroy(streak, 0.5f);
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

    public class RazorWindSlashFade : MonoBehaviour
    {
        private void Update()
        {
            transform.localScale *= (1f - Time.deltaTime * 2f);
        }
    }

    public class RazorWindStreakMove : MonoBehaviour
    {
        private Vector3 dir;
        private float speed;
        public void Init(Vector3 d, float s) { dir = d; speed = s; }
        private void Update()
        {
            transform.position += dir * speed * Time.deltaTime;
            transform.localScale *= (1f - Time.deltaTime * 3f);
        }
    }
}
