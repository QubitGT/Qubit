/*
 * Qubit Menu  Patches/Safety/Web/Patch_WebClient_UploadValues_Uri.cs
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
using Qubit.Libraries.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json;

namespace Qubit.Patches.Safety.Web
{
    [PatchHandler.SecurityPatch]
    [HarmonyPatch(typeof(WebClient), "UploadValues", new[] { typeof(Uri), typeof(System.Collections.Specialized.NameValueCollection) })]
    internal class Patch_WebClient_UploadValues_Uri
    {
        static bool Prefix(Uri address)
        {
            if (address != null && URLBlocker.IsBanned(address.ToString(), out var reason))
            {
                URLBlocker.Notify(address.ToString(), reason);
                return false;
            }
            return true;
        }
    }
}
