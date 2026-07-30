using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Keeps the build-safe part catalog synchronized and prevents standalone builds
    /// from silently shipping a subset of the authored parts.
    /// </summary>
    public sealed class RuntimeCatalogBuildValidator : IPreprocessBuildWithReport
    {
        private const string PartRoot = "Assets/Parts";
        public int callbackOrder => -1000;

        [MenuItem("Blade Spinners/Content/Sync Runtime Part Catalog")]
        public static void SyncRuntimeCatalog()
        {
            RepairBrokenPartImports();
            if (!StarterPartsConfig.TryEnsureResourcesConfig(out StarterPartsConfig config) || config == null)
                throw new InvalidOperationException("Unable to create or load StarterPartsConfig.");

            ValidateCatalog(config);
            Debug.Log($"[RuntimeCatalog] Synchronized and validated {config.GetRuntimePartCatalog().Count} parts.");
        }

        public static void SyncRuntimeCatalogFromCommandLine()
        {
            SyncRuntimeCatalog();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            RepairBrokenPartImports();
            StarterPartsConfig config =
                AssetDatabase.LoadAssetAtPath<StarterPartsConfig>("Assets/Resources/StarterPartsConfig.asset");
            if (config == null)
                throw new BuildFailedException("StarterPartsConfig is missing from Assets/Resources.");

            ValidateCatalog(config);
        }

        /// <summary>
        /// Unity can temporarily recover a ScriptableObject as DefaultAsset after
        /// an importer-worker crash. Force-reimport physically present part assets
        /// before catalog discovery so one poisoned Library artifact cannot make a
        /// valid authored part appear deleted.
        /// </summary>
        internal static int RepairBrokenPartImports()
        {
            if (!Directory.Exists(PartRoot))
            {
                throw new DirectoryNotFoundException(
                    $"Required part root does not exist: {PartRoot}.");
            }

            string[] assetFiles = Directory.GetFiles(
                PartRoot,
                "*.asset",
                SearchOption.AllDirectories);
            Array.Sort(
                assetFiles,
                StringComparer.OrdinalIgnoreCase);

            List<string> repaired =
                new List<string>();
            List<string> stillBroken =
                new List<string>();
            for (int i = 0; i < assetFiles.Length; i++)
            {
                string assetPath =
                    assetFiles[i].Replace('\\', '/');
                if (AssetDatabase.LoadAssetAtPath<BeyPart>(
                        assetPath) != null)
                {
                    continue;
                }

                AssetDatabase.ImportAsset(
                    assetPath,
                    ImportAssetOptions.ForceUpdate
                    | ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<BeyPart>(
                        assetPath) != null)
                {
                    repaired.Add(assetPath);
                }
                else
                {
                    stillBroken.Add(assetPath);
                }
            }

            if (stillBroken.Count > 0)
            {
                throw new BuildFailedException(
                    "Part assets could not be imported as BeyPart: "
                    + string.Join(", ", stillBroken.Take(10))
                    + ". Check their YAML, script GUID, and Unity importer log.");
            }

            if (repaired.Count > 0)
            {
                Debug.LogWarning(
                    $"[RuntimeCatalog] Repaired {repaired.Count} cached " +
                    $"part import(s): {string.Join(", ", repaired)}");
            }
            return repaired.Count;
        }

        private static void ValidateCatalog(StarterPartsConfig config)
        {
            HashSet<BeyPart> authored = LoadAuthoredParts();
            List<BeyPart> runtimeList = config.GetRuntimePartCatalog();
            HashSet<BeyPart> runtime = new HashSet<BeyPart>(runtimeList);

            List<string> missing = authored
                .Where(part => part != null && !runtime.Contains(part))
                .Select(part => part.PartID)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            List<string> stale = runtime
                .Where(part => part != null && !authored.Contains(part))
                .Select(part => part.PartID)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            if (runtimeList.Count == authored.Count
                && runtime.Count == authored.Count
                && missing.Count == 0
                && stale.Count == 0)
            {
                return;
            }

            string missingPreview = string.Join(", ", missing.Take(10));
            string stalePreview = string.Join(", ", stale.Take(10));
            throw new BuildFailedException(
                $"Runtime part catalog is out of sync. Authored={authored.Count}, " +
                $"runtime entries={runtimeList.Count}, distinct runtime={runtime.Count}, " +
                $"missing={missing.Count} [{missingPreview}], stale={stale.Count} [{stalePreview}]. " +
                "Run Blade Spinners > Content > Sync Runtime Part Catalog.");
        }

        private static HashSet<BeyPart> LoadAuthoredParts()
        {
            HashSet<BeyPart> result = new HashSet<BeyPart>();
            string[] guids = AssetDatabase.FindAssets("t:BeyPart");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (part != null)
                    result.Add(part);
            }

            return result;
        }
    }
}
