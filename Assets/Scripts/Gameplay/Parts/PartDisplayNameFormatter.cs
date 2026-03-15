using System;
using System.Collections.Generic;
using System.Globalization;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    public static class PartDisplayNameFormatter
    {
        public static string ToShortDisplayName(BeyPart part)
        {
            if (part == null)
                return "None";

            string rawName = part.PartName;
            if (string.IsNullOrWhiteSpace(rawName))
                return GetFriendlyType(part.PartType);

            string normalized = rawName.Replace('-', '_').Trim();
            string[] tokens = normalized.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return GetFriendlyType(part.PartType);

            bool startsWithRun = string.Equals(tokens[0], "RUN", StringComparison.OrdinalIgnoreCase);
            if (startsWithRun)
            {
                return $"Run {GetFriendlyType(part.PartType)}";
            }

            List<string> kept = new List<string>();
            string friendlyType = GetFriendlyType(part.PartType);
            string typeNoSpace = friendlyType.Replace(" ", "");

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                if (int.TryParse(token, out _))
                    continue;

                if (string.Equals(token, part.PartType.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, friendlyType, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, typeNoSpace, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                kept.Add(ToTitleCaseWord(token));
            }

            if (kept.Count == 0)
                return friendlyType;

            return string.Join(" ", kept);
        }

        private static string GetFriendlyType(PartType type)
        {
            return type switch
            {
                PartType.FusionWheel => "Fusion Wheel",
                PartType.EnergyRing => "Energy Ring",
                PartType.FaceBolt => "Face Bolt",
                _ => ToTitleCaseWord(type.ToString())
            };
        }

        private static string ToTitleCaseWord(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            string lower = token.ToLowerInvariant();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lower);
        }
    }
}
