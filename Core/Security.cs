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

using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Libraries.Core;
using System;
using System.IO;
using System.Linq;
using System.Net;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Qubit.Core
{
    public static class Security
    {
        /// <summary>
        /// Sends an RPC event as if it happened <paramref name="timeOffset"/> milliseconds earlier (or later) on the server clock.
        /// </summary>
        public static bool SendTimedRPC(PhotonView photonView, int timeOffset, string method, RaiseEventOptions options, params object[] parameters)
        {
            if (photonView != null && parameters != null && !string.IsNullOrEmpty(method))
            {
                Hashtable rpcData = new Hashtable
                {
                    { 0, photonView.ViewID },
                    { 2, PhotonNetwork.ServerTimestamp + timeOffset },
                    { 3, method },
                    { 4, parameters }
                };

                if (photonView.Prefix > 0)
                    rpcData[1] = (short)photonView.Prefix;

                if (PhotonNetwork.PhotonServerSettings.RpcList.Contains(method))
                    rpcData[5] = (byte)PhotonNetwork.PhotonServerSettings.RpcList.IndexOf(method);

                if (options.Receivers == ReceiverGroup.All || (options.TargetActors != null && options.TargetActors.Contains(Networking.LocalPlayer.ActorNumber)))
                {
                    if (options.Receivers == ReceiverGroup.All)
                        options.Receivers = ReceiverGroup.Others;

                    if (options.TargetActors != null && options.TargetActors.Contains(Networking.LocalPlayer.ActorNumber))
                        options.TargetActors = options.TargetActors.Where(id => id != Networking.LocalPlayer.ActorNumber).ToArray();

                    PhotonNetwork.ExecuteRpc(rpcData, PhotonNetwork.LocalPlayer);
                }
                else
                {
                    ManagerRegistry.NetworkingClient.LoadBalancingPeer.OpRaiseEvent(Photon.Pun.PunEvent.RPC, rpcData, options, new SendOptions
                    {
                        Reliability = true,
                        DeliveryMode = DeliveryMode.ReliableUnsequenced,
                        Encrypt = false
                    });
                }
            }
            return false;
        }




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

        /// <summary>
        /// Returns the given URL's raw output as a string.
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>Website Data</returns>
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
