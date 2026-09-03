using System;
using System.Collections.Generic;
using System.Linq;
using BladeSpinners.Abilities;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Makes the name-based Face Bolt resolver the single source of truth and prevents
    /// legacy explicit references from hiding most of the runtime ability pool.
    /// </summary>
    public sealed class FaceBoltAbilityBuildValidator : IPreprocessBuildWithReport
    {
        private const string FaceBoltFolder = "Assets/Parts/Face Bolts";
        private const int ExpectedFaceBoltCount = 150;

        public int callbackOrder => -900;

        [MenuItem("Blade Spinners/Content/Sync Face Bolt Ability Pool")]
        public static void SyncFaceBoltAbilityPool()
        {
            string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { FaceBoltFolder });
            int clearedReferences = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part == null || part.PartType != Core.PartType.FaceBolt)
                    continue;

                SerializedObject serializedPart = new SerializedObject(part);
                SerializedProperty ability = serializedPart.FindProperty("equippedAbility");
                if (ability == null || ability.objectReferenceValue == null)
                    continue;

                ability.objectReferenceValue = null;
                serializedPart.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(part);
                clearedReferences++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidationSummary summary = Validate();
            Debug.Log(
                $"[FaceBoltAbilities] Cleared {clearedReferences} legacy references. " +
                $"Validated {summary.FaceBoltCount} Face Bolts across {summary.ResolvedTypes} ability types.");
        }

        public static void SyncFaceBoltAbilityPoolFromCommandLine()
        {
            SyncFaceBoltAbilityPool();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Validate();
        }

        private static ValidationSummary Validate()
        {
            RuntimeCatalogBuildValidator
                .RepairBrokenPartImports();
            HashSet<Type> expectedTypes = LoadExpectedAbilityTypes();
            HashSet<Type> resolvedTypes = new HashSet<Type>();
            List<string> unresolved = new List<string>();
            List<string> invalidMetadata = new List<string>();
            int explicitReferenceCount = 0;
            int faceBoltCount = 0;

            FaceBoltAbilityResolver.EnsureInitialized();

            string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { FaceBoltFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part == null || part.PartType != Core.PartType.FaceBolt)
                    continue;

                faceBoltCount++;
                if (part.EquippedAbility != null)
                    explicitReferenceCount++;

                BeyAbility resolved = FaceBoltAbilityResolver.Resolve(part);
                if (resolved == null)
                {
                    unresolved.Add(part.PartID);
                    continue;
                }

                resolvedTypes.Add(resolved.GetType());
                if (string.IsNullOrWhiteSpace(resolved.AbilityName)
                    || string.Equals(resolved.AbilityName, "New Ability", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(resolved.Description)
                    || resolved.ManaCost <= 0f
                    || resolved.CooldownDuration <= 0f)
                {
                    invalidMetadata.Add($"{part.PartID}:{resolved.GetType().Name}");
                }
            }

            List<string> missingTypes = expectedTypes
                .Where(type => !resolvedTypes.Contains(type))
                .Select(type => type.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            List<string> unexpectedTypes = resolvedTypes
                .Where(type => !expectedTypes.Contains(type))
                .Select(type => type.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (faceBoltCount != ExpectedFaceBoltCount
                || explicitReferenceCount != 0
                || unresolved.Count != 0
                || invalidMetadata.Count != 0
                || missingTypes.Count != 0
                || unexpectedTypes.Count != 0)
            {
                throw new BuildFailedException(
                    $"Face Bolt ability pool is invalid. FaceBolts={faceBoltCount}/{ExpectedFaceBoltCount}, " +
                    $"legacyRefs={explicitReferenceCount}, unresolved={unresolved.Count} " +
                    $"[{Preview(unresolved)}], invalidMetadata={invalidMetadata.Count} " +
                    $"[{Preview(invalidMetadata)}], resolvedTypes={resolvedTypes.Count}/{expectedTypes.Count}, " +
                    $"missingTypes=[{Preview(missingTypes)}], unexpectedTypes=[{Preview(unexpectedTypes)}]. " +
                    "Run Blade Spinners > Content > Sync Face Bolt Ability Pool.");
            }

            return new ValidationSummary(faceBoltCount, resolvedTypes.Count);
        }

        private static HashSet<Type> LoadExpectedAbilityTypes()
        {
            List<BeyAbility> pool = AbilityFactory.CreateRuntimeAbilityPool();
            HashSet<Type> result = new HashSet<Type>();
            for (int i = 0; i < pool.Count; i++)
            {
                BeyAbility ability = pool[i];
                if (ability == null)
                    continue;

                result.Add(ability.GetType());
                UnityEngine.Object.DestroyImmediate(ability);
            }

            return result;
        }

        private static string Preview(List<string> values)
        {
            return string.Join(", ", values.Take(10));
        }

        private readonly struct ValidationSummary
        {
            public int FaceBoltCount { get; }
            public int ResolvedTypes { get; }

            public ValidationSummary(int faceBoltCount, int resolvedTypes)
            {
                FaceBoltCount = faceBoltCount;
                ResolvedTypes = resolvedTypes;
            }
        }
    }
}
