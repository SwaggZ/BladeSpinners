using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BladeSpinners.Audio;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    [Serializable]
    internal sealed class MusicMetadataDocument
    {
        public List<MusicMetadataRecord> tracks =
            new List<MusicMetadataRecord>();
    }

    [Serializable]
    internal sealed class MusicMetadataRecord
    {
        public string file;
        public string title;
        public string author;
        public string situation;
    }

    /// <summary>
    /// JSON-backed authoring service. Reconciliation keeps every MP3 in the
    /// Background folder represented without requiring clip references in scenes.
    /// </summary>
    internal static class MusicMetadataAuthoring
    {
        internal const string MusicFolder =
            "Assets/SoundEffects/Music/Background";
        internal const string MusicLogoFolder =
            MusicFolder + "/Logos";
        internal const string MetadataAssetPath =
            MusicFolder + "/music-metadata.json";

        internal static MusicMetadataDocument LoadAndReconcile(
            bool saveChanges)
        {
            MusicMetadataDocument document = Load();
            bool changed = Reconcile(document);
            if (changed && saveChanges)
                Save(document);
            return document;
        }

        internal static void Save(MusicMetadataDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            string json = JsonUtility.ToJson(document, true) + Environment.NewLine;
            File.WriteAllText(
                MetadataAssetPath,
                json,
                new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                MetadataAssetPath,
                ImportAssetOptions.ForceUpdate);
        }

        private static MusicMetadataDocument Load()
        {
            if (!File.Exists(MetadataAssetPath))
                return new MusicMetadataDocument();

            string json = File.ReadAllText(MetadataAssetPath);
            MusicMetadataDocument document =
                JsonUtility.FromJson<MusicMetadataDocument>(json);
            if (document == null)
                document = new MusicMetadataDocument();
            if (document.tracks == null)
                document.tracks = new List<MusicMetadataRecord>();
            return document;
        }

        private static bool Reconcile(MusicMetadataDocument document)
        {
            if (!Directory.Exists(MusicFolder))
            {
                throw new DirectoryNotFoundException(
                    $"Music folder does not exist: {MusicFolder}");
            }

            Dictionary<string, MusicMetadataRecord> existing =
                new Dictionary<string, MusicMetadataRecord>(
                    StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < document.tracks.Count; i++)
            {
                MusicMetadataRecord record = document.tracks[i];
                if (record == null
                    || string.IsNullOrWhiteSpace(record.file)
                    || existing.ContainsKey(record.file.Trim()))
                {
                    continue;
                }
                existing.Add(record.file.Trim(), record);
            }

            string[] files = Directory.GetFiles(
                    MusicFolder,
                    "*.mp3",
                    SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            List<MusicMetadataRecord> reconciled =
                new List<MusicMetadataRecord>(files.Length);
            bool changed =
                document.tracks.Count != files.Length;

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                if (!existing.TryGetValue(
                        file,
                        out MusicMetadataRecord record))
                {
                    record = new MusicMetadataRecord
                    {
                        file = file,
                        title = Path.GetFileNameWithoutExtension(file),
                        author = "Unknown Artist",
                        situation = MusicSituation.MainMenu.ToString()
                    };
                    changed = true;
                }

                if (!string.Equals(
                        record.file,
                        file,
                        StringComparison.Ordinal))
                {
                    changed = true;
                }
                record.file = file;
                if (string.IsNullOrWhiteSpace(record.title))
                {
                    record.title = Path.GetFileNameWithoutExtension(file);
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(record.author))
                {
                    record.author = "Unknown Artist";
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(record.situation))
                {
                    record.situation = MusicSituation.MainMenu.ToString();
                    changed = true;
                }
                reconciled.Add(record);
            }

            if (!changed)
            {
                for (int i = 0; i < reconciled.Count; i++)
                {
                    if (!ReferenceEquals(
                            document.tracks[i],
                            reconciled[i])
                        || !string.Equals(
                            document.tracks[i].file,
                            reconciled[i].file,
                            StringComparison.Ordinal))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            document.tracks = reconciled;
            return changed;
        }
    }

    /// <summary>
    /// Editable Unity table for track title, author, and gameplay situation.
    /// The table is persisted to music-metadata.json and compiled into the catalog.
    /// </summary>
    internal sealed class MusicMetadataWindow : EditorWindow
    {
        private MusicMetadataDocument document;
        private Vector2 scroll;
        private string status = string.Empty;

        [MenuItem("Blade Spinners/Audio/Music Metadata")]
        private static void Open()
        {
            MusicMetadataWindow window =
                GetWindow<MusicMetadataWindow>("Music Metadata");
            window.minSize = new Vector2(650f, 380f);
            window.Show();
        }

        private void OnEnable()
        {
            Reload();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Situation Music",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Every MP3 in Assets/SoundEffects/Music/Background appears here. " +
                "Choose its situation and edit the public title/author shown by the " +
                "Now Playing banner. Artwork is loaded automatically from " +
                "Background/Logos/<MP3 filename>.jpg.",
                MessageType.Info);

            if (document == null)
            {
                if (GUILayout.Button("Reload"))
                    Reload();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("FILE", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "SITUATION",
                EditorStyles.boldLabel,
                GUILayout.Width(130f));
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < document.tracks.Count; i++)
            {
                MusicMetadataRecord track = document.tracks[i];
                if (track == null)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(track.file);
                MusicSituation situation = ParseSituation(track.situation);
                MusicSituation next = (MusicSituation)EditorGUILayout.EnumPopup(
                    situation,
                    GUILayout.Width(130f));
                track.situation = next.ToString();
                EditorGUILayout.EndHorizontal();
                track.title = EditorGUILayout.TextField(
                    "Display Title",
                    track.title);
                track.author = EditorGUILayout.TextField(
                    "Author",
                    track.author);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", GUILayout.Height(28f)))
                Reload();
            if (GUILayout.Button(
                    "Save + Sync Runtime Catalog",
                    GUILayout.Height(28f)))
            {
                try
                {
                    MusicMetadataAuthoring.Save(document);
                    SoundCatalogBuildProcessor.SyncCatalog(true);
                    status = "Saved metadata and synchronized the runtime catalog.";
                }
                catch (Exception exception)
                {
                    status = exception.Message;
                    Debug.LogException(exception);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(status))
                EditorGUILayout.HelpBox(status, MessageType.None);
        }

        private void Reload()
        {
            try
            {
                document =
                    MusicMetadataAuthoring.LoadAndReconcile(true);
                status =
                    $"Loaded {document.tracks.Count} background tracks.";
            }
            catch (Exception exception)
            {
                document = null;
                status = exception.Message;
                Debug.LogException(exception);
            }
        }

        private static MusicSituation ParseSituation(string value)
        {
            return Enum.TryParse(
                value,
                true,
                out MusicSituation situation)
                ? situation
                : MusicSituation.MainMenu;
        }
    }
}
