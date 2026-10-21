/*
 * Qubit Menu  Patches/Safety/AntiCheat/CompetitiveFPSSpoof.cs
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
using UnityEngine;

namespace Qubit.Patches.Safety.AntiCheat
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PackCompetitiveData))]
    public class CompetitiveFPSSpoof
    {
        public static bool enabled;
        public static bool weighted;

        public static int lowestFPS = 70;
        public static int highestFPS = 90;

        public static readonly short[] weightedValues = { 87, 88, 88, 89, 89, 89, 90, 90, 90, 91, 91, 92 };

        public static bool Prefix(ref short __result)
        {
            if (!enabled)
                return true;

            __result = weighted
                ? weightedValues[Random.Range(0, weightedValues.Length)]
                : (short)Random.Range(lowestFPS, highestFPS);

            return false;
        }
    }
}
