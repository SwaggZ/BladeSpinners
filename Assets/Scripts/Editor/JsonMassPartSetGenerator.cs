using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Editor
{
    public class JsonMassPartSetGenerator : EditorWindow
    {
        [Serializable]
        private class JsonSetFile
        {
            public string datasetName = "PartSetBatch";
            public List<JsonSetEntry> sets = new List<JsonSetEntry>();
        }

        [Serializable]
        private class JsonSetEntry
        {
            public string setName = "Set_001";
            public int seed = 1000;
            public string rarity = nameof(RarityTier.Common);
            public string colorHex = "#3366E6";
            public string emblemAssetPath = string.Empty;
        }

        [Serializable]
        private class SetGenerationEntry
        {
            public string setName = "Set_001";
            public int seed = 1000;
            public RarityTier rarity = RarityTier.Common;
            public Color color = new Color(0.2f, 0.4f, 0.9f);
            public Sprite emblem;
        }

        private readonly List<SetGenerationEntry> entries = new List<SetGenerationEntry>();
        private string datasetName = "PartSetBatch";
        private string jsonFilePath = string.Empty;
        private Vector2 scroll;

        [MenuItem("Blade Spinners/Generate Massive Part Sets (JSON)")]
        public static void ShowWindow()
        {
            JsonMassPartSetGenerator window = GetWindow<JsonMassPartSetGenerator>("JSON Mass Set Generator");
            window.minSize = new Vector2(520f, 460f);
        }

        private void OnEnable()
        {
            if (entries.Count == 0)
            {
                entries.Add(new SetGenerationEntry());
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("JSON Massive Part Set Generator", EditorStyles.boldLabel);
            GUILayout.Space(6f);

            datasetName = EditorGUILayout.TextField("Dataset Name", datasetName);

            EditorGUILayout.BeginHorizontal();
            jsonFilePath = EditorGUILayout.TextField("JSON File", jsonFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80f)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select JSON Set File", GetDefaultFolder(), "json");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    jsonFilePath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load JSON", GUILayout.Height(24f)))
            {
                LoadFromJson();
            }

            if (GUILayout.Button("Save JSON", GUILayout.Height(24f)))
            {
                SaveToJson();
            }

            if (GUILayout.Button("Save JSON As...", GUILayout.Height(24f)))
            {
                SaveToJsonAs();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Entry", GUILayout.Height(22f)))
            {
                int index = entries.Count + 1;
                entries.Add(new SetGenerationEntry
                {
                    setName = $"Set_{index:000}",
                    seed = 1000 + (index - 1)
                });
            }

            using (new EditorGUI.DisabledScope(entries.Count == 0))
            {
                if (GUILayout.Button("Remove Last", GUILayout.Height(22f)))
                {
                    entries.RemoveAt(entries.Count - 1);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6f);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < entries.Count; i++)
            {
                SetGenerationEntry entry = entries[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Set Entry {i + 1}", EditorStyles.boldLabel);
                entry.setName = EditorGUILayout.TextField("Name", entry.setName);
                entry.seed = EditorGUILayout.IntField("Seed", entry.seed);
                entry.rarity = (RarityTier)EditorGUILayout.EnumPopup("Rarity", entry.rarity);
                entry.color = EditorGUILayout.ColorField("Color", entry.color);
                entry.emblem = (Sprite)EditorGUILayout.ObjectField("Emblem", entry.emblem, typeof(Sprite), false);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                $"This will create up to {entries.Count} full sets ({entries.Count * 5} BeyPart assets). Existing set names are skipped (no overwrite). Save JSON to keep this dataset reusable.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Generate Massive Part Sets From Loaded JSON", GUILayout.Height(34f)))
                {
                    GenerateFromEntries();
                }
            }
        }

        private bool CanGenerate()
        {
            if (entries.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(entries[i].setName))
                {
                    return false;
                }
            }

            return true;
        }

        private void GenerateFromEntries()
        {
            if (!CanGenerate())
            {
                Debug.LogError("[JsonMassPartSetGenerator] Invalid entries. Every set must have a name.");
                return;
            }

            int generatedSets = 0;
            int generatedParts = 0;
            int skippedSets = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < entries.Count; i++)
                {
                    SetGenerationEntry entry = entries[i];

                    if (SetAlreadyExists(entry.setName))
                    {
                        skippedSets++;
                        continue;
                    }

                    PartSetGenerator.GenerateSet(
                        entry.setName,
                        entry.seed,
                        entry.rarity,
                        entry.color,
                        entry.emblem,
                        false);
                    generatedSets++;
                    generatedParts += 5;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[JsonMassPartSetGenerator] Generated {generatedSets} sets ({generatedParts} parts) from JSON-backed dataset. Skipped {skippedSets} duplicate set names.");
            EditorUtility.DisplayDialog("JSON Mass Generation Complete", $"Generated {generatedSets} sets ({generatedParts} parts). Skipped {skippedSets} duplicates.", "OK");
        }

        private static bool SetAlreadyExists(string setName)
        {
            if (string.IsNullOrWhiteSpace(setName))
            {
                return false;
            }

            string trimmedName = setName.Trim();
            string[] expectedAssetPaths =
            {
                $"Assets/Parts/Tips/{trimmedName}_Tip.asset",
                $"Assets/Parts/Tracks/{trimmedName}_Track.asset",
                $"Assets/Parts/Fusion Wheels/{trimmedName}_FusionWheel.asset",
                $"Assets/Parts/Energy Rings/{trimmedName}_EnergyRing.asset",
                $"Assets/Parts/Face Bolts/{trimmedName}_FaceBolt.asset"
            };

            for (int i = 0; i < expectedAssetPaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(expectedAssetPaths[i]) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void LoadFromJson()
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath))
            {
                Debug.LogError("[JsonMassPartSetGenerator] Select a JSON file first.");
                return;
            }

            if (!File.Exists(jsonFilePath))
            {
                Debug.LogError($"[JsonMassPartSetGenerator] File not found: {jsonFilePath}");
                return;
            }

            string json = File.ReadAllText(jsonFilePath);
            JsonSetFile file = JsonUtility.FromJson<JsonSetFile>(json);
            if (file == null)
            {
                Debug.LogError("[JsonMassPartSetGenerator] Could not parse JSON.");
                return;
            }

            datasetName = string.IsNullOrWhiteSpace(file.datasetName) ? "PartSetBatch" : file.datasetName;
            entries.Clear();

            if (file.sets != null)
            {
                for (int i = 0; i < file.sets.Count; i++)
                {
                    JsonSetEntry source = file.sets[i];
                    if (source == null)
                    {
                        continue;
                    }

                    Color parsedColor = ParseColor(source.colorHex, new Color(0.2f, 0.4f, 0.9f));
                    RarityTier parsedRarity = ParseRarity(source.rarity, RarityTier.Common);
                    Sprite emblem = LoadEmblemAtPath(source.emblemAssetPath);

                    entries.Add(new SetGenerationEntry
                    {
                        setName = string.IsNullOrWhiteSpace(source.setName) ? $"Set_{i + 1:000}" : source.setName,
                        seed = source.seed,
                        rarity = parsedRarity,
                        color = parsedColor,
                        emblem = emblem
                    });
                }
            }

            if (entries.Count == 0)
            {
                entries.Add(new SetGenerationEntry());
            }

            Repaint();
            Debug.Log($"[JsonMassPartSetGenerator] Loaded {entries.Count} entries from {jsonFilePath}");
        }

        private void SaveToJson()
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath))
            {
                SaveToJsonAs();
                return;
            }

            WriteJson(jsonFilePath);
        }

        private void SaveToJsonAs()
        {
            string selectedPath = EditorUtility.SaveFilePanel(
                "Save JSON Set File",
                GetDefaultFolder(),
                string.IsNullOrWhiteSpace(datasetName) ? "part_set_batch" : datasetName,
                "json");

            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            jsonFilePath = selectedPath;
            WriteJson(jsonFilePath);
        }

        private void WriteJson(string path)
        {
            JsonSetFile file = new JsonSetFile
            {
                datasetName = string.IsNullOrWhiteSpace(datasetName) ? "PartSetBatch" : datasetName,
                sets = new List<JsonSetEntry>()
            };

            for (int i = 0; i < entries.Count; i++)
            {
                SetGenerationEntry entry = entries[i];
                string emblemPath = string.Empty;
                if (entry.emblem != null)
                {
                    emblemPath = AssetDatabase.GetAssetPath(entry.emblem);
                }

                file.sets.Add(new JsonSetEntry
                {
                    setName = entry.setName,
                    seed = entry.seed,
                    rarity = entry.rarity.ToString(),
                    colorHex = ColorUtility.ToHtmlStringRGB(entry.color),
                    emblemAssetPath = emblemPath
                });
            }

            string prettyJson = JsonUtility.ToJson(file, true);
            File.WriteAllText(path, prettyJson);
            AssetDatabase.Refresh();
            Debug.Log($"[JsonMassPartSetGenerator] Saved {entries.Count} entries to {path}");
        }

        private static Color ParseColor(string colorHex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(colorHex))
            {
                return fallback;
            }

            string normalized = colorHex.StartsWith("#", StringComparison.Ordinal) ? colorHex : $"#{colorHex}";
            if (ColorUtility.TryParseHtmlString(normalized, out Color color))
            {
                return color;
            }

            return fallback;
        }

        private static RarityTier ParseRarity(string rarity, RarityTier fallback)
        {
            if (string.IsNullOrWhiteSpace(rarity))
            {
                return fallback;
            }

            if (Enum.TryParse(rarity, true, out RarityTier parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static Sprite LoadEmblemAtPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static string GetDefaultFolder()
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return fullPath;
        }
    }
}