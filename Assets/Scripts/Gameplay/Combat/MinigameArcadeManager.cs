using System;
using UnityEngine;
using BladeSpinners.Audio;

namespace BladeSpinners.Gameplay.Combat
{
    public enum ArcadeState
    {
        Browser,
        Playing,
        RoundOver
    }

    public static class MinigameArcadeManager
    {
        public static ArcadeState State { get; private set; } = ArcadeState.Browser;
        public static ClashMinigameType SelectedMinigame { get; private set; } = ClashMinigameType.RapidMash;

        public static int CurrentScore { get; private set; }
        public static int BestScore { get; private set; }
        public static int Streak { get; private set; }
        public static float TimeRemaining { get; private set; }
        public static float TotalDuration { get; private set; } = 15f;
        public static bool IsNewHighScore { get; private set; }

        // Feedback popup
        public static string FeedbackText { get; private set; } = string.Empty;
        public static Color FeedbackColor { get; private set; } = Color.white;
        public static float FeedbackTimer { get; private set; } = 0f;

        // Minigame Variables
        public static float ClashMeter { get; private set; } = 0.5f;

        // 1. Rapid Mash
        public static int MashCount { get; private set; }

        // 2. Precision Timing
        public static float NeedlePos { get; private set; } = 0f;
        public static float SweetSpotMin { get; private set; } = 0.35f;
        public static float SweetSpotMax { get; private set; } = 0.65f;
        private static float needleDir = 1f;
        private static float needleSpeed = 1.5f;

        // 3. Rhythm Beat (Taiko-no-Tatsujin Conveyor)
        public const float RhythmTargetX = 0.20f;
        public static readonly System.Collections.Generic.List<float> ActiveNotes = new System.Collections.Generic.List<float>();
        private static float noteSpawnTimer = 0f;
        private static float noteSpeed = 0.52f; // Normalized units per second

        // 4. Tension Balance
        public static float BalanceBobberPos { get; private set; } = 0.5f;
        public static float BalanceTargetPos { get; private set; } = 0.5f;
        private static float balanceBobberVel = 0f;

        // 5. Orbital Crosshair
        public static float OrbitAngle { get; private set; } = 0f;
        public static float TargetLockAngle { get; private set; } = 90f;
        private static float orbitSpeed = 420f;

        // 6. Reflex Trigger
        public static float ReflexStandbyTimer { get; private set; } = 0f;
        public static bool ReflexSignalActive { get; private set; } = false;
        public static float PlayerReactionTime { get; private set; } = -1f;
        public static bool FalseStart { get; private set; } = false;
        private static float reflexSignalStartTime = 0f;
        public static int ReflexRoundsRemaining { get; private set; } = 3;

        private const string PREFS_PREFIX = "ARCADE_HIGHSCORE_";

        public static int GetHighScore(ClashMinigameType type)
        {
            return PlayerPrefs.GetInt(PREFS_PREFIX + type.ToString(), 0);
        }

        public static void SaveHighScore(ClashMinigameType type, int score)
        {
            int current = GetHighScore(type);
            if (score > current)
            {
                PlayerPrefs.SetInt(PREFS_PREFIX + type.ToString(), score);
                PlayerPrefs.Save();
            }
        }

        public static void ResetAllHighScores()
        {
            foreach (ClashMinigameType type in Enum.GetValues(typeof(ClashMinigameType)))
            {
                PlayerPrefs.DeleteKey(PREFS_PREFIX + type.ToString());
            }
            PlayerPrefs.Save();
        }

        public static void StartSession(ClashMinigameType type)
        {
            SelectedMinigame = type;
            State = ArcadeState.Playing;
            CurrentScore = 0;
            Streak = 0;
            IsNewHighScore = false;
            FeedbackText = string.Empty;
            FeedbackTimer = 0f;
            BestScore = GetHighScore(type);

            ClashMeter = 0.5f;
            MashCount = 0;

            switch (type)
            {
                case ClashMinigameType.RapidMash:
                    TotalDuration = 15f;
                    TimeRemaining = TotalDuration;
                    break;

                case ClashMinigameType.PrecisionTiming:
                    TotalDuration = 20f;
                    TimeRemaining = TotalDuration;
                    NeedlePos = 0f;
                    needleDir = 1f;
                    needleSpeed = UnityEngine.Random.Range(1.3f, 1.7f);
                    SweetSpotMin = UnityEngine.Random.Range(0.25f, 0.45f);
                    SweetSpotMax = SweetSpotMin + UnityEngine.Random.Range(0.24f, 0.30f);
                    break;

                case ClashMinigameType.RhythmBeat:
                    TotalDuration = 22f;
                    TimeRemaining = TotalDuration;
                    ActiveNotes.Clear();
                    ActiveNotes.Add(0.60f);
                    ActiveNotes.Add(0.95f);
                    ActiveNotes.Add(1.30f);
                    noteSpawnTimer = 0.55f;
                    break;

                case ClashMinigameType.TensionBalance:
                    TotalDuration = 20f;
                    TimeRemaining = TotalDuration;
                    BalanceBobberPos = 0.5f;
                    BalanceTargetPos = 0.5f;
                    balanceBobberVel = 0f;
                    break;

                case ClashMinigameType.OrbitalCrosshair:
                    TotalDuration = 20f;
                    TimeRemaining = TotalDuration;
                    OrbitAngle = 0f;
                    orbitSpeed = UnityEngine.Random.Range(360f, 440f);
                    TargetLockAngle = UnityEngine.Random.Range(45f, 315f);
                    break;

                case ClashMinigameType.ReflexTrigger:
                    TotalDuration = 30f;
                    TimeRemaining = TotalDuration;
                    ReflexRoundsRemaining = 4;
                    SetupNextReflexRound();
                    break;
            }
        }

        private static void SetupNextReflexRound()
        {
            ReflexStandbyTimer = UnityEngine.Random.Range(1.2f, 2.8f);
            ReflexSignalActive = false;
            FalseStart = false;
            PlayerReactionTime = -1f;
            reflexSignalStartTime = 0f;
        }

        public static void AbortSession()
        {
            State = ArcadeState.Browser;
            FeedbackText = string.Empty;
        }

        public static void UpdateSession(bool lmbPressed, bool lmbHeld, float dt)
        {
            if (State != ArcadeState.Playing)
                return;

            TimeRemaining -= dt;
            if (FeedbackTimer > 0f)
                FeedbackTimer -= dt;

            switch (SelectedMinigame)
            {
                case ClashMinigameType.RapidMash:
                    UpdateRapidMash(lmbPressed, dt);
                    break;
                case ClashMinigameType.PrecisionTiming:
                    UpdatePrecisionTiming(lmbPressed, dt);
                    break;
                case ClashMinigameType.RhythmBeat:
                    UpdateRhythmBeat(lmbPressed, dt);
                    break;
                case ClashMinigameType.TensionBalance:
                    UpdateTensionBalance(lmbHeld, dt);
                    break;
                case ClashMinigameType.OrbitalCrosshair:
                    UpdateOrbitalCrosshair(lmbPressed, dt);
                    break;
                case ClashMinigameType.ReflexTrigger:
                    UpdateReflexTrigger(lmbPressed, dt);
                    break;
            }

            if (TimeRemaining <= 0f)
            {
                FinishRound();
            }
        }

        private static void SetFeedback(string text, Color color, float duration = 1.2f)
        {
            FeedbackText = text;
            FeedbackColor = color;
            FeedbackTimer = duration;
        }

        // ── 1. Rapid Mash ─────────────────────────────────────────────
        private static void UpdateRapidMash(bool lmbPressed, float dt)
        {
            // Continuous resistance push down
            ClashMeter = Mathf.Clamp01(ClashMeter - 0.28f * dt);

            if (lmbPressed)
            {
                MashCount++;
                ClashMeter = Mathf.Clamp01(ClashMeter + 0.085f);
                CurrentScore += 25;
                SoundManager.PlayBeyHit(Vector3.zero, 0.4f);

                if (ClashMeter >= 1f)
                {
                    // Meter breakthrough bonus!
                    Streak++;
                    int burstScore = 600 + Streak * 200;
                    CurrentScore += burstScore;
                    ClashMeter = 0.4f; // reset for next push
                    SoundManager.PlayUiConfirm();
                    SetFeedback($"BURST SURGE! +{burstScore}", new Color(0.2f, 1f, 0.6f));
                }
            }
        }

        // ── 2. Precision Timing ───────────────────────────────────────
        private static void UpdatePrecisionTiming(bool lmbPressed, float dt)
        {
            NeedlePos += needleDir * needleSpeed * dt;
            if (NeedlePos >= 1f) { NeedlePos = 1f; needleDir = -1f; }
            else if (NeedlePos <= 0f) { NeedlePos = 0f; needleDir = 1f; }

            if (lmbPressed)
            {
                bool isHit = NeedlePos >= SweetSpotMin && NeedlePos <= SweetSpotMax;
                if (isHit)
                {
                    Streak++;
                    int pts = 350 + Streak * 150;
                    CurrentScore += pts;
                    ClashMeter = Mathf.Clamp01(ClashMeter + 0.15f);
                    SoundManager.PlayUiConfirm();
                    SetFeedback($"PERFECT STRIKE! +{pts} (x{Streak})", new Color(0.2f, 1f, 0.5f));

                    // Relocate sweet spot
                    SweetSpotMin = UnityEngine.Random.Range(0.20f, 0.50f);
                    SweetSpotMax = SweetSpotMin + UnityEngine.Random.Range(0.24f, 0.32f);
                }
                else
                {
                    Streak = 0;
                    SoundManager.PlayBeyHit(Vector3.zero, 0.8f);
                    SetFeedback("MISSED TIMING!", new Color(1f, 0.3f, 0.3f));
                }
            }
        }

        // ── 3. Rhythm Beat (Taiko-no-Tatsujin Conveyor) ────────────────
        private static void UpdateRhythmBeat(bool lmbPressed, float dt)
        {
            // Move notes to the left towards RhythmTargetX (0.20f)
            for (int i = 0; i < ActiveNotes.Count; i++)
            {
                ActiveNotes[i] -= noteSpeed * dt;
            }

            // Spawn new notes at regular musical intervals
            noteSpawnTimer -= dt;
            if (noteSpawnTimer <= 0f)
            {
                float lastX = ActiveNotes.Count > 0 ? ActiveNotes[ActiveNotes.Count - 1] : 0.8f;
                float newX = Mathf.Max(1.05f, lastX + UnityEngine.Random.Range(0.35f, 0.55f));
                ActiveNotes.Add(newX);
                noteSpawnTimer = UnityEngine.Random.Range(0.68f, 0.92f);
            }

            // Check if player clicks
            if (lmbPressed)
            {
                float bestDist = float.MaxValue;
                int bestIdx = -1;
                for (int i = 0; i < ActiveNotes.Count; i++)
                {
                    float d = Mathf.Abs(ActiveNotes[i] - RhythmTargetX);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIdx = i;
                    }
                }

                if (bestIdx >= 0)
                {
                    if (bestDist <= 0.045f)
                    {
                        // PERFECT MATCH
                        Streak++;
                        int pts = 650 + Streak * 200;
                        CurrentScore += pts;
                        ActiveNotes.RemoveAt(bestIdx);
                        SoundManager.PlayUiConfirm();
                        SetFeedback($"PERFECT MATCH! +{pts} (x{Streak})", new Color(0.2f, 1f, 0.5f));
                    }
                    else if (bestDist <= 0.095f)
                    {
                        // GREAT MATCH
                        Streak++;
                        int pts = 350 + Streak * 100;
                        CurrentScore += pts;
                        ActiveNotes.RemoveAt(bestIdx);
                        SoundManager.PlayUiConfirm();
                        SetFeedback($"GREAT MATCH! +{pts} (x{Streak})", new Color(0.2f, 0.85f, 1f));
                    }
                    else if (bestDist <= 0.16f)
                    {
                        // OFF BEAT
                        Streak = 0;
                        ActiveNotes.RemoveAt(bestIdx);
                        SoundManager.PlayBeyHit(Vector3.zero, 0.7f);
                        SetFeedback("OFF BEAT!", new Color(1f, 0.3f, 0.3f));
                    }
                }
            }

            // Check for notes that passed beyond the target circle
            for (int i = ActiveNotes.Count - 1; i >= 0; i--)
            {
                if (ActiveNotes[i] < RhythmTargetX - 0.08f)
                {
                    ActiveNotes.RemoveAt(i);
                    Streak = 0;
                    SetFeedback("MISS!", new Color(1f, 0.25f, 0.25f), 0.6f);
                }
            }
        }

        // ── 4. Tension Balance ────────────────────────────────────────
        private static void UpdateTensionBalance(bool lmbHeld, float dt)
        {
            // Gentle swaying target
            BalanceTargetPos = 0.5f + 0.30f * Mathf.Sin(Time.unscaledTime * 1.3f) + 0.08f * Mathf.Cos(Time.unscaledTime * 2.1f);

            // Responsive physics
            float thrust = lmbHeld ? 3.8f : -3.2f;
            balanceBobberVel += thrust * dt;
            balanceBobberVel *= 0.84f;
            BalanceBobberPos = Mathf.Clamp01(BalanceBobberPos + balanceBobberVel * dt);

            bool inZone = Mathf.Abs(BalanceBobberPos - BalanceTargetPos) <= 0.22f;
            if (inZone)
            {
                Streak++;
                int pts = Mathf.RoundToInt(220f * dt * (1f + Streak * 0.02f));
                CurrentScore += Mathf.Max(1, pts);
                ClashMeter = Mathf.Clamp01(ClashMeter + 0.40f * dt);
                SetFeedback("BALANCED! +PTS", new Color(0.2f, 1f, 0.5f), 0.3f);
            }
            else
            {
                Streak = Mathf.Max(0, Streak - 2);
                ClashMeter = Mathf.Clamp01(ClashMeter - 0.10f * dt);
            }
        }

        // ── 5. Orbital Crosshair ──────────────────────────────────────
        private static void UpdateOrbitalCrosshair(bool lmbPressed, float dt)
        {
            OrbitAngle = (OrbitAngle + orbitSpeed * dt) % 360f;

            if (lmbPressed)
            {
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(OrbitAngle, TargetLockAngle));
                if (angleDiff <= 32f)
                {
                    Streak++;
                    int pts = 400 + Streak * 150;
                    CurrentScore += pts;
                    SoundManager.PlayUiConfirm();
                    SetFeedback($"LOCK HIT! +{pts} (x{Streak})", new Color(0.2f, 0.9f, 1f));
                    TargetLockAngle = (TargetLockAngle + UnityEngine.Random.Range(90f, 270f)) % 360f;
                }
                else
                {
                    Streak = 0;
                    SoundManager.PlayBeyHit(Vector3.zero, 0.7f);
                    SetFeedback("LOCK MISSED!", new Color(1f, 0.3f, 0.3f));
                }
            }
        }

        // ── 6. Reflex Trigger ─────────────────────────────────────────
        private static void UpdateReflexTrigger(bool lmbPressed, float dt)
        {
            if (!ReflexSignalActive)
            {
                ReflexStandbyTimer -= dt;

                if (lmbPressed)
                {
                    // False start!
                    FalseStart = true;
                    CurrentScore = Mathf.Max(0, CurrentScore - 250);
                    SoundManager.PlayBeyHit(Vector3.zero, 1.2f);
                    SetFeedback("FALSE START! -250 PTS", new Color(1f, 0.2f, 0.2f), 1.4f);
                    ReflexRoundsRemaining--;
                    if (ReflexRoundsRemaining <= 0)
                        FinishRound();
                    else
                        SetupNextReflexRound();
                    return;
                }

                if (ReflexStandbyTimer <= 0f)
                {
                    ReflexSignalActive = true;
                    reflexSignalStartTime = Time.unscaledTime;
                    SoundManager.PlayUiConfirm();
                    SetFeedback(">>> STRIKE NOW! CLICK! <<<", new Color(1f, 0.9f, 0.2f), 1f);
                }
            }
            else
            {
                if (lmbPressed)
                {
                    float reactTime = Time.unscaledTime - reflexSignalStartTime;
                    PlayerReactionTime = reactTime;
                    int pts;
                    if (reactTime < 0.24f) pts = 1800;
                    else if (reactTime < 0.32f) pts = 1200;
                    else if (reactTime < 0.45f) pts = 800;
                    else pts = 400;

                    CurrentScore += pts;
                    SoundManager.PlayUiConfirm();
                    SetFeedback($"REACTION: {Mathf.RoundToInt(reactTime * 1000f)}ms! +{pts} PTS", new Color(0.2f, 1f, 0.6f), 1.6f);

                    ReflexRoundsRemaining--;
                    if (ReflexRoundsRemaining <= 0)
                        FinishRound();
                    else
                        SetupNextReflexRound();
                }
            }
        }

        private static void FinishRound()
        {
            State = ArcadeState.RoundOver;
            int prevBest = GetHighScore(SelectedMinigame);
            if (CurrentScore > prevBest)
            {
                IsNewHighScore = true;
                SaveHighScore(SelectedMinigame, CurrentScore);
                BestScore = CurrentScore;
            }
            else
            {
                BestScore = prevBest;
            }
        }

        public static (string title, string jp, string category, string desc) GetMinigameInfo(ClashMinigameType type)
        {
            switch (type)
            {
                case ClashMinigameType.RapidMash:
                    return ("RAPID MASH", "[猛連打]", "POWER & SPEED", "Rapidly click [LEFT MOUSE BUTTON] to overpower resistance and trigger explosive meter bursts!");
                case ClashMinigameType.PrecisionTiming:
                    return ("PRECISION STRIKE", "[一閃]", "TIMING & FOCUS", "A rhythmic needle sweeps across the gauge. Click [LMB] when inside the critical target zone!");
                case ClashMinigameType.RhythmBeat:
                    return ("RHYTHM COMBO", "[連続撃]", "TEMPO & ACCURACY", "Rhythm orbs travel across the track. Click [LMB] when an orb aligns with the target circle to score PERFECT and GREAT matches!");
                case ClashMinigameType.TensionBalance:
                    return ("TENSION BALANCE", "[拮抗維持]", "CONTROL & FINESSE", "Hold [LMB] to thrust upward, release to sink. Keep the power core balanced inside the gliding zone!");
                case ClashMinigameType.OrbitalCrosshair:
                    return ("ORBITAL LOCK", "[旋風追尾]", "TRACKING & AGILITY", "An energy spark orbits around the circular radar dial. Click [LMB] when the spark aligns with the circular lock reticle!");
                case ClashMinigameType.ReflexTrigger:
                    return ("QUICK-DRAW REFLEX", "[瞬撃拔刀]", "PURE REACTION", "Stand by in silence (do NOT click). Click [LMB] instantly the moment the strike alert triggers!");
                default:
                    return ("CLASH MINIGAME", "", "CLASH", "");
            }
        }
    }
}
