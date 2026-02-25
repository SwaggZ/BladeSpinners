using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// ScriptableObject registry that holds references to every part available in the game.
    /// This includes template parts that can be instantiated or referenced by procedural generation.
    /// Permanently unlocked parts are tracked here, while run-temporary parts are in PartInventory.
    /// </summary>
    public class PartDatabase : ScriptableObject
    {
        [SerializeField]
        private List<BeyPart> allParts = new List<BeyPart>();

        /// <summary>
        /// Dictionary for fast lookup by part ID.
        /// </summary>
        private Dictionary<string, BeyPart> partsByID = new Dictionary<string, BeyPart>();

        private void OnEnable()
        {
            RebuildIndex();
        }

        /// <summary>
        /// Rebuilds the ID index when the database is loaded or modified.
        /// </summary>
        public void RebuildIndex()
        {
            partsByID.Clear();
            foreach (BeyPart part in allParts)
            {
                if (part != null && !string.IsNullOrEmpty(part.PartID))
                {
                    partsByID[part.PartID] = part;
                }
            }
        }

        /// <summary>
        /// Gets a part by its ID.
        /// </summary>
        public BeyPart GetPartByID(string partID)
        {
            if (string.IsNullOrEmpty(partID) || !partsByID.ContainsKey(partID))
                return null;

            return partsByID[partID];
        }

        /// <summary>
        /// Gets all parts of a specific type.
        /// </summary>
        public List<BeyPart> GetPartsByType(PartType partType)
        {
            List<BeyPart> result = new List<BeyPart>();
            foreach (BeyPart part in allParts)
            {
                if (part != null && part.PartType == partType)
                {
                    result.Add(part);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all parts with a specific rarity tier.
        /// </summary>
        public List<BeyPart> GetPartsByRarity(RarityTier rarity)
        {
            List<BeyPart> result = new List<BeyPart>();
            foreach (BeyPart part in allParts)
            {
                if (part != null && part.Rarity == rarity)
                {
                    result.Add(part);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all parts with a specific tag.
        /// </summary>
        public List<BeyPart> GetPartsByTag(PartTag tag)
        {
            List<BeyPart> result = new List<BeyPart>();
            foreach (BeyPart part in allParts)
            {
                if (part != null && part.Tags.Contains(tag))
                {
                    result.Add(part);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all parts in the database.
        /// </summary>
        public List<BeyPart> GetAllParts()
        {
            return new List<BeyPart>(allParts);
        }

        /// <summary>
        /// Adds a part to the database (used in editor).
        /// </summary>
        public void AddPart(BeyPart part)
        {
            if (part != null && !allParts.Contains(part))
            {
                allParts.Add(part);
                RebuildIndex();
            }
        }

        /// <summary>
        /// Removes a part from the database (used in editor).
        /// </summary>
        public void RemovePart(BeyPart part)
        {
            if (allParts.Remove(part))
            {
                RebuildIndex();
            }
        }

        public int TotalPartCount => allParts.Count;
    }
}
