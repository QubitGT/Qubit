/*
 * Qubit Menu  Core/Security.cs
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

using Photon.Pun;
using Qubit.Libraries.Core;
using System;
using System.IO;
using System.Net;

namespace Qubit.Core
{
    public static class Security
    {

        


        public static void RPCProtection()
        {
            if (!Networking.InRoom)
                return;

            try
            {
                ManagerRegistry.MonkeAgent.rpcErrorMax = int.MaxValue;
                ManagerRegistry.MonkeAgent.rpcCallLimit = int.MaxValue;
                ManagerRegistry.MonkeAgent.logErrorMax = int.MaxValue;
                ManagerRegistry.MonkeAgent.userRPCCalls.Clear();

                PhotonNetwork.MaxResendsBeforeDisconnect = int.MaxValue;
                PhotonNetwork.QuickResends = int.MaxValue;

                PhotonNetwork.SendAllOutgoingCommands();
            }
            catch { Logs.Log("RPC protection failed, are you in a lobby?"); }
        }


        




        public static string GetHttp(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            string html = "";

            if (data == null) return html;
            using StreamReader sr = new StreamReader(data);
            html = sr.ReadToEnd();

            return html;
        }

        



        public static string CurrentTimestamp()
        {
            DateTime utcNow = DateTime.UtcNow;
            return utcNow.ToString("o");
        }
    }
}
