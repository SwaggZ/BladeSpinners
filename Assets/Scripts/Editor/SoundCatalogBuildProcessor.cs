using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BladeSpinners.Abilities;
using BladeSpinners.Audio;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Converts the authoring-time SoundEffects folder hierarchy into a Resources asset
    /// that works in standalone builds. Empty folders remain valid, intentionally silent keys.
    /// </summary>
    public sealed class SoundCatalogBuildProcessor : IPreprocessBuildWithReport
    {
        private const string SoundRoot = "Assets/SoundEffects";
        private const string CatalogAssetPath = "Assets/Resources/RuntimeSoundCatalog.asset";

        public int callbackOrder => -900;

        [MenuItem("Blade Spinners/Audio/Sync Runtime Sound Catalog")]
        public static void SyncFromMenu()
        {
            SyncCatalog(true);
        }

        [MenuItem("Blade Spinners/Validation/Validate Sound Catalog")]
        public static void ValidateFromMenu()
        {
            RuntimeSoundCatalog catalog =
                AssetDatabase.LoadAssetAtPath<RuntimeSoundCatalog>(CatalogAssetPath);
            ValidateCatalog(catalog);
            Debug.Log(
                $"[SoundCatalog] Validation passed: {catalog.FolderCount} folders, " +
                $"{catalog.ClipCount} AudioClips.");
        }

        public static void SyncFromCommandLine()
        {
            SyncCatalog(true);
        }

        public static void ValidateFromCommandLine()
        {
            SyncCatalog(false);
            ValidateFromMenu();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            SyncCatalog(false);
            RuntimeSoundCatalog catalog =
                AssetDatabase.LoadAssetAtPath<RuntimeSoundCatalog>(CatalogAssetPath);

            try
            {
                ValidateCatalog(catalog);
            }
            catch (Exception exception)
            {
                throw new BuildFailedException(
                    $"Sound catalog validation failed before building: {exception.Message}");
            }
        }

        internal static void SyncCatalog(bool logResult)
        {
            if (!Directory.Exists(SoundRoot))
            {
                throw new DirectoryNotFoundException(
                    $"Required sound root does not exist: {SoundRoot}");
            }

            SortedDictionary<string, List<AudioClip>> clipsByFolder =
                DiscoverFoldersAndClips();
            List<RuntimeSoundCatalog.FolderEntry> entries =
                new List<RuntimeSoundCatalog.FolderEntry>(clipsByFolder.Count);

            foreach (KeyValuePair<string, List<AudioClip>> pair in clipsByFolder)
            {
                pair.Value.Sort((left, right) =>
                    string.Compare(
                        AssetDatabase.GetAssetPath(left),
                        AssetDatabase.GetAssetPath(right),
                        StringComparison.OrdinalIgnoreCase));
                entries.Add(new RuntimeSoundCatalog.FolderEntry(pair.Key, pair.Value));
            }
            List<RuntimeSoundCatalog.MusicTrackEntry> musicEntries =
                BuildMusicTrackEntries();

            RuntimeSoundCatalog catalog =
                AssetDatabase.LoadAssetAtPath<RuntimeSoundCatalog>(CatalogAssetPath);
            bool created = catalog == null;
            if (created)
            {
                catalog = ScriptableObject.CreateInstance<RuntimeSoundCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            bool changed = catalog.ReplaceEntries(entries);
            changed |= catalog.ReplaceMusicTracks(musicEntries);
            if (created || changed)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }

            ValidateCatalog(catalog);

            if (logResult)
            {
                int populatedFolderCount = entries.Count(entry => entry.Clips.Count > 0);
                Debug.Log(
                    $"[SoundCatalog] Synced {catalog.ClipCount} AudioClips across " +
                    $"{catalog.FolderCount} folders ({populatedFolderCount} populated) to " +
                    $"{CatalogAssetPath}, including {catalog.MusicTrackCount} " +
                    "situation-tagged music tracks.");
            }
        }

        private static List<RuntimeSoundCatalog.MusicTrackEntry>
            BuildMusicTrackEntries()
        {
            MusicMetadataDocument document =
                MusicMetadataAuthoring.LoadAndReconcile(true);
            List<RuntimeSoundCatalog.MusicTrackEntry> result =
                new List<RuntimeSoundCatalog.MusicTrackEntry>(
                    document.tracks.Count);

            for (int i = 0; i < document.tracks.Count; i++)
            {
                MusicMetadataRecord record = document.tracks[i];
                if (record == null
                    || string.IsNullOrWhiteSpace(record.file))
                {
                    throw new InvalidOperationException(
                        $"Music metadata entry {i} has no source file.");
                }
                if (!Enum.TryParse(
                        record.situation,
                        true,
                        out MusicSituation situation))
                {
                    throw new InvalidOperationException(
                        $"Music metadata for '{record.file}' has unknown situation " +
                        $"'{record.situation}'.");
                }

                string assetPath =
                    MusicMetadataAuthoring.MusicFolder
                    + "/"
                    + record.file;
                AudioClip clip =
                    AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip == null)
                {
                    throw new InvalidOperationException(
                        $"Music metadata references missing AudioClip '{assetPath}'.");
                }

                string logoAssetPath =
                    MusicMetadataAuthoring.MusicLogoFolder
                    + "/"
                    + Path.GetFileNameWithoutExtension(
                        record.file)
                    + ".jpg";
                Texture2D logo =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        logoAssetPath);
                if (logo == null)
                {
                    throw new InvalidOperationException(
                        $"Music track '{record.file}' is missing its same-name " +
                        $"banner logo at '{logoAssetPath}'.");
                }

                result.Add(
                    new RuntimeSoundCatalog.MusicTrackEntry(
                        clip,
                        logo,
                        record.file,
                        record.title,
                        record.author,
                        situation));
            }

            result.Sort((left, right) =>
            {
                int situationOrder =
                    left.Situation.CompareTo(right.Situation);
                return situationOrder != 0
                    ? situationOrder
                    : string.Compare(
                        left.SourceFile,
                        right.SourceFile,
                        StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        private static SortedDictionary<string, List<AudioClip>> DiscoverFoldersAndClips()
        {
            SortedDictionary<string, List<AudioClip>> result =
                new SortedDictionary<string, List<AudioClip>>(StringComparer.OrdinalIgnoreCase);

            string[] directories = Directory.GetDirectories(
                SoundRoot, "*", SearchOption.AllDirectories);
            for (int i = 0; i < directories.Length; i++)
            {
                string key = ToFolderKey(directories[i]);
                if (!string.IsNullOrEmpty(key) && !result.ContainsKey(key))
                    result.Add(key, new List<AudioClip>());
            }

            string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { SoundRoot });
            for (int i = 0; i < clipGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip == null)
                    continue;

                string folderPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                string key = ToFolderKey(folderPath);
                if (string.IsNullOrEmpty(key))
                    continue;

                if (!result.TryGetValue(key, out List<AudioClip> folderClips))
                {
                    folderClips = new List<AudioClip>();
                    result.Add(key, folderClips);
                }

                folderClips.Add(clip);
            }

            return result;
        }

        private static string ToFolderKey(string folderPath)
        {
            string normalizedPath = folderPath?.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(normalizedPath)
                || normalizedPath.Length <= SoundRoot.Length
                || !normalizedPath.StartsWith(SoundRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return SoundPaths.Normalize(normalizedPath.Substring(SoundRoot.Length + 1));
        }

        private static void ValidateCatalog(RuntimeSoundCatalog catalog)
        {
            if (catalog == null)
                throw new InvalidOperationException($"Catalog asset is missing at {CatalogAssetPath}.");

            List<string> missingFolders = new List<string>();
            for (int i = 0; i < SoundPaths.RequiredFolderKeys.Count; i++)
            {
                string key = SoundPaths.RequiredFolderKeys[i];
                if (!catalog.TryGetClips(key, out _))
                    missingFolders.Add(key);
            }

            List<BeyAbility> abilities = AbilityFactory.CreateRuntimeAbilityPool();
            try
            {
                for (int i = 0; i < abilities.Count; i++)
                {
                    BeyAbility ability = abilities[i];
                    if (ability == null)
                        continue;

                    string key = SoundPaths.Ability(ability.AbilityName);
                    if (!catalog.TryGetClips(key, out _))
                        missingFolders.Add(key);
                }
            }
            finally
            {
                for (int i = 0; i < abilities.Count; i++)
                {
                    if (abilities[i] != null)
                        UnityEngine.Object.DestroyImmediate(abilities[i]);
                }
            }

            if (missingFolders.Count > 0)
            {
                throw new InvalidOperationException(
                    "Missing required sound folders: " +
                    string.Join(", ", missingFolders.Distinct(StringComparer.OrdinalIgnoreCase)));
            }

            MusicMetadataDocument musicDocument =
                MusicMetadataAuthoring.LoadAndReconcile(false);
            if (catalog.MusicTrackCount != musicDocument.tracks.Count)
            {
                throw new InvalidOperationException(
                    $"Music metadata/catalog count mismatch. Metadata=" +
                    $"{musicDocument.tracks.Count}, runtime=" +
                    $"{catalog.MusicTrackCount}.");
            }

            HashSet<string> musicFiles =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < catalog.MusicTracks.Count; i++)
            {
                RuntimeSoundCatalog.MusicTrackEntry track =
                    catalog.MusicTracks[i];
                if (track == null
                    || track.Clip == null
                    || track.Logo == null
                    || string.IsNullOrWhiteSpace(track.SourceFile)
                    || string.IsNullOrWhiteSpace(track.Title)
                    || string.IsNullOrWhiteSpace(track.Author))
                {
                    throw new InvalidOperationException(
                        $"Runtime music track {i} is incomplete.");
                }
                if (!musicFiles.Add(track.SourceFile))
                {
                    throw new InvalidOperationException(
                        $"Duplicate runtime music source '{track.SourceFile}'.");
                }

                string musicAssetPath =
                    MusicMetadataAuthoring.MusicFolder
                    + "/"
                    + track.SourceFile;
                AudioImporter importer =
                    AssetImporter.GetAtPath(musicAssetPath)
                        as AudioImporter;
                if (importer == null
                    || importer.defaultSampleSettings.loadType
                        != AudioClipLoadType.Streaming
                    || !importer.defaultSampleSettings.preloadAudioData)
                {
                    throw new InvalidOperationException(
                        $"Background music '{track.SourceFile}' must use " +
                        "Streaming load type with Preload Audio Data enabled.");
                }
            }

            foreach (MusicSituation situation in
                     Enum.GetValues(typeof(MusicSituation)))
            {
                bool hasTracks =
                    catalog.TryGetMusicTracks(
                        situation,
                        out IReadOnlyList<
                            RuntimeSoundCatalog.MusicTrackEntry> tracks)
                    && tracks.Count > 0;
                if (situation
                    == MusicSituation.StartScreen)
                {
                    if (hasTracks && tracks.Count > 1)
                    {
                        throw new InvalidOperationException(
                            "StartScreen must have zero or one theme track.");
                    }
                    continue;
                }

                if (!hasTracks)
                {
                    throw new InvalidOperationException(
                        $"Music situation '{situation}' has no tracks.");
                }
            }

            for (int i = 0; i < catalog.Folders.Count; i++)
            {
                RuntimeSoundCatalog.FolderEntry entry = catalog.Folders[i];
                if (entry == null)
                    throw new InvalidOperationException($"Catalog entry {i} is null.");

                for (int clipIndex = 0; clipIndex < entry.Clips.Count; clipIndex++)
                {
                    if (entry.Clips[clipIndex] == null)
                    {
                        throw new InvalidOperationException(
                            $"Sound folder '{entry.Key}' has a missing clip reference.");
                    }
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class SoundCatalogAutoSync
    {
        private static bool syncScheduled;

        static SoundCatalogAutoSync()
        {
            Schedule();
        }

        internal static void Schedule()
        {
            if (syncScheduled)
                return;

            syncScheduled = true;
            EditorApplication.delayCall += RunScheduledSync;
        }

        private static void RunScheduledSync()
        {
            syncScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Schedule();
                return;
            }

            try
            {
                SoundCatalogBuildProcessor.SyncCatalog(false);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SoundCatalog] Automatic sync failed: {exception}");
            }
        }
    }

    internal sealed class SoundEffectsAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(
                    MusicMetadataAuthoring.MusicFolder + "/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AudioImporter importer = assetImporter as AudioImporter;
            if (importer == null)
                return;

            AudioImporterSampleSettings settings =
                importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat =
                AudioCompressionFormat.Vorbis;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (ContainsSoundPath(importedAssets)
                || ContainsSoundPath(deletedAssets)
                || ContainsSoundPath(movedAssets)
                || ContainsSoundPath(movedFromAssetPaths))
            {
                SoundCatalogAutoSync.Schedule();
            }
        }

        private static bool ContainsSoundPath(string[] paths)
        {
            if (paths == null)
                return false;

            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(paths[i])
                    && paths[i].StartsWith(
                        "Assets/SoundEffects", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
