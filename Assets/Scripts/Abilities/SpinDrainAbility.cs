using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    /// <summary>
    /// Spin Drain ability: steals spin from nearby enemy beys within a radius.
    /// High mana cost, powerful effect.
    /// </summary>
    [CreateAssetMenu(fileName = "SpinDrainAbility", menuName = "Blade Spinners/Abilities/Spin Drain")]
    public class SpinDrainAbility : BeyAbility
    {
        [Header("Spin Drain Settings")]
        [SerializeField] private float drainRadius = 6f;
        [SerializeField] private float spinStolen = 20f;

        private void OnEnable()
        {
            abilityName = "Spin Drain";
            description = "Steals spin from all nearby enemies.";
            manaCost = 80f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;

            BeyConfiguration playerConfig = beyController.BeyConfiguration;
            if (playerConfig == null) return;

            Vector3 origin = beyController.transform.position;
            float totalStolen = 0f;

            // Find all enemy beys within radius
            var enemies = Object.FindObjectsByType<BladeSpinners.Gameplay.EnemyBeyController>(
                FindObjectsSortMode.None);

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.BeyConfiguration == null) continue;

                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist > drainRadius) continue;

                // Steal spin (scaled by proximity)
                float proximityFactor = 1f - (dist / drainRadius);
                float stolen = spinStolen * proximityFactor;
                float actualStolen = Mathf.Min(stolen, enemy.BeyConfiguration.CurrentSpin);

                enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - actualStolen);
                totalStolen += actualStolen;
            }

            // Give stolen spin to player
            if (totalStolen > 0f)
            {
                playerConfig.SetSpin(playerConfig.CurrentSpin + totalStolen);
                SpawnDrainVisual(origin, drainRadius);
                Debug.Log($"[Ability] Spin Drain stole {totalStolen:F1} total spin!");
            }
            else
            {
                Debug.Log("[Ability] Spin Drain — no enemies in range.");
            }
        }

        private void SpawnDrainVisual(Vector3 center, float radius)
        {
            // Contracting purple rings
            for (int i = 0; i < 3; i++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "DrainRing";
                ring.transform.position = center;
                ring.transform.localScale = new Vector3(radius * 2f, 0.03f, radius * 2f);
                Collider col = ring.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                Renderer rend = ring.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.6f, 0.1f, 0.8f, 0.4f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1.2f, 0.2f, 2f)); }
                    rend.material = mat;
                }
                ring.AddComponent<DrainRingContract>().Init(0.4f + i * 0.15f);
                Object.Destroy(ring, 0.5f + i * 0.15f);
            }

            // Center absorb flash
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "DrainFlash";
            flash.transform.position = center + Vector3.up * 0.2f;
            flash.transform.localScale = Vector3.one * 0.4f;
            Collider fc = flash.GetComponent<Collider>();
            if (fc != null) fc.enabled = false;
            Renderer fr = flash.GetComponent<Renderer>();
            if (fr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.8f, 0.2f, 1f, 0.7f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(2f, 0.4f, 3f)); }
                fr.material = mat;
            }
            Object.Destroy(flash, 0.4f);
        }
    }

    public class DrainRingContract : MonoBehaviour
    {
        private float duration;
        private float elapsed;
        private float startScale;
        public void Init(float dur) { duration = dur; startScale = transform.localScale.x; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = Mathf.Lerp(startScale, 0.1f, t);
            transform.localScale = new Vector3(s, 0.03f, s);
        }
    }
}
