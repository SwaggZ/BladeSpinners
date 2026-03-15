using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BladeSpinners.Core;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Bulk generator for creating many full Bey part sets in one run.
    /// Uses PartSetGenerator for actual asset creation to keep behavior consistent.
    /// </summary>
    public class MassPartSetGenerator : EditorWindow
    {
        [Serializable]
        private class SetGenerationEntry
        {
            public string setName = "Set_001";
            public int seed = 1000;
            public RarityTier rarity = RarityTier.Common;
            public Sprite emblem;
            public Color color = new Color(0.2f, 0.4f, 0.9f);
        }

        private int entryCount = 1;
        private readonly List<SetGenerationEntry> entries = new List<SetGenerationEntry>();
        private int randomizeBaseSeed = 120000;

        [MenuItem("Blade Spinners/Generate Massive Part Sets")]
        public static void ShowWindow()
        {
            MassPartSetGenerator window = GetWindow<MassPartSetGenerator>("Mass Part Set Generator");
            window.minSize = new Vector2(420f, 430f);
        }

        private void OnGUI()
        {
            GUILayout.Label("Bulk Part Set Generation", EditorStyles.boldLabel);
            GUILayout.Space(8f);

            entryCount = Mathf.Max(0, EditorGUILayout.IntField("Set Entry Count", entryCount));
            ResizeEntryList(entryCount);

            GUILayout.Space(6f);
            DrawEntries();

            GUILayout.Space(10f);
            randomizeBaseSeed = EditorGUILayout.IntField("Randomize Base Seed", randomizeBaseSeed);
            if (GUILayout.Button("Randomize All Seeds", GUILayout.Height(24f)))
            {
                RandomizeAllSeeds();
            }

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                $"This will create {entries.Count} full sets ({entries.Count * 5} BeyPart assets). Existing assets with matching names are overwritten.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Generate Massive Part Sets", GUILayout.Height(34f)))
                {
                    GenerateMassiveSets();
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

        private void GenerateMassiveSets()
        {
            if (!CanGenerate())
            {
                Debug.LogError("[MassPartSetGenerator] Invalid settings. Ensure all entries have a set name.");
                return;
            }

            int generatedSets = 0;
            int generatedParts = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < entries.Count; i++)
                {
                    SetGenerationEntry entry = entries[i];
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

            Debug.Log($"[MassPartSetGenerator] Generated {generatedSets} sets ({generatedParts} parts) from custom entry list.");
            EditorUtility.DisplayDialog("Mass Generation Complete",
                $"Generated {generatedSets} sets ({generatedParts} total parts).", "OK");
        }

        private void ResizeEntryList(int targetSize)
        {
            while (entries.Count < targetSize)
            {
                int index = entries.Count + 1;
                entries.Add(new SetGenerationEntry
                {
                    setName = $"Set_{index:000}",
                    seed = 1000 + (index - 1)
                });
            }

            while (entries.Count > targetSize)
            {
                entries.RemoveAt(entries.Count - 1);
            }
        }

        private void DrawEntries()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SetGenerationEntry entry = entries[i];

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Set Entry {i + 1}", EditorStyles.boldLabel);
                entry.setName = EditorGUILayout.TextField("Name", entry.setName);
                entry.seed = EditorGUILayout.IntField("Seed", entry.seed);
                entry.rarity = (RarityTier)EditorGUILayout.EnumPopup("Rarity", entry.rarity);
                entry.emblem = (Sprite)EditorGUILayout.ObjectField("Emblem", entry.emblem, typeof(Sprite), false);
                entry.color = EditorGUILayout.ColorField("Color", entry.color);
                EditorGUILayout.EndVertical();

                if (i < entries.Count - 1)
                {
                    GUILayout.Space(4f);
                }
            }
        }

        private void RandomizeAllSeeds()
        {
            if (entries.Count == 0)
            {
                return;
            }

            System.Random rng = new System.Random(randomizeBaseSeed ^ Environment.TickCount);
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].seed = rng.Next(1000, int.MaxValue);
            }
        }
    }
}
