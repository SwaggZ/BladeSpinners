using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BladeSpinners.Core
{
    [Serializable]
    public sealed class RunRecord
    {
        public string recordedUtc;
        public float durationSeconds;
        public int arenasCleared;
        public int totalArenas;
        public int runSeed;
        public bool completed;
    }

    /// <summary>
    /// Persistent, run-independent personal best history. Only the union of
    /// the ten fastest completions and ten deepest attempts is retained.
    /// </summary>
    public static class RunRecordStore
    {
        private const string FileName =
            "bladespinners_run_records.json";
        private const int LeaderboardSize = 10;

        [Serializable]
        private sealed class RunRecordData
        {
            public int version = 1;
            public List<RunRecord> records =
                new List<RunRecord>();
        }

        private static RunRecordData cachedData;

        private static string SavePath => Path.Combine(
            Application.persistentDataPath,
            FileName);

        public static void Record(
            float durationSeconds,
            int arenasCleared,
            int totalArenas,
            int runSeed,
            bool completed)
        {
            RunRecordData data = Load();
            data.records.Add(new RunRecord
            {
                recordedUtc = DateTime.UtcNow.ToString("o"),
                durationSeconds = Mathf.Max(0f, durationSeconds),
                arenasCleared = Mathf.Clamp(
                    arenasCleared,
                    0,
                    Mathf.Max(1, totalArenas)),
                totalArenas = Mathf.Max(1, totalArenas),
                runSeed = runSeed,
                completed = completed
            });

            PruneToLeaderboards(data);
            Save(data);
        }

        public static IReadOnlyList<RunRecord>
            GetFastestCompleted()
        {
            List<RunRecord> result =
                new List<RunRecord>(Load().records);
            result.RemoveAll(record =>
                record == null || !record.completed);
            result.Sort(CompareFastest);
            if (result.Count > LeaderboardSize)
            {
                result.RemoveRange(
                    LeaderboardSize,
                    result.Count - LeaderboardSize);
            }
            return result;
        }

        public static IReadOnlyList<RunRecord> GetDeepest()
        {
            List<RunRecord> result =
                new List<RunRecord>(Load().records);
            result.RemoveAll(record => record == null);
            result.Sort(CompareDeepest);
            if (result.Count > LeaderboardSize)
            {
                result.RemoveRange(
                    LeaderboardSize,
                    result.Count - LeaderboardSize);
            }
            return result;
        }

        private static RunRecordData Load()
        {
            if (cachedData != null)
                return cachedData;

            cachedData = new RunRecordData();
            if (!File.Exists(SavePath))
                return cachedData;

            try
            {
                string json = File.ReadAllText(SavePath);
                RunRecordData loaded =
                    JsonUtility.FromJson<RunRecordData>(json);
                if (loaded != null)
                {
                    loaded.records ??= new List<RunRecord>();
                    cachedData = loaded;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[RunRecords] Failed to load records: "
                    + exception.Message);
            }
            return cachedData;
        }

        private static void Save(RunRecordData data)
        {
            string temporaryPath = SavePath + ".tmp";
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(SavePath))
                {
                    string backupPath = SavePath + ".bak";
                    File.Replace(
                        temporaryPath,
                        SavePath,
                        backupPath,
                        true);
                }
                else
                {
                    File.Move(temporaryPath, SavePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[RunRecords] Failed to save records: "
                    + exception.Message);
                try
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
            }
        }

        private static void PruneToLeaderboards(
            RunRecordData data)
        {
            List<RunRecord> fastest =
                new List<RunRecord>(data.records);
            fastest.RemoveAll(record =>
                record == null || !record.completed);
            fastest.Sort(CompareFastest);

            List<RunRecord> deepest =
                new List<RunRecord>(data.records);
            deepest.RemoveAll(record => record == null);
            deepest.Sort(CompareDeepest);

            HashSet<RunRecord> retained =
                new HashSet<RunRecord>();
            for (int i = 0;
                 i < Mathf.Min(LeaderboardSize, fastest.Count);
                 i++)
            {
                retained.Add(fastest[i]);
            }
            for (int i = 0;
                 i < Mathf.Min(LeaderboardSize, deepest.Count);
                 i++)
            {
                retained.Add(deepest[i]);
            }
            data.records.RemoveAll(record =>
                record == null || !retained.Contains(record));
        }

        private static int CompareFastest(
            RunRecord left,
            RunRecord right)
        {
            int timeOrder = left.durationSeconds.CompareTo(
                right.durationSeconds);
            return timeOrder != 0
                ? timeOrder
                : string.CompareOrdinal(
                    left.recordedUtc,
                    right.recordedUtc);
        }

        private static int CompareDeepest(
            RunRecord left,
            RunRecord right)
        {
            int depthOrder = right.arenasCleared.CompareTo(
                left.arenasCleared);
            if (depthOrder != 0)
                return depthOrder;

            int completionOrder =
                right.completed.CompareTo(left.completed);
            if (completionOrder != 0)
                return completionOrder;

            return left.durationSeconds.CompareTo(
                right.durationSeconds);
        }
    }
}
