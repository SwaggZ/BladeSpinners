using System;
using System.Collections.Generic;
using UnityEngine;

namespace BladeSpinners.Audio
{
    /// <summary>
    /// Build-safe snapshot of Assets/SoundEffects. The editor generator populates this
    /// asset because AssetDatabase folder discovery is not available in a player build.
    /// </summary>
    public sealed class RuntimeSoundCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class FolderEntry
        {
            [SerializeField] private string key;
            [SerializeField] private List<AudioClip> clips = new List<AudioClip>();

            public string Key => key;
            public IReadOnlyList<AudioClip> Clips => clips;

            public FolderEntry(string key, IEnumerable<AudioClip> clips)
            {
                this.key = SoundPaths.Normalize(key);
                this.clips = clips != null
                    ? new List<AudioClip>(clips)
                    : new List<AudioClip>();
            }
        }

        [Serializable]
        public sealed class MusicTrackEntry
        {
            [SerializeField] private AudioClip clip;
            [SerializeField] private Texture2D logo;
            [SerializeField] private string sourceFile;
            [SerializeField] private string title;
            [SerializeField] private string author;
            [SerializeField] private MusicSituation situation;

            public AudioClip Clip => clip;
            public Texture2D Logo => logo;
            public string SourceFile => sourceFile;
            public string Title => title;
            public string Author => author;
            public MusicSituation Situation => situation;

            public MusicTrackEntry(
                AudioClip clip,
                Texture2D logo,
                string sourceFile,
                string title,
                string author,
                MusicSituation situation)
            {
                this.clip = clip;
                this.logo = logo;
                this.sourceFile = sourceFile?.Trim() ?? string.Empty;
                this.title = string.IsNullOrWhiteSpace(title)
                    ? (clip != null ? clip.name : "Unknown Track")
                    : title.Trim();
                this.author = string.IsNullOrWhiteSpace(author)
                    ? "Unknown Artist"
                    : author.Trim();
                this.situation = situation;
            }

            public MusicTrackInfo ToTrackInfo()
            {
                return new MusicTrackInfo(
                    clip,
                    logo,
                    title,
                    author,
                    situation);
            }
        }

        [SerializeField] private List<FolderEntry> folders = new List<FolderEntry>();
        [SerializeField] private List<MusicTrackEntry> musicTracks =
            new List<MusicTrackEntry>();

        private Dictionary<string, FolderEntry> lookup;
        private Dictionary<MusicSituation, List<MusicTrackEntry>> musicLookup;

        public IReadOnlyList<FolderEntry> Folders => folders;
        public IReadOnlyList<MusicTrackEntry> MusicTracks => musicTracks;
        public int FolderCount => folders.Count;
        public int MusicTrackCount => musicTracks.Count;

        public int ClipCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < folders.Count; i++)
                {
                    if (folders[i] != null && folders[i].Clips != null)
                        count += folders[i].Clips.Count;
                }

                return count;
            }
        }

        public bool TryGetClips(string key, out IReadOnlyList<AudioClip> clips)
        {
            EnsureLookup();
            if (lookup.TryGetValue(SoundPaths.Normalize(key), out FolderEntry entry))
            {
                clips = entry.Clips;
                return true;
            }

            clips = Array.Empty<AudioClip>();
            return false;
        }

        public bool TryGetMusicTracks(
            MusicSituation situation,
            out IReadOnlyList<MusicTrackEntry> tracks)
        {
            EnsureMusicLookup();
            if (musicLookup.TryGetValue(
                    situation,
                    out List<MusicTrackEntry> entries))
            {
                tracks = entries;
                return true;
            }

            tracks = Array.Empty<MusicTrackEntry>();
            return false;
        }

        /// <summary>
        /// Replaces generated data only when its keys or clip references changed.
        /// Returns true when the asset needs saving.
        /// </summary>
        public bool ReplaceEntries(IReadOnlyList<FolderEntry> entries)
        {
            if (EntriesMatch(entries))
                return false;

            folders.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                    folders.Add(entries[i]);
            }

            lookup = null;
            return true;
        }

        public bool ReplaceMusicTracks(
            IReadOnlyList<MusicTrackEntry> tracks)
        {
            if (MusicEntriesMatch(tracks))
                return false;

            musicTracks.Clear();
            if (tracks != null)
            {
                for (int i = 0; i < tracks.Count; i++)
                    musicTracks.Add(tracks[i]);
            }

            musicLookup = null;
            return true;
        }

        private bool EntriesMatch(IReadOnlyList<FolderEntry> entries)
        {
            if (entries == null || folders.Count != entries.Count)
                return false;

            for (int i = 0; i < folders.Count; i++)
            {
                FolderEntry current = folders[i];
                FolderEntry incoming = entries[i];
                if (current == null || incoming == null
                    || !string.Equals(current.Key, incoming.Key, StringComparison.Ordinal)
                    || current.Clips.Count != incoming.Clips.Count)
                {
                    return false;
                }

                for (int clipIndex = 0; clipIndex < current.Clips.Count; clipIndex++)
                {
                    if (current.Clips[clipIndex] != incoming.Clips[clipIndex])
                        return false;
                }
            }

            return true;
        }

        private bool MusicEntriesMatch(
            IReadOnlyList<MusicTrackEntry> tracks)
        {
            if (tracks == null || musicTracks.Count != tracks.Count)
                return false;

            for (int i = 0; i < musicTracks.Count; i++)
            {
                MusicTrackEntry current = musicTracks[i];
                MusicTrackEntry incoming = tracks[i];
                if (current == null
                    || incoming == null
                    || current.Clip != incoming.Clip
                    || current.Logo != incoming.Logo
                    || current.Situation != incoming.Situation
                    || !string.Equals(
                        current.SourceFile,
                        incoming.SourceFile,
                        StringComparison.Ordinal)
                    || !string.Equals(
                        current.Title,
                        incoming.Title,
                        StringComparison.Ordinal)
                    || !string.Equals(
                        current.Author,
                        incoming.Author,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private void OnEnable()
        {
            lookup = null;
            musicLookup = null;
        }

        private void EnsureLookup()
        {
            if (lookup != null)
                return;

            lookup = new Dictionary<string, FolderEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < folders.Count; i++)
            {
                FolderEntry entry = folders[i];
                if (entry == null)
                    continue;

                string normalizedKey = SoundPaths.Normalize(entry.Key);
                if (!string.IsNullOrEmpty(normalizedKey))
                    lookup[normalizedKey] = entry;
            }
        }

        private void EnsureMusicLookup()
        {
            if (musicLookup != null)
                return;

            musicLookup =
                new Dictionary<MusicSituation, List<MusicTrackEntry>>();
            for (int i = 0; i < musicTracks.Count; i++)
            {
                MusicTrackEntry entry = musicTracks[i];
                if (entry == null || entry.Clip == null)
                    continue;

                if (!musicLookup.TryGetValue(
                        entry.Situation,
                        out List<MusicTrackEntry> entries))
                {
                    entries = new List<MusicTrackEntry>();
                    musicLookup.Add(entry.Situation, entries);
                }
                entries.Add(entry);
            }
        }
    }
}
