using System;
using System.Reflection;
using BladeSpinners.Core;
using BladeSpinners.Gameplay.Movement;
using BladeSpinners.Gameplay.Parts;
using UnityEditor;
using UnityEngine;

namespace BladeSpinners.Editor
{
    /// <summary>
    /// Regression checks for Orbit movement on bowl floors below world Y=0.
    /// </summary>
    public static class OrbitTipPhysicsValidator
    {
        private const float VelocityTolerance = 0.001f;
        private const float RadiusTolerance = 0.12f;

        private static readonly FieldInfo GroundedField =
            typeof(BeyMovementController).GetField(
                "isGrounded", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo RigidbodyField =
            typeof(BeyMovementController).GetField(
                "rb", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo OrbitCenterField =
            typeof(BeyMovementController).GetField(
                "orbitCenter", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo OrbitRadiusField =
            typeof(BeyMovementController).GetField(
                "orbitRadius", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("Blade Spinners/Validation/Test Orbit Tip Physics")]
        public static void Validate()
        {
            GameObject root = null;

            try
            {
                int orbitTipCount = ValidateOrbitTipAssets();

                root = new GameObject("OrbitTipPhysicsValidator");
                Rigidbody rigidbody = root.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                BeyMovementController movement = root.AddComponent<BeyMovementController>();

                SetRigidbody(movement, rigidbody);
                movement.SetDirectionOverride(Vector3.forward, Vector3.right);
                movement.CacheInput(1f, 0f);
                SetGrounded(movement, true);
                root.transform.position = new Vector3(0f, -3f, 0f);
                rigidbody.linearVelocity = new Vector3(0f, -2f, 0f);

                movement.ApplyOrbitMovement(
                    OrbitTip.LocalOrbitRadius,
                    OrbitTip.ForwardTravelSpeed,
                    OrbitTip.AngularSpeedDegrees);
                AssertVerticalAndFinite(
                    rigidbody.linearVelocity, -2f, "sunken arena");

                float configuredRadius = ReadFloatField(
                    OrbitRadiusField, movement, "orbitRadius");
                if (Mathf.Abs(configuredRadius - OrbitTip.LocalOrbitRadius)
                    > VelocityTolerance)
                {
                    throw new InvalidOperationException(
                        $"Orbit radius was {configuredRadius:F3}, expected the small " +
                        $"local radius {OrbitTip.LocalOrbitRadius:F3}.");
                }

                // Simulate one full 1.5-second local orbit. The anchor must travel in
                // a straight line while the Bey stays close to the small local radius.
                int stepCount = Mathf.RoundToInt(
                    360f / OrbitTip.AngularSpeedDegrees
                    / Time.fixedDeltaTime);
                float minLocalX = float.PositiveInfinity;
                float maxLocalX = float.NegativeInfinity;
                float maximumRadiusError = 0f;
                AdvanceTestBody(root, rigidbody);
                RecordLocalOrbit(
                    root, movement, ref minLocalX, ref maxLocalX,
                    ref maximumRadiusError);

                for (int step = 1; step < stepCount; step++)
                {
                    movement.ApplyOrbitMovement(
                        OrbitTip.LocalOrbitRadius,
                        OrbitTip.ForwardTravelSpeed,
                        OrbitTip.AngularSpeedDegrees);
                    AssertVerticalAndFinite(
                        rigidbody.linearVelocity, -2f, "moving local orbit");
                    AdvanceTestBody(root, rigidbody);
                    RecordLocalOrbit(
                        root, movement, ref minLocalX, ref maxLocalX,
                        ref maximumRadiusError);
                }

                Vector3 finalAnchor = ReadVectorField(
                    OrbitCenterField, movement, "orbitCenter");
                float expectedForwardTravel =
                    OrbitTip.ForwardTravelSpeed * Time.fixedDeltaTime * stepCount;
                if (Mathf.Abs(finalAnchor.z - expectedForwardTravel) > 0.05f
                    || Mathf.Abs(finalAnchor.x + OrbitTip.LocalOrbitRadius) > 0.05f)
                {
                    throw new InvalidOperationException(
                        $"Orbit anchor did not travel straight forward. Expected near " +
                        $"(-{OrbitTip.LocalOrbitRadius:F2}, {expectedForwardTravel:F2}), " +
                        $"got ({finalAnchor.x:F2}, {finalAnchor.z:F2}).");
                }
                if (maxLocalX - minLocalX
                    < OrbitTip.LocalOrbitRadius * 1.7f)
                {
                    throw new InvalidOperationException(
                        "The Bey did not circle both sides of its moving local anchor.");
                }
                if (maximumRadiusError > RadiusTolerance)
                {
                    throw new InvalidOperationException(
                        $"Local orbit drifted by {maximumRadiusError:F3} m; allowed " +
                        $"{RadiusTolerance:F3} m.");
                }
                if (root.transform.position.z
                    < expectedForwardTravel - OrbitTip.LocalOrbitRadius - 0.2f)
                {
                    throw new InvalidOperationException(
                        "Orbit completed a circle but failed to make global forward progress.");
                }

                SetGrounded(movement, false);
                Vector3 airborneVelocity = new Vector3(2f, 3f, 4f);
                rigidbody.linearVelocity = airborneVelocity;
                movement.ApplyOrbitMovement(
                    OrbitTip.LocalOrbitRadius,
                    OrbitTip.ForwardTravelSpeed,
                    OrbitTip.AngularSpeedDegrees);
                if ((rigidbody.linearVelocity - airborneVelocity).sqrMagnitude
                    > VelocityTolerance * VelocityTolerance)
                {
                    throw new InvalidOperationException(
                        "Orbit movement changed Rigidbody velocity while airborne.");
                }

                // Landing at a new point must re-anchor from the current radial angle,
                // while continuing to preserve vertical physics.
                SetGrounded(movement, true);
                root.transform.position = new Vector3(-5f, -3f, 0f);
                rigidbody.linearVelocity = new Vector3(0f, 3f, 0f);
                movement.ApplyOrbitMovement(
                    OrbitTip.LocalOrbitRadius,
                    OrbitTip.ForwardTravelSpeed,
                    OrbitTip.AngularSpeedDegrees);
                AssertVerticalAndFinite(
                    rigidbody.linearVelocity, 3f, "landing re-anchor");
                AdvanceTestBody(root, rigidbody);
                Vector3 landingOffset = root.transform.position - ReadVectorField(
                    OrbitCenterField, movement, "orbitCenter");
                landingOffset.y = 0f;
                if (Mathf.Abs(
                    landingOffset.magnitude - OrbitTip.LocalOrbitRadius)
                    > RadiusTolerance)
                {
                    throw new InvalidOperationException(
                        "Orbit did not re-anchor locally after landing.");
                }

                Debug.Log(
                    $"[OrbitTipPhysics] Passed: all {orbitTipCount} authored Orbit tips " +
                    $"use a {OrbitTip.LocalOrbitRadius:F2} m local orbit around an anchor " +
                    $"that moved {expectedForwardTravel:F2} m forward; grounded and " +
                    "landing movement stayed planar; airborne velocity was untouched.");
            }
            finally
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        private static int ValidateOrbitTipAssets()
        {
            const string wardenPath = "Assets/Parts/Tips/Warden_Tip.asset";
            string[] guids = AssetDatabase.FindAssets(
                "t:BeyPart", new[] { "Assets/Parts/Tips" });
            int orbitTipCount = 0;
            bool foundWarden = false;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BeyPart tip = AssetDatabase.LoadAssetAtPath<BeyPart>(path);
                if (tip == null || tip.TipBehavior != TipBehaviorType.Orbit)
                    continue;

                orbitTipCount++;
                foundWarden |= string.Equals(
                    path, wardenPath, StringComparison.OrdinalIgnoreCase);
                ITipBehavior behavior =
                    TipBehaviorFactory.CreateTipBehavior(tip.TipBehavior);
                if (behavior == null || behavior.BehaviorType != TipBehaviorType.Orbit)
                {
                    throw new InvalidOperationException(
                        $"Orbit behavior factory resolution failed for {path}.");
                }
            }

            if (orbitTipCount == 0)
                throw new InvalidOperationException("No authored Orbit tips were found.");
            if (!foundWarden)
            {
                throw new InvalidOperationException(
                    $"Expected Warden_Tip to be included in the Orbit set: {wardenPath}.");
            }

            return orbitTipCount;
        }

        private static void SetGrounded(
            BeyMovementController movement, bool isGrounded)
        {
            if (GroundedField == null)
            {
                throw new MissingFieldException(
                    typeof(BeyMovementController).FullName, "isGrounded");
            }

            GroundedField.SetValue(movement, isGrounded);
        }

        private static void SetRigidbody(
            BeyMovementController movement, Rigidbody rigidbody)
        {
            if (RigidbodyField == null)
            {
                throw new MissingFieldException(
                    typeof(BeyMovementController).FullName, "rb");
            }

            // Awake is not invoked consistently for objects constructed by an edit-mode
            // command-line validator, so inject the dependency used in normal play mode.
            RigidbodyField.SetValue(movement, rigidbody);
        }

        private static void AdvanceTestBody(
            GameObject root, Rigidbody rigidbody)
        {
            Vector3 velocity = rigidbody.linearVelocity;
            root.transform.position +=
                new Vector3(velocity.x, 0f, velocity.z) * Time.fixedDeltaTime;
        }

        private static void RecordLocalOrbit(
            GameObject root,
            BeyMovementController movement,
            ref float minLocalX,
            ref float maxLocalX,
            ref float maximumRadiusError)
        {
            Vector3 center = ReadVectorField(
                OrbitCenterField, movement, "orbitCenter");
            Vector3 offset = root.transform.position - center;
            offset.y = 0f;
            minLocalX = Mathf.Min(minLocalX, offset.x);
            maxLocalX = Mathf.Max(maxLocalX, offset.x);
            maximumRadiusError = Mathf.Max(
                maximumRadiusError,
                Mathf.Abs(offset.magnitude - OrbitTip.LocalOrbitRadius));
        }

        private static Vector3 ReadVectorField(
            FieldInfo field, object target, string fieldName)
        {
            if (field == null)
            {
                throw new MissingFieldException(
                    typeof(BeyMovementController).FullName, fieldName);
            }

            return (Vector3)field.GetValue(target);
        }

        private static float ReadFloatField(
            FieldInfo field, object target, string fieldName)
        {
            if (field == null)
            {
                throw new MissingFieldException(
                    typeof(BeyMovementController).FullName, fieldName);
            }

            return (float)field.GetValue(target);
        }

        private static void AssertVerticalAndFinite(
            Vector3 velocity,
            float expectedVerticalVelocity,
            string scenario)
        {
            if (!IsFinite(velocity))
            {
                throw new InvalidOperationException(
                    $"Orbit produced non-finite velocity during {scenario}: {velocity}.");
            }

            if (Mathf.Abs(velocity.y - expectedVerticalVelocity) > VelocityTolerance)
            {
                throw new InvalidOperationException(
                    $"Orbit changed vertical velocity during {scenario}. " +
                    $"Expected {expectedVerticalVelocity:F3}, got {velocity.y:F3}.");
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x)
                && float.IsFinite(value.y)
                && float.IsFinite(value.z);
        }
    }
}
