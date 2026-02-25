using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Tracks parts collected during the current run (temporary parts only).
    /// Permanently unlocked parts are stored in PartDatabase instead.
    /// </summary>
    public class PartInventory
    {
        private Dictionary<PartType, List<BeyPart>> partsBySlot = new Dictionary<PartType, List<BeyPart>>();

        /// <summary>
        /// Event fired when a part is added to inventory.
        /// </summary>
        public event System.Action<BeyPart> OnPartAdded;

        /// <summary>
        /// Event fired when a part is removed from inventory.
        /// </summary>
        public event System.Action<BeyPart> OnPartRemoved;

        public PartInventory()
        {
            // Initialize empty lists for each slot type
            foreach (PartType slotType in System.Enum.GetValues(typeof(PartType)))
            {
                partsBySlot[slotType] = new List<BeyPart>();
            }
        }

        /// <summary>
        /// Adds a part to the inventory.
        /// If inventory is full for this slot, returns false and doesn't add.
        /// </summary>
        public bool AddPart(BeyPart part)
        {
            if (part == null)
                return false;

            // Check if inventory is full for this slot type
            if (partsBySlot[part.PartType].Count >= GameConstants.INVENTORY_ITEMS_PER_SLOT)
                return false;

            partsBySlot[part.PartType].Add(part);
            OnPartAdded?.Invoke(part);
            return true;
        }

        /// <summary>
        /// Removes a specific part from the inventory.
        /// </summary>
        public bool RemovePart(BeyPart part)
        {
            if (part == null)
                return false;

            bool removed = partsBySlot[part.PartType].Remove(part);
            if (removed)
            {
                OnPartRemoved?.Invoke(part);
            }
            return removed;
        }

        /// <summary>
        /// Gets all parts of a specific slot type.
        /// </summary>
        public List<BeyPart> GetPartsBySlotType(PartType slotType)
        {
            return new List<BeyPart>(partsBySlot[slotType]);
        }

        /// <summary>
        /// Gets all parts currently in inventory.
        /// </summary>
        public List<BeyPart> GetAllParts()
        {
            List<BeyPart> allParts = new List<BeyPart>();
            foreach (var partList in partsBySlot.Values)
            {
                allParts.AddRange(partList);
            }
            return allParts;
        }

        /// <summary>
        /// Checks if a specific part is in inventory.
        /// </summary>
        public bool Contains(BeyPart part)
        {
            return part != null && partsBySlot[part.PartType].Contains(part);
        }

        /// <summary>
        /// Gets the count of parts in a specific slot.
        /// </summary>
        public int GetPartCount(PartType slotType)
        {
            return partsBySlot[slotType].Count;
        }

        /// <summary>
        /// Clears all parts from inventory (used when a run ends).
        /// </summary>
        public void Clear()
        {
            foreach (var partList in partsBySlot.Values)
            {
                partList.Clear();
            }
        }
    }
}
