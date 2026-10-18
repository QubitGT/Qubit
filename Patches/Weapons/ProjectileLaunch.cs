/*
 * Qubit Menu  Patches/Weapons/ProjectileLaunch.cs
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

using Qubit.Libraries.Audio;
using Qubit.Core;
using HarmonyLib;
using static Qubit.Frontend.Settings;
using static Qubit.Utilities.Assets;

namespace Qubit.Patches.Weapons
{
    [HarmonyPatch(typeof(ProjectileWeapon), nameof(ProjectileWeapon.LaunchProjectile))]
    public class ProjectileLaunch
    {
        public static bool enabled;

        public static void Prefix(ProjectileWeapon __instance)
        {
            if (enabled)
            {
                References.Rigidbody.linearVelocity = __instance.GetLaunchVelocity();

                if (dynamicSounds)
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/AngryBirds/launch.ogg", "Audio/Mods/Fun/AngryBirds/launch.ogg", clip => clip.Play(buttonClickVolume / 10f));
            }
        }
    }
}
