using UnityEngine;
using UnityEngine.InputSystem;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;

namespace BladeSpinners.Gameplay
{
    /// <summary>
    /// Handles player input and translates it to player Bey commands.
    /// Supports both keyboard and gamepad input using the new Input System.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField]
        private BeyMovementController beyMovementController;

        [SerializeField]
        private BeyConfiguration beyConfiguration;

        [SerializeField]
        private BeyTiltController beyTiltController;

        [SerializeField]
        private bool debugInput = false;

        private float currentForwardInput = 0f;
        private float currentSteeringInput = 0f;
        private bool isBootsActive = false;

        private void Update()
        {
            if (beyMovementController == null || beyConfiguration == null)
            {
                if (debugInput)
                    Debug.LogError("[PlayerInput] Missing components!");
                return;
            }

            ReadInput();
            ApplyMovement();
        }

        private void ReadInput()
        {
            float fwd = 0f;
            float str = 0f;
            bool boost = false;
            bool brake = false;
            bool jump = false;
            bool ability = false;

            // 1. Keyboard & Mouse Input
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) fwd += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) fwd -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) str -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) str += 1f;

                if (keyboard.leftShiftKey.isPressed) boost = true;
                if (keyboard.cKey.isPressed) brake = true;
                if (keyboard.spaceKey.wasPressedThisFrame) jump = true;
                if (keyboard.eKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame) ability = true;
            }
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed) boost = true;
            }

            // 2. Gamepad Input (Xbox & PlayStation / Generic)
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                Vector2 dpad = gamepad.dpad.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    fwd += stick.y;
                    str += stick.x;
                }
                else if (dpad.sqrMagnitude > 0.04f)
                {
                    fwd += dpad.y;
                    str += dpad.x;
                }

                if (gamepad.rightTrigger.isPressed || gamepad.rightShoulder.isPressed) boost = true;
                if (gamepad.leftTrigger.isPressed || gamepad.leftShoulder.isPressed) brake = true;
                if (gamepad.aButton.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame) jump = true;
                if (gamepad.yButton.wasPressedThisFrame || gamepad.xButton.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame || gamepad.buttonNorth.wasPressedThisFrame) ability = true;
            }

            currentForwardInput = Mathf.Clamp(fwd, -1f, 1f);
            currentSteeringInput = Mathf.Clamp(str, -1f, 1f);

            if (boost)
            {
                beyMovementController.StartBoost();
                isBootsActive = true;
            }
            else
            {
                beyMovementController.StopBoost();
                isBootsActive = false;
            }

            if (brake)
                beyMovementController.ApplyBrake();

            if (jump)
                beyMovementController.Jump();

            if (ability)
                TryActivateAbility();

            if (debugInput && (currentForwardInput != 0 || currentSteeringInput != 0))
                Debug.Log($"[PlayerInput] INPUT READ - Forward: {currentForwardInput:F2}, Steering: {currentSteeringInput:F2}");
        }

        private void ApplyMovement()
        {
            if (beyMovementController == null)
            {
                if (debugInput)
                    Debug.LogError("[PlayerInput] BeyMovementController is null!");
                return;
            }

            beyMovementController.CacheInput(currentForwardInput, currentSteeringInput);

            if (debugInput && (currentForwardInput != 0 || currentSteeringInput != 0))
                Debug.Log($"[PlayerInput] INPUT CACHED - Forward: {currentForwardInput:F2}, Steering: {currentSteeringInput:F2}");
        }

        private void TryActivateAbility()
        {
            AbilityActivationService.TryActivateEquipped(
                beyConfiguration, beyMovementController);
        }

        public float CurrentForwardInput => currentForwardInput;
        public float CurrentSteeringInput => currentSteeringInput;
        public bool IsBoostActive => isBootsActive;
    }
}
