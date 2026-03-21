using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Abilities;

namespace BladeSpinners.Editor
{
    public class FaceBoltAbilityReportWindow : EditorWindow
    {
        private const string BaseAbilityFolder = "Assets/Abilities/Assets";
        private const string GeneratedAbilityFolder = "Assets/Abilities/Generated/FaceBoltUnique";

        private struct Row
        {
            public string FaceBoltName;
            public string PartId;
            public string AbilityName;
            public string AbilityRarity;
            public string Source;
            public string AssetPath;
        }

        private readonly List<Row> rows = new List<Row>();
        private Vector2 scroll;
        private string status = "Click Refresh to scan Face Bolt parts.";

        [MenuItem("Blade Spinners/Reports/Face Bolt Ability Report")]
        public static void ShowWindow()
        {
            FaceBoltAbilityReportWindow window = GetWindow<FaceBoltAbilityReportWindow>("Face Bolt Abilities");
            window.minSize = new Vector2(860f, 440f);
            window.Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Face Bolt → Ability Report", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(status, MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Height(26f)))
            {
                Refresh();
            }

            if (GUILayout.Button("Bake Unique Assignments", GUILayout.Height(26f)))
            {
                BakeUniqueAssignments();
            }

            using (new EditorGUI.DisabledScope(rows.Count == 0))
            {
                if (GUILayout.Button("Export Markdown", GUILayout.Height(26f)))
                {
                    ExportMarkdown();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8f);

            DrawHeader();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(row.FaceBoltName, GUILayout.Width(210f));
                EditorGUILayout.LabelField(row.PartId, GUILayout.Width(220f));
                EditorGUILayout.LabelField(row.AbilityName, GUILayout.Width(165f));
                EditorGUILayout.LabelField(row.AbilityRarity, GUILayout.Width(95f));
                EditorGUILayout.LabelField(row.Source, GUILayout.Width(95f));

                if (GUILayout.Button("Ping", GUILayout.Width(56f)))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(row.AssetPath);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Face Bolt", EditorStyles.boldLabel, GUILayout.Width(210f));
            EditorGUILayout.LabelField("Part ID", EditorStyles.boldLabel, GUILayout.Width(220f));
            EditorGUILayout.LabelField("Ability", EditorStyles.boldLabel, GUILayout.Width(165f));
            EditorGUILayout.LabelField("Rarity", EditorStyles.boldLabel, GUILayout.Width(95f));
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel, GUILayout.Width(95f));
            GUILayout.Space(56f);
            EditorGUILayout.EndHorizontal();
        }

        private void Refresh()
        {
            rows.Clear();

            string[] searchRoots =
            {
                "Assets/Parts/Face Bolts",
                "Assets/Parts"
            };

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int rootIndex = 0; rootIndex < searchRoots.Length; rootIndex++)
            {
                string root = searchRoots[rootIndex];
                if (!AssetDatabase.IsValidFolder(root))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { root });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrWhiteSpace(path) || !seen.Add(path))
                        continue;

                    BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                    if (part == null || part.PartType != Core.PartType.FaceBolt)
                        continue;

                    BeyAbility explicitAbility = part.EquippedAbility;
                    BeyAbility resolved = FaceBoltAbilityResolver.Resolve(part);
                    BeyAbility finalAbility = explicitAbility != null ? explicitAbility : resolved;

                    rows.Add(new Row
                    {
                        FaceBoltName = string.IsNullOrWhiteSpace(part.PartName) ? Path.GetFileNameWithoutExtension(path) : part.PartName,
                        PartId = string.IsNullOrWhiteSpace(part.PartID) ? "(none)" : part.PartID,
                        AbilityName = finalAbility != null ? finalAbility.AbilityName : "(none)",
                        AbilityRarity = finalAbility != null ? finalAbility.Rarity.ToString() : "-",
                        Source = explicitAbility != null ? "Explicit" : "Resolved",
                        AssetPath = path
                    });
                }
            }

            rows.Sort((a, b) => string.Compare(a.FaceBoltName, b.FaceBoltName, StringComparison.OrdinalIgnoreCase));
            int duplicateCount = CountDuplicateAbilityNames();
            status = duplicateCount > 0
                ? $"Found {rows.Count} Face Bolt parts. Duplicate abilities detected: {duplicateCount}."
                : $"Found {rows.Count} Face Bolt parts. All assigned abilities are unique.";
        }

        private void BakeUniqueAssignments()
        {
            List<(BeyPart part, string path)> faceBolts = CollectFaceBolts();
            if (faceBolts.Count == 0)
            {
                status = "No Face Bolt parts found. Nothing was assigned.";
                return;
            }

            EnsureFolderPath(GeneratedAbilityFolder);

            List<BeyAbility> allAbilityAssets = LoadAbilityAssets(new[] { BaseAbilityFolder, GeneratedAbilityFolder });
            if (allAbilityAssets.Count == 0)
            {
                status = "No ability assets found in Assets/Abilities. Assignment aborted.";
                return;
            }

            Dictionary<Type, List<BeyAbility>> byType = GroupAbilitiesByType(allAbilityAssets);
            HashSet<BeyAbility> used = new HashSet<BeyAbility>();
            int assignedCount = 0;
            int generatedCount = 0;

            for (int i = 0; i < faceBolts.Count; i++)
            {
                BeyPart part = faceBolts[i].part;
                if (part == null)
                    continue;

                BeyAbility resolved = FaceBoltAbilityResolver.Resolve(part);
                Type desiredType = resolved != null ? resolved.GetType() : null;

                BeyAbility selected = TakeUnusedAbilityOfType(byType, desiredType, used);
                if (selected == null)
                {
                    BeyAbility template = FindTemplateAbility(byType, desiredType);
                    if (template == null)
                        template = allAbilityAssets[0];

                    selected = CreateOrUpdateUniqueVariant(template, part);
                    if (selected != null)
                    {
                        if (!byType.TryGetValue(selected.GetType(), out List<BeyAbility> list))
                        {
                            list = new List<BeyAbility>();
                            byType[selected.GetType()] = list;
                        }

                        if (!list.Contains(selected))
                            list.Add(selected);

                        generatedCount++;
                    }
                }

                if (selected == null)
                    continue;

                AssignAbility(part, selected);
                used.Add(selected);
                assignedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Refresh();
            status = $"Baked unique assignments for {assignedCount}/{faceBolts.Count} Face Bolts. Generated {generatedCount} unique ability variants.";
            Debug.Log($"[FaceBoltAbilityReport] {status}");
        }

        private static List<(BeyPart part, string path)> CollectFaceBolts()
        {
            var result = new List<(BeyPart part, string path)>();
            string[] searchRoots = { "Assets/Parts/Face Bolts", "Assets/Parts" };
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int rootIndex = 0; rootIndex < searchRoots.Length; rootIndex++)
            {
                string root = searchRoots[rootIndex];
                if (!AssetDatabase.IsValidFolder(root))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:BeyPart", new[] { root });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrWhiteSpace(path) || !seen.Add(path))
                        continue;

                    BeyPart part = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                    if (part == null || part.PartType != Core.PartType.FaceBolt)
                        continue;

                    result.Add((part, path));
                }
            }

            result.Sort((a, b) => string.Compare(
                string.IsNullOrWhiteSpace(a.part.PartName) ? Path.GetFileNameWithoutExtension(a.path) : a.part.PartName,
                string.IsNullOrWhiteSpace(b.part.PartName) ? Path.GetFileNameWithoutExtension(b.path) : b.part.PartName,
                StringComparison.OrdinalIgnoreCase));

            return result;
        }

        private static List<BeyAbility> LoadAbilityAssets(string[] searchRoots)
        {
            var result = new List<BeyAbility>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int rootIndex = 0; rootIndex < searchRoots.Length; rootIndex++)
            {
                string root = searchRoots[rootIndex];
                if (!AssetDatabase.IsValidFolder(root))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:BeyAbility", new[] { root });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrWhiteSpace(path) || !seen.Add(path))
                        continue;

                    BeyAbility ability = AssetDatabase.LoadAssetAtPath<BeyAbility>(path);
                    if (ability != null)
                        result.Add(ability);
                }
            }

            return result;
        }

        private static Dictionary<Type, List<BeyAbility>> GroupAbilitiesByType(List<BeyAbility> abilities)
        {
            var byType = new Dictionary<Type, List<BeyAbility>>();
            for (int i = 0; i < abilities.Count; i++)
            {
                BeyAbility ability = abilities[i];
                if (ability == null)
                    continue;

                Type type = ability.GetType();
                if (!byType.TryGetValue(type, out List<BeyAbility> list))
                {
                    list = new List<BeyAbility>();
                    byType[type] = list;
                }

                list.Add(ability);
            }

            return byType;
        }

        private static BeyAbility TakeUnusedAbilityOfType(Dictionary<Type, List<BeyAbility>> byType, Type desiredType, HashSet<BeyAbility> used)
        {
            if (desiredType != null && byType.TryGetValue(desiredType, out List<BeyAbility> typedList))
            {
                for (int i = 0; i < typedList.Count; i++)
                {
                    BeyAbility candidate = typedList[i];
                    if (candidate != null && !used.Contains(candidate))
                        return candidate;
                }
            }

            foreach (KeyValuePair<Type, List<BeyAbility>> kv in byType)
            {
                List<BeyAbility> list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    BeyAbility candidate = list[i];
                    if (candidate != null && !used.Contains(candidate))
                        return candidate;
                }
            }

            return null;
        }

        private static BeyAbility FindTemplateAbility(Dictionary<Type, List<BeyAbility>> byType, Type desiredType)
        {
            if (desiredType != null && byType.TryGetValue(desiredType, out List<BeyAbility> typedList) && typedList.Count > 0)
                return typedList[0];

            foreach (KeyValuePair<Type, List<BeyAbility>> kv in byType)
            {
                List<BeyAbility> list = kv.Value;
                if (list.Count > 0)
                    return list[0];
            }

            return null;
        }

        private static BeyAbility CreateOrUpdateUniqueVariant(BeyAbility template, BeyPart part)
        {
            if (template == null || part == null)
                return null;

            string faceBoltName = string.IsNullOrWhiteSpace(part.PartName) ? part.name : part.PartName;
            string safeName = SanitizeAssetName(faceBoltName);
            string assetPath = $"{GeneratedAbilityFolder}/{safeName}.asset";

            BeyAbility existing = AssetDatabase.LoadAssetAtPath<BeyAbility>(assetPath);
            if (existing != null)
            {
                if (existing.GetType() == template.GetType())
                {
                    SetAbilityDisplayName(existing, $"{template.AbilityName} [{faceBoltName}]");
                    EditorUtility.SetDirty(existing);
                    return existing;
                }

                AssetDatabase.DeleteAsset(assetPath);
            }

            BeyAbility variant = UnityEngine.Object.Instantiate(template);
            variant.name = safeName;
            SetAbilityDisplayName(variant, $"{template.AbilityName} [{faceBoltName}]");
            AssetDatabase.CreateAsset(variant, assetPath);
            EditorUtility.SetDirty(variant);
            return variant;
        }

        private static void AssignAbility(BeyPart part, BeyAbility ability)
        {
            SerializedObject so = new SerializedObject(part);
            SerializedProperty equipped = so.FindProperty("equippedAbility");
            if (equipped == null)
                return;

            equipped.objectReferenceValue = ability;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(part);
        }

        private static void SetAbilityDisplayName(BeyAbility ability, string displayName)
        {
            if (ability == null)
                return;

            SerializedObject so = new SerializedObject(ability);
            SerializedProperty prop = so.FindProperty("abilityName");
            if (prop == null)
                return;

            prop.stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string SanitizeAssetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "UnnamedFaceBolt";

            string sanitized = Regex.Replace(value.Trim(), "[^a-zA-Z0-9 _-]", "");
            sanitized = sanitized.Replace("/", "-").Replace("\\", "-");
            return string.IsNullOrWhiteSpace(sanitized) ? "UnnamedFaceBolt" : sanitized;
        }

        private static void EnsureFolderPath(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private int CountDuplicateAbilityNames()
        {
            Dictionary<string, int> countByAbility = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows.Count; i++)
            {
                string abilityName = rows[i].AbilityName;
                if (string.IsNullOrWhiteSpace(abilityName) || abilityName == "(none)")
                    continue;

                if (!countByAbility.ContainsKey(abilityName))
                    countByAbility[abilityName] = 0;

                countByAbility[abilityName]++;
            }

            int duplicateRows = 0;
            foreach (KeyValuePair<string, int> kv in countByAbility)
            {
                if (kv.Value > 1)
                    duplicateRows += kv.Value - 1;
            }

            return duplicateRows;
        }

        private void ExportMarkdown()
        {
            Refresh();

            if (rows.Count == 0)
            {
                status = "No Face Bolt rows found. Nothing was exported.";
                Debug.LogWarning("[FaceBoltAbilityReport] Export skipped because no Face Bolt rows were found.");
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputPath = Path.Combine(projectRoot, "FACEBOLT_ABILITY_REPORT.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Face Bolt Ability Report");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("| Face Bolt | Part ID | Ability | Rarity | Source |");
            sb.AppendLine("|---|---|---|---|---|");

            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                sb.Append("| ").Append(EscapeCell(row.FaceBoltName))
                  .Append(" | ").Append(EscapeCell(row.PartId))
                  .Append(" | ").Append(EscapeCell(row.AbilityName))
                  .Append(" | ").Append(EscapeCell(row.AbilityRarity))
                  .Append(" | ").Append(EscapeCell(row.Source))
                  .AppendLine(" |");
            }

            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
                AssetDatabase.Refresh();

                status = $"Exported report to {outputPath}";
                Debug.Log($"[FaceBoltAbilityReport] Exported {rows.Count} rows to {outputPath}");
                EditorUtility.RevealInFinder(outputPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FaceBoltAbilityReport] Default export path failed ({outputPath}). Falling back to save dialog. {ex.Message}");

                string fallbackPath = EditorUtility.SaveFilePanel(
                    "Export Face Bolt Ability Report",
                    projectRoot,
                    "FACEBOLT_ABILITY_REPORT",
                    "md");

                if (string.IsNullOrWhiteSpace(fallbackPath))
                {
                    status = $"Export failed at default path and fallback was cancelled: {ex.Message}";
                    return;
                }

                try
                {
                    string fallbackDirectory = Path.GetDirectoryName(fallbackPath);
                    if (!string.IsNullOrWhiteSpace(fallbackDirectory) && !Directory.Exists(fallbackDirectory))
                        Directory.CreateDirectory(fallbackDirectory);

                    File.WriteAllText(fallbackPath, sb.ToString(), new UTF8Encoding(false));
                    if (fallbackPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                        AssetDatabase.Refresh();

                    status = $"Exported report to {fallbackPath}";
                    Debug.Log($"[FaceBoltAbilityReport] Exported {rows.Count} rows to {fallbackPath}");
                    EditorUtility.RevealInFinder(fallbackPath);
                }
                catch (Exception fallbackEx)
                {
                    status = $"Export failed: {fallbackEx.Message}";
                    Debug.LogError($"[FaceBoltAbilityReport] Failed to export report. Default path error: {ex}. Fallback path error: {fallbackEx}");
                }
            }
        }

        private static string EscapeCell(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("|", "\\|");
        }
    }
}
