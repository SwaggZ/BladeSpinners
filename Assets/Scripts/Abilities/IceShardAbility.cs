using UnityEngine;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Abilities
{
    [CreateAssetMenu(fileName = "IceShardAbility", menuName = "Blade Spinners/Abilities/Ice Shard")]
    public class IceShardAbility : BeyAbility
    {
        [Header("Ice Shard")]
        [SerializeField] private int shardCount = 5;
        [SerializeField] private float shardSpeed = 20f;
        [SerializeField] private float shardDamage = 12f;
        [SerializeField] private float spreadAngle = 50f;
        [SerializeField] private float freezeChance = 0.3f;
        [SerializeField] private float freezeDuration = 1.5f;

        private void OnEnable()
        {
            abilityName = "Ice Shard";
            description = "Fires a fan of razor-sharp ice shards that pierce through enemies.";
            manaCost = 55f;
            rarity = Core.AbilityRarity.Uncommon;
        }

        public override void Activate(BeyMovementController beyController)
        {
            if (beyController == null || beyController.BeyConfiguration == null) return;

            Vector3 forward = beyController.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                Rigidbody ownerRb = beyController.Rb;
                if (ownerRb != null && ownerRb.linearVelocity.sqrMagnitude > 0.1f)
                    forward = ownerRb.linearVelocity.normalized;
                else
                    forward = Vector3.forward;
            }
            forward.Normalize();

            float halfSpread = spreadAngle * 0.5f;
            float step = shardCount > 1 ? spreadAngle / (shardCount - 1) : 0f;

            for (int i = 0; i < shardCount; i++)
            {
                float angle = -halfSpread + step * i;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;

                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "IceShard";
                shard.transform.position = beyController.transform.position + dir * 0.5f + Vector3.up * 0.2f;
                shard.transform.localScale = new Vector3(0.08f, 0.08f, 0.4f);
                shard.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, 45f);

                Collider col = shard.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                Rigidbody rb = shard.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.linearVelocity = dir * shardSpeed;

                DBZAuraHelper.ApplyTransparentMat(shard, new Color(0.6f, 0.85f, 1f, 0.3f), new Color(0.5f, 1.2f, 2.5f));

                IceShardProjectile proj = shard.AddComponent<IceShardProjectile>();
                proj.Initialize(beyController, shardDamage, freezeChance, freezeDuration);
                Object.Destroy(shard, 3f);
            }
            Debug.Log("[Ability] Ice Shard!");
        }
    }

    public class IceShardProjectile : MonoBehaviour
    {
        private BeyMovementController owner;
        private float damage;
        private float freezeChance;
        private float freezeDur;

        public void Initialize(BeyMovementController ownerController, float dmg, float fc, float fd)
        {
            owner = ownerController;
            damage = dmg;
            freezeChance = fc;
            freezeDur = fd;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!AbilityTargetQuery.TryResolveCollider(
                    owner, other, AbilityTargetRelation.Enemy, out BeyMovementController hit))
            {
                return;
            }

            hit.BeyConfiguration.SetSpin(hit.BeyConfiguration.CurrentSpin - damage);
            if (Random.value < freezeChance)
                FreezeRuntime.Apply(hit, freezeDur);
            Destroy(gameObject);
        }
    }
}
