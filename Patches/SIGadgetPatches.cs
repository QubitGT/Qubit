/*
 * Qubit Menu  Patches/SIGadgetPatches.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software  /Derivative work of Seralyth Software, Goldentrophy Software
 * https://github.com/QubitGT/Qubit
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using HarmonyLib;
using GorillaGameModes;
using Qubit.Libraries.Networking;
using System.Collections.Generic;

namespace Qubit.Patches
{
    /// <summary>
    /// Gadget stat modifiers for super infection gadgets. Each toggle is driven by
    /// its matching module; the patches apply the stat changes every authority update.
    /// </summary>
    public static class GadgetPatches
    {
        // Thrusters
        public static bool infiniteThrusterFuel;
        public static int thrusterSpeedMode; // 0 normal, 1 fast, 2 super fast, 3 slow
        private static readonly float[] ThrusterSpeeds = { 0f, 25f, 50f, 6.5f };
        private static float thrusterOgForce, thrusterOgHorizontal, thrusterOgVertical;

        [HarmonyPatch(typeof(SIGadgetWristJet), "OnUpdateAuthority")]
        private static class ThrusterPatch
        {
            private static void Postfix(SIGadgetWristJet __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                if (thrusterOgVertical == 0f)
                {
                    thrusterOgForce = __instance.jetForce;
                    thrusterOgHorizontal = __instance.maxHorizontalSpeed;
                    thrusterOgVertical = __instance.maxVerticalSpeed;
                }

                if (infiniteThrusterFuel)
                    __instance.fuelSpendRate = 0f;

                bool normal = ThrusterSpeeds[thrusterSpeedMode] == 0f;
                __instance.jetForce = normal ? thrusterOgForce : ThrusterSpeeds[thrusterSpeedMode];
                __instance.maxHorizontalSpeed = normal ? thrusterOgHorizontal : ThrusterSpeeds[thrusterSpeedMode];
                __instance.maxVerticalSpeed = normal ? thrusterOgVertical : ThrusterSpeeds[thrusterSpeedMode];
            }
        }

        // Yo-Yo
        public static bool noYoyoCooldown;
        public static int yoyoStrengthMode; // 0 normal, 1 strong, 2 super strong
        public static int yoyoDistanceMode; // 0 normal, 1 far, 2 super far
        private static readonly float[] YoyoYankSpeeds = { 11f, 18f, 45f };
        private static readonly float[] YoyoThrowMultipliers = { 1.5f, 3f, 6.7f };

        [HarmonyPatch(typeof(SIGadgetDashYoyo), "OnUpdateAuthority")]
        private static class YoyoPatch
        {
            private static void Postfix(SIGadgetDashYoyo __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                var t = Traverse.Create(__instance);
                if (noYoyoCooldown)
                {
                    t.Field(nameof(SIGadgetDashYoyo.m_postYankCooldown)).SetValue(0f);
                    t.Field(nameof(SIGadgetDashYoyo.m_waitBeforeAutoReturn)).SetValue(0f);
                    t.Field(nameof(SIGadgetDashYoyo.m_maxYankRecheckTime)).SetValue(0f);
                    t.Field(nameof(SIGadgetDashYoyo.m_yankMinDistance)).SetValue(0f);
                }
                t.Field(nameof(SIGadgetDashYoyo.m_minThrowSpeed)).SetValue(YoyoYankSpeeds[yoyoStrengthMode]);
                t.Field(nameof(SIGadgetDashYoyo.m_yankMinSpeed)).SetValue(YoyoYankSpeeds[yoyoStrengthMode]);
                t.Field(nameof(SIGadgetDashYoyo.m_yankMaxSpeed)).SetValue(YoyoYankSpeeds[yoyoStrengthMode]);
                t.Field(nameof(SIGadgetDashYoyo.m_throwMultiplierDefault)).SetValue(YoyoThrowMultipliers[yoyoDistanceMode]);
                t.Field(nameof(SIGadgetDashYoyo.m_throwMultiplierUpgrade)).SetValue(YoyoThrowMultipliers[yoyoDistanceMode]);
                t.Field(nameof(SIGadgetDashYoyo.m_maxYankRecheckTime)).SetValue(YoyoThrowMultipliers[yoyoDistanceMode]);
            }
        }

        // Stilts
        public static int stiltLengthMode; // 0 normal, 1 long, 2 super long, 3 max
        public static bool fastStiltExtension;
        public static bool fastMotorizedStilt;
        private static readonly Dictionary<SIGadgetStilt, float[]> stiltOriginals = new Dictionary<SIGadgetStilt, float[]>();
        private static readonly float[][] StiltLengths = { new[] { 4f, 4f, 4f }, new[] { 8f, 8f, 8f }, new[] { 20f, 20f, 20f } };

        [HarmonyPatch(typeof(SIGadgetStilt), "OnUpdateAuthority")]
        private static class StiltPatch
        {
            private static void Postfix(SIGadgetStilt __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                var t = Traverse.Create(__instance);
                if (!stiltOriginals.TryGetValue(__instance, out float[] og))
                {
                    og = new[]
                    {
                        t.Field(nameof(SIGadgetStilt.maxLengthNormal)).GetValue<float>(),
                        t.Field(nameof(SIGadgetStilt.extendSpeedNormal)).GetValue<float>(),
                        t.Field(nameof(SIGadgetStilt.retractSpeedNormal)).GetValue<float>(),
                        t.Field(nameof(SIGadgetStilt.maxLengthUpgraded)).GetValue<float>(),
                        t.Field(nameof(SIGadgetStilt.extendSpeedUpgraded)).GetValue<float>(),
                        t.Field(nameof(SIGadgetStilt.retractSpeedUpgraded)).GetValue<float>(),
                        t.Field(nameof(SIGadgetStilt.rotateSpeedFactor)).GetValue<float>()
                    };
                    stiltOriginals.Add(__instance, og);
                }

                if (stiltLengthMode > 0)
                {
                    float[] lengths = StiltLengths[stiltLengthMode - 1];
                    t.Field(nameof(SIGadgetStilt.maxLengthNormal)).SetValue(lengths[0]);
                    t.Field(nameof(SIGadgetStilt.maxLengthUpgraded)).SetValue(lengths[1]);
                    t.Field(nameof(SIGadgetStilt.maxArmLength)).SetValue(lengths[2]);
                }
                else
                {
                    t.Field(nameof(SIGadgetStilt.maxLengthNormal)).SetValue(og[0]);
                    t.Field(nameof(SIGadgetStilt.maxLengthUpgraded)).SetValue(og[3]);
                    t.Field(nameof(SIGadgetStilt.maxArmLength)).SetValue(og[4]);
                }

                t.Field(nameof(SIGadgetStilt.extendSpeedNormal)).SetValue(fastStiltExtension ? 100f : og[1]);
                t.Field(nameof(SIGadgetStilt.retractSpeedNormal)).SetValue(fastStiltExtension ? 100f : og[2]);
                t.Field(nameof(SIGadgetStilt.rotateSpeedFactor)).SetValue(fastMotorizedStilt ? 5555f : og[6]);
            }
        }

        // Blasters
        public static bool noBlasterCooldown;

        [HarmonyPatch(typeof(SIGadgetChargeBlaster), "OnUpdateAuthority")]
        private static class ChargeBlasterPatch
        {
            private static void Postfix(SIGadgetChargeBlaster __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() == GameModeType.SuperInfect)
                    Traverse.Create(__instance).Field("fireCooldown").SetValue(noBlasterCooldown ? 0.0001f : 0.2f);
            }
        }

        [HarmonyPatch(typeof(SIGadgetCooldownBlaster), "OnUpdateAuthority")]
        private static class CooldownBlasterPatch
        {
            private static void Postfix(SIGadgetCooldownBlaster __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() == GameModeType.SuperInfect)
                    __instance.fireCooldown = noBlasterCooldown ? 0.0001f : 0.5f;
            }
        }

        [HarmonyPatch(typeof(SIPlayer), "PlayerKnockback")]
        private static class AntiBlasterKnockbackPatch
        {
            public static bool enabled;

            private static bool Prefix()
            {
                return !enabled;
            }
        }

        public static bool antiBlasterKnockback
        {
            get => AntiBlasterKnockbackPatch.enabled;
            set => AntiBlasterKnockbackPatch.enabled = value;
        }

        // Zipline
        public static bool fastZipline;
        public static bool infiniteZiplineUses;
        private static readonly Dictionary<SIGadgetLaserZipline, object[]> ziplineOriginals = new Dictionary<SIGadgetLaserZipline, object[]>();
        private static bool ziplineRestored;

        [HarmonyPatch(typeof(SIGadgetLaserZipline), "OnUpdateAuthority")]
        private static class ZiplinePatch
        {
            private static void Postfix(SIGadgetLaserZipline __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                var t = Traverse.Create(__instance);
                if (!ziplineOriginals.TryGetValue(__instance, out object[] og))
                {
                    og = new object[]
                    {
                        t.Field(nameof(SIGadgetLaserZipline.upgradedSpeedBoost)).GetValue<float>(),
                        t.Field(nameof(SIGadgetLaserZipline.groundedCooldown)).GetValue<ResettableUseCounter>()
                    };
                    ziplineOriginals.Add(__instance, og);
                }

                if (infiniteZiplineUses)
                {
                    t.Field(nameof(SIGadgetLaserZipline.groundedCooldown)).SetValue(new ResettableUseCounter(int.MaxValue, int.MaxValue, null));
                    ziplineRestored = false;
                }
                else if (!ziplineRestored)
                {
                    t.Field(nameof(SIGadgetLaserZipline.groundedCooldown)).SetValue(og[1]);
                    ziplineRestored = true;
                }

                t.Field(nameof(SIGadgetLaserZipline.upgradedSpeedBoost)).SetValue(fastZipline ? 30f : (float)og[0]);
            }
        }

        // AirGrab
        public static bool infiniteAirGrabUses;
        public static bool noAirGrabDecay;
        private static readonly Dictionary<SIGadgetAirGrab, object[]> airGrabOriginals = new Dictionary<SIGadgetAirGrab, object[]>();
        private static bool airGrabRestored;

        [HarmonyPatch(typeof(SIGadgetAirGrab), "FixedUpdate")]
        private static class AirGrabPatch
        {
            private static void Postfix(SIGadgetAirGrab __instance)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                var t = Traverse.Create(__instance);
                if (!airGrabOriginals.TryGetValue(__instance, out object[] og))
                {
                    og = new object[]
                    {
                        t.Field(nameof(SIGadgetAirGrab.m_maxHoldTimeUpgraded)).GetValue<float>(),
                        t.Field(nameof(SIGadgetAirGrab._groundedUseCounter)).GetValue<ResettableUseCounter>()
                    };
                    airGrabOriginals.Add(__instance, og);
                }

                if (infiniteAirGrabUses)
                {
                    t.Field(nameof(SIGadgetAirGrab._groundedUseCounter)).SetValue(new ResettableUseCounter(int.MaxValue, int.MaxValue, null));
                    airGrabRestored = false;
                }
                else if (!airGrabRestored)
                {
                    t.Field(nameof(SIGadgetAirGrab._groundedUseCounter)).SetValue(og[1]);
                    airGrabRestored = true;
                }

                t.Field(nameof(SIGadgetAirGrab.m_maxHoldTimeUpgraded)).SetValue(noAirGrabDecay ? float.MaxValue : (float)og[0]);
            }
        }

        // AirJuke
        public static bool infiniteAirJukeUses;
        public static bool strongerAirJuke;
        private static readonly Dictionary<SIGadgetAirJuke, object[]> airJukeOriginals = new Dictionary<SIGadgetAirJuke, object[]>();
        private static bool airJukeRestored;

        [HarmonyPatch(typeof(SIGadgetAirJuke), "FixedUpdate")]
        private static class AirJukePatch
        {
            private static void Postfix(SIGadgetAirJuke __instance)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                var t = Traverse.Create(__instance);
                if (!airJukeOriginals.TryGetValue(__instance, out object[] og))
                {
                    og = new object[]
                    {
                        t.Field(nameof(SIGadgetAirJuke.m_maxDashSpeedUpgraded)).GetValue<float>(),
                        t.Field(nameof(SIGadgetAirJuke.m_handMaxSpeed)).GetValue<float>(),
                        t.Field(nameof(SIGadgetAirJuke._groundedUseCounter)).GetValue<ResettableUseCounter>()
                    };
                    airJukeOriginals.Add(__instance, og);
                }

                if (infiniteAirJukeUses)
                {
                    t.Field(nameof(SIGadgetAirJuke._groundedUseCounter)).SetValue(new ResettableUseCounter(int.MaxValue, int.MaxValue, null));
                    airJukeRestored = false;
                }
                else if (!airJukeRestored)
                {
                    t.Field(nameof(SIGadgetAirJuke._groundedUseCounter)).SetValue(og[2]);
                    airJukeRestored = true;
                }

                if (strongerAirJuke)
                {
                    t.Field(nameof(SIGadgetAirJuke.m_maxDashSpeedUpgraded)).SetValue(25f);
                    t.Field(nameof(SIGadgetAirJuke.m_maxDashSpeedDefault)).SetValue(25f);
                    t.Field(nameof(SIGadgetAirJuke.m_handMaxSpeed)).SetValue(24f);
                }
                else
                {
                    t.Field(nameof(SIGadgetAirJuke.m_maxDashSpeedUpgraded)).SetValue((float)og[0]);
                    t.Field(nameof(SIGadgetAirJuke.m_handMaxSpeed)).SetValue((float)og[1]);
                }
            }
        }

        // Tentacles
        public static bool infiniteTentacleFuel;
        public static int tentacleLengthMode; // 0 normal .. 4 max
        public static int tentacleSpeedMode; // 0 normal, 1 fast, 2 super fast
        private static readonly float[] TentacleLengths = { 1.5f, 5f, 10f, 15f, 20f };
        private static readonly float[] TentacleLengthFactors = { 5f, 10f, 15f, 20f, 25f };
        private static readonly float[] TentacleSpeeds = { 0f, 10f, 20f };
        private static readonly Dictionary<SIGadgetTentacleArm, float> tentacleOriginals = new Dictionary<SIGadgetTentacleArm, float>();

        [HarmonyPatch(typeof(SIGadgetTentacleArm), "OnUpdateAuthority")]
        private static class TentaclePatch
        {
            private static void Postfix(SIGadgetTentacleArm __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                var t = Traverse.Create(__instance);
                if (!tentacleOriginals.TryGetValue(__instance, out float ogSpeed))
                {
                    ogSpeed = __instance.MaxTentacleJumpSpeed;
                    tentacleOriginals.Add(__instance, ogSpeed);
                }

                if (infiniteTentacleFuel)
                {
                    __instance.FuelPerSecond_Holding = 0f;
                    __instance.FuelCost_Grab = 0f;
                    __instance.FuelCost_JumpSpeed = 0f;
                }

                __instance.LengthFactor = TentacleLengths[tentacleLengthMode];
                __instance.MaxGrabAngle = 180f;
                t.Field("m_tentacleReachDistance").SetValue(TentacleLengthFactors[tentacleLengthMode]);
                t.Field("m_tentacleJumpSpeed").SetValue(TentacleSpeeds[tentacleSpeedMode] > 0f ? TentacleSpeeds[tentacleSpeedMode] : ogSpeed);
            }
        }

        // Platform deployer
        public static bool infinitePlatforms;
        private static readonly Dictionary<SIGadgetPlatformDeployer, int> platformOriginals = new Dictionary<SIGadgetPlatformDeployer, int>();

        [HarmonyPatch(typeof(SIGadgetPlatformDeployer), "OnUpdateAuthority")]
        private static class PlatformPatch
        {
            private static void Postfix(SIGadgetPlatformDeployer __instance, float dt)
            {
                if (Qubit.Core.ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
                    return;

                var t = Traverse.Create(__instance);
                if (!platformOriginals.TryGetValue(__instance, out int ogUses))
                {
                    ogUses = t.Field("m_maxRegularUses").GetValue<int>();
                    platformOriginals.Add(__instance, ogUses);
                }

                t.Field("m_maxRegularUses").SetValue(infinitePlatforms ? ogUses * 1000 : ogUses);
            }
        }
    }
}
