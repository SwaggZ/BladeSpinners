using System;
using System.Collections.Generic;

namespace BladeSpinners.Audio
{
    /// <summary>
    /// Runtime keys mirror folders below Assets/SoundEffects.
    /// Keep interaction code pointed at these keys instead of individual clips.
    /// </summary>
    public static class SoundPaths
    {
        public const string HitBeyAgainstBey = "Hits/BayXBay";
        public const string HitBeyAgainstWall = "Hits/BayXWall";

        public const string GuiButton = "GUI/Button";
        public const string GuiEquipPart = "GUI/Equip Part";
        public const string GuiGameStart = "GUI/Game Start";
        public const string GuiGameWin = "GUI/Game Win";
        public const string GuiGameLose = "GUI/Game Lose";
        public const string GuiStartScreenTransition =
            "GUI/Start Screen Transition";

        public const string BackgroundMusic = "Music/Background";

        public static readonly IReadOnlyList<string> RequiredFolderKeys =
            Array.AsReadOnly(new[]
            {
                HitBeyAgainstBey,
                HitBeyAgainstWall,
                GuiButton,
                GuiEquipPart,
                GuiGameStart,
                GuiGameWin,
                GuiGameLose,
                GuiStartScreenTransition,
                BackgroundMusic
            });

        public static string Ability(string abilityName)
        {
            return $"Abilities/{NormalizeSegment(abilityName)}";
        }

        public static string Normalize(string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? string.Empty
                : key.Trim().Replace('\\', '/').Trim('/');
        }

        private static string NormalizeSegment(string segment)
        {
            return string.IsNullOrWhiteSpace(segment)
                ? "Unknown"
                : segment.Trim().Replace("/", "-").Replace("\\", "-");
        }
    }
}
