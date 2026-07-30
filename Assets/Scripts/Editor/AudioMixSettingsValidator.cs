#if UNITY_EDITOR
using System;
using System.Reflection;
using BladeSpinners.Audio;
using BladeSpinners.Gameplay.UI;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    public static class AudioMixSettingsValidator
    {
        private const float Epsilon = 0.0001f;

        [MenuItem("Blade Spinners/Audio/Validate Audio Mix Controls")]
        public static void Validate()
        {
            ValidateEffectiveLevels();
            ValidatePreferenceRoundTrip();
            ValidateRuntimeWiring();

            Debug.Log(
                "[AudioMix] Passed: Master, Sound Effects, Music, and GUI " +
                "levels clamp, persist, expose independent UI controls, and " +
                "resolve through Master x Category output.");
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static void ValidateEffectiveLevels()
        {
            AudioMixLevels levels = new AudioMixLevels(
                0.5f,
                0.8f,
                0.6f,
                0.4f);

            AssertApproximately(
                0.4f,
                levels.EffectiveSoundEffects,
                "Master x Sound Effects");
            AssertApproximately(
                0.3f,
                levels.EffectiveMusic,
                "Master x Music");
            AssertApproximately(
                0.2f,
                levels.EffectiveGui,
                "Master x GUI");

            AudioMixLevels clamped = new AudioMixLevels(
                2f,
                -1f,
                1.5f,
                0.25f)
                .Clamped();
            AssertApproximately(1f, clamped.Master, "Master clamp");
            AssertApproximately(
                0f,
                clamped.SoundEffects,
                "Sound Effects clamp");
            AssertApproximately(1f, clamped.Music, "Music clamp");
            AssertApproximately(0.25f, clamped.Gui, "GUI clamp");
        }

        private static void ValidatePreferenceRoundTrip()
        {
            string[] keys =
            {
                AudioMixPreferences.MasterKey,
                AudioMixPreferences.SoundEffectsKey,
                AudioMixPreferences.MusicKey,
                AudioMixPreferences.GuiKey
            };
            bool[] existed = new bool[keys.Length];
            float[] previous = new float[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                existed[i] = PlayerPrefs.HasKey(keys[i]);
                previous[i] = PlayerPrefs.GetFloat(keys[i], 0f);
            }

            try
            {
                AudioMixPreferences.Save(
                    new AudioMixLevels(
                        0.73f,
                        0.61f,
                        0.49f,
                        0.37f));
                AudioMixLevels loaded =
                    AudioMixPreferences.Load();
                AssertApproximately(
                    0.73f,
                    loaded.Master,
                    "saved Master");
                AssertApproximately(
                    0.61f,
                    loaded.SoundEffects,
                    "saved Sound Effects");
                AssertApproximately(
                    0.49f,
                    loaded.Music,
                    "saved Music");
                AssertApproximately(
                    0.37f,
                    loaded.Gui,
                    "saved GUI");
            }
            finally
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (existed[i])
                        PlayerPrefs.SetFloat(keys[i], previous[i]);
                    else
                        PlayerPrefs.DeleteKey(keys[i]);
                }
                PlayerPrefs.Save();
            }
        }

        private static void ValidateRuntimeWiring()
        {
            BindingFlags privateInstance =
                BindingFlags.Instance | BindingFlags.NonPublic;
            Type soundManagerType = typeof(SoundManager);
            AssertFloatField(
                soundManagerType,
                "masterVolume",
                privateInstance);
            AssertFloatField(
                soundManagerType,
                "sfxVolume",
                privateInstance);
            AssertFloatField(
                soundManagerType,
                "musicVolume",
                privateInstance);
            AssertFloatField(
                soundManagerType,
                "uiVolume",
                privateInstance);

            MethodInfo setter = soundManagerType.GetMethod(
                nameof(SoundManager.SetMixVolumes),
                BindingFlags.Static | BindingFlags.Public);
            if (setter == null
                || setter.GetParameters().Length != 5)
            {
                throw new InvalidOperationException(
                    "SoundManager.SetMixVolumes is missing its four " +
                    "category values and persistence control.");
            }

            Type uiType = typeof(RuntimeGameUiController);
            AssertFloatField(
                uiType,
                "settingsMasterVolume",
                privateInstance);
            AssertFloatField(
                uiType,
                "settingsSoundEffectsVolume",
                privateInstance);
            AssertFloatField(
                uiType,
                "settingsMusicVolume",
                privateInstance);
            AssertFloatField(
                uiType,
                "settingsGuiVolume",
                privateInstance);

            if (uiType.GetMethod(
                    "LoadAudioSettings",
                    privateInstance) == null
                || uiType.GetMethod(
                    "ApplyAudioSettings",
                    privateInstance) == null)
            {
                throw new InvalidOperationException(
                    "Runtime settings UI is missing audio preference " +
                    "load/apply wiring.");
            }
        }

        private static void AssertFloatField(
            Type type,
            string fieldName,
            BindingFlags flags)
        {
            if (type.GetField(fieldName, flags)?.FieldType
                != typeof(float))
            {
                throw new InvalidOperationException(
                    $"{type.Name}.{fieldName} is missing.");
            }
        }

        private static void AssertApproximately(
            float expected,
            float actual,
            string label)
        {
            if (Mathf.Abs(expected - actual) > Epsilon)
            {
                throw new InvalidOperationException(
                    $"{label} expected {expected:F3}, got {actual:F3}.");
            }
        }
    }
}
#endif
