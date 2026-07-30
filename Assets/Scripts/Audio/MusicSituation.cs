using System;
using UnityEngine;

namespace BladeSpinners.Audio
{
    public enum MusicSituation
    {
        MainMenu,
        Inventory,
        Battle,
        BossBattle,
        Victory,
        Lose,
        StartScreen
    }

    /// <summary>
    /// Public, immutable now-playing data used by the music banner and diagnostics.
    /// </summary>
    public readonly struct MusicTrackInfo
    {
        public AudioClip Clip { get; }
        public Texture2D Logo { get; }
        public string Title { get; }
        public string Author { get; }
        public MusicSituation Situation { get; }
        public bool IsValid => Clip != null;

        public MusicTrackInfo(
            AudioClip clip,
            Texture2D logo,
            string title,
            string author,
            MusicSituation situation)
        {
            Clip = clip;
            Logo = logo;
            Title = string.IsNullOrWhiteSpace(title)
                ? (clip != null ? clip.name : "Unknown Track")
                : title.Trim();
            Author = string.IsNullOrWhiteSpace(author)
                ? "Unknown Artist"
                : author.Trim();
            Situation = situation;
        }
    }
}
