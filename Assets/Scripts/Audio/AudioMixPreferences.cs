using System;
using UnityEngine;

namespace BladeSpinners.Audio
{
    /// <summary>
    /// User-facing audio levels. Master is applied globally and each category is
    /// applied to its own SoundManager sources, so final output is Master x Category.
    /// </summary>
    [Serializable]
    public struct AudioMixLevels
    {
        public const float DefaultMaster = 1f;
        public const float DefaultSoundEffects = 0.9f;
        public const float DefaultMusic = 0.45f;
        public const float DefaultGui = 0.8f;

        public float Master;
        public float SoundEffects;
        public float Music;
        public float Gui;

        public AudioMixLevels(
            float master,
            float soundEffects,
            float music,
            float gui)
        {
            Master = master;
            SoundEffects = soundEffects;
            Music = music;
            Gui = gui;
        }

        public static AudioMixLevels Defaults =>
            new AudioMixLevels(
                DefaultMaster,
                DefaultSoundEffects,
                DefaultMusic,
                DefaultGui);

        public AudioMixLevels Clamped()
        {
            return new AudioMixLevels(
                Mathf.Clamp01(Master),
                Mathf.Clamp01(SoundEffects),
                Mathf.Clamp01(Music),
                Mathf.Clamp01(Gui));
        }

        public float EffectiveSoundEffects =>
            SoundManager.CalculateEffectiveVolume(
                Master,
                SoundEffects);

        public float EffectiveMusic =>
            SoundManager.CalculateEffectiveVolume(
                Master,
                Music);

        public float EffectiveGui =>
            SoundManager.CalculateEffectiveVolume(
                Master,
                Gui);
    }

    /// <summary>
    /// Audio preferences are separate from run/collection saves, so saving a
    /// loadout cannot overwrite the player's mix.
    /// </summary>
    public static class AudioMixPreferences
    {
        public const string MasterKey =
            "BladeSpinners.Audio.MasterVolume";
        public const string SoundEffectsKey =
            "BladeSpinners.Audio.SoundEffectsVolume";
        public const string MusicKey =
            "BladeSpinners.Audio.MusicVolume";
        public const string GuiKey =
            "BladeSpinners.Audio.GuiVolume";

        public static AudioMixLevels Load()
        {
            return new AudioMixLevels(
                PlayerPrefs.GetFloat(
                    MasterKey,
                    AudioMixLevels.DefaultMaster),
                PlayerPrefs.GetFloat(
                    SoundEffectsKey,
                    AudioMixLevels.DefaultSoundEffects),
                PlayerPrefs.GetFloat(
                    MusicKey,
                    AudioMixLevels.DefaultMusic),
                PlayerPrefs.GetFloat(
                    GuiKey,
                    AudioMixLevels.DefaultGui))
                .Clamped();
        }

        public static void Save(AudioMixLevels levels)
        {
            AudioMixLevels clamped = levels.Clamped();
            PlayerPrefs.SetFloat(MasterKey, clamped.Master);
            PlayerPrefs.SetFloat(
                SoundEffectsKey,
                clamped.SoundEffects);
            PlayerPrefs.SetFloat(MusicKey, clamped.Music);
            PlayerPrefs.SetFloat(GuiKey, clamped.Gui);
            PlayerPrefs.Save();
        }
    }
}
