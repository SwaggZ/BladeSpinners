using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "MeteorStrikeAbility", menuName = "Blade Spinners/Abilities/Meteor Strike")]
    public class MeteorStrikeAbility : BeyAbility
    {
        [Header("Meteor Strike")]
        [SerializeField] private float launchHeight = 6f;
        [SerializeField] private float impactRadius = 7f;
        [SerializeField] private float impactDamage = 30f;
        [SerializeField] private float knockbackImpulse = 20f;
        [SerializeField] private float hangTime = 0.6f;

        private void OnEnable()
        {
            abilityName = "Meteor Strike";
            description = "Launch skyward then crash down like a meteor, devastating nearby enemies.";
            manaCost = 85f;
            rarity = Core.AbilityRarity.Legendary;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;
            MeteorRuntime.Begin(beyController, launchHeight, hangTime, impactRadius, impactDamage, knockbackImpulse);
        }
    }

    public class MeteorRuntime : MonoBehaviour
    {
        private BeyMovementController controller;
        private float impactRadius, impactDamage, knockback;
        private float hangTimer;
        private bool ascending = true;
        private float peakY;
        private Vector3 targetPos;

        public static void Begin(BeyMovementController ctrl, float height, float hang, float radius, float damage, float kb)
        {
            MeteorRuntime existing = ctrl.GetComponent<MeteorRuntime>();
            if (existing != null) return;
            MeteorRuntime m = ctrl.gameObject.AddComponent<MeteorRuntime>();
            m.controller = ctrl;
            m.impactRadius = radius;
            m.impactDamage = damage;
            m.knockback = kb;
            m.hangTimer = hang;
            m.targetPos = ctrl.transform.position;
            m.peakY = ctrl.transform.position.y + height;
            if (ctrl.Rb != null)
            {
                ctrl.Rb.useGravity = false;
                ctrl.Rb.linearVelocity = Vector3.up * height * 3f;
            }
            SpawnLaunchTrail(ctrl.transform.position);
        }

        private void Update()
        {
            if (controller == null || controller.Rb == null) { Destroy(this); return; }
            if (ascending)
            {
                if (controller.transform.position.y >= peakY)
                {
                    ascending = false;
                    controller.Rb.linearVelocity = Vector3.zero;
                }
            }
            else if (hangTimer > 0f)
            {
                hangTimer -= Time.deltaTime;
                controller.Rb.linearVelocity = Vector3.zero;
                if (hangTimer <= 0f)
                {
                    controller.Rb.linearVelocity = Vector3.down * peakY * 5f;
                }
            }
            else if (controller.transform.position.y <= targetPos.y + 0.2f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            if (controller.Rb != null)
            {
                controller.Rb.useGravity = true;
                controller.Rb.linearVelocity = Vector3.zero;
            }
            controller.transform.position = new Vector3(controller.transform.position.x, targetPos.y, controller.transform.position.z);

            Vector3 origin = controller.transform.position;
            BeyConfiguration ownerConfig = controller.BeyConfiguration;
            BeyMovementController[] beys = Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            foreach (BeyMovementController bey in beys)
            {
                if (bey == null || bey.BeyConfiguration == null || bey.BeyConfiguration == ownerConfig) continue;
                if (bey.BeyConfiguration.IsEnemy == ownerConfig.IsEnemy) continue;
                float dist = Vector3.Distance(origin, bey.transform.position);
                if (dist > impactRadius) continue;
                float falloff = 1f - (dist / impactRadius);
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - impactDamage * falloff);
                Rigidbody rb = bey.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (bey.transform.position - origin);
                    dir.y = 0.3f;
                    rb.AddForce(dir.normalized * knockback * falloff, ForceMode.Impulse);
                }
            }
            SpawnImpactVisual(origin, impactRadius);
            Destroy(this);
        }

        private static void SpawnLaunchTrail(Vector3 pos)
        {
            GameObject trail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trail.name = "MeteorLaunchFlash";
            trail.transform.position = pos;
            trail.transform.localScale = Vector3.one * 0.8f;
            Collider c = trail.GetComponent<Collider>(); if (c != null) c.enabled = false;
            Renderer r = trail.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.5f, 0f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(3f, 1.5f, 0f)); }
                r.material = mat;
            }
            Object.Destroy(trail, 0.4f);
        }

        private void SpawnImpactVisual(Vector3 pos, float radius)
        {
            // Fiery impact core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "MeteorImpact";
            core.transform.position = pos;
            core.transform.localScale = Vector3.one * 2f;
            Collider c = core.GetComponent<Collider>(); if (c != null) c.enabled = false;
            Renderer r = core.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.4f, 0f, 0.7f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(4f, 2f, 0.2f)); }
                r.material = mat;
            }
            Object.Destroy(core, 0.4f);

            // Shockwave ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "MeteorShockwave";
            ring.transform.position = pos;
            ring.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            Collider rc = ring.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            Renderer rr = ring.GetComponent<Renderer>();
            if (rr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(1f, 0.6f, 0.1f, 0.5f);
                if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(2f, 1f, 0f)); }
                rr.material = mat;
            }
            WaveExpandRuntime.Spawn(ring, radius, 0.4f);

            // Debris particles
            for (int i = 0; i < 8; i++)
            {
                GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.name = "MeteorDebris";
                debris.transform.position = pos + Vector3.up * 0.2f;
                debris.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
                debris.transform.rotation = Random.rotation;
                Collider dc = debris.GetComponent<Collider>(); if (dc != null) dc.enabled = false;
                Renderer dr = debris.GetComponent<Renderer>();
                if (dr != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.4f, 0.25f, 0.1f);
                    if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f)); }
                    dr.material = mat;
                }
                Rigidbody drb = debris.AddComponent<Rigidbody>();
                drb.mass = 0.1f;
                float angle = i * 45f * Mathf.Deg2Rad;
                drb.linearVelocity = new Vector3(Mathf.Cos(angle) * Random.Range(3f, 6f), Random.Range(3f, 7f), Mathf.Sin(angle) * Random.Range(3f, 6f));
                Object.Destroy(debris, 1f);
            }
        }
    }
}
