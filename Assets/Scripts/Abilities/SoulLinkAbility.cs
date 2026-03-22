using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "SoulLinkAbility", menuName = "Blade Spinners/Abilities/Soul Link")]
    public class SoulLinkAbility : BeyAbility
    {
        [Header("Soul Link")]
        [SerializeField] private float radius = 10f;
        [SerializeField] private float damageShare = 0.5f;
        [SerializeField] private float duration = 5f;
        [SerializeField] private float initialDamage = 12f;

        private void OnEnable()
        {
            abilityName = "Soul Link";
            description = "Link your soul to the nearest enemy — any spin they lose, you steal half.";
            manaCost = 65f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            BeyMovementController target = FindNearest(beyController, radius);
            if (target == null) return;

            if (target.BeyConfiguration != null)
                target.BeyConfiguration.SetSpin(target.BeyConfiguration.CurrentSpin - initialDamage);

            SoulLinkRuntime.Apply(beyController, target, damageShare, duration);
            SpawnLinkVisual(beyController, target, duration);
            Debug.Log("[Ability] Soul Link!");
        }

        private static BeyMovementController FindNearest(BeyMovementController self, float radius)
        {
            BeyMovementController nearest = null;
            float minDist = float.MaxValue;
            Collider[] hits = Physics.OverlapSphere(self.transform.position, radius);
            foreach (Collider col in hits)
            {
                if (col.gameObject == self.gameObject) continue;
                BeyMovementController bey = col.GetComponentInParent<BeyMovementController>();
                if (bey == null || bey == self) continue;
                float dist = Vector3.Distance(self.transform.position, bey.transform.position);
                if (dist < minDist) { minDist = dist; nearest = bey; }
            }
            return nearest;
        }

        private void SpawnLinkVisual(BeyMovementController self, BeyMovementController target, float dur)
        {
            // Orb on self
            GameObject selfOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            selfOrb.name = "SoulLinkOrb";
            selfOrb.transform.SetParent(self.transform, false);
            selfOrb.transform.localPosition = Vector3.up * 0.5f;
            selfOrb.transform.localScale = Vector3.one * 0.3f;
            Collider c1 = selfOrb.GetComponent<Collider>(); if (c1 != null) c1.enabled = false;
            ApplyMat(selfOrb, new Color(0.8f, 0.2f, 1f, 0.5f), new Color(2f, 0.5f, 3f));
            Object.Destroy(selfOrb, dur);

            // Orb on target
            GameObject targetOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetOrb.name = "SoulLinkOrb";
            targetOrb.transform.SetParent(target.transform, false);
            targetOrb.transform.localPosition = Vector3.up * 0.5f;
            targetOrb.transform.localScale = Vector3.one * 0.3f;
            Collider c2 = targetOrb.GetComponent<Collider>(); if (c2 != null) c2.enabled = false;
            ApplyMat(targetOrb, new Color(0.5f, 0f, 0.8f, 0.5f), new Color(1.5f, 0f, 2.5f));
            Object.Destroy(targetOrb, dur);

            // Link beam between them
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "SoulLinkBeam";
            beam.transform.localScale = new Vector3(0.06f, 0.06f, 1f);
            Collider cb = beam.GetComponent<Collider>(); if (cb != null) cb.enabled = false;
            ApplyMat(beam, new Color(0.7f, 0.1f, 1f, 0.4f), new Color(2f, 0.3f, 3f));
            beam.AddComponent<SoulLinkBeamStretch>().Init(self.transform, target.transform);
            Object.Destroy(beam, dur);
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

    public class SoulLinkRuntime : MonoBehaviour
    {
        private BeyMovementController self, target;
        private float share, timer, lastTargetSpin;

        public static void Apply(BeyMovementController s, BeyMovementController t, float shareRatio, float dur)
        {
            SoulLinkRuntime sl = s.gameObject.AddComponent<SoulLinkRuntime>();
            sl.self = s;
            sl.target = t;
            sl.share = shareRatio;
            sl.timer = dur;
            sl.lastTargetSpin = t.BeyConfiguration != null ? t.BeyConfiguration.CurrentSpin : 0f;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f || target == null) { Destroy(this); return; }
            if (target.BeyConfiguration == null || self.BeyConfiguration == null) return;

            float currentSpin = target.BeyConfiguration.CurrentSpin;
            float lost = lastTargetSpin - currentSpin;
            lastTargetSpin = currentSpin;
            if (lost > 0f)
                self.BeyConfiguration.SetSpin(self.BeyConfiguration.CurrentSpin + lost * share);
        }
    }

    public class SoulLinkBeamStretch : MonoBehaviour
    {
        private Transform a, b;
        public void Init(Transform from, Transform to) { a = from; b = to; }
        private void Update()
        {
            if (a == null || b == null) { Destroy(gameObject); return; }
            Vector3 mid = (a.position + b.position) * 0.5f + Vector3.up * 0.5f;
            transform.position = mid;
            float dist = Vector3.Distance(a.position, b.position);
            transform.localScale = new Vector3(0.06f, 0.06f, dist);
            transform.LookAt(b.position + Vector3.up * 0.5f);
        }
    }
}
