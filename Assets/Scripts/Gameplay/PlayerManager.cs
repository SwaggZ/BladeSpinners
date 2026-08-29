using UnityEngine;
using BladeSpinners.Gameplay.Parts;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Combat;
using BladeSpinners.Gameplay;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Manages the complete player Bey setup and coordinates all its systems.
    /// Handles initialization and provides access to all player components.
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField]
        private BeyConfiguration beyConfiguration;

        [SerializeField]
        private BeyMovementController movementController;

        [SerializeField]
        private BeyTiltController tiltController;

        [SerializeField]
        private BeyCollisionDetector collisionDetector;

        [SerializeField]
        private PlayerInputHandler inputHandler;

        [SerializeField]
        private Camera mainCamera;

        [SerializeField]
        private ThirdPersonCameraController cameraController;

        [SerializeField]
        private PartDatabase partDatabase;

        [SerializeField]
        private BeyStatRingsUI statRingsUI;

        private PartInventory runInventory;

        private void Awake()
        {
            // Initialize inventory for this run
            runInventory = new PartInventory();

            // Wire up all components
            WireComponents();

            // Spawn player Bey
            InitializePlayerBey();
        }

        private void WireComponents()
        {
            // Ensure all components are assigned
            // BeyConfiguration is not a MonoBehaviour, so create it if needed
            if (beyConfiguration == null)
                beyConfiguration = new BeyConfiguration();

            if (movementController == null)
                movementController = GetComponent<BeyMovementController>();

            if (tiltController == null)
                tiltController = GetComponent<BeyTiltController>();

            if (collisionDetector == null)
                collisionDetector = GetComponent<BeyCollisionDetector>();

            // Wire references via reflection (since they're serialized private)
            if (tiltController != null)
            {
                var movementField = typeof(BeyTiltController).GetField("movementController", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                movementField?.SetValue(tiltController, movementController);
                
                var configField = typeof(BeyTiltController).GetField("beyConfiguration", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configField?.SetValue(tiltController, beyConfiguration);
            }

            if (collisionDetector != null)
            {
                var configField = typeof(BeyCollisionDetector).GetField("beyConfiguration", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configField?.SetValue(collisionDetector, beyConfiguration);
                
                var movementField = typeof(BeyCollisionDetector).GetField("movementController", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                movementField?.SetValue(collisionDetector, movementController);
            }

            // Assign references
            if (movementController != null)
            {
                // Wire BeyConfiguration to movement controller via reflection
                var configField = typeof(BeyMovementController).GetField("beyConfiguration", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configField?.SetValue(movementController, beyConfiguration);
            }

            if (inputHandler == null)
                inputHandler = GetComponent<PlayerInputHandler>();

            if (inputHandler != null)
            {
                var movementField = typeof(PlayerInputHandler).GetField("beyMovementController", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                movementField?.SetValue(inputHandler, movementController);
                
                var configField = typeof(PlayerInputHandler).GetField("beyConfiguration", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configField?.SetValue(inputHandler, beyConfiguration);
            }

            if (cameraController != null)
            {
                cameraController.SetBeyTransform(transform);
            }

            if (statRingsUI == null)
                statRingsUI = GetComponent<BeyStatRingsUI>();

            if (statRingsUI == null)
                statRingsUI = gameObject.AddComponent<BeyStatRingsUI>();

            statRingsUI.Initialize(beyConfiguration, movementController, transform);

            if (beyConfiguration != null)
            {
                beyConfiguration.OwnerTransform = transform;
            }

            Effects.BeyGroundTrailEffect trailEffect = GetComponent<Effects.BeyGroundTrailEffect>();
            if (trailEffect == null)
                trailEffect = gameObject.AddComponent<Effects.BeyGroundTrailEffect>();
            trailEffect.Initialize(beyConfiguration);
        }

        /// <summary>
        /// Re-initializes stat rings against the CURRENT beyConfiguration and
        /// movementController. Must be called by RuntimeRunBuilder after all
        /// reflection field-sets are complete, because Awake() fires synchronously
        /// during AddComponent and captures a stale empty BeyConfiguration.
        /// </summary>
        public void RewireStatRings()
        {
            if (beyConfiguration != null)
            {
                beyConfiguration.OwnerTransform = transform;
            }
            if (statRingsUI == null) return;
            statRingsUI.Initialize(beyConfiguration, movementController, transform);
        }

            private void InitializePlayerBey()
        {
            if (beyConfiguration == null)
                return;

            // Parts are now owned by BeyAssembler (drag parts into its inspector slots).
            // BeyAssembler.SetConfiguration() pushes them into BeyConfiguration automatically.
            BeyAssembler assembler = GetComponent<BeyAssembler>();
            if (assembler != null)
                assembler.SetConfiguration(beyConfiguration);

            Debug.Log("✅ Player Bey initialized — parts come from BeyAssembler slots.");
        }

        /// <summary>
        /// Equips a part to the Bey during a run.
        /// </summary>
        public void EquipPart(BeyPart part)
        {
            if (beyConfiguration == null)
                return;

            beyConfiguration.EquipPart(part);
        }

        /// <summary>
        /// Unequips a part from a specific slot.
        /// </summary>
        public void UnequipPart(Core.PartType slotType)
        {
            if (beyConfiguration == null)
                return;

            beyConfiguration.UnequipPart(slotType);
        }

        /// <summary>
        /// Adds a collected part to the run inventory.
        /// </summary>
        public void AddPartToInventory(BeyPart part)
        {
            if (part == null) return;

            // Prevent duplicates — same ScriptableObject reference already in run inventory
            if (runInventory.Contains(part))
            {
                Debug.Log($"[PartDrop] Skipped duplicate: {part.PartName} already in run inventory.");
                return;
            }

            bool added = runInventory.AddPart(part);
            if (!added)
                Debug.LogWarning($"Could not add part {part.PartName} to inventory - slot full");
        }

        /// <summary>
        /// Gets the current run inventory.
        /// </summary>
        public PartInventory GetRunInventory()
        {
            return runInventory;
        }

        // Getters for all components
        public BeyConfiguration BeyConfiguration => beyConfiguration;
        public BeyMovementController MovementController => movementController;
        public BeyTiltController TiltController => tiltController;
        public BeyCollisionDetector CollisionDetector => collisionDetector;
        public PlayerInputHandler InputHandler => inputHandler;
        public ThirdPersonCameraController CameraController => cameraController;
        public PartDatabase PartDatabase => partDatabase;
        public BeyStatRingsUI StatRingsUI => statRingsUI;
    }
}
