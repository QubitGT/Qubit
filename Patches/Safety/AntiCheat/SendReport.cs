/*
 * Qubit Menu  Patches/Safety/AntiCheat/SendReport.cs
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
using Qubit.Libraries.Interface;
using static Qubit.Patches.PatchHandler;

namespace Qubit.Patches.Safety.AntiCheat
{
    [SecurityPatch]
    [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.SendReport))]
    public class SendReport
    {
        public static bool AntiCheatSelf;
        public static bool AntiCheatAll;
        public static bool AntiCheatReasonHide;
        public static bool AntiACReport;

        private static bool Prefix(string susReason, string susId, string susNick)
        {
            if (susReason.ToLower() == "empty rig")
                return false;

            if (AntiCheatSelf || AntiCheatAll)
            {
                if (susId == PhotonNetwork.LocalPlayer.UserId)
                    Notifications.SendFormat("green", "ANTI-CHEAT", $"You have been reported for {(AntiCheatReasonHide ? "hidden reason" : susReason)}.");
                else
                {
                    if (AntiCheatAll)
                        Notifications.SendFormat("green", "ANTI-CHEAT", $"{susNick} was reported for {(AntiCheatReasonHide ? "hidden reason" : susReason)}.");
                }
            }

            if (AntiACReport)
            {
                Modules.Categories.Safety.AntiReportFRT(PhotonNetwork.LocalPlayer);
                Notifications.Clear();
                Notifications.SendFormat("purple", "ANTI-REPORT", "The anti cheat attempted to report you, you have been disconnected.");
            }

            return false;
        }
    }
}
