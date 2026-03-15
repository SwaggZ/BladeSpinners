using UnityEngine;
using BladeSpinners.Gameplay;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.World
{
    /// <summary>
    /// Collectible world pickup that grants a dropped BeyPart to the player's run inventory.
    /// Spawned when an enemy bursts and passes part-drop roll logic.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PartDropPickup : MonoBehaviour
    {
        [SerializeField] private BeyPart droppedPart;

        private bool collected;

        public BeyPart DroppedPart => droppedPart;

        public void Initialize(BeyPart part)
        {
            droppedPart = part;
            collected = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || droppedPart == null)
                return;

            PlayerManager player = other.GetComponentInParent<PlayerManager>();
            if (player == null)
                return;

            TryCollect(player);
        }

        public bool TryCollect(PlayerManager player)
        {
            if (collected || droppedPart == null || player == null)
                return false;

            collected = true;
            player.AddPartToInventory(droppedPart);
            Debug.Log($"[PartDrop] Collected {PartDisplayNameFormatter.ToShortDisplayName(droppedPart)} ({droppedPart.Rarity}).");
            Destroy(gameObject);
            return true;
        }
    }
}
