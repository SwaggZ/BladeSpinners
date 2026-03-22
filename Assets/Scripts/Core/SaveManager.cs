using System;
using System.Collections.Generic;
using System.IO;
using BladeSpinners.Gameplay.Parts;
using UnityEngine;

namespace BladeSpinners.Core
{
    public static class SaveManager
    {
        private const string SaveFileName = "bladespinners_save.json";

        [Serializable]
        private class SaveData
        {
            public List<string> ownedPartIDs = new List<string>();
            public string loadoutTip = "";
            public string loadoutTrack = "";
            public string loadoutFusionWheel = "";
            public string loadoutEnergyRing = "";
            public string loadoutFaceBolt = "";
        }

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(List<BeyPart> ownedParts, Dictionary<PartType, BeyPart> loadout)
        {
            SaveData data = new SaveData();

            if (ownedParts != null)
            {
                for (int i = 0; i < ownedParts.Count; i++)
                {
                    if (ownedParts[i] != null && !string.IsNullOrEmpty(ownedParts[i].PartID))
                        data.ownedPartIDs.Add(ownedParts[i].PartID);
                }
            }

            if (loadout != null)
            {
                data.loadoutTip = GetID(loadout, PartType.Tip);
                data.loadoutTrack = GetID(loadout, PartType.Track);
                data.loadoutFusionWheel = GetID(loadout, PartType.FusionWheel);
                data.loadoutEnergyRing = GetID(loadout, PartType.EnergyRing);
                data.loadoutFaceBolt = GetID(loadout, PartType.FaceBolt);
            }

            string json = JsonUtility.ToJson(data, true);
            try
            {
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Failed to write save: {e.Message}");
            }
        }

        public static bool TryLoad(
            List<BeyPart> catalog,
            out List<BeyPart> ownedParts,
            out Dictionary<PartType, BeyPart> loadout)
        {
            ownedParts = null;
            loadout = null;

            if (!File.Exists(SavePath))
                return false;

            string json;
            try
            {
                json = File.ReadAllText(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Failed to read save: {e.Message}");
                return false;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Failed to parse save: {e.Message}");
                return false;
            }

            if (data == null)
                return false;

            Dictionary<string, BeyPart> lookup = BuildLookup(catalog);

            ownedParts = new List<BeyPart>();
            for (int i = 0; i < data.ownedPartIDs.Count; i++)
            {
                if (lookup.TryGetValue(data.ownedPartIDs[i], out BeyPart part))
                    ownedParts.Add(part);
            }

            if (ownedParts.Count == 0)
                return false;

            loadout = new Dictionary<PartType, BeyPart>();
            TryResolve(lookup, data.loadoutTip, PartType.Tip, loadout);
            TryResolve(lookup, data.loadoutTrack, PartType.Track, loadout);
            TryResolve(lookup, data.loadoutFusionWheel, PartType.FusionWheel, loadout);
            TryResolve(lookup, data.loadoutEnergyRing, PartType.EnergyRing, loadout);
            TryResolve(lookup, data.loadoutFaceBolt, PartType.FaceBolt, loadout);

            return true;
        }

        public static bool HasSave() => File.Exists(SavePath);

        private static string GetID(Dictionary<PartType, BeyPart> loadout, PartType type)
        {
            if (loadout.TryGetValue(type, out BeyPart part) && part != null)
                return part.PartID ?? "";
            return "";
        }

        private static void TryResolve(
            Dictionary<string, BeyPart> lookup, string id, PartType type,
            Dictionary<PartType, BeyPart> loadout)
        {
            if (!string.IsNullOrEmpty(id) && lookup.TryGetValue(id, out BeyPart part) && part.PartType == type)
                loadout[type] = part;
        }

        private static Dictionary<string, BeyPart> BuildLookup(List<BeyPart> catalog)
        {
            Dictionary<string, BeyPart> map = new Dictionary<string, BeyPart>();
            if (catalog == null) return map;
            for (int i = 0; i < catalog.Count; i++)
            {
                BeyPart p = catalog[i];
                if (p != null && !string.IsNullOrEmpty(p.PartID) && !map.ContainsKey(p.PartID))
                    map[p.PartID] = p;
            }
            return map;
        }
    }
}
