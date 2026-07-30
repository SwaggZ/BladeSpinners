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
            if (Gamepad.current != null)
            {
                ReadGamepadInput();
            }
            else
            {
                ReadKeyboardInput();
            }

            if (debugInput && (currentForwardInput != 0 || currentSteeringInput != 0))
                Debug.Log($"[PlayerInput] INPUT READ - Forward: {currentForwardInput:F2}, Steering: {currentSteeringInput:F2}");
        }

        private void ReadGamepadInput()
        {
            var gamepad = Gamepad.current;
            if (gamepad == null) return;

            currentForwardInput = gamepad.leftStick.ReadValue().y;
            currentSteeringInput = gamepad.leftStick.ReadValue().x;

            if (gamepad.rightTrigger.isPressed)
            {
                beyMovementController.StartBoost();
                isBootsActive = true;
            }
            else
            {
                beyMovementController.StopBoost();
                isBootsActive = false;
            }

            if (gamepad.leftTrigger.isPressed)
                beyMovementController.ApplyBrake();

            if (gamepad.aButton.wasPressedThisFrame)
                beyMovementController.Jump();

            if (gamepad.yButton.wasPressedThisFrame)
                TryActivateAbility();
        }

        private void ReadKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float horizontalInput = 0;
            float verticalInput = 0;

            if (keyboard.aKey.isPressed) horizontalInput = -1;
            if (keyboard.dKey.isPressed) horizontalInput = 1;
            if (keyboard.wKey.isPressed) verticalInput = 1;
            if (keyboard.sKey.isPressed) verticalInput = -1;

            currentForwardInput = verticalInput;
            currentSteeringInput = horizontalInput;

            if (keyboard.leftShiftKey.isPressed)
            {
                beyMovementController.StartBoost();
                isBootsActive = true;
            }
            else
            {
                beyMovementController.StopBoost();
                isBootsActive = false;
            }

            if (keyboard.cKey.wasPressedThisFrame)
                beyMovementController.ApplyBrake();

            if (keyboard.spaceKey.wasPressedThisFrame)
                beyMovementController.Jump();

            if (keyboard.eKey.wasPressedThisFrame)
                TryActivateAbility();
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
