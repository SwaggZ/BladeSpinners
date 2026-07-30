using System;
using System.Reflection;
using BladeSpinners.Audio;
using BladeSpinners.Gameplay.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Play Mode probe for the any-button Start Screen path. SessionState lets
    /// the validator survive the domain reload caused by entering Play Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class StartScreenPlayModeValidator
    {
        private const string RunningKey =
            "BladeSpinners.StartScreenValidation.Running";
        private const double TimeoutSeconds = 25d;
        private static readonly BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static double startedAt;
        private static bool inputQueued;
        private static int validationStage;
        private static double stageStartedAt;
        private static uint musicStartBeforeNavigation;
        private static Keyboard validationKeyboard;

        static StartScreenPlayModeValidator()
        {
            if (SessionState.GetBool(
                    RunningKey,
                    false))
            {
                Subscribe();
            }
        }

        [MenuItem(
            "Blade Spinners/Validation/Start Screen Play Mode")]
        public static void ValidateFromMenu()
        {
            Begin(false);
        }

        public static void ValidateFromCommandLine()
        {
            Begin(true);
        }

        private static void Begin(bool commandLine)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Start Screen validation requires Edit Mode.");

            SessionState.SetBool(
                RunningKey,
                true);
            SessionState.SetBool(
                RunningKey + ".CommandLine",
                commandLine);
            startedAt =
                EditorApplication.timeSinceStartup;
            inputQueued = false;
            validationStage = 0;
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        private static void Subscribe()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            if (startedAt <= 0d)
            {
                startedAt =
                    EditorApplication.timeSinceStartup;
            }
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                startedAt =
                    EditorApplication.timeSinceStartup;
                inputQueued = false;
                validationStage = 0;
            }
            else if (state
                     == PlayModeStateChange.EnteredEditMode
                     && SessionState.GetBool(
                         RunningKey,
                         false))
            {
                Complete(0);
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(
                    RunningKey,
                    false))
            {
                Unsubscribe();
                return;
            }

            if (EditorApplication.timeSinceStartup
                    - startedAt
                > TimeoutSeconds)
            {
                Fail(
                    new TimeoutException(
                        "Start Screen Play Mode validation timed out."));
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            try
            {
                RuntimeGameUiController ui =
                    UnityEngine.Object
                        .FindFirstObjectByType<
                            RuntimeGameUiController>();
                if (ui == null)
                    return;

                string state =
                    ReadRootState(ui);
                if (!inputQueued)
                {
                    if (!string.Equals(
                            state,
                            "StartScreen",
                            StringComparison.Ordinal)
                        || SoundManager.CurrentMusicSituation
                            != MusicSituation.StartScreen
                        || SoundManager.CurrentMusicStartId
                            == 0u
                        || Time.unscaledTime < 0.75f)
                    {
                        return;
                    }

                    validationKeyboard =
                        Keyboard.current
                        ?? InputSystem.AddDevice<
                            Keyboard>(
                            "StartScreenValidationKeyboard");
                    InputSystem.QueueStateEvent(
                        validationKeyboard,
                        new KeyboardState(
                            Key.Space));
                    inputQueued = true;
                    return;
                }

                if (validationStage == 0)
                {
                    if (!string.Equals(
                            state,
                            "MainMenu",
                            StringComparison.Ordinal))
                    {
                        return;
                    }
                    if (SoundManager.CurrentMusicSituation
                            != MusicSituation.MainMenu
                        || SoundManager.CurrentMusicStartId
                            < 2u)
                    {
                        throw new InvalidOperationException(
                            "Start Screen reached Main Menu without changing " +
                            "from the title music situation.");
                    }

                    musicStartBeforeNavigation =
                        SoundManager.CurrentMusicStartId;
                    SetMainMenuPanel(
                        ui,
                        "Inventory");
                    validationStage = 1;
                    stageStartedAt =
                        EditorApplication.timeSinceStartup;
                    return;
                }

                if (validationStage == 1)
                {
                    if (EditorApplication.timeSinceStartup
                            - stageStartedAt
                        < 0.20d)
                    {
                        return;
                    }
                    if (SoundManager.CurrentMusicStartId
                            != musicStartBeforeNavigation
                        || SoundManager.CurrentMusicSituation
                            != MusicSituation.MainMenu
                        || !SoundManager.IsMusicSituationQueued(
                            MusicSituation.Inventory))
                    {
                        throw new InvalidOperationException(
                            "Opening Inventory interrupted the Main Menu song " +
                            "instead of queuing Inventory.");
                    }

                    SoundManager.SkipToNextMusic();
                    validationStage = 2;
                    return;
                }

                if (validationStage == 2)
                {
                    if (SoundManager.CurrentMusicSituation
                            != MusicSituation.Inventory
                        || SoundManager.CurrentMusicStartId
                            <= musicStartBeforeNavigation)
                    {
                        return;
                    }

                    musicStartBeforeNavigation =
                        SoundManager.CurrentMusicStartId;
                    SetMainMenuPanel(
                        ui,
                        "Home");
                    validationStage = 3;
                    stageStartedAt =
                        EditorApplication.timeSinceStartup;
                    return;
                }

                if (EditorApplication.timeSinceStartup
                        - stageStartedAt
                    < 0.20d)
                {
                    return;
                }
                if (SoundManager.CurrentMusicStartId
                        != musicStartBeforeNavigation
                    || SoundManager.CurrentMusicSituation
                        != MusicSituation.Inventory
                    || !SoundManager.IsMusicSituationQueued(
                        MusicSituation.MainMenu))
                {
                    throw new InvalidOperationException(
                        "Returning to Main Menu interrupted the Inventory song " +
                        "instead of queuing Main Menu.");
                }

                Debug.Log(
                    "[StartScreenPlayMode] Passed: launch rendered in " +
                    "StartScreen, an Input System button began the transition, " +
                    "MainMenu started its own softly transitioned music, menu " +
                    "navigation queued without interruption, and Next Song " +
                    "consumed the queued category.");
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static string ReadRootState(
            RuntimeGameUiController ui)
        {
            FieldInfo rootState =
                typeof(RuntimeGameUiController)
                    .GetField(
                        "rootState",
                        PrivateInstance);
            object value =
                rootState?.GetValue(ui);
            return value?.ToString()
                ?? string.Empty;
        }

        private static void SetMainMenuPanel(
            RuntimeGameUiController ui,
            string panelName)
        {
            MethodInfo setter =
                typeof(RuntimeGameUiController)
                    .GetMethod(
                        "SetMainMenuPanel",
                        PrivateInstance);
            ParameterInfo[] parameters =
                setter?.GetParameters();
            if (setter == null
                || parameters == null
                || parameters.Length != 1
                || !parameters[0]
                    .ParameterType.IsEnum)
            {
                throw new MissingMethodException(
                    "RuntimeGameUiController.SetMainMenuPanel is unavailable.");
            }

            object panel = Enum.Parse(
                parameters[0].ParameterType,
                panelName);
            setter.Invoke(
                ui,
                new[] { panel });
        }

        private static void Fail(Exception exception)
        {
            Debug.LogException(exception);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            Complete(1);
        }

        private static void Complete(int exitCode)
        {
            bool commandLine =
                SessionState.GetBool(
                    RunningKey + ".CommandLine",
                    false);
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(
                RunningKey + ".CommandLine");
            if (validationKeyboard != null
                && validationKeyboard.added
                && validationKeyboard
                    .name
                    == "StartScreenValidationKeyboard")
            {
                InputSystem.RemoveDevice(
                    validationKeyboard);
            }
            validationKeyboard = null;
            Unsubscribe();

            if (commandLine)
                EditorApplication.Exit(exitCode);
        }

        private static void Unsubscribe()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.update -= Tick;
        }
    }
}
