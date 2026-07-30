using UnityEngine;
using BladeSpinners.Gameplay.Movement;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "SpectralChainsAbility", menuName = "Blade Spinners/Abilities/Spectral Chains")]
    public class SpectralChainsAbility : BeyAbility
    {
        [Header("Spectral Chains")]
        [SerializeField] private float radius = 8f;
        [SerializeField] private float rootDuration = 3f;
        [SerializeField] private float damage = 12f;
        [SerializeField] private int maxTargets = 3;

        private void OnEnable()
        {
            abilityName = "Spectral Chains";
            description = "Ethereal chains erupt from the ground, binding nearby enemies in place.";
            manaCost = 65f;
            rarity = Core.AbilityRarity.Rare;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null) return;
            Vector3 pos = beyController.transform.position;
            int rooted = 0;

            foreach (BeyMovementController enemy in
                     AbilityTargetQuery.FindUniqueBeysInRadius(
                         beyController, pos, radius, AbilityTargetRelation.Enemy))
            {
                if (rooted >= maxTargets) break;
                if (enemy.BeyConfiguration != null)
                    enemy.BeyConfiguration.SetSpin(enemy.BeyConfiguration.CurrentSpin - damage);
                SpectralChainRoot.Apply(enemy, rootDuration);
                SpawnChainVisual(beyController.transform.position, enemy, rootDuration);
                rooted++;
            }

            // Origin burst
            GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            burst.name = "ChainBurst";
            burst.transform.position = pos + Vector3.up * 0.2f;
            burst.transform.localScale = Vector3.one * 0.6f;
            Collider bc = burst.GetComponent<Collider>(); if (bc != null) bc.enabled = false;
            ApplyMat(burst, new Color(0.5f, 0.8f, 1f, 0.4f), new Color(1.5f, 2.5f, 4f));
            Object.Destroy(burst, 0.4f);

            Debug.Log("[Ability] Spectral Chains!");
        }

        private void SpawnChainVisual(Vector3 origin, BeyMovementController target, float dur)
        {
            // Chain link beam
            GameObject chain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chain.name = "SpectralChain";
            chain.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
            Collider cc = chain.GetComponent<Collider>(); if (cc != null) cc.enabled = false;
            ApplyMat(chain, new Color(0.4f, 0.6f, 1f, 0.5f), new Color(1f, 1.5f, 3f));
            chain.AddComponent<SpectralChainStretch>().Init(origin, target.transform);
            Object.Destroy(chain, dur);

            // Root anchor at enemy feet
            GameObject anchor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anchor.name = "ChainAnchor";
            anchor.transform.SetParent(target.transform, false);
            anchor.transform.localPosition = Vector3.zero;
            anchor.transform.localScale = new Vector3(1.2f, 0.03f, 1.2f);
            Collider ac = anchor.GetComponent<Collider>(); if (ac != null) ac.enabled = false;
            ApplyMat(anchor, new Color(0.3f, 0.5f, 1f, 0.3f), new Color(0.8f, 1.2f, 2.5f));
            Object.Destroy(anchor, dur);

            // Chain particles at target
            for (int i = 0; i < 3; i++)
            {
                GameObject link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                link.name = "ChainLink";
                link.transform.SetParent(target.transform, false);
                link.transform.localPosition = Random.insideUnitSphere * 0.5f;
                link.transform.localScale = Vector3.one * 0.1f;
                link.transform.localRotation = Random.rotation;
                Collider lc = link.GetComponent<Collider>(); if (lc != null) lc.enabled = false;
                ApplyMat(link, new Color(0.5f, 0.7f, 1f, 0.6f), new Color(1.5f, 2f, 4f));
                link.AddComponent<SpectralChainLinkOrbit>().Init(0.5f);
                Object.Destroy(link, dur);
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

    public class SpectralChainRoot : MonoBehaviour
    {
        private float timer;
        private Vector3 lockedPos;

        public static void Apply(BeyMovementController ctrl, float dur)
        {
            SpectralChainRoot existing = ctrl.GetComponent<SpectralChainRoot>();
            if (existing != null) { existing.timer = Mathf.Max(existing.timer, dur); return; }
            SpectralChainRoot sc = ctrl.gameObject.AddComponent<SpectralChainRoot>();
            sc.timer = dur;
            sc.lockedPos = ctrl.transform.position;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) { Destroy(this); return; }
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; }
            transform.position = lockedPos;
        }
    }

    public class SpectralChainStretch : MonoBehaviour
    {
        private Vector3 origin;
        private Transform target;
        public void Init(Vector3 o, Transform t) { origin = o; target = t; }
        private void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            Vector3 mid = (origin + target.position) * 0.5f + Vector3.up * 0.3f;
            transform.position = mid;
            float dist = Vector3.Distance(origin, target.position);
            transform.localScale = new Vector3(0.08f, 0.08f, dist);
            transform.LookAt(target.position);
        }
    }

    public class SpectralChainLinkOrbit : MonoBehaviour
    {
        private float orbitDist, angle;
        public void Init(float dist) { orbitDist = dist; angle = Random.Range(0f, 360f); }
        private void Update()
        {
            angle += 200f * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            transform.localPosition = new Vector3(Mathf.Cos(rad) * orbitDist, 0.3f, Mathf.Sin(rad) * orbitDist);
            transform.Rotate(Vector3.one, 300f * Time.deltaTime, Space.Self);
        }
    }
}
