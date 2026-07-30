using System.Collections.Generic;
using BladeSpinners.Gameplay.Movement;
using UnityEngine;

namespace BladeSpinners.Abilities
{
    [System.Flags]
    public enum AbilityTargetRelation
    {
        None = 0,
        Self = 1 << 0,
        Ally = 1 << 1,
        Enemy = 1 << 2,
        All = Self | Ally | Enemy
    }

    /// <summary>
    /// Centralizes faction filtering and resolves physics hits to unique bey roots.
    /// </summary>
    public static class AbilityTargetQuery
    {
        public static bool IsMatch(
            BeyMovementController caster,
            BeyMovementController candidate,
            AbilityTargetRelation allowedRelations)
        {
            if (candidate == null || allowedRelations == AbilityTargetRelation.None)
                return false;

            if (candidate == caster)
                return (allowedRelations & AbilityTargetRelation.Self) != 0;

            if (caster == null
                || caster.BeyConfiguration == null
                || candidate.BeyConfiguration == null)
            {
                return false;
            }

            bool isAlly =
                caster.BeyConfiguration.IsEnemy == candidate.BeyConfiguration.IsEnemy;
            AbilityTargetRelation relation =
                isAlly ? AbilityTargetRelation.Ally : AbilityTargetRelation.Enemy;
            return (allowedRelations & relation) != 0;
        }

        public static List<BeyMovementController> FindAll(
            BeyMovementController caster,
            AbilityTargetRelation allowedRelations)
        {
            BeyMovementController[] beys =
                Object.FindObjectsByType<BeyMovementController>(FindObjectsSortMode.None);
            var results = new List<BeyMovementController>();

            foreach (BeyMovementController bey in beys)
            {
                if (IsMatch(caster, bey, allowedRelations))
                    results.Add(bey);
            }

            return results;
        }

        public static List<BeyMovementController> FindUniqueBeysInRadius(
            BeyMovementController caster,
            Vector3 center,
            float radius,
            AbilityTargetRelation allowedRelations)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            var uniqueBeys = new HashSet<BeyMovementController>();
            var results = new List<BeyMovementController>();

            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                BeyMovementController bey = hit.GetComponentInParent<BeyMovementController>();
                if (!IsMatch(caster, bey, allowedRelations) || !uniqueBeys.Add(bey))
                    continue;

                results.Add(bey);
            }

            return results;
        }

        public static BeyMovementController FindNearest(
            BeyMovementController caster,
            Vector3 origin,
            float maxDistance,
            AbilityTargetRelation allowedRelations)
        {
            BeyMovementController nearest = null;
            float bestDistanceSquared = maxDistance * maxDistance;

            foreach (BeyMovementController candidate in FindAll(caster, allowedRelations))
            {
                float distanceSquared = (candidate.transform.position - origin).sqrMagnitude;
                if (distanceSquared > bestDistanceSquared)
                    continue;

                bestDistanceSquared = distanceSquared;
                nearest = candidate;
            }

            return nearest;
        }

        public static bool TryResolveCollider(
            BeyMovementController caster,
            Collider collider,
            AbilityTargetRelation allowedRelations,
            out BeyMovementController target)
        {
            target = collider != null
                ? collider.GetComponentInParent<BeyMovementController>()
                : null;
            return IsMatch(caster, target, allowedRelations);
        }
    }
}
