# Phase 1 Scene Setup Guide

> Date baseline: all existing entries in this file are tagged as updated on 21/3/2026.

This guide walks you through setting up a basic test scene with the player Bey using the Phase 1 systems.

## Step 1: Create the Scene and Basic Setup

1. Create a new scene called "TestArena"
2. Create a Plane for ground (scale it to 50x50 to give space)
   - Set Layer to "Ground"
   - Position at (0, 0, 0)
   - Keep default BoxCollider
3. Create a Cube for walls around the edges (optional, for testing boundaries)

## Step 2: Create the Ground Layer

1. Go to Layer menu → Add Layer "Ground"
2. Select the Plane and assign it to Ground layer
3. This layer will be used for ground detection raycasts

## Step 3: Create Physics Setup

1. Edit → Project Settings → Physics
2. Verify gravity is (0, -9.81, 0) - standard gravity
3. Check that Rigidbody default drag is reasonable (~0.1-0.3)

## Step 4: Create the Player Bey GameObject

1. Create an empty GameObject called "PlayerBey"
2. Add child Sphere (scale 0.3 x 0.3 x 0.3) - this is the visual representation
   - Material: Create a new material with bright color
   - This sphere will render as the Bey
3. Position at (0, 1, 0)

## Step 5: Add Required Components

Select PlayerBey and add these components in order:

### 5.1 Rigidbody
- Mass: 1
- Drag: 0.3
- Angular Drag: 0.5
- Gravity: Enabled
- Constraints: Freeze Rotation (all axes) - we handle rotation manually
- Collision Detection: Continuous (important for fast-moving objects)

### 5.2 Sphere Collider (for physics)
- Keep default settings
- Is Trigger: OFF
- This is for physics collisions

### 5.3 Sphere Collider (for Bey collision detection)
- Add another Sphere Collider for trigger events
- Is Trigger: ON
- This is for detecting Bey-to-Bey collisions with BeyCollisionDetector

### 5.4 BeyConfiguration
- Create new BeyConfiguration in code (handled by PlayerManager)
- This is a runtime class, not a component

### 5.5 Add Components (in this order)
```
- BeyMovementController
- BeyTiltController  
- BeyCollisionDetector
- PlayerInputHandler
- PlayerManager (main coordinator)
```

## Step 6: Assign Component References

Select PlayerManager and in the Inspector:
1. Assign the child Sphere to the Transform
2. Assign BeyConfiguration reference
3. Assign BeyMovementController reference
4. Assign BeyTiltController reference
5. Assign BeyCollisionDetector reference
6. Assign PlayerInputHandler reference
7. Create a Camera and assign ThirdPersonCameraController to it
8. Assign the Camera to PlayerManager

Note: Some of these can auto-detect via GetComponent, but it's safer to assign explicitly

## Step 7: Setup Input Manager (if not already done)

Go to Edit → Project Settings → Input Manager and verify these axes exist:
- "Horizontal" (A/D or Left/Right arrow)
- "Vertical" (W/S or Up/Down arrow)
- "Jump" (Space)
- Optional gamepad axes for camera control

## Step 8: Create a Test Part (ScriptableObject)

This is minimal just to test:

1. Right-click in Project → Create → BeyPart
2. Name it "TestBallTip"
3. In Inspector set:
   - Part Name: "Test Ball Tip"
   - Part Type: Tip
   - Occupies Slots: [Tip]
   - Rarity: Common
   - Tip Behavior: Ball
   - Behavior-Based Stamina Drain Modifier: 1.0
   - Uphill Resistance: 1.1
   - Save it

4. Create 4 more parts for other slots (Track, FusionWheel, EnergyRing, FaceBolt)
   - Use reasonable defaults for each

## Step 9: Create Temporary Code to Load Parts

Add to PlayerManager.Start() or Awake():

```csharp
// Temporary test code - replace with proper loading later
BeyPart testTip = Resources.Load<BeyPart>("Parts/TestBallTip");
BeyPart testTrack = Resources.Load<BeyPart>("Parts/TestTrack");
BeyPart testWheel = Resources.Load<BeyPart>("Parts/TestFusionWheel");
BeyPart testRing = Resources.Load<BeyPart>("Parts/TestEnergyRing");
BeyPart testFace = Resources.Load<BeyPart>("Parts/TestFaceBolt");

if (beyConfiguration != null)
{
    if (testTip != null) beyConfiguration.EquipPart(testTip);
    if (testTrack != null) beyConfiguration.EquipPart(testTrack);
    if (testWheel != null) beyConfiguration.EquipPart(testWheel);
    if (testRing != null) beyConfiguration.EquipPart(testRing);
    if (testFace != null) beyConfiguration.EquipPart(testFace);
}
```

## Step 10: Setup Camera

1. Create an empty GameObject as child of PlayerBey called "CameraRig"
2. Add a Main Camera as child of CameraRig
3. Assign CameraRig to have ThirdPersonCameraController script
4. In PlayerManager, set CameraRig reference

Or simpler: Just add ThirdPersonCameraController to the main Camera and set beyTransform to PlayerBey

## Step 11: Test!

1. Press Play
2. Use WASD to move the Bey
3. Shift to boost
4. Space to jump
5. C to brake
6. Mouse right-stick or gamepad right-stick to rotate camera
7. Watch spin decrease over time
8. Watch wobble when spin gets low

## Troubleshooting

### Bey doesn't move
- Check BeyMovementController is attached
- Check Rigidbody is not kinematic
- Check PlayerInputHandler is forwarding input

### Camera doesn't follow
- Verify ThirdPersonCameraController has beyTransform set
- Check Main Camera tag is set to "MainCamera"

### Spin doesn't decrease
- Verify BeyConfiguration.DrainSpin is being called
- Check that total drain rate isn't zero

### Collision doesn't work
- Verify BeyCollisionDetector has an OnTriggerEnter (needs trigger collider)
- Check Ground layer is set correctly

## Next Steps

Once this works:
1. Create additional test parts with different tips
2. Duplicate PlayerBey and test AI movement (Phase 3)
3. Add particle effects for visual feedback
4. Create simple UI to show spin and mana

---

**Go to**: [PHASE_1_SUMMARY.md](PHASE_1_SUMMARY.md) for architecture overview
