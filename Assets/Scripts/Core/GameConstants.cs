namespace BladeSpinners.Core
{
    /// <summary>
    /// Global game constants used throughout the project.
    /// </summary>
    public static class GameConstants
    {
        // Spin & Stamina
        public const float DEFAULT_STARTING_SPIN = 100f;
        public const float MIN_SPIN = 0f;
        public const float MAX_SPIN = 200f;
        public const float SPIN_WOBBLE_THRESHOLD = 0.25f; // Wobble when spin < 25% of max

        // Boost & Movement
        public const float BOOST_FORCE_MULTIPLIER = 1.5f;
        public const float BOOST_STAMINA_DRAIN_MULTIPLIER = 3f;
        public const float BASE_FORWARD_FORCE = 78f;
        public const float BASE_TURN_SPEED = 180f; // degrees per second
        public const float JUMP_FORCE = 5f;

        // Collision & Physics
        public const float COLLISION_SPIN_EXCHANGE_BASE = 10f;
        public const float COLLISION_KNOCKBACK_BASE = 15f; // Base impulse strength on hit
        public const float WEIGHT_KNOCKBACK_MULTIPLIER = 0.5f;
        public const float UPHILL_CHECK_DISTANCE = 0.5f;

        // Stamina Drain Rates (base values before modifications)
        public const float BASE_MASS_DRAIN_RATE = 0.5f; // per second, modified by weight
        public const float BASE_BEHAVIOR_DRAIN_RATE = 0.3f; // per second, modified by tip type

        // Mana System
        public const float DEFAULT_MANA_POOL = 100f;
        public const float MIN_MANA = 0f;
        public const float MAX_MANA = 300f;

        // Pickup Values
        public const float PICKUP_SPIN_SMALL = 15f;
        public const float PICKUP_SPIN_MEDIUM = 30f;
        public const float PICKUP_SPIN_LARGE = 60f;
        public const float PICKUP_STAMINA_DRAIN_REDUCTION = 0.5f; // 50% drain reduction
        public const float PICKUP_STAMINA_DURATION = 10f; // seconds

        // Procedural Generation - Depth Scaling
        public const int MIN_DUNGEON_MAPS = 8;
        public const int MAX_DUNGEON_MAPS = 20;
        public const int BOSS_MAP_DEPTH = 5; // Boss appears at depth N
        
        // Stat Ranges (scaled by depth and rarity)
        public const float MIN_TRACK_HEIGHT = 0.025f;
        public const float MAX_TRACK_HEIGHT = 0.05f;
        public const float MIN_WEIGHT = 10f;
        public const float MAX_WEIGHT = 50f;
        public const float MIN_MANA_POOL = 50f;
        public const float MAX_MANA_POOL = 300f;
        public const float MIN_MANA_REGEN = 5f;
        public const float MAX_MANA_REGEN = 50f;

        // Passive Effect Cooldowns
        public const float IMPACT_SHIELD_COOLDOWN = 5f; // seconds
        public const float MOMENTUM_HARVEST_SPEED_THRESHOLD = 20f; // minimum speed to gain stacks
        public const float MOMENTUM_HARVEST_CHECK_INTERVAL = 1f; // check every second

        // Pickup Spawning
        public const int COMBAT_ROOM_PICKUPS = 3;
        public const int LOOT_ROOM_PICKUPS = 8;
        public const int BOSS_ROOM_PICKUPS = 1; // Guaranteed large spin pickup
        public const float PICKUP_SPAWN_HEIGHT = 1f;

        // Enemy AI
        public const float ENEMY_INTERCEPT_PREDICTION_TIME = 1f;
        public const float ENEMY_COLLISION_DISTANCE_THRESHOLD = 5f;
        public const float ENEMY_REPOSITION_DISTANCE = 8f;
        public const int ENEMY_MAX_PER_COMBAT_ROOM = 5;

        // Tilt & Visual Feedback
        public const float MAX_TILT_ANGLE = 60f;
        public const float WOBBLE_ANIMATION_SPEED = 10f;

        // Ability Base Costs (adjusted per concrete ability)
        public const float ABILITY_COST_HIGH = 80f;
        public const float ABILITY_COST_MODERATE = 50f;
        public const float ABILITY_COST_LOW = 25f;

        // Save System
        public const string PERSISTENT_SAVE_KEY = "BladeSpinners_PersistentSave";
        public const string RUN_SAVE_KEY = "BladeSpinners_RunSave";

        // UI
        public const float HUD_UPDATE_FREQUENCY = 0.1f; // Update every 0.1 seconds
        public const int INVENTORY_ITEMS_PER_SLOT = 20; // Max parts displayed per slot type

        // Arena Generation
        public const float ARENA_MIN_RADIUS = 20f;
        public const float ARENA_MAX_RADIUS = 45f;
        public const float ARENA_MIN_DEPTH = 1f;    // bowl concavity depth
        public const float ARENA_MAX_DEPTH = 2f;
        public const float ARENA_RIM_HEIGHT = 10f;     // wall segments around the bowl lip
        public const float ARENA_RIM_THICKNESS = 0.6f;
        public const float ARENA_FLOOR_FLAT_RATIO = 0.35f; // fraction of radius that is flat floor
        public const int   ARENA_RING_SEGMENTS = 64;       // mesh resolution
        public const int   ARENA_RADIAL_SEGMENTS = 24;
        public const int   ARENA_MAX_INNER_WALLS = 10;
        public const int   ARENA_MAX_PLATFORMS = 10;
        public const float ARENA_PLATFORM_MAX_HEIGHT = 0.6f;
        public const float ARENA_INNER_WALL_MAX_HEIGHT = 1.8f;
        public const int   ARENA_MAX_PICKUPS = 10;
        public const int   ARENA_MIN_RIM_WALLS = 1;
        public const int   ARENA_MAX_RIM_WALLS = 9;
        public const float ARENA_RIM_WALL_ARC_FRACTION = 0.35f; // each wall covers this fraction of its slot
    }
}
