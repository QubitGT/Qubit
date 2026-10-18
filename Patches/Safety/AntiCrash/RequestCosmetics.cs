/*
 * Qubit Menu  Patches/Safety/AntiCrash/RequestCosmetics.cs
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
using ExitGames.Client.Photon;
using GorillaExtensions;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

namespace Qubit.Patches.Safety.AntiCrash
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.RequestCosmetics))]
    public class RequestCosmetics
    {
        private static readonly List<float> callTimestamps = new List<float>();
        public static bool Prefix(VRRig __instance)
        {
            if (AntiCrashPatches.enabled && __instance.isLocal)
            {
                callTimestamps.Add(Time.time);
                callTimestamps.RemoveAll(t => Time.time - t > 1);

                return callTimestamps.Count < 15;
            }
            return true;
        }
    }
}
