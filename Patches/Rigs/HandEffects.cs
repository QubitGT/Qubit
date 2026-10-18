/*
 * Qubit Menu  Patches/Rigs/HandEffects.cs
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

using Qubit.Core;
using GorillaLocomotion;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Security = Qubit.Core.Security;

namespace Qubit.Patches.Rigs
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.SetHandEffectData))]
    public class HandEffects
    {
        public static bool enabled;
        public static bool tapsEnabled = true;
        public static bool doOverride;
        public static float overrideVolume = 99999f;
        public static int tapMultiplier = 1;
        public static int material = -1;

        private static bool Prefix(VRRig __instance, HandEffectContext effectContext, int audioClipIndex, bool isDownTap, bool isLeftHand, float handTapVolume, float handTapSpeed, Vector3 dirFromHitToHand)
        {
            if (enabled)
            {
                if (__instance.isLocal)
                {
                    if (doOverride)
                    {
                        effectContext.soundFX = References.LocalRig.GetHandSurfaceData(audioClipIndex).audio;
                        effectContext.speed = overrideVolume;
                        effectContext.soundVolume = overrideVolume;

                        if (Networking.InRoom)
                        {
                            if (tapMultiplier > 1)
                            {
                                for (int i = 0; i < tapMultiplier; i++)
                                {
                                    References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, audioClipIndex, isLeftHand, handTapSpeed);
                                }
                                Security.RPCProtection();
                            }
                        }

                        return false;
                    }

                    if (!tapsEnabled)
                    {
                        effectContext.speed = 0f;
                        effectContext.soundVolume = 0f;

                        References.Tagger.handTapVolume = 0f;
                        References.Tagger.handTapSpeed = 0f;
                        References.Tagger.audioClipIndex = -1;

                        return false;
                    }

                    if (material > 0)
                    {
                        References.Tagger.audioClipIndex = material;
                        audioClipIndex = material;

                        if (isLeftHand)
                            References.Player.leftHand.materialTouchIndex = material;
                        else
                            References.Player.rightHand.materialTouchIndex = material;

                        effectContext.soundFX = References.LocalRig.GetHandSurfaceData(material).audio;

                        return false;
                    }
                }
            }
            return true;
        }
    }
}
