/*
 * Qubit Menu  Patches/Safety/AntiBan/CheckAutoBanListForName.cs
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
using GorillaNetworking;
using HarmonyLib;
using System;

namespace Qubit.Patches.Safety.AntiBan
{
    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.CheckAutoBanListForName))]
    public class CheckAutoBanListForName
    {
        public static bool Prefix(string nameToCheck, ref bool __result)
        {
            if (BanPatches.enabled)
            {
                __result = true;
                return false;
            }

            return true;
        }

        public static bool CheckBanList(string nameToCheck)
        {
            nameToCheck = nameToCheck.ToLower();
            nameToCheck = new string(Array.FindAll<char>(nameToCheck.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));

            foreach (string twoWeekNames in References.Computer.anywhereTwoWeek)
            {
                if (nameToCheck.IndexOf(twoWeekNames) >= 0)
                    return false;
            }

            foreach (string oneWeekNames in References.Computer.anywhereOneWeek)
            {
                if (nameToCheck.IndexOf(oneWeekNames) >= 0 && !nameToCheck.Contains("fagol"))
                    return false;
            }

            string[] exactNames = References.Computer.exactOneWeek;
            for (int i = 0; i < exactNames.Length; i++)
            {
                if (exactNames[i] == nameToCheck)
                    return false;
            }

            return true;
        }
    }
}
