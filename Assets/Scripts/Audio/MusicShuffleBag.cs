using System;
using System.Collections.Generic;
using UnityEngine;

namespace BladeSpinners.Audio
{
    /// <summary>
    /// Randomized, non-repeating playlist state for one music category.
    /// Every playable track is consumed once before the bag is reshuffled.
    /// </summary>
    public sealed class MusicShuffleBag
    {
        private readonly System.Random random;
        private readonly List<
            RuntimeSoundCatalog.MusicTrackEntry> sourceTracks =
                new List<RuntimeSoundCatalog.MusicTrackEntry>();
        private readonly List<
            RuntimeSoundCatalog.MusicTrackEntry> remainingTracks =
                new List<RuntimeSoundCatalog.MusicTrackEntry>();

        public int SourceCount => sourceTracks.Count;
        public int RemainingCount => remainingTracks.Count;

        public MusicShuffleBag(int seed)
        {
            random = new System.Random(seed);
        }

        public RuntimeSoundCatalog.MusicTrackEntry Take(
            IReadOnlyList<
                RuntimeSoundCatalog.MusicTrackEntry> tracks,
            AudioClip excludedClip)
        {
            SynchronizeSource(tracks);
            if (sourceTracks.Count == 0)
                return null;

            if (remainingTracks.Count == 0)
                Refill();

            int selectedIndex =
                FindSelectableIndex(excludedClip);
            if (selectedIndex < 0
                && sourceTracks.Count > 1)
            {
                // This can occur after returning to a partially consumed category
                // whose only remaining track is the globally last-played song.
                // Start a fresh shuffle so playback never stalls or repeats.
                Refill();
                selectedIndex =
                    FindSelectableIndex(excludedClip);
            }

            if (selectedIndex < 0)
                return null;

            RuntimeSoundCatalog.MusicTrackEntry selected =
                remainingTracks[selectedIndex];
            remainingTracks.RemoveAt(selectedIndex);
            return selected;
        }

        private void SynchronizeSource(
            IReadOnlyList<
                RuntimeSoundCatalog.MusicTrackEntry> tracks)
        {
            List<RuntimeSoundCatalog.MusicTrackEntry> playable =
                new List<
                    RuntimeSoundCatalog.MusicTrackEntry>();
            HashSet<AudioClip> seenClips =
                new HashSet<AudioClip>();

            if (tracks != null)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    RuntimeSoundCatalog.MusicTrackEntry track =
                        tracks[i];
                    if (track == null
                        || track.Clip == null
                        || !seenClips.Add(track.Clip))
                    {
                        continue;
                    }
                    playable.Add(track);
                }
            }

            if (SourcesMatch(playable))
                return;

            sourceTracks.Clear();
            sourceTracks.AddRange(playable);
            remainingTracks.Clear();
        }

        private bool SourcesMatch(
            IReadOnlyList<
                RuntimeSoundCatalog.MusicTrackEntry> tracks)
        {
            if (tracks.Count != sourceTracks.Count)
                return false;

            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i] != sourceTracks[i])
                    return false;
            }
            return true;
        }

        private void Refill()
        {
            remainingTracks.Clear();
            remainingTracks.AddRange(sourceTracks);

            // Fisher-Yates gives each category a new unbiased order.
            for (int i = remainingTracks.Count - 1;
                 i > 0;
                 i--)
            {
                int swapIndex = random.Next(i + 1);
                RuntimeSoundCatalog.MusicTrackEntry previous =
                    remainingTracks[i];
                remainingTracks[i] =
                    remainingTracks[swapIndex];
                remainingTracks[swapIndex] = previous;
            }
        }

        private int FindSelectableIndex(
            AudioClip excludedClip)
        {
            if (remainingTracks.Count == 0)
                return -1;

            int nextIndex = remainingTracks.Count - 1;
            if (excludedClip == null
                || remainingTracks[nextIndex].Clip
                    != excludedClip)
            {
                return nextIndex;
            }

            int eligibleCount = 0;
            for (int i = 0;
                 i < remainingTracks.Count - 1;
                 i++)
            {
                if (remainingTracks[i].Clip
                    != excludedClip)
                {
                    eligibleCount++;
                }
            }
            if (eligibleCount == 0)
                return -1;

            int selectedOrdinal =
                random.Next(eligibleCount);
            for (int i = 0;
                 i < remainingTracks.Count - 1;
                 i++)
            {
                if (remainingTracks[i].Clip
                    == excludedClip)
                {
                    continue;
                }
                if (selectedOrdinal == 0)
                    return i;
                selectedOrdinal--;
            }

            return -1;
        }
    }
}
