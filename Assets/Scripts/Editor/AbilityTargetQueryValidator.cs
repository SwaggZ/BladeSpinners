using System;
using System.Collections.Generic;
using System.Reflection;
using BladeSpinners.Abilities;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Focused regression check for compound-collider ability targeting.
    /// </summary>
    public static class AbilityTargetQueryValidator
    {
        private static readonly Vector3 TestCenter = new Vector3(10000f, 10000f, 10000f);
        private static readonly FieldInfo ConfigurationField =
            typeof(BeyMovementController).GetField(
                "beyConfiguration", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Blade Spinners/Validation/Test Unique Ability Targeting")]
        public static void Validate()
        {
            GameObject source = null;
            GameObject ally = null;
            GameObject firstTarget = null;
            GameObject secondTarget = null;

            try
            {
                source = CreateCompoundBey("AbilityQuery_Source", TestCenter, 3, false);
                ally = CreateCompoundBey(
                    "AbilityQuery_Ally", TestCenter + Vector3.left, 2, false);
                firstTarget = CreateCompoundBey(
                    "AbilityQuery_TargetA", TestCenter + Vector3.right, 5, true);
                secondTarget = CreateCompoundBey(
                    "AbilityQuery_TargetB", TestCenter + Vector3.forward * 2f, 2, true);
                Physics.SyncTransforms();

                Collider[] rawHits = Physics.OverlapSphere(TestCenter, 5f);
                if (rawHits.Length < 12)
                {
                    throw new InvalidOperationException(
                        $"Expected at least 12 compound-collider hits, but found {rawHits.Length}.");
                }

                BeyMovementController sourceBey = source.GetComponent<BeyMovementController>();
                BeyMovementController allyBey = ally.GetComponent<BeyMovementController>();
                BeyMovementController firstBey = firstTarget.GetComponent<BeyMovementController>();
                BeyMovementController secondBey = secondTarget.GetComponent<BeyMovementController>();
                List<BeyMovementController> enemies =
                    AbilityTargetQuery.FindUniqueBeysInRadius(
                        sourceBey, TestCenter, 5f, AbilityTargetRelation.Enemy);

                if (enemies.Count != 2
                    || !enemies.Contains(firstBey)
                    || !enemies.Contains(secondBey)
                    || enemies.Contains(sourceBey)
                    || enemies.Contains(allyBey))
                {
                    throw new InvalidOperationException(
                        "Player-side enemy targeting did not return exactly the two enemy beys.");
                }

                AssertRelations(sourceBey, allyBey, firstBey, secondBey);

                Debug.Log(
                    $"[AbilityTargetQuery] Passed: {rawHits.Length} collider hits resolved to " +
                    $"{enemies.Count} unique enemies; self, ally, enemy, and all relations passed.");
            }
            finally
            {
                DestroyTestObject(source);
                DestroyTestObject(ally);
                DestroyTestObject(firstTarget);
                DestroyTestObject(secondTarget);
            }
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static void AssertRelations(
            BeyMovementController source,
            BeyMovementController ally,
            BeyMovementController firstEnemy,
            BeyMovementController secondEnemy)
        {
            List<BeyMovementController> self = AbilityTargetQuery.FindUniqueBeysInRadius(
                source, TestCenter, 5f, AbilityTargetRelation.Self);
            List<BeyMovementController> allies = AbilityTargetQuery.FindUniqueBeysInRadius(
                source, TestCenter, 5f, AbilityTargetRelation.Ally);
            List<BeyMovementController> all = AbilityTargetQuery.FindUniqueBeysInRadius(
                source, TestCenter, 5f, AbilityTargetRelation.All);
            List<BeyMovementController> enemyCasterTargets =
                AbilityTargetQuery.FindUniqueBeysInRadius(
                    firstEnemy, TestCenter, 5f, AbilityTargetRelation.Enemy);
            List<BeyMovementController> enemyCasterAllies =
                AbilityTargetQuery.FindUniqueBeysInRadius(
                    firstEnemy, TestCenter, 5f, AbilityTargetRelation.Ally);

            if (self.Count != 1 || self[0] != source)
                throw new InvalidOperationException("Self targeting relation failed.");
            if (allies.Count != 1 || allies[0] != ally)
                throw new InvalidOperationException("Ally targeting relation failed.");
            if (all.Count != 4
                || !all.Contains(source)
                || !all.Contains(ally)
                || !all.Contains(firstEnemy)
                || !all.Contains(secondEnemy))
            {
                throw new InvalidOperationException("All targeting relation failed.");
            }
            if (enemyCasterTargets.Count != 2
                || !enemyCasterTargets.Contains(source)
                || !enemyCasterTargets.Contains(ally))
            {
                throw new InvalidOperationException(
                    "Enemy-side casts did not target the two player-faction beys.");
            }
            if (enemyCasterAllies.Count != 1 || enemyCasterAllies[0] != secondEnemy)
            {
                throw new InvalidOperationException(
                    "Enemy-side ally filtering did not exclude the caster.");
            }
        }

        private static GameObject CreateCompoundBey(
            string name,
            Vector3 position,
            int colliderCount,
            bool isEnemy)
        {
            if (ConfigurationField == null)
                throw new MissingFieldException(
                    typeof(BeyMovementController).FullName, "beyConfiguration");

            GameObject root = new GameObject(name);
            root.transform.position = position;
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            BeyMovementController controller = root.AddComponent<BeyMovementController>();
            ConfigurationField.SetValue(
                controller, new BeyConfiguration { IsEnemy = isEnemy });

            for (int i = 0; i < colliderCount; i++)
            {
                GameObject colliderObject = new GameObject($"Collider_{i}");
                colliderObject.transform.SetParent(root.transform, false);
                SphereCollider collider = colliderObject.AddComponent<SphereCollider>();
                collider.radius = 0.35f;
            }

            return root;
        }

        private static void DestroyTestObject(GameObject testObject)
        {
            if (testObject != null)
                UnityEngine.Object.DestroyImmediate(testObject);
        }
    }
}
