/*
 * Qubit Menu  Patches/Events/GravityFXToggle.cs
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

// Credits to Zlothy29IQ on GitHub. I saw he made it first and just took it. Thanks. Thanks. Thanks. Thanks
using HarmonyLib;

namespace Qubit.Patches.Events
{
    [HarmonyPatch(typeof(AprilFoolsGravityFX), nameof(AprilFoolsGravityFX.Start))]
    public class GravityFXToggle
    {
        public static bool enabled;
        private static bool Prefix() => !enabled;
    }
}
