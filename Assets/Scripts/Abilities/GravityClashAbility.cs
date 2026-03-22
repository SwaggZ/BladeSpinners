using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "GravityClashAbility", menuName = "Blade Spinners/Abilities/Gravity Clash")]
    public class GravityClashAbility : BeyAbility
    {
        [Header("Gravity Clash")]
        [SerializeField] private float searchRadius = 12f;
        [SerializeField] private float pullImpulse = 20f;
        [SerializeField] private float impactSpinDamage = 14f;

        private void OnEnable()
        {
            abilityName = "Gravity Clash";
            description = "Pulls nearby enemies toward each other and forces a brutal collision.";
            manaCost = 75f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            EnemyBeyController[] enemies = Object.FindObjectsByType<EnemyBeyController>(FindObjectsSortMode.None);
            List<EnemyBeyController> candidates = new List<EnemyBeyController>();

            foreach (EnemyBeyController enemy in enemies)
            {
                if (enemy == null || enemy.BeyConfiguration == null)
                    continue;

                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb == null)
                    continue;

                float dist = Vector3.Distance(beyController.transform.position, enemy.transform.position);
                if (dist <= searchRadius)
                    candidates.Add(enemy);
            }

            if (candidates.Count < 2)
            {
                Debug.Log("[Ability] Gravity Clash needs at least 2 enemies in range.");
                return;
            }

            EnemyBeyController first = null;
            EnemyBeyController second = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                for (int j = i + 1; j < candidates.Count; j++)
                {
                    float pairDist = Vector3.Distance(candidates[i].transform.position, candidates[j].transform.position);
                    if (pairDist < bestDist)
                    {
                        bestDist = pairDist;
                        first = candidates[i];
                        second = candidates[j];
                    }
                }
            }

            if (first == null || second == null)
                return;

            Vector3 midpoint = (first.transform.position + second.transform.position) * 0.5f;
            Vector3 firstDir = (midpoint - first.transform.position).normalized;
            Vector3 secondDir = (midpoint - second.transform.position).normalized;

            Rigidbody firstRb = first.GetComponent<Rigidbody>();
            Rigidbody secondRb = second.GetComponent<Rigidbody>();
            if (firstRb == null || secondRb == null)
                return;

            firstRb.AddForce(firstDir * pullImpulse, ForceMode.VelocityChange);
            secondRb.AddForce(secondDir * pullImpulse, ForceMode.VelocityChange);

            first.BeyConfiguration.SetSpin(first.BeyConfiguration.CurrentSpin - impactSpinDamage);
            second.BeyConfiguration.SetSpin(second.BeyConfiguration.CurrentSpin - impactSpinDamage);

            SpawnGravityVisual(midpoint, first.transform.position, second.transform.position);
            Debug.Log("[Ability] Gravity Clash!");
        }

        private void SpawnGravityVisual(Vector3 midpoint, Vector3 pos1, Vector3 pos2)
        {
            // Gravity vortex at midpoint
            GameObject vortex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vortex.name = "GravityVortex";
            vortex.transform.position = midpoint;
            vortex.transform.localScale = Vector3.one * 0.6f;
            Collider vc = vortex.GetComponent<Collider>();
            if (vc != null) vc.enabled = false;
            Renderer vr = vortex.GetComponent<Renderer>();
            if (vr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.3f, 0f, 0.5f, 0.6f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.8f, 0f, 2f)); }
                vr.material = mat;
            }
            vortex.AddComponent<GravityVortexSpin>().Init(0.5f);
            Object.Destroy(vortex, 0.6f);

            // Pull lines from each target to midpoint
            SpawnPullLine(pos1, midpoint);
            SpawnPullLine(pos2, midpoint);

            // Impact flash at midpoint
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "GravityImpact";
            flash.transform.position = midpoint;
            flash.transform.localScale = Vector3.one * 1.5f;
            Collider fc = flash.GetComponent<Collider>();
            if (fc != null) fc.enabled = false;
            Renderer fr = flash.GetComponent<Renderer>();
            if (fr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.6f, 0.2f, 1f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1.5f, 0.5f, 3f)); }
                fr.material = mat;
            }
            Object.Destroy(flash, 0.3f);
        }

        private void SpawnPullLine(Vector3 from, Vector3 to)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GravityPullLine";
            line.transform.position = (from + to) * 0.5f;
            float dist = Vector3.Distance(from, to);
            line.transform.localScale = new Vector3(0.05f, 0.05f, dist);
            line.transform.LookAt(to);
            Collider col = line.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Renderer rend = line.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.5f, 0.1f, 0.8f, 0.6f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, 0.2f, 2.5f)); }
                rend.material = mat;
            }
            Object.Destroy(line, 0.3f);
        }
    }

    public class GravityVortexSpin : MonoBehaviour
    {
        private float timer;
        public void Init(float duration) { timer = duration; }
        private void Update()
        {
            timer -= Time.deltaTime;
            transform.Rotate(Vector3.up, 720f * Time.deltaTime);
            float s = transform.localScale.x + Time.deltaTime * 3f;
            transform.localScale = Vector3.one * s;
        }
    }
}
