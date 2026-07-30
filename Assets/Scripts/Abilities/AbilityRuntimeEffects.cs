using UnityEngine;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    public class AbilityRuntimeEffects : MonoBehaviour
    {
        private BeyMovementController ownerController;

        public static AbilityRuntimeEffects GetOrCreate(BeyMovementController controller)
        {
            if (controller == null) return null;
            AbilityRuntimeEffects fx = controller.GetComponent<AbilityRuntimeEffects>();
            if (fx == null)
            {
                fx = controller.gameObject.AddComponent<AbilityRuntimeEffects>();
            }

            fx.ownerController = controller;
            return fx;
        }

        public void ApplyTempMassBoost(float deltaMass, float duration)
        {
            if (ownerController == null || ownerController.Rb == null || duration <= 0f)
                return;

            ownerController.Rb.mass += deltaMass;
            CancelInvoke(nameof(RemoveMassBoost));
            massBoostActive = deltaMass;
            Invoke(nameof(RemoveMassBoost), duration);
        }

        private float massBoostActive;

        private void RemoveMassBoost()
        {
            if (ownerController != null && ownerController.Rb != null && massBoostActive != 0f)
            {
                ownerController.Rb.mass = Mathf.Max(0.1f, ownerController.Rb.mass - massBoostActive);
            }

            massBoostActive = 0f;
        }

        public void SpawnPoisonCloud(float radius, float duration, float dps)
        {
            if (ownerController == null || ownerController.BeyConfiguration == null) return;
            GameObject cloud = new GameObject("PoisonCloud");
            cloud.transform.position = ownerController.transform.position;

            PoisonCloudRuntime runtime = cloud.AddComponent<PoisonCloudRuntime>();
            runtime.Initialize(ownerController, radius, duration, dps);
        }
    }

    public class PoisonCloudRuntime : MonoBehaviour
    {
        private BeyMovementController ownerController;
        private float radius;
        private float duration;
        private float dps;
        private float tickTimer;

        public void Initialize(
            BeyMovementController owner,
            float cloudRadius,
            float cloudDuration,
            float damagePerSecond)
        {
            ownerController = owner;
            radius = Mathf.Max(0.5f, cloudRadius);
            duration = Mathf.Max(0.5f, cloudDuration);
            dps = Mathf.Max(0f, damagePerSecond);

            CreateVisual();
            Destroy(gameObject, duration + 0.25f);
        }

        private void Update()
        {
            duration -= Time.deltaTime;
            if (duration <= 0f)
                return;

            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f)
                return;

            tickTimer = 0.25f;
            float damage = dps * 0.25f;

            foreach (BeyMovementController bey in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         ownerController,
                         transform.position,
                         radius,
                         AbilityTargetRelation.Enemy))
            {
                bey.BeyConfiguration.SetSpin(bey.BeyConfiguration.CurrentSpin - damage);
            }
        }

        private void CreateVisual()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "PoisonCloudVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = new Vector3(radius * 2f, 0.12f, radius * 2f);
            visual.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            Collider col = visual.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(ShaderProvider.URPUnlit);
                mat.SetColor("_BaseColor", new Color(0.45f, 0.95f, 0.25f, 0.35f));
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                renderer.material = mat;
            }
        }
    }
}
