/*
 * Qubit Menu  Patches/Safety/AntiBan/AntiBanCrash1.cs
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
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.Internal;
using Qubit.Libraries.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qubit.Patches.Safety.AntiBan
{
    [HarmonyPatch(typeof(PlayFabUnityHttp), nameof(PlayFabUnityHttp.MakeApiCall))]
    public class AntiBanCrash1
    {
        public static bool enabled;

        public static bool Prefix(object reqContainerObj)
        {
            if (!enabled || reqContainerObj == null)
                return true;

            CallRequestContainer callRequestContainer = (CallRequestContainer)reqContainerObj;

            if (callRequestContainer.ErrorCallback != null)
            {
                Action<PlayFabError> errorCallback = callRequestContainer.ErrorCallback;
                PlayFabError fakeError = null;
                void overrideError(PlayFabError error)
                {
                    if (error.ErrorMessage.ToLower().Contains("ban") || error.ErrorMessage.ToLower().Contains("banned") || error.ErrorMessage.ToLower().Contains("suspended") || error.ErrorMessage.ToLower().Contains("suspension"))
                    {
                        if (error.ErrorMessage.ToLower().Contains("this ip"))
                            Notifications.SendFormat("red", "ANTI-BAN", "Your IP address is currently banned.");
                        else
                            Notifications.SendFormat("red", "ANTI-BAN", "Your account is currently banned.");
                        Dictionary<string, List<string>>.Enumerator enumerator = error.ErrorDetails.GetEnumerator();
                        if (enumerator.Current.Value[0] == null || enumerator.Current.Value[0] == "Indefinite")
                        {
                            fakeError = new PlayFabError
                            {
                                Error = PlayFabErrorCode.UnknownError,
                                ErrorMessage = $"Your {(error.ErrorMessage.ToLower().Contains("this ip") ? "IP address" : "account")} has been banned indefinitely.",
                                ErrorDetails = new Dictionary<string, List<string>>()
                            };
                        }
                        else if (enumerator.Current.Value[0] != "Indefinite")
                        {
                            fakeError = new PlayFabError
                            {
                                Error = PlayFabErrorCode.UnknownError,
                                ErrorMessage = $"Your {(error.ErrorMessage.ToLower().Contains("this ip") ? "IP address" : "account")} has been banned. Hours left: {(int)((DateTime.Parse(enumerator.Current.Value[0]) - DateTime.UtcNow).TotalHours + 1.0)}",
                                ErrorDetails = new Dictionary<string, List<string>>()
                            };
                        }
                        errorCallback?.Invoke(fakeError);
                        return;
                    }
                    errorCallback?.Invoke(error);
                }

                callRequestContainer.ErrorCallback = overrideError;
                if (!Networking.InRoom)
                    Task.Run(async () =>
                    {
                        while (References.Computer == null)
                            await Task.Delay(16);

                        References.Computer.GeneralFailureMessage(fakeError.ErrorMessage);
                    });
            }

            return true;
        }
    }
}
