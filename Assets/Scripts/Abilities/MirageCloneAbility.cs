using System.Collections.Generic;
using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "MirageCloneAbility", menuName = "Blade Spinners/Abilities/Mirage Clone")]
    public class MirageCloneAbility : BeyAbility
    {
        [Header("Mirage Clone")]
        [SerializeField] private int cloneCount = 3;
        [SerializeField] private float spawnRadius = 2.5f;
        [SerializeField] private float cloneDuration = 5f;
        [SerializeField] private float cloneExplosionDamage = 8f;

        private void OnEnable()
        {
            abilityName = "Mirage Clone";
            description = "Spawns phantom copies that orbit and confuse enemies. When destroyed, each clone explodes.";
            manaCost = 65f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null)
                return;

            for (int i = 0; i < cloneCount; i++)
            {
                float angle = i * (360f / cloneCount) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
                Vector3 spawnPos = beyController.transform.position + offset;

                GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                clone.name = "MirageClone";
                clone.transform.position = spawnPos;
                clone.transform.localScale = beyController.transform.localScale;

                Renderer rend = clone.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                    mat.color = new Color(0.5f, 0.8f, 1f, 0.55f);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", new Color(0.3f, 0.8f, 1.5f));
                    }
                    rend.material = mat;
                }

                MirageCloneRuntime cloneRuntime = clone.AddComponent<MirageCloneRuntime>();
                cloneRuntime.Initialize(beyController, cloneDuration, cloneExplosionDamage);
            }

            Debug.Log($"[Ability] Mirage Clone! Spawned {cloneCount} clones.");
        }
    }

    public class MirageCloneRuntime : MonoBehaviour
    {
        private BeyMovementController owner;
        private float duration;
        private float explosionDamage;
        private float orbitAngle;
        private float orbitRadius;
        private float orbitSpeed = 120f;   // degrees per second

        public void Initialize(BeyMovementController ownerCtrl, float dur, float explDmg)
        {
            owner = ownerCtrl;
            duration = dur;
            explosionDamage = explDmg;
            orbitRadius = Vector3.Distance(transform.position, owner.transform.position);
            orbitAngle = Mathf.Atan2(transform.position.z - owner.transform.position.z,
                                      transform.position.x - owner.transform.position.x) * Mathf.Rad2Deg;

            // Remove the collider so it doesn't physically push beys
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Destroy(gameObject, duration);
        }

        private void Update()
        {
            if (owner == null)
            {
                Explode();
                return;
            }

            orbitAngle += orbitSpeed * Time.deltaTime;
            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
            transform.position = owner.transform.position + offset;
        }

        private void OnTriggerEnter(Collider other)
        {
            BeyMovementController bey = other.GetComponentInParent<BeyMovementController>();
            if (bey == null || owner == null || bey == owner) return;
            if (bey.BeyConfiguration == null || owner.BeyConfiguration == null) return;
            if (bey.BeyConfiguration.IsEnemy == owner.BeyConfiguration.IsEnemy) return;

            Explode(bey);
        }

        private void OnDestroy()
        {
            if (gameObject.scene.isLoaded)
                Explode(null);
        }

        private bool exploded;
        private void Explode(BeyMovementController directHit = null)
        {
            if (exploded) return;
            exploded = true;

            if (directHit != null && directHit.BeyConfiguration != null)
                directHit.BeyConfiguration.SetSpin(directHit.BeyConfiguration.CurrentSpin - explosionDamage);

            // Small visual flash
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.position = transform.position;
            flash.transform.localScale = Vector3.one * 0.8f;
            Collider col = flash.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Renderer rend = flash.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
                mat.color = new Color(0.5f, 0.9f, 1f);
                rend.material = mat;
            }
            Object.Destroy(flash, 0.2f);
        }
    }
}
