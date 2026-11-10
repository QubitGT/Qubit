/*
 * Qubit Menu  Patches/Safety/ZonePatches.cs
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
using Photon.Pun;
using Photon.Realtime;
using Qubit.Extensions;
using System.Linq;

namespace Qubit.Patches.Safety
{
    /// <summary>
    /// App Quit On Enter. Hooks the zone state send — when zone sync is sent to a
    /// player entering your zone, they receive a poisoned table instead.
    /// </summary>
    [HarmonyPatch(typeof(GameEntityManager), "SendZoneStateToPlayerOrTarget")]
    public class AppQuitOnEnter
    {
        public static bool enabled;

        private static bool Prefix(GTZone zone, Player player)
        {
            if (!enabled || player == null)
                return true;

            if (zone == GTZone.ghostReactor || zone == GTZone.ghostReactorDrill)
                return true;

            Modules.Categories.Master.ZoneAppQuit(Players.GetVRRigFromPlayer(player));
            return false;
        }
    }

    /// <summary>
    /// Auto Try Get Zone Auth. When you are alone in a zone you hold authority over,
    /// IsInZone reports true so zone authority style mods treat you as zone owner.
    /// </summary>
    [HarmonyPatch(typeof(GameEntityManager), "IsInZone")]
    public class AutoZoneAuth
    {
        public static bool enabled;

        private static bool Prefix(GameEntityManager __instance, ref bool __result)
        {
            if (!enabled)
                return true;

            __result = __instance.IsAuthority()
                && __instance.zone != GTZone.ghostReactor
                && __instance.zone != GTZone.ghostReactorDrill
                && Players.ActiveRigs.Any(r => r != null && r.zoneEntity.currentZone == __instance.zone);
            return false;
        }
    }

    /// <summary>
    /// Anti Break Audio. Filters CreateItemRPC batches from other players that
    /// carry the audio break entity hash, so your audio can't be broken remotely.
    /// </summary>
    [HarmonyPatch(typeof(GameEntityManager), nameof(GameEntityManager.CreateItemRPC))]
    public class AntiBreakAudio
    {
        public static bool enabled;

        private const int AudioBreakEntityHash = 1551901997;
        private const int BreakMovementEntityHash = 1517967647;
        private const int KickEntityHash = -20887423;

        public static bool enabledBreakMovement;
        public static bool enabledKick;

        private static bool Prefix(int[] entityTypeId, int[] netId, PhotonMessageInfo info)
        {
            if (!enabled && !enabledBreakMovement && !enabledKick)
                return true;

            if (info.Sender == Photon.Pun.PhotonNetwork.LocalPlayer)
                return true;

            if (enabled && entityTypeId != null && entityTypeId.Contains(AudioBreakEntityHash))
                return false;

            if (enabledBreakMovement && entityTypeId != null && entityTypeId.Contains(BreakMovementEntityHash))
                return false;

            if (enabledKick && netId != null && netId.Length > 40 && entityTypeId != null && entityTypeId.Contains(KickEntityHash))
                return false;

            return true;
        }
    }
}
