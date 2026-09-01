#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Shrine;

namespace BladeSpinners.Editor
{
    public static class SaveDataEditorTools
    {
        [MenuItem("BladeSpinners/Reset All Save Data (New Player Profile)", false, 100)]
        public static void ResetAllSaveData()
        {
            if (EditorUtility.DisplayDialog(
                "Reset Save Data & Profile?",
                "Are you sure you want to completely wipe all player save data, inventory, tournament records, run wins, and unlocked shrine blessings? This will return the game to a fresh new player state.",
                "Yes, Reset Everything",
                "Cancel"))
            {
                // 1. Delete persistent JSON files
                string savePath = Path.Combine(Application.persistentDataPath, "bladespinners_save.json");
                if (File.Exists(savePath)) File.Delete(savePath);

                string recordsPath = Path.Combine(Application.persistentDataPath, "bladespinners_run_records.json");
                if (File.Exists(recordsPath)) File.Delete(recordsPath);

                string recordsBakPath = Path.Combine(Application.persistentDataPath, "bladespinners_run_records.json.bak");
                if (File.Exists(recordsBakPath)) File.Delete(recordsBakPath);

                // 2. Clear PlayerPrefs (wins, blessings, settings)
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();

                // 3. Reset in-memory managers if loaded
                ShrineBlessingsUnlockManager.ResetToStarterPool();

                Debug.Log("[SaveDataEditorTools] All save data, inventory, records, and PlayerPrefs successfully reset to default new player state!");
                EditorUtility.DisplayDialog("Reset Complete", "All player data has been reset to a brand new player profile.", "OK");
            }
        }
    }
}
#endif
