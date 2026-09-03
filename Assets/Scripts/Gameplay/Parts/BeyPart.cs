using UnityEngine;
using System.Collections.Generic;
using BladeSpinners.Core;
using BladeSpinners.Abilities;

namespace BladeSpinners.Gameplay.Parts
{
    /// <summary>
    /// Base ScriptableObject for all Beyblade parts. Each part owns specific stats
    /// based on its slot type and contributes to the overall BeyConfiguration.
    /// Supports hybrid parts that occupy multiple slots via occupiesSlots list.
    /// </summary>
    public class BeyPart : ScriptableObject
    {
        [SerializeField]
        protected string partName = "New Part";

        [SerializeField]
        protected PartType partType = PartType.Tip;

        /// <summary>
        /// For hybrid parts, list all slots this part occupies.
        /// Most parts occupy only their own slot; Final Drive would list [Tip, Track].
        /// </summary>
        [SerializeField]
        protected List<PartType> occupiesSlots = new List<PartType>();

        [SerializeField]
        protected RarityTier rarity = RarityTier.Common;

        [SerializeField]
        protected List<PartTag> tags = new List<PartTag>();

        /// <summary>
        /// Unique identifier for this part (used in saves).
        /// </summary>
        [SerializeField]
        protected string partID = "";

        /// <summary>
        /// Description shown in UI.
        /// </summary>
        [TextArea(2, 4)]
        [SerializeField]
        protected string description = "";

        /// <summary>
        /// Icon displayed in inventory and menus.
        /// </summary>
        [SerializeField]
        protected Sprite icon;

        // ===== PROCEDURAL MODEL PARAMETERS =====

        /// <summary>
        /// Seed for procedural mesh generation. Different seeds produce
        /// different-looking models even for the same part type.
        /// </summary>
        [Header("Procedural Model")]
        [SerializeField]
        protected int meshSeed = 0;

        /// <summary>
        /// Primary color of this part's mesh.
        /// </summary>
        [SerializeField]
        protected Color primaryColor = new Color(0.7f, 0.7f, 0.7f);

        /// <summary>
        /// Secondary/accent color (used for highlights/details).
        /// </summary>
        [SerializeField]
        protected Color secondaryColor = Color.white;

        // ===== STAT OWNERSHIP BY SLOT TYPE =====

        // Tip Stats (movement behavior, drift, uphill resistance, tilt, orbit style, slope multiplier, behavior-based stamina drain)
        [SerializeField]
        protected Core.TipBehaviorType tipBehavior = Core.TipBehaviorType.Ball;

        /// <summary>
        /// Stamina drain multiplier specific to this tip's movement behavior.
        /// Rubber/Flat tips drain faster (higher values), Sharp/Spike drain slower (lower values).
        /// </summary>
        [SerializeField]
        [Range(0.5f, 2.5f)]
        protected float behaviorBasedStaminaDrainModifier = 1f;

        /// <summary>
        /// Multiplier for uphill movement difficulty. Lower = easier uphill.
        /// </summary>
        [SerializeField]
        [Range(0.3f, 2f)]
        protected float uphillResistanceMultiplier = 1f;

        /// <summary>
        /// Multiplier for slope sliding behavior. 
        /// </summary>
        [SerializeField]
        [Range(0.5f, 2f)]
        protected float slopeMultiplier = 1f;

        /// <summary>
        /// Spin threshold at which this tip switches to its alternative behavior.
        /// -1 means no threshold behavior.
        /// </summary>
        [SerializeField]
        protected float spinThreshold = -1f;

        /// <summary>
        /// Alternative tip behavior when spin drops below spinThreshold.
        /// </summary>
        [SerializeField]
        protected Core.TipBehaviorType altTipBehavior = Core.TipBehaviorType.Ball;

        // Track Stats (height, jump arc modifier)
        /// <summary>
        /// Height of the Bey. Affects hitbox elevation and visibility over obstacles.
        /// </summary>
        [SerializeField]
        protected float trackHeight = 1f;

        /// <summary>
        /// Modifier applied to jump arc calculations.
        /// </summary>
        [SerializeField]
        [Range(0.5f, 1.5f)]
        protected float jumpArcModifier = 1f;

        // Fusion Wheel Stats (weight, mass-based stamina drain)
        /// <summary>
        /// Weight value. Directly affects knockback given/received and collision spin exchange.
        /// </summary>
        [SerializeField]
        protected float weight = 25f;

        /// <summary>
        /// Stamina drain rate per second based on mass.
        /// Heavier wheels drain spin faster over time.
        /// </summary>
        [SerializeField]
        [Range(0.1f, 2f)]
        protected float massBasedStaminaDrainRate = 0.5f;

        // Energy Ring Stats (mana pool, mana regen)
        /// <summary>
        /// Maximum mana pool size for ability usage.
        /// </summary>
        [SerializeField]
        protected float manaPoolSize = 100f;

        /// <summary>
        /// Mana regeneration per second.
        /// </summary>
        [SerializeField]
        protected float manaRegenRate = 20f;

        /// <summary>
        /// Optional authored passive override. When empty, the runtime resolver assigns
        /// a stable passive from this Energy Ring's ID.
        /// </summary>
        [SerializeField]
        protected BeyPassive equippedPassive;

        // Face Bolt Stats (ability reference)
        /// <summary>
        /// Reference to the ability this Face Bolt provides.
        /// </summary>
        [SerializeField]
        protected Abilities.BeyAbility equippedAbility;

        /// <summary>
        /// Optional Face Bolt emblem image (used for UI and ability hologram visuals).
        /// </summary>
        [SerializeField]
        protected Sprite faceBoltEmblem;

        public string PartName => partName;
        public PartType PartType => partType;
        public List<PartType> OccupiesSlots
        {
            get
            {
                if (occupiesSlots != null && occupiesSlots.Count > 0)
                    return occupiesSlots;
                return new List<PartType> { partType };
            }
        }
        public RarityTier Rarity => rarity;
        public List<PartTag> Tags => tags;
        public string PartID => partID;
        public string Description => description;
        public Sprite Icon => icon;

        // Procedural Model
        public int MeshSeed => meshSeed;
        public Color PrimaryColor => primaryColor;
        public Color SecondaryColor => secondaryColor;

        // Tip
        public Core.TipBehaviorType TipBehavior => tipBehavior;
        public float BehaviorBasedStaminaDrainModifier => behaviorBasedStaminaDrainModifier;
        public float UphillResistanceMultiplier => uphillResistanceMultiplier;
        public float SlopeMultiplier => slopeMultiplier;
        public float SpinThreshold => spinThreshold;
        public Core.TipBehaviorType AltTipBehavior => altTipBehavior;

        // Track
        public float TrackHeight => trackHeight;
        public float JumpArcModifier => jumpArcModifier;

        // Fusion Wheel
        public float Weight => weight;
        public float MassBasedStaminaDrainRate => massBasedStaminaDrainRate;

        // Energy Ring
        public float ManaPoolSize => manaPoolSize;
        public float ManaRegenRate => manaRegenRate;
        public BeyPassive EquippedPassive => equippedPassive;

        // Face Bolt
        public Abilities.BeyAbility EquippedAbility => equippedAbility;
        public Sprite FaceBoltEmblem => faceBoltEmblem;

        /// <summary>
        /// Returns true if this part occupies multiple slots (hybrid).
        /// </summary>
        public bool IsHybrid => occupiesSlots.Count > 1;

        /// <summary>
        /// Validates that the part data is consistent with its type.
        /// Returns true if valid, outputs error message if not.
        /// </summary>
        public virtual bool Validate(out string errorMessage)
        {
            errorMessage = "";

            if (occupiesSlots == null || occupiesSlots.Count == 0)
            {
                occupiesSlots = new List<PartType> { partType };
            }

            if (!occupiesSlots.Contains(partType))
            {
                errorMessage = $"Part type {partType} not in occupiesSlots list";
                return false;
            }

            if (string.IsNullOrEmpty(partID))
            {
                errorMessage = "Part ID is empty";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when this part is created procedurally. Override in subclasses for custom logic.
        /// </summary>
        public virtual void OnProceduralGeneration(int dungeonDepth, Core.EnemyArchetype archetype)
        {
            // Base implementation does nothing. Subclasses override as needed.
        }
    }
}
