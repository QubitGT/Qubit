/*
 * Qubit Menu  Patches/Rigs/Torso.cs
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
using Qubit.Core;
using Qubit.Modules.Categories;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Qubit.Patches.Rigs
{
    /// <summary>
    /// Body pose control,'s Bullshit4 PostTick patch.
    /// Body modes rotate the rig and remap the head and hands on top; Rig modes rotate
    /// the whole rig without remapping, so the limbs follow the rotation.
    /// </summary>
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    public class Torso
    {
        public enum PoseMode
        {
            Spin,               // Y-axis spin (original mode 0)
            Spaz,               // Random rotation each tick (original mode 1)
            BackwardsBody,      // Y + 180
            RecRoomRotary,      // External rotary object (original mode 3)
            UpsideDownBody,     // Z 180
            ForwardsBody,       // X -86
            CrawlingBody,       // X 87
            BackwardsRig,       // Y + 180, no remap
            UpsideDownRig,      // Z 180, no remap
            ForwardsRig,        // X -86, no remap
            CrawlingRig,        // X 87, no remap
            SpazRig,            // Random rotation, no remap
            SpinBotY,           // Continuous Y spin, no remap (Spin Bot V1)
            SpinBotZ,           // Continuous Z spin, no remap (Spin Bot V2)
            SpinBotX,           // Continuous X spin, no remap (Spin Bot V3)
            SpinBotForwards,    // Continuous Y spin leaned forwards (Spin Bot V4)
            SpinBotCrawling,    // Continuous Y spin leaned backwards (Spin Bot V5)
            UpsideDownSpinBot,  // Upside down continuous Y spin
            Backflip           // One-shot X-axis flip on joystick click
        }

        public static event Action VRRigLateUpdate;
        public static bool enabled;
        public static PoseMode mode = PoseMode.Spin;

        private static float spinAngle;
        private static float nextSpinTime;
        private static float backflipAngle;
        internal static bool backflipActive;

        /// <summary>
        /// Triggers a one-shot backflip animation.
        /// </summary>
        public static void DoBackflip()
        {
            backflipAngle = 0f;
            backflipActive = true;
        }

        public static void Postfix(VRRig __instance)
        {
            if (__instance.isLocal)
            {
                if (enabled)
                {
                    Quaternion rotation;
                    bool remap = true;
                    float headYaw = References.HeadCollider.transform.rotation.eulerAngles.y;

                    switch (mode)
                    {
                        case PoseMode.Spin:
                            rotation = Quaternion.Euler(0f, Time.time * 180f % 360, 0f);
                            break;
                        case PoseMode.Spaz:
                        case PoseMode.SpazRig:
                            rotation = Random.rotationUniform;
                            remap = mode == PoseMode.Spaz;
                            break;
                        case PoseMode.BackwardsBody:
                            rotation = Quaternion.Euler(0f, headYaw + 180f, 0f);
                            break;
                        case PoseMode.RecRoomRotary:
                            rotation = Quaternion.Euler(0f, Rig.recBodyRotary.transform.rotation.eulerAngles.y, 0f);
                            break;
                        case PoseMode.UpsideDownBody:
                        case PoseMode.UpsideDownRig:
                            rotation = Quaternion.Euler(0f, headYaw, 180f);
                            remap = mode == PoseMode.UpsideDownBody;
                            break;
                        case PoseMode.ForwardsBody:
                        case PoseMode.ForwardsRig:
                            rotation = Quaternion.Euler(-86f, headYaw, 0f);
                            remap = mode == PoseMode.ForwardsBody;
                            break;
                        case PoseMode.CrawlingBody:
                        case PoseMode.CrawlingRig:
                            rotation = Quaternion.Euler(87f, headYaw, 0f);
                            remap = mode == PoseMode.CrawlingBody;
                            break;
                        case PoseMode.BackwardsRig:
                            rotation = Quaternion.Euler(0f, headYaw + 180f, 0f);
                            remap = false;
                            break;
                        case PoseMode.SpinBotY:
                        case PoseMode.SpinBotZ:
                        case PoseMode.SpinBotX:
                        case PoseMode.SpinBotForwards:
                        case PoseMode.SpinBotCrawling:
                        case PoseMode.UpsideDownSpinBot:
                            if (Time.time > nextSpinTime)
                            {
                                nextSpinTime = Time.time;
                                spinAngle += 280f * Time.deltaTime;
                            }

                            rotation = mode switch
                            {
                                PoseMode.SpinBotY => Quaternion.Euler(0f, spinAngle, 0f),
                                PoseMode.SpinBotZ => Quaternion.Euler(0f, headYaw, spinAngle),
                                PoseMode.SpinBotX => Quaternion.Euler(spinAngle, headYaw, 0f),
                                PoseMode.SpinBotForwards => Quaternion.Euler(-86f, spinAngle, 0f),
                                PoseMode.SpinBotCrawling => Quaternion.Euler(87f, spinAngle, 0f),
                                PoseMode.UpsideDownSpinBot => Quaternion.Euler(0f, spinAngle, 180f),
                                _ => Quaternion.identity
                            };
                            remap = false;
                            break;
                        case PoseMode.Backflip:
                            if (backflipActive)
                            {
                                backflipAngle += 1440f * Time.deltaTime;
                                if (backflipAngle >= 360f)
                                {
                                    backflipAngle = 0f;
                                    backflipActive = false;
                                }
                            }
                            rotation = Quaternion.Euler(backflipAngle, headYaw, 0f);
                            remap = false;
                            break;
                        default:
                            rotation = Quaternion.identity;
                            break;
                    }

                    __instance.transform.rotation = rotation;

                    if (remap)
                    {
                        __instance.head.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                        __instance.leftHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                        __instance.rightHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                    }
                }

                VRRigLateUpdate?.Invoke();
            }
        }
    }
}
